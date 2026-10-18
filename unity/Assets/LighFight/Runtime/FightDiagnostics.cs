using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
namespace LighFight.Runtime {
 // No bootstrap or prefab attachment. Owner creates inactive and activates only by explicit opt-in.
 [DisallowMultipleComponent]public sealed class FightDiagnostics:MonoBehaviour {
  public Func<string> ContextProvider;
  public string Summary{get;private set;}="Diagnostics off";
  public string LogPath{get;private set;}="";
  public string JournalError{get;private set;}="";
  public int SampleCount{get;private set;}
  public int JournalSampleCount=>rows;
  public double LastSampleAgeSeconds=>double.IsNaN(lastSample)?double.NaN:Math.Max(0,Time.realtimeSinceStartupAsDouble-lastSample);
  public string SuspensionReason=>appPaused?(appFocused?"application pause":"application pause + focus lost"):!appFocused?"focus lost":"none";
  public string JournalStatus {
   get {
    string capture=!isActiveAndEnabled?"off":suspended?"suspended: "+SuspensionReason:skipNextFrame?"warming":!double.IsNaN(lastSample)&&LastSampleAgeSeconds>3?"live; sample stale (cause unknown)":"live";
    string state=ioFailed?"error: "+JournalError:limitReached||isActiveAndEnabled&&Time.realtimeSinceStartupAsDouble-sessionStart>1200?"limit reached (20 min); OFF/ON restarts":journal!=null?"recording":"closed";
    return "Capture "+capture+" | samples "+SampleCount+" | last "+(double.IsNaN(lastSample)?"never":Number(LastSampleAgeSeconds,"F1")+" s ago")+"\nCSV "+state+" | saved "+rows+"/1200";
   }
  }
  readonly FightDiagnosticWindow window=new FightDiagnosticWindow(2048);
  readonly FrameTiming[] timing=new FrameTiming[1];
  StreamWriter journal;double lastRefresh,cpuPrevious=-1,sessionStart,lastSample=double.NaN;bool limitReached;int rows;bool suspended,skipNextFrame,ioFailed;bool appPaused,appFocused=true;
#if UNITY_IOS && !UNITY_EDITOR
  bool nativeFailed;
 #endif
  string context="";
  const int MaxRows=1200;
#if UNITY_IOS && !UNITY_EDITOR
  [DllImport("__Internal")]static extern double LFDiagnosticsCPUSeconds();
  [DllImport("__Internal")]static extern ulong LFDiagnosticsFootprintBytes();
  [DllImport("__Internal")]static extern int LFDiagnosticsThermalState();
  [DllImport("__Internal")]static extern int LFDiagnosticsLowPower();
#endif
  public void SetContext(string value){context=value??"";}
  void OnEnable(){rows=0;SampleCount=0;lastSample=double.NaN;limitReached=false;ioFailed=false;
#if UNITY_IOS && !UNITY_EDITOR
   nativeFailed=false;
#endif
   appPaused=false;appFocused=Application.isFocused;suspended=!appFocused;skipNextFrame=true;JournalError="";LogPath="";window.ClearSamples();lastRefresh=sessionStart=Time.realtimeSinceStartupAsDouble;cpuPrevious=ReadNative().cpu;Summary="Diagnostics warming; network/ping unavailable";OpenJournal();}
  void OnDisable(){CloseJournal();window.ClearSamples();cpuPrevious=-1;Summary="Diagnostics off";}
  void OnApplicationPause(bool paused){appPaused=paused;Suspend(appPaused||!appFocused);}
  void OnApplicationFocus(bool focused){appFocused=focused;Suspend(appPaused||!appFocused);}
  void Suspend(bool value){if(!isActiveAndEnabled)return;suspended=value;window.ClearSamples();lastRefresh=Time.realtimeSinceStartupAsDouble;cpuPrevious=-1;skipNextFrame=true;}
  void Update(){if(suspended)return;if(skipNextFrame){skipNextFrame=false;cpuPrevious=ReadNative().cpu;lastRefresh=Time.realtimeSinceStartupAsDouble;return;}
   window.Add(Time.unscaledDeltaTime);
   // Never enables FrameTimingStats or changes graphics settings. Old/repeated frames are rejected.
   if(FrameTimingManager.IsFeatureEnabled()){FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0)window.AddGpu(timing[0].frameStartTimestamp,timing[0].gpuFrameTime);}
   double now=Time.realtimeSinceStartupAsDouble;if(now-lastRefresh<1)return;
   var sample=window.Take();var native=ReadNative();double cpu=FightDiagnosticWindow.CpuCores(cpuPrevious,native.cpu,now-lastRefresh);cpuPrevious=native.cpu;lastRefresh=now;
   string current=context;try{if(ContextProvider!=null)current=ContextProvider()??"";}catch{current="context unavailable";}
   if(current.Length>1024)current=current.Substring(0,1024);
   Summary="FPS "+Number(sample.Fps,"F1")+" | p95 "+Number(sample.P95Ms,"F1")+" ms"+(sample.Overflow?" [buffer overflow]":"")+"\nGPU "+Number(sample.GpuMs,"F2")+" ms | CPU "+Number(cpu,"F2")+" cores | footprint "+(native.bytes>0?(native.bytes/1048576d).ToString("F1",CultureInfo.InvariantCulture)+" MiB":"unavailable")+"\nThermal "+Thermal(native.thermal)+" | low power "+(native.lowPower<0?"unavailable":native.lowPower==1?"on":"off")+" | network/ping unavailable\n"+current;
   SampleCount++;lastSample=now;
   WriteRow(now-sessionStart,sample,native,cpu,current);
   Debug.Log("LF_DIAG "+Summary.Replace('\n',' ') + " | "+JournalStatus.Replace('\n',' '));
  }
  struct Native{public double cpu;public ulong bytes;public int thermal,lowPower;}
  Native ReadNative(){var n=new Native{cpu=-1,bytes=0,thermal=-1,lowPower=-1};
#if UNITY_IOS && !UNITY_EDITOR
   if(!nativeFailed)try{n.cpu=LFDiagnosticsCPUSeconds();n.bytes=LFDiagnosticsFootprintBytes();n.thermal=LFDiagnosticsThermalState();n.lowPower=LFDiagnosticsLowPower();}catch(Exception){nativeFailed=true;}
#endif
   return n;
  }
  static string Thermal(int v){switch(v){case 0:return "nominal";case 1:return "fair";case 2:return "serious";case 3:return "critical";default:return "unavailable";}}
  static string Number(double v,string format="R")=>double.IsNaN(v)||double.IsInfinity(v)?"unavailable":v.ToString(format,CultureInfo.InvariantCulture);
  static string Csv(string s)=>"\""+(s??"").Replace("\"","\"\"").Replace('\r',' ').Replace('\n',' ')+"\"";
  void OpenJournal(){try{string dir=Path.Combine(Application.persistentDataPath,"Diagnostics");Directory.CreateDirectory(dir);LogPath=Path.Combine(dir,"fight-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff",CultureInfo.InvariantCulture)+"-"+Guid.NewGuid().ToString("N")+".csv");journal=new StreamWriter(new FileStream(LogPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read));journal.WriteLine("elapsedSeconds,frames,fps,p95Ms,overflow,rejectedFrames,gpuMs,cpuCoreEquivalents,footprintBytes,thermalState,lowPower,context");journal.Flush();}catch(Exception e){FailJournal(e);}}
  void WriteRow(double elapsed,FightDiagnosticWindow.Sample s,Native n,double cpu,string c){if(journal==null||ioFailed)return;if(elapsed>1200||rows>=MaxRows){limitReached=true;CloseJournal();return;}try{journal.WriteLine(string.Join(",",Number(elapsed),s.Frames,Number(s.Fps),Number(s.P95Ms),s.Overflow?1:0,s.Rejected,Number(s.GpuMs),Number(cpu),n.bytes>0?n.bytes.ToString(CultureInfo.InvariantCulture):"unavailable",n.thermal,n.lowPower,Csv(c)));journal.Flush();rows++;if(rows>=MaxRows){limitReached=true;CloseJournal();}}catch(Exception e){FailJournal(e);}}
  void FailJournal(Exception e){if(ioFailed)return;ioFailed=true;JournalError=e.GetType().Name;try{journal?.Dispose();}catch{}journal=null;Debug.LogWarning("LF_DIAG journal unavailable: "+JournalError);}
  void CloseJournal(){try{journal?.Dispose();journal=null;}catch(Exception e){FailJournal(e);}}
 }
 // Pure fixed-storage sampling model. Take allocates nothing and sorts only the preallocated active range.
 public sealed class FightDiagnosticWindow {
  public struct Sample{public int Frames,Rejected;public double Fps,P95Ms,GpuMs;public bool Overflow;}
  readonly double[] times;int count,frames,rejected,gpuCount;double elapsed,gpuTotal;ulong lastGpu;
  public FightDiagnosticWindow(int capacity){if(capacity<1)throw new ArgumentOutOfRangeException(nameof(capacity));times=new double[capacity];}
  public bool Add(double dt){if(double.IsNaN(dt)||double.IsInfinity(dt)||dt<=0){rejected++;return false;}frames++;elapsed+=dt;if(count<times.Length)times[count++]=dt;return true;}
  public bool AddGpu(ulong timestamp,double ms){if(timestamp==0||timestamp<=lastGpu||double.IsNaN(ms)||double.IsInfinity(ms)||ms<=0)return false;lastGpu=timestamp;gpuTotal+=ms;gpuCount++;return true;}
  public Sample Take(){bool overflow=frames>times.Length;double p=double.NaN;if(count>0&&!overflow){Array.Sort(times,0,count);p=times[(int)Math.Ceiling(count*.95)-1]*1000;}var s=new Sample{Frames=frames,Rejected=rejected,Fps=elapsed>0?frames/elapsed:double.NaN,P95Ms=p,Overflow=overflow,GpuMs=gpuCount>0?gpuTotal/gpuCount:double.NaN};ClearWindow();return s;}
  void ClearWindow(){count=frames=rejected=gpuCount=0;elapsed=gpuTotal=0;}
  public void ClearSamples(){ClearWindow();} // Preserve the GPU watermark across pause/disable; cached old frames stay rejected.
  public void Reset(){ClearWindow();lastGpu=0;}
  public static double CpuCores(double before,double after,double wall){return before>=0&&after>=before&&wall>0&&!double.IsInfinity(after)&&!double.IsInfinity(wall)&&!double.IsNaN(after)?(after-before)/wall:double.NaN;}
 }
}
