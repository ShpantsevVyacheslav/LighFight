using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Runtime;

namespace LighFight.Editor {
 // Diagnostic only: reuse motion rotations on the provider's untouched native rig.
 // No bone offsets, scales, mesh vertices, UVs or weights are edited.
 public static class TripoRetargetReview {
  sealed class Joint {
   public Transform source,target;
   public Quaternion sourceRest,targetRest,axis;
   public string path;
   public AnimationCurve[] curves={new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
   public Quaternion previous;
  }
  static Dictionary<string,Transform> Bones(GameObject model)=>model.GetComponentInChildren<SkinnedMeshRenderer>().bones.GroupBy(t=>NativeRigBones.CanonicalName(t.name)).ToDictionary(g=>g.Key,g=>g.Single());
  static Dictionary<Transform,Matrix4x4> Bind(GameObject model) {
   var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();return skin.bones.Select((bone,i)=>new{bone,matrix=skin.transform.localToWorldMatrix*skin.sharedMesh.bindposes[i].inverse}).ToDictionary(x=>x.bone,x=>x.matrix);
  }
  static Quaternion LocalBind(Transform bone,Dictionary<Transform,Matrix4x4> bind)=>Quaternion.Inverse(bind.ContainsKey(bone.parent)?bind[bone.parent].rotation:bone.parent.rotation)*bind[bone].rotation;
  static Quaternion Frame(Dictionary<string,Transform> bones) {
   var right=(bones["RightArm"].position-bones["LeftArm"].position).normalized;
   var up=(bones["Head"].position-bones["Hips"].position).normalized;
   return Quaternion.LookRotation(Vector3.Cross(right,up).normalized,up);
  }
  public static void Run() {
   QualitySettings.skinWeights=SkinWeights.Unlimited;
   string targetPath="Assets/LighFight/Art/Drafts/TripoRose/native-pack.fbx";
   var sourceModel=AssetDatabase.LoadAssetAtPath<GameObject>(NativeStanceReview.Source);
   var targetModel=AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
   var sourceClip=AssetDatabase.LoadAllAssetsAtPath(NativeStanceReview.Source).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview"));
   var scene=EditorSceneManager.NewPreviewScene();var source=UnityEngine.Object.Instantiate(sourceModel);var target=UnityEngine.Object.Instantiate(targetModel);
   EditorSceneManager.MoveGameObjectToScene(source,scene);EditorSceneManager.MoveGameObjectToScene(target,scene);
   try {
    foreach(var actor in new[]{source,target})foreach(var animator in actor.GetComponentsInChildren<Animator>()){animator.Rebind();animator.Update(0);animator.enabled=false;}
    var from=Bones(source);var to=Bones(target);var sourceBind=Bind(source);var targetBind=Bind(target);var alignment=Frame(to)*Quaternion.Inverse(Frame(from));
    var joints=to.Where(kv=>from.ContainsKey(kv.Key)).Select(kv=>new Joint{source=from[kv.Key],target=kv.Value,sourceRest=LocalBind(from[kv.Key],sourceBind),targetRest=LocalBind(kv.Value,targetBind),
     axis=Quaternion.Inverse(targetBind[kv.Value].rotation)*alignment*sourceBind[from[kv.Key]].rotation,path=AnimationUtility.CalculateTransformPath(kv.Value,target.transform)}).ToArray();
    if(joints.Length<50)throw new InvalidOperationException("Insufficient native rig coverage: "+joints.Length);
    var clip=new AnimationClip{name="ReviewRetargetedGuard",frameRate=60};int frames=Mathf.CeilToInt(sourceClip.length*60);
    for(int frame=0;frame<=frames;frame++) {
     float time=sourceClip.length*frame/frames;sourceClip.SampleAnimation(source,time);
     foreach(var joint in joints) {
      var delta=Quaternion.Inverse(joint.sourceRest)*joint.source.localRotation;
      var pose=joint.targetRest*joint.axis*delta*Quaternion.Inverse(joint.axis);
      if(frame>0&&Quaternion.Dot(pose,joint.previous)<0)pose=new Quaternion(-pose.x,-pose.y,-pose.z,-pose.w);
      joint.previous=pose;joint.target.localRotation=pose;
      for(int component=0;component<4;component++)joint.curves[component].AddKey(time,pose[component]);
     }
    }
    foreach(var joint in joints)for(int component=0;component<4;component++) {
     var curve=joint.curves[component];for(int key=0;key<curve.length;key++){AnimationUtility.SetKeyLeftTangentMode(curve,key,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,key,AnimationUtility.TangentMode.Linear);}
     AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(joint.path,typeof(Transform),"m_LocalRotation."+"xyzw"[component]),curve);
    }
    const string path="Assets/LighFight/Art/Drafts/TripoRose/retarget-guard-review.anim";
    var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(saved){EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);clip=saved;}else AssetDatabase.CreateAsset(clip,path);
    NativeFighterImport.ValidateClip(target,clip);AssetDatabase.SaveAssets();source.SetActive(false);
    string output=Path.GetFullPath("../art/previews/rose-tripo-v1/retarget-review");Directory.CreateDirectory(output);
    foreach(float phase in new[]{0f,.35f,.7f,1f}){clip.SampleAnimation(target,clip.length*phase);Capture(scene,target,output+"/guard-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png");}
    File.WriteAllText(output+"/audit.json","{\"mappedJoints\":"+joints.Length+",\"meshModified\":false,\"boneOffsetsModified\":false,\"diagnosticOnly\":true}");
   }finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(target);EditorSceneManager.ClosePreviewScene(scene);}
  }
  static void Capture(UnityEngine.SceneManagement.Scene scene,GameObject actor,string path) {
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LighFight/Art/Drafts/TripoRose/rose-h31-unity-native-pack_basecolor.JPEG");
   var material=new Material(Shader.Find("Unlit/Texture")){mainTexture=texture};var cameraObject=new GameObject("Retarget review camera");EditorSceneManager.MoveGameObjectToScene(cameraObject,scene);
   var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.nearClipPlane=.01f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.19f,.22f);camera.orthographic=true;
   var render=new RenderTexture(1000,1200,24);render.Create();camera.targetTexture=render;var image=new Texture2D(1000,1200,TextureFormat.RGB24,false);var old=RenderTexture.active;
   var objects=new List<GameObject>();var meshes=new List<Mesh>();var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();Bounds bounds=default;bool first=true;
   try {
    foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh,false);meshes.Add(mesh);var copy=new GameObject("CPU surface");objects.Add(copy);EditorSceneManager.MoveGameObjectToScene(copy,scene);copy.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);copy.transform.localScale=Vector3.Scale(skin.transform.lossyScale,new Vector3(1/skin.transform.localScale.x,1/skin.transform.localScale.y,1/skin.transform.localScale.z));copy.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=copy.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;skin.enabled=false;if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
    camera.transform.position=bounds.center+Vector3.forward*bounds.size.y*3;camera.transform.LookAt(bounds.center);camera.orthographicSize=Mathf.Max(bounds.size.y*.58f,bounds.size.x*.65f);ShaderUtil.allowAsyncCompilation=false;camera.Render();RenderTexture.active=render;image.ReadPixels(new Rect(0,0,1000,1200),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
   }finally{foreach(var skin in skins)skin.enabled=true;foreach(var obj in objects)UnityEngine.Object.DestroyImmediate(obj);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(render);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(material);}
  }
 }
}
