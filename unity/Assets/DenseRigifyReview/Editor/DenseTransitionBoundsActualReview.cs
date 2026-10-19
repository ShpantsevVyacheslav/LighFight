using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static partial class DenseRigifyReimportReview {
 sealed class FeatureBoxes {
  public string[] names;int[] joints;double[] lower,upper;int groups;bool rebased;
  public FeatureBoxes(string path){using(var r=new BinaryReader(File.OpenRead(path))){uint magic=r.ReadUInt32();if(magic!=0x31584246&&magic!=0x32584246)throw new Exception("FBX1/2");if(magic==0x32584246)rebased=r.ReadInt32()!=0;groups=r.ReadInt32();int n=r.ReadInt32();names=new string[n];for(int i=0;i<n;i++)names[i]=System.Text.Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));joints=new int[groups*4];for(int i=0;i<joints.Length;i++)joints[i]=r.ReadInt32();lower=new double[groups*16];upper=new double[lower.Length];for(int i=0;i<lower.Length;i++)lower[i]=r.ReadDouble();for(int i=0;i<upper.Length;i++)upper[i]=r.ReadDouble();}}
  public Bounds Evaluate(Matrix4x4[] m){double[] lo={double.PositiveInfinity,double.PositiveInfinity,double.PositiveInfinity},hi={double.NegativeInfinity,double.NegativeInfinity,double.NegativeInfinity};for(int g=0;g<groups;g++)for(int a=0;a<3;a++){double l=0,h=0,mag=0;for(int k=0;k<4;k++)for(int q=0;q<4;q++){double c=m[joints[g*4+k]][a,q]-(rebased&&k>0?m[joints[g*4]][a,q]:0),x=lower[g*16+k*4+q],y=upper[g*16+k*4+q];l+=c*(c>=0?x:y);h+=c*(c>=0?y:x);mag+=Math.Abs(c)*Math.Max(Math.Abs(x),Math.Abs(y));}double e=mag*32*2.220446049250313e-16;lo[a]=Math.Min(lo[a],l-e);hi[a]=Math.Max(hi[a],h+e);}var b=new Bounds();b.SetMinMax(new Vector3((float)lo[0],(float)lo[1],(float)lo[2])-Vector3.one*1e-5f,new Vector3((float)hi[0],(float)hi[1],(float)hi[2])+Vector3.one*1e-5f);return b;}
 }
 sealed class TransitionSnapshot {public string stage;public double time;public Vector3[] p,s;public Quaternion[] q;public Matrix4x4[] matrices;public Bounds bounds;}
 static bool refinedTransitionReview;
 public static void RunRebasedTransitionBoundsReview(){refinedTransitionReview=true;RunTransitionBoundsActualReview();}
 public static void RunTransitionBoundsActualReview(){if(pendingReview!=null)throw new Exception("Busy");Output=BaseOutput+(refinedTransitionReview?"/finite-transition-rebased-v1":"/finite-transition-v1");pendingReview=TransitionBoundsSteps();reviewTicks=0;EditorApplication.update+=PumpReview;EditorApplication.QueuePlayerLoopUpdate();}
 static IEnumerator TransitionBoundsSteps(){
  Directory.CreateDirectory(Output);var clock=System.Diagnostics.Stopwatch.StartNew();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(previous.path)&&SceneManager.sceneCount==1?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  GameObject actor=null,camGO=null;Material white=null;RenderTexture rt=null;Texture2D read=null;
  try{
   var boxes=new FeatureBoxes(refinedTransitionReview?"/tmp/lighfight-original-feature-rebased128.fbox":"/tmp/lighfight-original-feature-summary.fbox");var config=LoadConfig();var source=Source.Read(config.oracle);
   if(!boxes.names.SequenceEqual(source.names))throw new Exception("Original named joint order");
   actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DenseRigifyReview/PlayableReview/SailorDenseBOXReview.prefab"),scene);
   foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
   var owner=actor.GetComponent<DenseAdaptiveReviewOwner>();owner.enabled=true;var bridge=actor.GetComponent<DenseAdaptivePoseBridge>();var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;
   var warm=owner.Warmup();while(warm.MoveNext())yield return warm.Current;(warm as IDisposable)?.Dispose();
   var animator=actor.GetComponentInChildren<Animator>();animator.enabled=true;animator.Play("Idle",0,0);animator.Update(0); // explicit imported-default neutral review hold
   var bones=skin.bones;var binds=skin.sharedMesh.bindposes;var order=boxes.names.Select(n=>Array.FindIndex(bones,b=>b.name==n)).ToArray();if(order.Any(i=>i<0))throw new Exception("Named target bones");
   var transport=skin.transform.worldToLocalMatrix*BasisMatrix*source.meshWorld; // original p -> restored mesh local, fixed bind-only
   var domain=new SortedSet<double>{0,.07,.14,.21,.28};for(int i=1;i<34;i++)domain.Add(Math.Min(.28,i/120.0));
   var snapshots=new List<TransitionSnapshot>();
   for(int stage=0;stage<2;stage++){
    if(stage==0)owner.PlayDance();else{owner.AdvanceReviewFrame((float)bridge.AuthoredDuration);owner.ReturnToNeutral();}
    double prior=0;
    foreach(double t in domain){owner.AdvanceReviewFrame((float)(t-prior));prior=t;
     var snap=new TransitionSnapshot{stage=stage==0?"entry":"return",time=t,p=bones.Select(b=>b.localPosition).ToArray(),q=bones.Select(b=>b.localRotation).ToArray(),s=bones.Select(b=>b.localScale).ToArray(),matrices=new Matrix4x4[order.Length]};
     for(int b=0;b<order.Length;b++)snap.matrices[b]=bones[order[b]].localToWorldMatrix*binds[order[b]]*transport;
     snap.bounds=boxes.Evaluate(snap.matrices);snapshots.Add(snap);
     if(clock.Elapsed.TotalSeconds>600)throw new TimeoutException("600s capture budget");
    }
   }
   using(var w=new BinaryWriter(File.Create(Output+"/actual-transition-matrices.bin"))){w.Write(0x31524354);w.Write(snapshots.Count);w.Write(boxes.names.Length);foreach(var n in boxes.names){var bytes=System.Text.Encoding.UTF8.GetBytes(n);w.Write(bytes.Length);w.Write(bytes);}foreach(var snap in snapshots){w.Write(snap.stage=="entry"?0:1);w.Write(snap.time);foreach(var m in snap.matrices)for(int r=0;r<4;r++)for(int c=0;c<4;c++)w.Write((double)m[r,c]);}}
   var union=snapshots[0].bounds;foreach(var snap in snapshots)union.Encapsulate(snap.bounds);union.Expand(.05f);if(refinedTransitionReview){var steady=new Bounds();steady.SetMinMax(new Vector3(-.995787f,-.042100f,-.648709f),new Vector3(.931090f,1.255259f,.829446f));union.Encapsulate(steady);} // explicit .025m each side; source finite steadyunion, negativeY is conservative notfloor
   white=new Material(Shader.Find("Unlit/Color"));white.color=Color.white;skin.sharedMaterial=white;
   camGO=new GameObject("Full landscape entry return camera");var camera=camGO.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.aspect=1280f/720;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.transform.position=new Vector3(union.center.x,union.center.y,-5);camera.orthographicSize=DenseFiniteDanceBounds.FullCanvasOrthographicSize(union,camera.aspect);
   rt=new RenderTexture(1280,720,24);rt.Create();camera.targetTexture=rt;read=new Texture2D(1280,720,TextureFormat.RGB24,false,true);
   var vertices=skin.sharedMesh.vertices;var weights=ReadTypedWeights(skin.sharedMesh);var rows=new List<string>{"stage,time,vertices,maskPixels"};int count=0;
   foreach(var snap in snapshots.Where(s=>new[]{0.0,.07,.14,.21,.28}.Contains(s.time))){
    for(int i=0;i<bones.Length;i++){bones[i].SetLocalPositionAndRotation(snap.p[i],snap.q[i]);bones[i].localScale=snap.s[i];}
    var actual=bones.Select((b,i)=>b.localToWorldMatrix*binds[i]).ToArray();var lo=snap.bounds.min-Vector3.one*1e-5f;var hi=snap.bounds.max+Vector3.one*1e-5f;
    for(int i=0;i<vertices.Length;i++){var v=vertices[i];var w=weights[i];var p=actual[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+actual[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+actual[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+actual[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;if(p.x<lo.x||p.y<lo.y||p.z<lo.z||p.x>hi.x||p.y>hi.y||p.z>hi.z)throw new Exception("Complete feature summary misses actual renderer vertex"+i);}
    yield return null;yield return null;camera.Render();yield return null;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,1280,720),0,0);read.Apply();RenderTexture.active=old;int pixels=DenseFiniteBoundsGpuProbe.AssertOpaqueMaskInside(read);rows.Add(snap.stage+","+snap.time+","+vertices.Length+","+pixels);File.WriteAllBytes(Output+"/"+snap.stage+"-"+snap.time.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+".png",read.EncodeToPNG());count++;if(clock.Elapsed.TotalSeconds>600)throw new TimeoutException("600s actual budget");
   }
   File.WriteAllLines(Output+"/direct-holdouts.csv",rows);File.WriteAllText(Output+"/COMPLETE.txt","PASS finite actual owner entry/return "+snapshots.Count+" captures, "+count+" fullmesh/GPU holdouts. Originalweights ungrounded native rootidentity only. Finite not continuous. Bounds="+union+" seconds="+clock.Elapsed.TotalSeconds);
  }finally{if(actor){var o=actor.GetComponent<DenseAdaptiveReviewOwner>();if(o)o.Dispose();UnityEngine.Object.DestroyImmediate(actor);}if(camGO)UnityEngine.Object.DestroyImmediate(camGO);if(white)UnityEngine.Object.DestroyImmediate(white);if(read)UnityEngine.Object.DestroyImmediate(read);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
 }
}
