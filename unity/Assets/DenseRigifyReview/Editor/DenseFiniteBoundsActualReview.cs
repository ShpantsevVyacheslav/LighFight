using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static partial class DenseRigifyReimportReview {
 public static void RunFiniteBoundsActualReview(){if(pendingReview!=null)throw new Exception("Busy");Output=BaseOutput+"/finite-bounds-actual-v1";pendingReview=FiniteBoundsSteps();reviewTicks=0;EditorApplication.update+=PumpReview;EditorApplication.QueuePlayerLoopUpdate();}
 static IEnumerator FiniteBoundsSteps(){
  Directory.CreateDirectory(Output);DenseFiniteBoundsReview.ValidateMath();
  var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(previous.path)&&SceneManager.sceneCount==1?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  GameObject actor=null,camGO=null;Material white=null;RenderTexture rt=null;Texture2D read=null;
  try {
   actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DenseRigifyReview/PlayableReview/SailorDenseBOXReview.prefab"),scene);
   foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
   var animator=actor.GetComponentInChildren<Animator>();animator.enabled=false;
   var bridge=actor.GetComponent<DenseAdaptivePoseBridge>();var warm=bridge.Warmup();while(warm.MoveNext())yield return warm.Current;(warm as IDisposable)?.Dispose();
   var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;
   var provider=actor.AddComponent<DenseFiniteDanceBounds>();provider.bridge=bridge;
   var vertices=skin.sharedMesh.vertices;var weights=ReadTypedWeights(skin.sharedMesh);var binds=skin.sharedMesh.bindposes;var matrices=new Matrix4x4[binds.Length];
   white=new Material(Shader.Find("Unlit/Color"));white.color=Color.white;skin.sharedMaterial=white;
   camGO=new GameObject("Full landscape finite bounds camera");var camera=camGO.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.nearClipPlane=.01f;camera.farClipPlane=100;
   rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);rt.Create();camera.targetTexture=rt;read=new Texture2D(1280,720,TextureFormat.RGB24,false,true);
   var rows=new List<string>{"yaw,scale,time,vertices,maskPixels"};int cases=0;
   foreach(float yaw in new[]{-90f,90f,170f})foreach(float scale in new[]{.7f,1f,1.8f}){
    bridge.actorAnchor.SetPositionAndRotation(new Vector3(2.1f,-.3f,1.7f),Quaternion.Euler(0,yaw,0));bridge.actorAnchor.localScale=Vector3.one*scale;
    foreach(double t in DenseFiniteBoundsGpuProbe.ExtremeTimes){
     bridge.Apply(t);for(int i=0;i<matrices.Length;i++)matrices[i]=skin.bones[i].localToWorldMatrix*binds[i];Bounds world;provider.TryGetBounds(Matrix4x4.identity,out world);var lo=world.min-Vector3.one*1e-5f;var hi=world.max+Vector3.one*1e-5f;
     for(int i=0;i<vertices.Length;i++){var v=vertices[i];var w=weights[i];var p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;if(p.x<lo.x||p.y<lo.y||p.z<lo.z||p.x>hi.x||p.y>hi.y||p.z>hi.z)throw new Exception("Actual shaderLBS outside cached source bounds at"+t+" vertex"+i);}
     camera.transform.rotation=Quaternion.Euler(0,yaw,0);camera.transform.position=bridge.actorAnchor.position-camera.transform.forward*5;
     DenseFiniteBoundsGpuProbe.FitFullLandscapeCanvas(provider,camera,1280,720);
     yield return null;yield return null;camera.Render();yield return null;camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,1280,720),0,0);read.Apply();RenderTexture.active=prior;
     int pixels=DenseFiniteBoundsGpuProbe.AssertOpaqueMaskInside(read);rows.Add(yaw+","+scale+","+t+","+vertices.Length+","+pixels);cases++;
     if(scale==1f&&yaw==90f)File.WriteAllBytes(Output+"/mask-"+t.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)+".png",read.EncodeToPNG());
     File.WriteAllLines(Output+"/actual-cases.csv",rows);
    }
   }
   File.WriteAllText(Output+"/COMPLETE.txt","PASS pure math + "+cases+" actual shaderLBS fullmesh containment/GPU full-landscape masks. Originalweights, finite steadyDance UNGROUNDED; no continuous/entry/return/physicalfloor/artist acceptance. SMR.localBounds unchanged.");
  }finally{if(actor){var b=actor.GetComponent<DenseAdaptivePoseBridge>();if(b)b.Dispose();UnityEngine.Object.DestroyImmediate(actor);}if(camGO)UnityEngine.Object.DestroyImmediate(camGO);if(white)UnityEngine.Object.DestroyImmediate(white);if(read)UnityEngine.Object.DestroyImmediate(read);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
 }
}
