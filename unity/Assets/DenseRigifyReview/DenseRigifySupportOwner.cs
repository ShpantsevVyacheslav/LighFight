using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using LighFight.Runtime;
using UnityEngine;

// Review-only full original weighted-shoe set. No cold decode in Awake/OnEnable.
// Prototype's existing typed loading owner discovers this via IActorCurveWarmup.
public sealed class DenseRigifySupportOwner : MonoBehaviour,IActorCurveWarmup
{
 public TextAsset supportBytes;
 public CandidateFootGrounding grounding;
 public Transform groundOffset;
 public Transform[] bones;
 public float upwardCap=.18f;
 public int sourceVertices,expectedOriginalFootPoints;
 CandidateFootGrounding.Point[] ownedPoints,priorPoints;
 Transform priorRig;Transform[] priorBones;Vector3 priorRest;float priorCap;
 int[] importedIndices,originalIndices;
 int generation;bool disposed;
 public bool IsWarm=>ownedPoints!=null&&!disposed;
 public int PointCount=>ownedPoints==null?0:ownedPoints.Length;
 public int[] ImportedFootIndices=>importedIndices;
 public int[] OriginalFootIndices=>originalIndices;
 public static int ResidentSupportPoints{get;private set;}
 public IEnumerator Warmup()
 {
  if(disposed)throw new ObjectDisposedException(nameof(DenseRigifySupportOwner));
  if(IsWarm)yield break;
  if(!supportBytes||!grounding||!groundOffset||bones==null)throw new InvalidOperationException("Incomplete dense source support contract");
  int ticket=++generation;
  using(var source=new MemoryStream(supportBytes.bytes,false))
  using(var z=new DeflateStream(source,CompressionMode.Decompress))
  using(var reader=new BinaryReader(z))
  {
   if(reader.ReadUInt32()!=0x31465344)throw new InvalidDataException("Dense fullshoe DSF1 magic");
   int fullVertices=reader.ReadInt32(),rendererVertices=reader.ReadInt32(),boneCount=reader.ReadInt32(),count=reader.ReadInt32();
   if(fullVertices!=sourceVertices||boneCount!=bones.Length||count!=expectedOriginalFootPoints||rendererVertices<fullVertices)throw new InvalidDataException("Dense fullshoe source/count mismatch");
   var points=new CandidateFootGrounding.Point[count];var original=new int[count];var imported=new int[count];
   for(int p=0;p<count;p++)
   {
    if(disposed||ticket!=generation)yield break;
    original[p]=reader.ReadInt32();imported[p]=reader.ReadInt32();int n=reader.ReadByte();
    if(original[p]<0||original[p]>=fullVertices||imported[p]<0||imported[p]>=rendererVertices||n<1||n>4)throw new InvalidDataException("Dense fullshoe index/influence count");
    var influences=new CandidateFootGrounding.Influence[n];
    for(int i=0;i<n;i++){int bone=reader.ReadUInt16();float weight=reader.ReadSingle();var point=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());if(bone>=bones.Length||weight<=0||float.IsNaN(weight)||float.IsInfinity(weight))throw new InvalidDataException("Dense fullshoe named influence");influences[i]=new CandidateFootGrounding.Influence{bone=bone,weight=weight,bindPoint=point};}
    points[p]=new CandidateFootGrounding.Point{influences=influences};
    if((p+1)%4096==0)yield return null;
   }
   if(reader.Read()!=-1)throw new InvalidDataException("Dense fullshoe trailing bytes");
   if(disposed||ticket!=generation)yield break;
   priorPoints=grounding.shoePoints;priorRig=grounding.nativeRig;priorBones=grounding.bones;priorRest=grounding.nativeRest;priorCap=grounding.maxCorrection;
   ownedPoints=points;originalIndices=original;importedIndices=imported;ResidentSupportPoints+=count;
   grounding.nativeRig=groundOffset;grounding.nativeRest=groundOffset.localPosition;grounding.bones=bones;grounding.shoePoints=ownedPoints;grounding.maxCorrection=upwardCap;grounding.InvalidateMinimumCache();
  }
 }
 public void CancelWarmup(){generation++;}
 public void Dispose()
 {
  if(disposed)return;disposed=true;CancelWarmup();
  if(ownedPoints!=null){if(grounding&&ReferenceEquals(grounding.shoePoints,ownedPoints)){grounding.ResetForSample();grounding.shoePoints=priorPoints;grounding.nativeRig=priorRig;grounding.nativeRest=priorRest;grounding.bones=priorBones;grounding.maxCorrection=priorCap;grounding.InvalidateMinimumCache();}ResidentSupportPoints-=ownedPoints.Length;}
  ownedPoints=null;originalIndices=null;importedIndices=null;priorPoints=null;priorBones=null;
 }
 void OnDestroy(){Dispose();}
 // Disable deliberately retains warm support until actor-owner destruction/disposal.
}
