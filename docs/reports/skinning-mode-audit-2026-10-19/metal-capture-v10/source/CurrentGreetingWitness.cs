using System;using System.IO;using System.Collections;using System.Security.Cryptography;using UnityEngine;using UnityEngine.Rendering;
// Explicit V8 Greeting-only diagnostic. No mutation of pose, streaming, materials or quality.
public static class CurrentGreetingWitness {
 [Serializable]sealed class Header{public string stage,utc,targets,sourceIdentitySHA,status="OBSERVATIONAL; no dispatch/timing/PNT tolerance acceptance";public int actor,requestFrame,requestDraws,completeFrame,completeDraws,count,stride,vertices;public double requestUptime,completeUptime;public Matrix4x4 rendererWorld,rootWorld,rootRigid;}
 sealed class Pending{public GraphicsBuffer buffer;public AsyncGPUReadbackRequest request;public Header header;public string path;public byte[] raw;public bool error;}
 static Pending[] Begin(GameObject[] actors,string prefix,string stage,CurrentSkinningRenderCounter counter,string sha){
  var result=new Pending[actors.Length];try{for(int i=0;i<actors.Length;i++){var skins=actors[i].GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length!=1)throw new Exception("Greeting witness requires exactly one audited renderer per actor");var s=skins[0];var p=new Pending{path=prefix+"-"+stage+"-a"+i};result[i]=p;p.buffer=s.GetVertexBuffer();if(p.buffer==null)throw new Exception("Actual current buffer unavailable");p.header=new Header{stage=stage,actor=i,sourceIdentitySHA=sha,utc=DateTime.UtcNow.ToString("O"),requestUptime=Time.realtimeSinceStartupAsDouble,requestFrame=Time.frameCount,requestDraws=counter.renders,count=p.buffer.count,stride=p.buffer.stride,targets=p.buffer.target.ToString(),vertices=s.sharedMesh.vertexCount,rendererWorld=s.localToWorldMatrix,rootWorld=s.rootBone?s.rootBone.localToWorldMatrix:Matrix4x4.identity,rootRigid=Matrix4x4.TRS(s.rootBone?s.rootBone.position:s.transform.position,s.rootBone?s.rootBone.rotation:s.transform.rotation,Vector3.one)};p.request=AsyncGPUReadback.Request(p.buffer,request=>{p.error=request.hasError;if(!p.error){try{p.raw=request.GetData<byte>().ToArray();}catch{p.error=true;}}p.header.completeFrame=Time.frameCount;p.header.completeDraws=counter.renders;p.header.completeUptime=Time.realtimeSinceStartupAsDouble;});}
  return result;}catch{Dispose(result);throw;}
 }
 static void Dispose(Pending[] ps){if(ps!=null)foreach(var p in ps)if(p!=null&&p.buffer!=null){p.buffer.Dispose();p.buffer=null;}}
 static IEnumerator Complete(Pending[] ps,CurrentSkinningRenderCounter counter,CurrentBufferCalibration.Layout layout){
  double start=Time.realtimeSinceStartupAsDouble;foreach(var p in ps){while(p.raw==null&&!p.error){if(Time.realtimeSinceStartupAsDouble-start>10)throw new TimeoutException("Greeting simultaneous readback deadline10s");yield return null;}if(p.error)throw new Exception("Greeting actual readback error");byte[] raw=p.raw;if(raw.Length!=checked(p.header.count*p.header.stride))throw new Exception("Greeting raw size contract");File.WriteAllBytes(p.path+".raw",raw);File.WriteAllText(p.path+".json",JsonUtility.ToJson(p.header,true));if((long)(p.header.vertices-1)*layout.stride+Math.Max(layout.positionOffset,Math.Max(layout.normalOffset,layout.tangentOffset))+12>raw.Length)throw new Exception("Greeting calibrated layout does not fit");using(var w=new BinaryWriter(File.Create(p.path+"-pnt.bin"))){w.Write(p.header.vertices);for(int i=0;i<p.header.vertices;i++)foreach(int offset in new[]{layout.positionOffset,layout.normalOffset,layout.tangentOffset})for(int k=0;k<3;k++){float v=BitConverter.ToSingle(raw,i*layout.stride+offset+k*4);if(float.IsNaN(v)||float.IsInfinity(v))throw new Exception("Nonfinite Greeting output");w.Write(v);}}}
 }
 public static IEnumerator Capture(GameObject[] actors,CurrentBufferCalibration.Layout layout,CurrentSkinningRenderCounter counter,string imagePath){
  var identity=Resources.Load<TextAsset>("CurrentGreetingSourceIdentity");if(!identity)throw new Exception("Missing audited Greeting source identity");string sha;using(var h=SHA256.Create())sha=BitConverter.ToString(h.ComputeHash(identity.bytes)).Replace("-","").ToLowerInvariant();File.WriteAllText(Path.Combine(Path.GetDirectoryName(imagePath),"greeting-source-identity.json"),identity.text);string prefix=imagePath+"-witness";Pending[] before=null,after=null;
  try{
   CurrentGreetingState.Write(actors,prefix+"-before-state.json","screenshotEOF-before",counter,sha);
   before=Begin(actors,prefix,"before",counter,sha);int frame=Time.frameCount,draws=counter.renders;
   var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(imagePath,image.EncodeToPNG());UnityEngine.Object.Destroy(image);
   if(Time.frameCount!=frame||counter.renders!=draws)throw new Exception("Screenshot and both requests crossed automatic frame");
   CurrentGreetingState.Write(actors,prefix+"-png-state.json","screenshotEOF-after",counter,sha);
   var complete=Complete(before,counter,layout);while(complete.MoveNext())yield return complete.Current;
   CurrentGreetingState.Write(actors,prefix+"-before-completed-state.json","beforeAsyncCompleted",counter,sha);
   draws=counter.renders;yield return new WaitForEndOfFrame();if(counter.renders-draws!=1)throw new Exception("Greeting afterAsync EOF requires exactly1 automatic draw");
   CurrentGreetingState.Write(actors,prefix+"-after-state.json","afterExplicitEOF",counter,sha);after=Begin(actors,prefix,"after",counter,sha);
   complete=Complete(after,counter,layout);while(complete.MoveNext())yield return complete.Current;
   CurrentGreetingState.Write(actors,prefix+"-after-completed-state.json","afterAsyncCompleted",counter,sha);
   draws=counter.renders;yield return new WaitForEndOfFrame();if(counter.renders-draws!=1)throw new Exception("Greeting final afterAsync EOF requires exactly1 automatic draw");
   CurrentGreetingState.Write(actors,prefix+"-final-state.json","finalExplicitEOF",counter,sha);
  }finally{Dispose(before);Dispose(after);}
 }
}
