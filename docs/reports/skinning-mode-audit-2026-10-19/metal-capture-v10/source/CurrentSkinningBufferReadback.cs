using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
// UNRELEASED diagnostic only. No caller in gameplay or existing measured fixture.
// Call only after an actual automatic-render EOF while the entire pose is held.
public static class CurrentSkinningBufferReadback {
 [Serializable]public sealed class Capture {
  public int requestFrame,completeFrame,bufferCount,bufferStride,rawBytes,vertices;public string bufferTargets;public bool skinnedMotionVectors;public string accessor="GetVertexBuffer (current frame), reacquired each request";
  public string[] inputAttributes;public int positionStream,positionOffset,inputStride;public string positionFormat,status;
  public Matrix4x4 rendererLocalToWorld,rendererTransformLocalToWorld,rootBoneLocalToWorld,rootBoneRigidToWorld;[NonSerialized]public byte[] raw;
 }
 public static IEnumerator Read(SkinnedMeshRenderer skin,string path,Action<Capture> complete){
  if(!skin||!skin.sharedMesh||!SystemInfo.supportsAsyncGPUReadback)throw new InvalidOperationException("Actual SMR/async GPU readback capability required");
  var mesh=skin.sharedMesh;var capture=new Capture{requestFrame=Time.frameCount,vertices=mesh.vertexCount,rendererLocalToWorld=skin.localToWorldMatrix,rendererTransformLocalToWorld=skin.transform.localToWorldMatrix,rootBoneLocalToWorld=skin.rootBone?skin.rootBone.localToWorldMatrix:skin.transform.localToWorldMatrix,rootBoneRigidToWorld=Matrix4x4.TRS(skin.rootBone?skin.rootBone.position:skin.transform.position,skin.rootBone?skin.rootBone.rotation:skin.transform.rotation,Vector3.one)};
  // Input descriptor is provenance, NOT a claimed post-skin output layout.
  capture.positionStream=mesh.GetVertexAttributeStream(VertexAttribute.Position);capture.positionOffset=mesh.GetVertexAttributeOffset(VertexAttribute.Position);capture.positionFormat=mesh.GetVertexAttributeFormat(VertexAttribute.Position).ToString();capture.inputStride=mesh.GetVertexBufferStride(capture.positionStream);
  var attributes=mesh.GetVertexAttributes();capture.inputAttributes=new string[attributes.Length];for(int i=0;i<attributes.Length;i++){var d=attributes[i];capture.inputAttributes[i]=d.attribute+" format="+d.format+" dimension="+d.dimension+" stream="+d.stream+" offset="+mesh.GetVertexAttributeOffset(d.attribute)+" stride="+mesh.GetVertexBufferStride(d.stream); }
  GraphicsBuffer buffer=null;
  try{
   buffer=skin.GetVertexBuffer();if(buffer==null)throw new InvalidOperationException("No actual current-frame skinned GPU buffer");
   capture.skinnedMotionVectors=skin.skinnedMotionVectors;capture.bufferCount=buffer.count;capture.bufferStride=buffer.stride;capture.bufferTargets=buffer.target.ToString();
   var request=AsyncGPUReadback.Request(buffer);double start=Time.realtimeSinceStartupAsDouble;
   while(!request.done){if(Time.realtimeSinceStartupAsDouble-start>10)throw new TimeoutException("GPU buffer request did not complete within10s");yield return null;}
   if(request.hasError)throw new InvalidOperationException("GPU buffer readback error; no CPU/Bake substitute");
   var data=request.GetData<byte>();capture.raw=data.ToArray();capture.rawBytes=capture.raw.Length;capture.completeFrame=Time.frameCount;
   if(capture.rawBytes!=checked(capture.bufferCount*capture.bufferStride))throw new InvalidOperationException("Raw buffer size/count/stride contract differs");
   capture.status="RAW_CAPTURE_ONLY; layout and dispatch UNVALIDATED";
   Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path+".raw",capture.raw);File.WriteAllText(path+".json",JsonUtility.ToJson(capture,true));complete(capture);
  }finally{if(buffer!=null)buffer.Dispose();}
 }
 [Serializable]public struct Layout{public int stride,offset;public override string ToString()=>stride+":"+offset;}
 // Calibration-only enumeration against ALL asymmetric known vertices; there is no
 // guessed stride40/offset0 fallback. Caller intersects matches from2distinct poses
 // AND validates held-out pose, normal/tangent layout and actual mesh descriptors.
 public static List<Layout> PositionCandidates(byte[] raw,Vector3[] knownLocal,float tolerance){
  if(raw==null||knownLocal==null||knownLocal.Length<4||tolerance<=0)throw new ArgumentException("Nontrivial calibration fixture required");
  var matches=new List<Layout>();
  for(int stride=12;stride<=128;stride+=4)for(int offset=0;offset+12<=stride;offset+=4){
   if((long)(knownLocal.Length-1)*stride+offset+12>raw.Length)continue;bool valid=true;
   for(int i=0;i<knownLocal.Length;i++){int n=i*stride+offset;var p=new Vector3(BitConverter.ToSingle(raw,n),BitConverter.ToSingle(raw,n+4),BitConverter.ToSingle(raw,n+8));if(!Finite(p)||Vector3.Distance(p,knownLocal[i])>tolerance){valid=false;break;}}
   if(valid)matches.Add(new Layout{stride=stride,offset=offset});
  }
  return matches;
 }
 // Independent original unlimited-influence position oracle. No BakeMesh and no
 // guessed output coordinate contract. Caller compares world and calibrated local.
 public static Vector3[] OriginalWorldPositions(SkinnedMeshRenderer skin){
  var mesh=skin.sharedMesh;var vertices=mesh.vertices;var bind=mesh.bindposes;var bones=skin.bones;var matrices=new Matrix4x4[bones.Length];
  for(int i=0;i<bones.Length;i++){if(!bones[i]||i>=bind.Length)throw new InvalidOperationException("Original bone/bind mismatch");matrices[i]=bones[i].localToWorldMatrix*bind[i];}
  var counts=mesh.GetBonesPerVertex();var weights=mesh.GetAllBoneWeights();
  // Mesh-owned Allocator.None views: never dispose or retain after mesh mutation.
  {var world=new Vector3[vertices.Length];int cursor=0;
   for(int i=0;i<vertices.Length;i++){Vector3 value=Vector3.zero;for(int k=0;k<counts[i];k++){var influence=weights[cursor++];if(influence.boneIndex<0||influence.boneIndex>=matrices.Length)throw new InvalidOperationException("Bone index");value+=matrices[influence.boneIndex].MultiplyPoint3x4(vertices[i])*influence.weight;}if(!Finite(value))throw new InvalidOperationException("Nonfinite original position");world[i]=value;}
   if(cursor!=weights.Length)throw new InvalidOperationException("Unlimited influence count mismatch");return world;
  }
 }
 public static void WriteMatrixState(SkinnedMeshRenderer skin,string path){
  using(var writer=new BinaryWriter(File.Create(path))){writer.Write(Time.frameCount);writer.Write(skin.bones.Length);Write(writer,skin.localToWorldMatrix);Write(writer,skin.worldToLocalMatrix.transpose);
   foreach(var bone in skin.bones){Write(writer,bone.localToWorldMatrix);Write(writer,bone.worldToLocalMatrix.transpose);}
  }
 }
 static void Write(BinaryWriter w,Matrix4x4 m){for(int i=0;i<16;i++)w.Write(m[i]);}
 static bool Finite(Vector3 v)=>!(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));
}
