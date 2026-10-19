using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Editor {
 public static class CatalogBattleCapture {
  public static void Run(){
   const int count=80,width=800,height=400;const float fps=20;
   string folder=Path.GetFullPath("../art/drafts/catalog-battle-v1");Directory.CreateDirectory(folder+"/frames");
   var scene=EditorSceneManager.NewPreviewScene();var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var arena=UnityEngine.Object.Instantiate(content.arenas[0]);EditorSceneManager.MoveGameObjectToScene(arena,scene);
   foreach(var crowd in arena.GetComponentsInChildren<MeetingSpectators>(true))crowd.enabled=false;
   var actors=new[]{UnityEngine.Object.Instantiate(content.fighters[8]),UnityEngine.Object.Instantiate(content.fighters[9])};
   foreach(var actor in actors){EditorSceneManager.MoveGameObjectToScene(actor,scene);foreach(var a in actor.GetComponentsInChildren<Animator>()){a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.updateMode=AnimatorUpdateMode.Normal;}}
   var views=actors.Select(x=>x.GetComponent<FighterView>()).ToArray();var match=new Match(true);match.A.X=.03f;match.B.X=1.02f;
   var cameraObj=new GameObject("Catalogue real scene camera");EditorSceneManager.MoveGameObjectToScene(cameraObj,scene);var cam=cameraObj.AddComponent<Camera>();cam.scene=scene;cam.orthographic=true;cam.orthographicSize=1.16f;cam.nearClipPlane=.05f;cam.farClipPlane=35;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.035f,.055f);cam.transform.position=new Vector3(0,1.13f,-5);cam.transform.LookAt(new Vector3(0,1.11f,0));
   var keyObj=new GameObject("Catalogue key");EditorSceneManager.MoveGameObjectToScene(keyObj,scene);var key=keyObj.AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.15f;key.color=new Color(1,.94f,.86f);key.transform.rotation=Quaternion.Euler(32,-28,0);
   var fillObj=new GameObject("Catalogue fill");EditorSceneManager.MoveGameObjectToScene(fillObj,scene);var fill=fillObj.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.color=new Color(.70f,.83f,1);fill.transform.rotation=Quaternion.Euler(18,140,0);RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.28f,.31f,.37f);ShaderUtil.allowAsyncCompilation=false;QualitySettings.skinWeights=SkinWeights.Unlimited;
   var skins=actors.SelectMany(x=>x.GetComponentsInChildren<SkinnedMeshRenderer>()).ToArray();var frozen=skins.Select(s=>{s.updateWhenOffscreen=true;var o=new GameObject("Actual baked game surface");EditorSceneManager.MoveGameObjectToScene(o,scene);o.AddComponent<MeshFilter>().sharedMesh=new Mesh{indexFormat=IndexFormat.UInt32};o.AddComponent<MeshRenderer>().sharedMaterials=s.sharedMaterials;s.enabled=false;return o;}).ToArray();
   var transforms=actors.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();Vector3[] startP=null,startS=null;Quaternion[] startQ=null;int previousTick=0;
   var rt=new RenderTexture(width,height,24){antiAliasing=2};var image=new Texture2D(width,height,TextureFormat.RGB24,false);cam.targetTexture=rt;
   try{
    for(int frame=0;frame<count;frame++){
     int tick=Mathf.RoundToInt(frame/fps*60);
     while(previousTick<tick){previousTick++;if(previousTick==35)match.A.Start(0);if(previousTick==90)match.B.Start(1);if(previousTick==145)match.A.Start(4);match.Step();match.A.X=.03f;match.B.X=1.02f;views[0].PoseTowards(match.A,match.B,1f/60);views[1].PoseTowards(match.B,match.A,1f/60);}
     views[0].SampleContactPose(match.A,match.B);views[1].SampleContactPose(match.B,match.A);
     if(frame==0){startP=transforms.Select(t=>t.localPosition).ToArray();startQ=transforms.Select(t=>t.localRotation).ToArray();startS=transforms.Select(t=>t.localScale).ToArray();}
     if(frame>=count-9){float t=Mathf.SmoothStep(0,1,(frame-(count-9))/8f);for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=Vector3.Lerp(transforms[i].localPosition,startP[i],t);transforms[i].localRotation=Quaternion.Slerp(transforms[i].localRotation,startQ[i],t);transforms[i].localScale=Vector3.Lerp(transforms[i].localScale,startS[i],t);}}
     for(int i=0;i<skins.Length;i++){var mesh=frozen[i].GetComponent<MeshFilter>().sharedMesh;skins[i].BakeMesh(mesh,true);var matrix=skins[i].transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;var p=mesh.vertices;for(int j=0;j<p.Length;j++)p[j]=matrix.MultiplyPoint3x4(p[j]);mesh.vertices=p;var n=mesh.normals;for(int j=0;j<n.Length;j++)n[j]=normalMatrix.MultiplyVector(n[j]).normalized;mesh.normals=n;mesh.RecalculateBounds();}
     cam.Render();cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();RenderTexture.active=old;File.WriteAllBytes(folder+"/frames/frame-"+frame.ToString("D3")+".png",image.EncodeToPNG());if(frame%10==0){Debug.Log("CATALOG_FRAME "+frame);GC.Collect();}
    }
    File.WriteAllText(folder+"/capture.json","{\"capture\":\"Unity graphics Editor production-prefab/offscreen render\",\"arena\":0,\"fighters\":[8,9],\"width\":800,\"height\":400,\"fps\":20,\"frames\":80,\"hud\":false,\"sourceGeometryChanged\":false,\"loopReturn\":\"last0.4s bone TRS blend to first pose\",\"phoneFootage\":false,\"physicalContactAcceptance\":false}");Debug.Log("AGENT_RESULT actual_catalog_frames=80 no_HUD=true source_assets_unchanged=true");
   }finally{cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);foreach(var o in frozen)UnityEngine.Object.DestroyImmediate(o.GetComponent<MeshFilter>().sharedMesh);EditorSceneManager.ClosePreviewScene(scene);}
  }
 }
}
