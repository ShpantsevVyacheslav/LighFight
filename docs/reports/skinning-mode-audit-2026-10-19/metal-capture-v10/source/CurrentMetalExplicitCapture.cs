using System;using System.IO;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Apple;
// V10 review-only explicit Begin/End capture. No timing/native plugin/main changes.
public static class CurrentMetalExplicitCapture {
 [Serializable]sealed class Record{public string status="INCOMPLETE; inspect native trace commands",sourceSHA,layoutSHA,tracePath,beginUTC,endUTC,beforePoseSHA,afterPoseSHA;public bool supported,began,ended,allReadbacksCompleted;public int beginFrame,endFrame,beginDraws,endDraws,changedPoses;public double beginUptime,endUptime,splashFinishedUptime;public Read[] reads;}
 [Serializable]sealed class Read{public int pose,actor,sourceFrame,sourceDraws,completeFrame,completeDraws,count,stride;public string sourcePoseSHA,targets,path;public bool error;[NonSerialized]public GraphicsBuffer buffer;[NonSerialized]public byte[] raw;}
 public static IEnumerator Run(string path,string sourceSHA,string layoutSHA,double splashFinishedUptime,GameObject[] actors,CurrentSkinningRenderCounter counter,Func<string> actualPoseSHA,Action advancePose){
  if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Metal||!SystemInfo.supportsAsyncGPUReadback)throw new Exception("Metal/actual readback required");
  if(!SplashScreen.isFinished||splashFinishedUptime<=0||Time.realtimeSinceStartupAsDouble-splashFinishedUptime<2)throw new Exception("Observed splash+real settle fence required");
  path=Path.GetFullPath(path);if(!path.EndsWith(".gputrace",StringComparison.OrdinalIgnoreCase)||File.Exists(path)||Directory.Exists(path))throw new Exception("Unique trace path required");Directory.CreateDirectory(Path.GetDirectoryName(path));
  var r=new Record{sourceSHA=sourceSHA,layoutSHA=layoutSHA,tracePath=path,splashFinishedUptime=splashFinishedUptime,supported=FrameCapture.IsDestinationSupported(FrameCaptureDestination.GPUTraceDocument),beginFrame=Time.frameCount,beginDraws=counter.renders,beginUptime=Time.realtimeSinceStartupAsDouble,beginUTC=DateTime.UtcNow.ToString("O"),beforePoseSHA=actualPoseSHA()};var reads=new List<Read>();string evidence=path+"-explicit-request.json";
  File.WriteAllText(evidence,JsonUtility.ToJson(r,true));if(!r.supported)throw new Exception("GPUTraceDocument unsupported; capture incomplete, not GPU skinning verdict");
  try{
   FrameCapture.BeginCaptureToFile(path);r.began=true;string previous=r.beforePoseSHA;
   for(int pose=0;pose<3;pose++){
    int draws=counter.renders;yield return null;advancePose();yield return new WaitForEndOfFrame();
    if(counter.renders-draws!=1)throw new Exception("Dynamic pose EOF requires exactly1 automatic draw");if(Time.frameCount-r.beginFrame>6)throw new Exception("Capture exceeds6frame budget");
    string fingerprint=actualPoseSHA();if(fingerprint==previous)throw new Exception("Actual dynamic pose unchanged");previous=fingerprint;r.changedPoses++;
    CurrentGreetingState.Write(actors,path+"-pose"+pose+"-state.json","capturePoseEOF",counter,sourceSHA);
    for(int actor=0;actor<actors.Length;actor++){
     var skins=actors[actor].GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length!=1)throw new Exception("Exactly one audited renderer per actor required");
     var read=new Read{pose=pose,actor=actor,sourceFrame=Time.frameCount,sourceDraws=counter.renders,sourcePoseSHA=fingerprint,path=path+"-pose"+pose+"-a"+actor};reads.Add(read);read.buffer=skins[0].GetVertexBuffer();if(read.buffer==null)throw new Exception("Actual current buffer absent");read.count=read.buffer.count;read.stride=read.buffer.stride;read.targets=read.buffer.target.ToString();
     AsyncGPUReadback.Request(read.buffer,q=>{read.error=q.hasError;if(!read.error){try{read.raw=q.GetData<byte>().ToArray();}catch{read.error=true;}}read.completeFrame=Time.frameCount;read.completeDraws=counter.renders;});
    }
   }
   bool pending=true;while(pending){pending=false;foreach(var read in reads){if(read.error)throw new Exception("Actual GPU readback error before EndCapture");if(read.raw==null)pending=true;}
    if(!pending)break;if(Time.frameCount-r.beginFrame>=5)throw new Exception("GPU completion cannot fit6frame capture budget");yield return null;
   }
   int beforeEOF=counter.renders;yield return new WaitForEndOfFrame();if(counter.renders-beforeEOF!=1)throw new Exception("Completion EOF requires exactly1 automatic draw");if(Time.frameCount-r.beginFrame>6)throw new Exception("Capture exceeds6frame budget");
   r.allReadbacksCompleted=true;r.afterPoseSHA=actualPoseSHA();r.status="FENCED REQUEST ONLY; native commands/dispatch UNACCEPTED";
  }finally{
   if(r.began){FrameCapture.EndCapture();r.ended=true;}r.endFrame=Time.frameCount;r.endDraws=counter.renders;r.endUptime=Time.realtimeSinceStartupAsDouble;r.endUTC=DateTime.UtcNow.ToString("O");r.reads=reads.ToArray();
   foreach(var read in reads){if(read.raw!=null){File.WriteAllBytes(read.path+".raw",read.raw);File.WriteAllText(read.path+".json",JsonUtility.ToJson(read,true));}if(read.buffer!=null)read.buffer.Dispose();}
   File.WriteAllText(evidence,JsonUtility.ToJson(r,true));
  }
 }
}
