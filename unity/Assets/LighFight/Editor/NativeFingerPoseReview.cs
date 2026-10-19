using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Editor {
 public static class NativeFingerPoseReview {
  public static void Run(){QualitySettings.skinWeights=SkinWeights.Unlimited;
   const string folder="Assets/LighFight/Art/Drafts/TripoIsland";
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/native-pack.fbx");var clips=AssetDatabase.LoadAllAssetsAtPath(folder+"/native-pack.fbx").OfType<AnimationClip>().ToArray();
   var fist=clips.Single(c=>c.name=="box_02");var open=clips.Single(c=>c.name=="box_01");var pose=NativeFingerPoseCapture.Capture(model,fist,1.4f,folder+"/native-fist.asset");
   var scene=EditorSceneManager.NewPreviewScene();var obj=UnityEngine.Object.Instantiate(model);EditorSceneManager.MoveGameObjectToScene(obj,scene);foreach(var a in obj.GetComponentsInChildren<Animator>()){a.Rebind();a.Update(0);a.enabled=false;}
   try{
    string output=Path.GetFullPath("../art/previews/island-tripo-v1/native-fists");var args=Environment.GetCommandLineArgs();int ix=Array.IndexOf(args,"-tripoOutput");if(ix>=0)output=Path.GetFullPath(args[ix+1]);Directory.CreateDirectory(output);
    open.SampleAnimation(obj,open.length*.35f);
    var transforms=obj.GetComponentsInChildren<Transform>();var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
    Capture(scene,obj,folder,output,"native-open");fist.SampleAnimation(obj,1.4f);Capture(scene,obj,folder,output,"native-closed");
    float fingerPositionDelta=transforms.Select((t,i)=>pose.joints.Any(j=>j.path==AnimationUtility.CalculateTransformPath(t,obj.transform))?Vector3.Distance(positions[i],t.localPosition):0).Max();
    for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
    var animator=obj.GetComponentInChildren<Animator>();var body=animator.GetComponentsInChildren<Transform>().Where(t=>!pose.joints.Any(j=>j.path==AnimationUtility.CalculateTransformPath(t,animator.transform))).ToArray();var before=body.Select(t=>t.localRotation).ToArray();
    var driver=obj.AddComponent<NativeFingerPoseDriver>();driver.Configure(animator,pose,new[]{"Idle","jab"});driver.ApplyForReview(true,.2f);
    float bodyError=body.Select((t,i)=>RotationError(before[i],t.localRotation)).DefaultIfEmpty().Max();if(bodyError>.0001f)throw new InvalidOperationException("Finger pose changed the body: "+bodyError);
    float jointError=pose.joints.Select(j=>RotationError(animator.transform.Find(j.path).localRotation,j.rotation)).Max();if(jointError>.001f)throw new InvalidOperationException("Native finger pose not reached: "+jointError);
    var avatar=pose.sourceAvatar;bool rejected=false;pose.sourceAvatar=null;
    try{driver.Configure(animator,pose,new[]{"Idle"});}catch(InvalidOperationException){rejected=true;}finally{pose.sourceAvatar=avatar;}
    if(!rejected)throw new InvalidOperationException("Wrong-rig mutation was accepted");
    File.WriteAllText(output+"/audit.json","{\"joints\":30,\"fingerPositionDelta\":"+fingerPositionDelta.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"bodyRotationErrorDegrees\":"+bodyError.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"fingerRotationErrorDegrees\":"+jointError.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"wrongRigRejected\":true,\"geometryModified\":false}");
    Capture(scene,obj,folder,output);Debug.Log("NATIVE_FINGER_RESULT 30 original joints, body unchanged, wrong-rig mutation rejected");
   }finally{UnityEngine.Object.DestroyImmediate(obj);EditorSceneManager.ClosePreviewScene(scene);}
  }
  static float RotationError(Quaternion a,Quaternion b){if(Quaternion.Dot(a,b)<0)b=new Quaternion(-b.x,-b.y,-b.z,-b.w);float d=Mathf.Max(Mathf.Abs(a.x-b.x),Mathf.Abs(a.y-b.y),Mathf.Abs(a.z-b.z),Mathf.Abs(a.w-b.w));return d*229.18312f;}
  static void Capture(UnityEngine.SceneManagement.Scene scene,GameObject obj,string folder,string output,string name="hand"){
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/island-h31-unity-native-pack_basecolor.JPEG");var material=new Material(Shader.Find("Unlit/Texture")){mainTexture=texture};var cameraObj=new GameObject("Native fingers review");EditorSceneManager.MoveGameObjectToScene(cameraObj,scene);var camera=cameraObj.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.nearClipPlane=.01f;camera.farClipPlane=100f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.17f,.2f);camera.orthographic=true;var target=new RenderTexture(1200,1000,24);target.Create();camera.targetTexture=target;var image=new Texture2D(1200,1000,TextureFormat.RGB24,false);var old=RenderTexture.active;var bakedObjects=new System.Collections.Generic.List<GameObject>();var bakedMeshes=new System.Collections.Generic.List<Mesh>();var originalSkins=obj.GetComponentsInChildren<SkinnedMeshRenderer>();
   try{
    var skins=obj.GetComponentsInChildren<SkinnedMeshRenderer>();foreach(var skin in skins){skin.sharedMaterials=skin.sharedMaterials.Select(_=>material).ToArray();skin.updateWhenOffscreen=true;}
    foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh,false);bakedMeshes.Add(mesh);var copy=new GameObject("CPU surface");EditorSceneManager.MoveGameObjectToScene(copy,scene);copy.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);copy.transform.localScale=Vector3.Scale(skin.transform.lossyScale,new Vector3(1/skin.transform.localScale.x,1/skin.transform.localScale.y,1/skin.transform.localScale.z));copy.AddComponent<MeshFilter>().sharedMesh=mesh;copy.AddComponent<MeshRenderer>().sharedMaterial=material;bakedObjects.Add(copy);skin.enabled=false;}
    var hand=obj.GetComponentsInChildren<Transform>().Single(t=>t.name=="Right_Hand");camera.transform.position=hand.position+new Vector3(.15f,.08f,.30f);camera.transform.LookAt(hand.position);camera.orthographicSize=.13f;
    ShaderUtil.allowAsyncCompilation=false;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1200,1000),0,0);image.Apply();File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());
   }finally{foreach(var item in bakedObjects)UnityEngine.Object.DestroyImmediate(item);foreach(var mesh in bakedMeshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var skin in originalSkins)skin.enabled=true;RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObj);UnityEngine.Object.DestroyImmediate(material);}
  }
 }
}
