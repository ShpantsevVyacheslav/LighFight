using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LighFight.Runtime;

public static class UniqueDanceRenderGate
{
 const string Folder="Assets/UniqueDancesReview";
 const string Output="../art/source/unique-dances-20261018/audit/render";
 public static void Girdle(){RenderPilot("Review.girdle.prefab","girdle");}
 public static void ExactGenericSailor(){RenderPilot("Review.exact-generic.prefab","exact-generic");}
 static void RenderPilot(string prefabName,string tag)
 {
  Directory.CreateDirectory(Output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  RenderSettings.ambientLight=new Color(.6f,.6f,.6f);
  var key=new GameObject("ReviewKey").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.1f;key.transform.rotation=Quaternion.Euler(32,-35,0);
  var cam=new GameObject("ReviewCamera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=1.55f;cam.backgroundColor=new Color(.15f,.17f,.2f);cam.clearFlags=CameraClearFlags.SolidColor;
  cam.transform.position=new Vector3(2.5f,1.2f,5);cam.transform.LookAt(new Vector3(0,.95f,0));
  foreach(int i in new[]{9})foreach(float phase in new[]{0f,.25f,.5f,.75f,.99999f})Render(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Fighter-"+i+"/"+prefabName),cam,i+"-"+tag+"-"+phase,phase);
  Debug.Log("AGENT_RESULT separate girdle paired review images rendered; no promotion");
 }
 public static void Run()
 {
  UniqueDanceCurvePack.PackOnly();Directory.CreateDirectory(Output);
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  RenderSettings.ambientLight=new Color(.6f,.6f,.6f);
  var key=new GameObject("ReviewKey").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.1f;key.transform.rotation=Quaternion.Euler(32,-35,0);
  var cam=new GameObject("ReviewCamera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=1.55f;cam.backgroundColor=new Color(.15f,.17f,.2f);cam.clearFlags=CameraClearFlags.SolidColor;
  cam.transform.position=new Vector3(2.5f,1.2f,5);cam.transform.LookAt(new Vector3(0,.95f,0));
  var original=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
  var report=new List<string>();
  for(int i=0;i<11;i++)
  {
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Fighter-"+i+"/Review.prefab");
   var sourceSkin=original.fighters[i].GetComponentInChildren<SkinnedMeshRenderer>();var targetSkin=prefab.GetComponentInChildren<SkinnedMeshRenderer>();
   if(sourceSkin.sharedMesh!=targetSkin.sharedMesh||!sourceSkin.sharedMaterials.SequenceEqual(targetSkin.sharedMaterials)||original.fighters[i].GetComponentInChildren<Animator>().avatar!=prefab.GetComponentInChildren<Animator>().avatar)throw new Exception("Original geometry/material/Avatar reference changed "+i);
   foreach(float phase in new[]{0f,.25f,.5f,.75f,.99999f})Render(prefab,cam,i+"-new-"+phase,phase);
   report.Add(i+" same original mesh/material/Avatar references; mesh="+NativeSupportIntegrationBuilder.MeshHash(targetSkin.sharedMesh));
  }
  foreach(float phase in new[]{0f,.25f,.5f,.75f,.99999f})Render(original.fighters[9],cam,"9-originalNative-"+phase,phase);
  File.WriteAllLines(Output+"/preservation.txt",report);
  // Only our intermediate assets are removed, after exact packed-key validation.
  foreach(string path in Directory.GetFiles(Folder,"Dance.transform.anim",SearchOption.AllDirectories))AssetDatabase.DeleteAsset(path.Replace('\\','/'));
  AssetDatabase.SaveAssets();Debug.Log("AGENT_RESULT packed 11 losslessly; render gate images and original refs preserved");
 }
 static void Render(GameObject prefab,Camera cam,string name,float phase)
 {
  var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
  try
  {
   var view=actor.GetComponent<FighterView>();var overlay=actor.GetComponent<NativeSupportOverlay>();var animator=actor.GetComponentInChildren<Animator>();
   view.FullPreviewMotion=true;view.PreviewNativeMotion("Dance",view.PreviewDuration("Dance")*phase);
   overlay.EvaluateForReview("Dance",phase,0,false);var exact=actor.GetComponent<ExactGenericDanceRetarget>();if(exact)exact.EvaluateForReview(view.PreviewDuration("Dance")*phase);actor.GetComponent<CandidateFootGrounding>().ApplyForReview();
   Save(cam,name+"-body");
   var oldP=cam.transform.position;var oldR=cam.transform.rotation;float oldSize=cam.orthographicSize;
   var center=(animator.GetBoneTransform(HumanBodyBones.Neck).position+animator.GetBoneTransform(HumanBodyBones.Chest).position)*.5f;
   cam.transform.position=center+Vector3.forward*4;cam.transform.LookAt(center);cam.orthographicSize=.38f;
   Save(cam,name+"-neck-front");
   cam.transform.position=center+new Vector3(2,0,4);cam.transform.LookAt(center);Save(cam,name+"-neck-oblique");
   cam.transform.SetPositionAndRotation(oldP,oldR);cam.orthographicSize=oldSize;
  }
  finally{foreach(var component in actor.GetComponents<MonoBehaviour>())if(component is IActorCurveWarmup warm)warm.Dispose();UnityEngine.Object.DestroyImmediate(actor);}
 }
 static void Save(Camera cam,string name)
 {
  var rt=RenderTexture.GetTemporary(960,720,24);var previous=RenderTexture.active;
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(960,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();File.WriteAllBytes(Output+"/"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
  finally{cam.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
 }
}
