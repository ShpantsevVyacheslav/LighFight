using System;using System.IO;using System.Collections;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Apple;
// Optional V9 review helper. NOT part of frozen V8 or production. No timing permit.
public static class CurrentMetalOneFrameCapture {
 [Serializable]sealed class Record{public string status="REQUESTED ONLY; inspect actual compute commands in Xcode",tracePath,sourceSHA,layoutSHA,requestUTC,finishedUTC,beforePoseSHA,afterPoseSHA;public bool supported,splashFinished,dynamicPoseChanged,pathObserved;public int requestFrame,finishedFrame,requestDraws,finishedDraws;public double requestUptime,finishedUptime,observedSplashFinishedUptime;}
 public static bool RequestedPath(out string path){var args=Environment.GetCommandLineArgs();path=null;for(int i=0;i<args.Length;i++)if(args[i]=="-lfMetalTracePath"){if(path!=null||i+1>=args.Length)throw new ArgumentException("Exactly one trace path required");path=Path.GetFullPath(args[++i]);}return path!=null;}
 // Called at automatic EOF only, after V8 correctness reviewed, with a real dynamic
 // pose driver/fingerprint supplied by the NEW V9 harness. Never call from V8.
 public static IEnumerator Run(string path,string sourceSHA,string layoutSHA,double observedSplashFinishedUptime,Func<int> automaticDraws,Func<string> actualPoseSHA,Action advanceActualPose){
  if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Metal)throw new Exception("Metal only");
  if(!SplashScreen.isFinished||observedSplashFinishedUptime<=0||Time.realtimeSinceStartupAsDouble-observedSplashFinishedUptime<2)throw new Exception("Completed startup fence required");
  if(string.IsNullOrEmpty(sourceSHA)||string.IsNullOrEmpty(layoutSHA))throw new Exception("Actual build/source and calibrated layout references required");
  path=Path.GetFullPath(path);if(!path.EndsWith(".gputrace",StringComparison.OrdinalIgnoreCase)||File.Exists(path)||Directory.Exists(path))throw new Exception("New unique .gputrace path required; no overwrite");Directory.CreateDirectory(Path.GetDirectoryName(path));
  var r=new Record{tracePath=path,observedSplashFinishedUptime=observedSplashFinishedUptime,sourceSHA=sourceSHA,layoutSHA=layoutSHA,supported=FrameCapture.IsDestinationSupported(FrameCaptureDestination.GPUTraceDocument),splashFinished=SplashScreen.isFinished,requestUTC=DateTime.UtcNow.ToString("O"),requestFrame=Time.frameCount,requestUptime=Time.realtimeSinceStartupAsDouble,requestDraws=automaticDraws(),beforePoseSHA=actualPoseSHA()};
  string evidence=path+"-request.json";File.WriteAllText(evidence,JsonUtility.ToJson(r,true));if(!r.supported)throw new Exception("GPUTraceDocument destination unavailable; no dispatch claim");
  FrameCapture.CaptureNextFrameToFile(path);
  yield return null;advanceActualPose(); // explicit next Update mutation, before next automatic render
  yield return new WaitForEndOfFrame();
  r.finishedUTC=DateTime.UtcNow.ToString("O");r.finishedFrame=Time.frameCount;r.finishedUptime=Time.realtimeSinceStartupAsDouble;r.finishedDraws=automaticDraws();r.afterPoseSHA=actualPoseSHA();r.dynamicPoseChanged=r.beforePoseSHA!=r.afterPoseSHA;r.pathObserved=File.Exists(path)||Directory.Exists(path);File.WriteAllText(evidence,JsonUtility.ToJson(r,true));
  if(r.finishedFrame!=r.requestFrame+1||r.finishedDraws!=r.requestDraws+1)throw new Exception("Capture frame/draw fence mismatch; trace unqualified");
  if(!r.dynamicPoseChanged)throw new Exception("Actual pose fingerprint did not change; trace not dynamic skinning proof");
  // Existence is metadata only; capture can still need native flush. No recursive
  // trace reading/hashing while the native writer may be active. Process exits;
  // offline inventory/hash and Xcode command inspection are separate evidence.
 }
}
