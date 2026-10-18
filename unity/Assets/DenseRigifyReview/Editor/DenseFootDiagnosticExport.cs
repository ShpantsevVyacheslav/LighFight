#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
// One state export only. Never runs a benchmark or changes the point set/math/tolerance.
public static class DenseFootDiagnosticExport {
 const string Prefab="Assets/DenseRigifyReview/PlayableReview/SailorDenseBOXReview.prefab";
 const string Packet="art/source/unique-dances-20261018/packet/dense-reimport/foot-feature-bvh/unity-adapter/actual-shared-kernel-diagnostic";
 [Serializable]sealed class Scalar {public string value,bits;public Scalar(float f){value=f.ToString("R",System.Globalization.CultureInfo.InvariantCulture);bits="0x"+BitConverter.ToInt32(BitConverter.GetBytes(f),0).ToString("X8");}}
 [Serializable]sealed class Influence {public int bone;public string boneName;public Scalar weight;public Scalar[] bindXYZ,verticalRow;}
 [Serializable]sealed class Witness {public int point,originalVertex,importedVertex;public Scalar coreLeaf,typedPoint;public Influence[] influences;}
 [Serializable]sealed class Report {
  public string status="not run",scope="one actual state; no benchmark/acceptance",fixtureSHA256,idsSHA256;
  public int pointCount,boneCount,nodeCount,rowsChangedAfterCandidateReset;public string[] boneNames,rowFloatBits;
  public Scalar accelerated,fullTyped,coreFullLeafScan,typedSinglePointScan,candidate,originalExpression;public string independentDouble;public int independentStepMismatches;public bool candidateParity;
  public Witness acceleratedWitness,typedWitness;public Vector3 nativeLocalBefore,nativeLocalAfter,nativeWorldAfter,groundOffsetLocalBefore,groundOffsetLocalAfter;public bool rigIsOwnerOffset;
  public int observableProbeBytes,observableProbeFirstByte;public long gcBefore,gcAfter,gcDelta,heapBefore,heapAfter,profilerMonoBefore,profilerMonoAfter;public bool threadAllocationCounterCalibrated;
 }
 public static void Run(){
  string project=Directory.GetParent(Application.dataPath).Parent.FullName,output=Path.Combine(project,Packet);Directory.CreateDirectory(output);var report=new Report();GameObject actor=null;
  try{
   GC.GetAllocatedBytesForCurrentThread();report.heapBefore=GC.GetTotalMemory(false);report.profilerMonoBefore=Profiler.GetMonoUsedSizeLong();report.gcBefore=GC.GetAllocatedBytesForCurrentThread();
   var probe=new byte[1048576];probe[0]=123;report.gcAfter=GC.GetAllocatedBytesForCurrentThread();report.gcDelta=report.gcAfter-report.gcBefore;report.heapAfter=GC.GetTotalMemory(false);report.profilerMonoAfter=Profiler.GetMonoUsedSizeLong();report.observableProbeBytes=probe.Length;report.observableProbeFirstByte=probe[0];GC.KeepAlive(probe);report.threadAllocationCounterCalibrated=report.gcDelta>=probe.Length;
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);if(!prefab)throw new Exception("Actual prefab absent");actor=UnityEngine.Object.Instantiate(prefab);actor.SetActive(false);
   var owner=actor.GetComponent<DenseRigifySupportOwner>();var warm=owner.Warmup();try{while(warm.MoveNext())if(warm.Current!=null)throw new Exception("Unexpected warmup yield");}finally{(warm as IDisposable)?.Dispose();}
   if(owner.PointCount!=143168)throw new Exception("Actual143168 point set required");foreach(var b in actor.GetComponentsInChildren<Behaviour>(true))b.enabled=false;
   var animator=actor.GetComponentInChildren<Animator>(true);var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DenseRigifyReview/PlayableReview/SourceGenericBOX.anim");clip.SampleAnimation(animator.gameObject,0);
   var g=owner.grounding;var points=g.shoePoints;var bones=g.bones;report.pointCount=points.Length;report.boneCount=bones.Length;report.rigIsOwnerOffset=g.nativeRig==owner.groundOffset;report.nativeLocalBefore=g.nativeRig.localPosition;report.groundOffsetLocalBefore=owner.groundOffset.localPosition;
   g.ResetForSample();report.nativeLocalAfter=g.nativeRig.localPosition;report.nativeWorldAfter=g.nativeRig.position;report.groundOffsetLocalAfter=owner.groundOffset.localPosition;
   var rows=new float[bones.Length*4];report.boneNames=bones.Select(b=>b.name).ToArray();for(int b=0;b<bones.Length;b++){var m=bones[b].localToWorldMatrix;int n=b*4;rows[n]=m.m10;rows[n+1]=m.m11;rows[n+2]=m.m12;rows[n+3]=m.m13;}
   report.rowFloatBits=rows.Select(f=>new Scalar(f).bits).ToArray();var indices=new int[points.Length*4];var weights=new float[indices.Length];var xyz=new float[points.Length*12];
   for(int p=0;p<points.Length;p++){var influences=points[p].influences;for(int k=0;k<influences.Length;k++){var i=influences[k];int q=p*4+k;indices[q]=i.bone;weights[q]=i.weight;xyz[q*3]=i.bindPoint.x;xyz[q*3+1]=i.bindPoint.y;xyz[q*3+2]=i.bindPoint.z;}}
   string fixture=Path.Combine(output,"actual-pose0.fsf");using(var writer=new BinaryWriter(File.Create(fixture))){writer.Write(0x31465346u);writer.Write(points.Length);writer.Write(bones.Length);writer.Write(1);foreach(int b in indices)writer.Write(b);foreach(float f in weights)writer.Write(f);foreach(float f in xyz)writer.Write(f);foreach(float f in rows)writer.Write(f);}
   string ids=Path.Combine(output,"actual-original-imported.ids");using(var writer=new BinaryWriter(File.Create(ids))){writer.Write(points.Length);foreach(int n in owner.OriginalFootIndices)writer.Write(n);foreach(int n in owner.ImportedFootIndices)writer.Write(n);}
   report.fixtureSHA256=Hash(fixture);report.idsSHA256=Hash(ids);
   var tree=new ExactFootFeatureBounds(bones.Length,indices,weights,xyz);report.nodeCount=tree.NodeCount;ExactFootFeatureBounds.Statistics stats;float accelerated=tree.Minimum(rows,out stats),full=DenseFootFeatureAdapter.FullFloatMinimum(points,rows);
   var leaf=(Func<int,float[],float>)Delegate.CreateDelegate(typeof(Func<int,float[],float>),tree,typeof(ExactFootFeatureBounds).GetMethod("Exact",BindingFlags.NonPublic|BindingFlags.Instance));
   report.originalExpression=new Scalar(DenseFootFeatureAdapter.OriginalExpressionMinimum(points,rows));report.independentDouble=DenseFootFeatureAdapter.IndependentDoubleMinimum(points,rows).ToString("R",System.Globalization.CultureInfo.InvariantCulture);
   float bestCore=float.PositiveInfinity,bestTyped=float.PositiveInfinity;int argCore=-1,argTyped=-1;var single=new CandidateFootGrounding.Point[1];
   for(int p=0;p<points.Length;p++){float a=leaf(p,rows);if(Bits(a)!=Bits(StepOracle(points[p],rows)))report.independentStepMismatches++;single[0]=points[p];float b=DenseFootFeatureAdapter.FullFloatMinimum(single,rows);if(a<bestCore){bestCore=a;argCore=p;}if(b<bestTyped){bestTyped=b;argTyped=p;}}
   report.accelerated=new Scalar(accelerated);report.fullTyped=new Scalar(full);report.coreFullLeafScan=new Scalar(bestCore);report.typedSinglePointScan=new Scalar(bestTyped);
   report.acceleratedWitness=Point(owner,argCore,leaf,rows);report.typedWitness=Point(owner,argTyped,leaf,rows);
   g.InvalidateMinimumCache();report.candidate=new Scalar(g.MeasureMinimum());for(int b=0;b<bones.Length;b++){var m=bones[b].localToWorldMatrix;int n=b*4;if(rows[n]!=m.m10||rows[n+1]!=m.m11||rows[n+2]!=m.m12||rows[n+3]!=m.m13)report.rowsChangedAfterCandidateReset++;}
   report.candidateParity=Bits(accelerated)==report.candidate.bits;report.status=accelerated==full&&report.independentStepMismatches==0&&report.candidateParity?"DIAGNOSTIC scalar parity; not performance acceptance":"RED integration parity; diagnostic only";
  }finally{if(actor)UnityEngine.Object.DestroyImmediate(actor);File.WriteAllText(Path.Combine(output,"witness.json"),JsonUtility.ToJson(report,true));}
  UnityEngine.Debug.Log("AGENT_RESULT DenseFootDiagnosticExport "+report.status+" accelerated="+report.accelerated.value+" "+report.accelerated.bits+" full="+report.fullTyped.value+" "+report.fullTyped.bits+" calibratedGC="+report.threadAllocationCounterCalibrated);
 }
 static Witness Point(DenseRigifySupportOwner owner,int n,Func<int,float[],float> leaf,float[] rows){
  var p=owner.grounding.shoePoints[n];var single=new[]{p};return new Witness{point=n,originalVertex=owner.OriginalFootIndices[n],importedVertex=owner.ImportedFootIndices[n],coreLeaf=new Scalar(leaf(n,rows)),typedPoint=new Scalar(DenseFootFeatureAdapter.FullFloatMinimum(single,rows)),influences=p.influences.Select(i=>new Influence{bone=i.bone,boneName=owner.bones[i.bone].name,weight=new Scalar(i.weight),bindXYZ=new[]{new Scalar(i.bindPoint.x),new Scalar(i.bindPoint.y),new Scalar(i.bindPoint.z)},verticalRow=new[]{new Scalar(rows[i.bone*4]),new Scalar(rows[i.bone*4+1]),new Scalar(rows[i.bone*4+2]),new Scalar(rows[i.bone*4+3])}}).ToArray()};
 }
 static string Bits(float f)=>new Scalar(f).bits;
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static float Round(double v)=>BitConverter.ToSingle(BitConverter.GetBytes((float)v),0);
 // Independent and deliberately allocating diagnostic oracle, NEVER timed.
 static float StepOracle(CandidateFootGrounding.Point point,float[] rows){float sum=0;foreach(var i in point.influences){int b=i.bone*4;var p=i.bindPoint;float x=Round((double)rows[b]*p.x),y=Round((double)rows[b+1]*p.y),z=Round((double)rows[b+2]*p.z);float dot=Round((double)Round((double)Round((double)x+y)+z)+rows[b+3]);sum=Round((double)sum+Round((double)dot*i.weight));}return sum;}
 static string Hash(string path){using(var sha=SHA256.Create())using(var f=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
}
#endif
