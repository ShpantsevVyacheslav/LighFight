using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DenseRigifyReview.Adaptive;
using LighFight.Runtime;
using UnityEngine;

// Explicit review evaluator. It has no Update/LateUpdate and never takes combat authority.
// The owning preview must warm before Apply, and dispose before destroying its actor.
public sealed class DenseAdaptivePoseBridge : MonoBehaviour, IActorCurveWarmup
{
 public TextAsset sourceKeys;
 public SkinnedMeshRenderer target;
 public Transform actorAnchor;
 AdaptiveDanceData data;
 AdaptiveDanceEvaluator evaluator;
 Transform[] nodes;
 Matrix4x4[] fixedBindGauge;
 int generation;
 bool disposed;
 public bool IsWarm => evaluator != null && !disposed;
 public double AuthoredDuration => IsWarm ? data.Duration : 0;
 public static int ResidentKeyPackets {get; private set;}
 public float LastLocalShearResidual {get; private set;}
 public double LastDecodeAndBindMilliseconds {get; private set;}
 static Matrix4x4 Basis
 {
  get { var m=Matrix4x4.zero;m.m00=-1;m.m12=1;m.m21=-1;m.m33=1;return m; }
 }
 public IEnumerator Warmup()
 {
  if(disposed)throw new ObjectDisposedException(nameof(DenseAdaptivePoseBridge));
  if(IsWarm)yield break;
  if(!sourceKeys||!target||!actorAnchor)throw new InvalidOperationException("Source keys, target mesh, explicit actor anchor required");
  int ticket=++generation;
  // Yield before loading; Prototype's opaque loading UI owns this warmup boundary.
  yield return null;
  if(disposed||ticket!=generation)yield break;
  long decodeStarted=System.Diagnostics.Stopwatch.GetTimestamp();
  AdaptiveDanceData loaded;
  using(var stream=new MemoryStream(sourceKeys.bytes,false))loaded=AdaptiveDanceData.Read(stream);
  var evaluated=new AdaptiveDanceEvaluator(loaded);
  var targetBones=target.bones;var binds=target.sharedMesh.bindposes;
  if(targetBones.Length!=binds.Length)throw new InvalidDataException("Target bind correspondence");
  var byName=new Dictionary<string,int>(targetBones.Length,StringComparer.Ordinal);
  for(int i=0;i<targetBones.Length;i++)if(!byName.TryAdd(targetBones[i].name,i))throw new InvalidDataException("Ambiguous target bone name");
  var mapped=new Transform[loaded.BoneCount];var gauges=new Matrix4x4[mapped.Length];
  var c=Basis;var inverseC=c.inverse;var sourceBind=new double[16];var anchorInverse=actorAnchor.worldToLocalMatrix;
  for(int i=0;i<mapped.Length;i++)
  {
   if(disposed||ticket!=generation)yield break;
   if(!byName.TryGetValue(loaded.BoneName(i),out int index))throw new InvalidDataException("Missing named source bone: "+loaded.BoneName(i));
   mapped[i]=targetBones[index];
   if(mapped[i]==actorAnchor||!mapped[i].IsChildOf(actorAnchor))throw new InvalidDataException("Bridge must not write actor world root");
   MathD.Multiply(loaded.armature,0,loaded.rest,i*16,sourceBind,0);
   var expected=c*Matrix(sourceBind,0)*inverseC;
   var actual=anchorInverse*target.localToWorldMatrix*binds[index].inverse;
   gauges[i]=expected.inverse*actual; // Fixed once, bind-only; never fit to a moving pose.
  }
  if(disposed||ticket!=generation)yield break;
  data=loaded;evaluator=evaluated;nodes=mapped;fixedBindGauge=gauges;ResidentKeyPackets++;LastDecodeAndBindMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-decodeStarted)*1000.0/System.Diagnostics.Stopwatch.Frequency;
 }
 public void Apply(double seconds)
 {
  if(!IsWarm)throw new InvalidOperationException("Cold bridge cannot decode or claim a pose");
  evaluator.Evaluate(seconds);var c=Basis;var inverseC=c.inverse;var anchor=actorAnchor.localToWorldMatrix;LastLocalShearResidual=0;
  for(int i=0;i<nodes.Length;i++)
  {
   var desired=anchor*c*Matrix(evaluator.WorldPose,i*16)*inverseC*fixedBindGauge[i];
   var local=nodes[i].parent.worldToLocalMatrix*desired;
   Vector3 p=local.GetColumn(3),x=local.GetColumn(0),y=local.GetColumn(1),z=local.GetColumn(2);
   var scale=new Vector3(x.magnitude,y.magnitude,z.magnitude);
   if(local.determinant<0)scale.x=-scale.x;
   if(Mathf.Abs(scale.x)<1e-9f||scale.y<1e-9f||scale.z<1e-9f)throw new InvalidOperationException("Singular local transform");
   var rotation=Quaternion.LookRotation(z/scale.z,y/scale.y);
   var reconstructed=Matrix4x4.TRS(p,rotation,scale);
   float residual=0;for(int r=0;r<3;r++)for(int col=0;col<3;col++)residual=Mathf.Max(residual,Mathf.Abs(local[r,col]-reconstructed[r,col]));
   LastLocalShearResidual=Mathf.Max(LastLocalShearResidual,residual);
   // The independent surface gate must qualify this float TRS transport; no automatic tolerance waiver.
   nodes[i].SetLocalPositionAndRotation(p,rotation);nodes[i].localScale=scale;
  }
 }
 static Matrix4x4 Matrix(double[] a,int offset){var m=new Matrix4x4();for(int r=0;r<4;r++)for(int c=0;c<4;c++)m[r,c]=(float)a[offset+r*4+c];return m;}
 public void CancelWarmup(){generation++;}
 public void Dispose(){if(disposed)return;disposed=true;CancelWarmup();if(evaluator!=null)ResidentKeyPackets--;evaluator=null;data=null;nodes=null;fixedBindGauge=null;}
 void OnDestroy(){Dispose();}
 // Disable keeps the owner's resident packet; it performs no sampling and no transform writes.
}
