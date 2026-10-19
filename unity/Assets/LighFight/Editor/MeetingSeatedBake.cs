using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using LighFight.Runtime;

namespace LighFight.Editor {
 // Offline authoring only. Candidates remain disabled until their actual seated captures pass review.
 public static class MeetingSeatedBake {
  const string Folder="Assets/LighFight/MeetingSpectatorReview";
  const float Seat=.613f;
  [Serializable] public class Result { public int index;public string id,pose,prefab;public float ankleError,handError,soleError;public int triangles; }
  [Serializable] class Receipt {public string status="POSE_CANDIDATES_REQUIRE_VISUAL_REVIEW";public Result[] rows;}
  static Transform Bone(Dictionary<string,Transform> bones,string name){if(!bones.TryGetValue(name,out var b))throw new InvalidOperationException("Missing seated joint "+name);return b;}
  static Dictionary<string,Transform> Bones(GameObject actor){
   var found=new Dictionary<string,Transform>();foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))foreach(var b in skin.bones)if(b&&!found.ContainsKey(NativeRigBones.CanonicalName(b.name)))found.Add(NativeRigBones.CanonicalName(b.name),b);return found;
  }
  static Bounds SurfaceBounds(GameObject actor,bool useScale=true){
   bool first=true;Bounds bounds=default;foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){var m=new Mesh();skin.BakeMesh(m,useScale);foreach(var v in m.vertices){var p=skin.transform.TransformPoint(v);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}UnityEngine.Object.DestroyImmediate(m);}if(first)throw new InvalidOperationException("No skinned source surface");return bounds;
  }
  static float SoleBottom(GameObject actor,string side){
   float bottom=float.PositiveInfinity;foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
    var source=skin.sharedMesh;var baked=new Mesh();skin.BakeMesh(baked,true);var vertices=baked.vertices;var footBones=skin.bones.Select(b=>NativeRigBones.CanonicalName(b.name)).Select(n=>n==side+"Foot"||n==side+"ToeBase").ToArray();
    using(var counts=source.GetBonesPerVertex())using(var weights=source.GetAllBoneWeights()){int offset=0;for(int i=0;i<vertices.Length;i++){float support=0;for(int n=0;n<counts[i];n++){var w=weights[offset++];if(footBones[w.boneIndex])support+=w.weight;}if(support>.7f)bottom=Mathf.Min(bottom,skin.transform.TransformPoint(vertices[i]).y);}}
    UnityEngine.Object.DestroyImmediate(baked);
   }if(float.IsPositiveInfinity(bottom))throw new InvalidOperationException("No weighted sole points for "+side);return bottom;
  }
  static void Aim(Transform bone,Transform end,Vector3 target){var from=end.position-bone.position;var to=target-bone.position;if(from.sqrMagnitude<1e-10f||to.sqrMagnitude<1e-10f)throw new InvalidOperationException("Degenerate seated limb");bone.rotation=Quaternion.FromToRotation(from,to)*bone.rotation;}
  static float Solve(Transform a,Transform b,Transform end,Vector3 target,Vector3 pole){
   float upper=Vector3.Distance(a.position,b.position),lower=Vector3.Distance(b.position,end.position);var delta=target-a.position;float requested=delta.magnitude;
   float distance=Mathf.Clamp(requested,Mathf.Abs(upper-lower)+.00001f,upper+lower-.00001f);var forward=delta.normalized;var bend=Vector3.ProjectOnPlane(pole,forward).normalized;if(bend.sqrMagnitude<.5f)throw new InvalidOperationException("Degenerate seated pole");
   float along=(upper*upper-lower*lower+distance*distance)/(2*distance);float height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));var knee=a.position+forward*along+bend*height;
   Aim(a,b,knee);Aim(b,end,a.position+forward*distance);return Vector3.Distance(end.position,target);
  }
  static void GripFingers(GameObject actor){
   foreach(var driver in actor.GetComponentsInChildren<NativeFingerPoseDriver>(true)){
    var so=new SerializedObject(driver);var pose=so.FindProperty("pose")?.objectReferenceValue as NativeFingerPose;var animator=so.FindProperty("animator")?.objectReferenceValue as Animator;if(!pose||!animator)continue;
    foreach(var joint in pose.joints){var b=animator.transform.Find(joint.path);if(b&&NativeRigBones.CanonicalName(b.name).StartsWith("RightHand",StringComparison.Ordinal))b.localRotation=Quaternion.Slerp(b.localRotation,joint.rotation,.65f);}
   }
  }
  static void OrientRestingHand(Dictionary<string,Transform> bones,string side,Transform hand){
   // Match the authored finger axis to the tabletop; a preserved bind rotation leaves palms facing out.
   if(!bones.TryGetValue(side+"HandMiddle1",out var middle))return;
   var forward=middle.position-hand.position;if(forward.sqrMagnitude<1e-8f)return;
   hand.rotation=Quaternion.FromToRotation(forward,Vector3.forward)*hand.rotation;
   if(bones.TryGetValue(side+"HandIndex1",out var index)&&bones.TryGetValue(side+"HandPinky1",out var pinky)){
    var across=index.position-pinky.position;var desired=side=="Left"?Vector3.right:Vector3.left;
    var projected=Vector3.ProjectOnPlane(across,Vector3.forward);if(projected.sqrMagnitude>1e-8f)hand.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(projected,desired,Vector3.forward),Vector3.forward)*hand.rotation;
   }
  }
  static Result Bake(GameObject source,int index,string id,bool smoking){
   var holder=new GameObject("Offline seated authoring");holder.SetActive(false);var actor=UnityEngine.Object.Instantiate(source,holder.transform);var output=new GameObject("Seated "+id+(smoking?" mouthpiece":" idle"));
   try{
    foreach(var animator in actor.GetComponentsInChildren<Animator>(true))animator.enabled=false;
    var bounds=SurfaceBounds(actor);if(bounds.size.y<.1f)throw new InvalidOperationException("Invalid standing source height");actor.transform.localScale*=1.78f/bounds.size.y;
    var bones=Bones(actor);var hips=Bone(bones,"Hips");actor.transform.position+=new Vector3(-hips.position.x,Seat+.10f-hips.position.y,-hips.position.z);
    float ankleError=0,handError=0;
    foreach(string side in new[]{"Left","Right"}){
     float sign=side=="Left"?-1:1;var thigh=Bone(bones,side+"UpLeg");var shin=Bone(bones,side+"Leg");var foot=Bone(bones,side+"Foot");var footRotation=foot.rotation;
     float ankleY=foot.position.y-SoleBottom(actor,side);float legReach=(Vector3.Distance(thigh.position,shin.position)+Vector3.Distance(shin.position,foot.position))*.99f;float legDrop=thigh.position.y-ankleY;
     if(legDrop>=legReach)throw new InvalidOperationException("Chair too high for source leg proportions "+id);
     float footForward=Mathf.Min(.41f,thigh.position.z+Mathf.Sqrt(legReach*legReach-legDrop*legDrop)*.97f);
     ankleError=Mathf.Max(ankleError,Solve(thigh,shin,foot,new Vector3(thigh.position.x,ankleY,footForward),Vector3.forward));foot.rotation=footRotation;
     var arm=Bone(bones,side+"Arm");var fore=Bone(bones,side+"ForeArm");var hand=Bone(bones,side+"Hand");var handRotation=hand.rotation;
     var target=new Vector3(sign*.19f,Seat+.26f,.18f);if(smoking&&side=="Right")target=Bone(bones,"Head").position+new Vector3(.055f,-.075f,.18f);
     if(!smoking||side!="Right"){float armReach=(Vector3.Distance(arm.position,fore.position)+Vector3.Distance(fore.position,hand.position))*.97f;var delta=target-arm.position;if(delta.magnitude>armReach)target=arm.position+delta.normalized*armReach;}
     handError=Mathf.Max(handError,Solve(arm,fore,hand,target,new Vector3(sign,0,.15f)));hand.rotation=handRotation;if(!smoking||side!="Right")OrientRestingHand(bones,side,hand);
    }
    if(ankleError>.015f||handError>.025f)throw new InvalidOperationException("Seated IK target unreachable: "+ankleError+" / "+handError);
    float soleError=Mathf.Max(Mathf.Abs(SoleBottom(actor,"Left")),Mathf.Abs(SoleBottom(actor,"Right")));if(soleError>.015f)throw new InvalidOperationException("Seated sole support mismatch "+soleError);
    if(smoking)GripFingers(actor);
    string key="seated-"+index+(smoking?"-mouthpiece":"-idle");int triangles=0,n=0;
    foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
     if(!skin.sharedMesh)continue;var mesh=new Mesh{name=key+"-"+n,indexFormat=IndexFormat.UInt32};skin.BakeMesh(mesh,true);var matrix=skin.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;
     var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);mesh.vertices=vertices;
     var normals=mesh.normals;for(int i=0;i<normals.Length;i++)normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;mesh.normals=normals;
     var tangents=mesh.tangents;for(int i=0;i<tangents.Length;i++){var t=matrix.MultiplyVector(new Vector3(tangents[i].x,tangents[i].y,tangents[i].z)).normalized;tangents[i]=new Vector4(t.x,t.y,t.z,tangents[i].w*(matrix.determinant<0?-1:1));}mesh.tangents=tangents;
     mesh.bindposes=new Matrix4x4[0];mesh.boneWeights=new BoneWeight[0];mesh.RecalculateBounds();triangles+=mesh.triangles.Length/3;
     string path=Folder+"/"+key+"-"+n+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){existing.Clear();existing.indexFormat=mesh.indexFormat;existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.tangents=mesh.tangents;existing.uv=mesh.uv;existing.uv2=mesh.uv2;existing.colors=mesh.colors;existing.subMeshCount=mesh.subMeshCount;for(int sub=0;sub<mesh.subMeshCount;sub++)existing.SetIndices(mesh.GetIndices(sub),mesh.GetTopology(sub),sub);existing.bounds=mesh.bounds;EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
     var obj=new GameObject("Static seated surface "+n++);obj.transform.SetParent(output.transform,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterials=skin.sharedMaterials;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
    }
    var marker=output.AddComponent<SeatedSpectatorPose>();marker.fighterId=id;marker.poseEvidence="Pending actual capture: "+key;marker.seatedPoseVisuallyAccepted=false;
    var grip=new GameObject("Mouthpiece grip");grip.transform.SetParent(output.transform,false);grip.transform.position=Bone(bones,"RightHand").position;marker.mouthpieceGrip=grip.transform;marker.holdingMouthpiece=smoking;var mouth=new GameObject("Mouth");mouth.transform.SetParent(output.transform,false);mouth.transform.position=Bone(bones,"Head").position+new Vector3(0,-.075f,.18f);marker.mouth=mouth.transform;
    string prefab=Folder+"/"+key+".prefab";PrefabUtility.SaveAsPrefabAsset(output,prefab);
    Capture(output,key);return new Result{index=index,id=id,pose=smoking?"mouthpiece":"idle",prefab=prefab,ankleError=ankleError,handError=handError,soleError=soleError,triangles=triangles};
   }finally{UnityEngine.Object.DestroyImmediate(holder);UnityEngine.Object.DestroyImmediate(output);}
  }
  static void Capture(GameObject model,string key){
   var scene=EditorSceneManager.NewPreviewScene();var clone=UnityEngine.Object.Instantiate(model);EditorSceneManager.MoveGameObjectToScene(clone,scene);var cameraObj=new GameObject("Seated review camera");EditorSceneManager.MoveGameObjectToScene(cameraObj,scene);var camera=cameraObj.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.12f,.14f);camera.transform.position=new Vector3(2,1.4f,2.6f);camera.transform.LookAt(new Vector3(0,.82f,.12f));
   var lamp=new GameObject("Seated review key");EditorSceneManager.MoveGameObjectToScene(lamp,scene);var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;lamp.transform.rotation=Quaternion.Euler(35,-30,0);
   var seat=GameObject.CreatePrimitive(PrimitiveType.Cube);EditorSceneManager.MoveGameObjectToScene(seat,scene);seat.transform.position=new Vector3(0,Seat-.025f,-.02f);seat.transform.localScale=new Vector3(.48f,.05f,.48f);
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);EditorSceneManager.MoveGameObjectToScene(floor,scene);floor.transform.position=new Vector3(0,-.03f,.1f);floor.transform.localScale=new Vector3(2,.06f,2);
   var supportMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color"));supportMaterial.color=new Color(.3f,.34f,.37f);seat.GetComponent<Renderer>().sharedMaterial=supportMaterial;floor.GetComponent<Renderer>().sharedMaterial=supportMaterial;
   camera.scene=scene;
   var rt=new RenderTexture(700,850,24);var image=new Texture2D(700,850,TextureFormat.RGB24,false);try{camera.targetTexture=rt;camera.Render();camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,700,850),0,0);image.Apply();RenderTexture.active=old;string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/drafts/meeting-seated-v1"));Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,key+".png"),image.EncodeToPNG());}finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(supportMaterial);}
  }
  static void Probe(GameContent content){
   int index=int.Parse(Environment.GetEnvironmentVariable("LF_SEATED_INDICES")??"9");var holder=new GameObject("Seated probe");holder.SetActive(false);var actor=UnityEngine.Object.Instantiate(content.fighters[index],holder.transform);
   try{var bones=Bones(actor);Debug.Log("SEATED_PROBE actorScale="+actor.transform.lossyScale+" falseBounds="+SurfaceBounds(actor,false)+" trueBounds="+SurfaceBounds(actor,true));
    foreach(string name in new[]{"Hips","Head","LeftUpLeg","LeftLeg","LeftFoot","LeftArm","LeftForeArm","LeftHand"})Debug.Log("SEATED_PROBE "+name+" world="+Bone(bones,name).position+" scale="+Bone(bones,name).lossyScale);
    foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){var mesh=skin.sharedMesh;var vertices=mesh.vertices;var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();Bounds bounds=default;bool first=true;
     using(var counts=mesh.GetBonesPerVertex())using(var weights=mesh.GetAllBoneWeights()){int offset=0;for(int i=0;i<vertices.Length;i++){Vector3 world=Vector3.zero;for(int n=0;n<counts[i];n++){var w=weights[offset++];world+=matrices[w.boneIndex].MultiplyPoint3x4(vertices[i])*w.weight;}if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);}}
     Debug.Log("SEATED_PROBE manualLBS="+bounds+" skinScale="+skin.transform.lossyScale+" sourceBounds="+mesh.bounds);
    }
   }finally{UnityEngine.Object.DestroyImmediate(holder);}Debug.Log("AGENT_RESULT seated_probe_complete=true");
  }
  public static void Run(){
   Directory.CreateDirectory(Path.Combine(Application.dataPath,"LighFight/MeetingSpectatorReview"));AssetDatabase.Refresh();var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");if(!content||content.streamed)throw new InvalidOperationException("Offline source catalog required");
   if(Environment.GetEnvironmentVariable("LF_SEATED_PROBE_ONLY")=="1"){Probe(content);return;}
   var rows=new List<Result>();foreach(var token in (Environment.GetEnvironmentVariable("LF_SEATED_INDICES")??"8,9,10,3").Split(',')){int i=int.Parse(token);string id=i<content.fighterMetadata.Length?content.fighterMetadata[i].id:null;if(string.IsNullOrEmpty(id))id="fighter-"+i;rows.Add(Bake(content.fighters[i],i,id,false));rows.Add(Bake(content.fighters[i],i,id,true));}
   AssetDatabase.SaveAssets();string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/drafts/meeting-seated-v1"));Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(new Receipt{rows=rows.ToArray()},true));Debug.Log("AGENT_RESULT seated_candidates="+rows.Count+" visual_acceptance=pending catalog_unchanged=true");
  }
 }
}
