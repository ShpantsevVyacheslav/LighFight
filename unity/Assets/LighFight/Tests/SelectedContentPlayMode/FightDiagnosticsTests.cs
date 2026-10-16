using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using NUnit.Framework;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class FightDiagnosticsTests {
  [Test]public void InvalidDeltaIsRejectedWithoutPoisoningWindow(){var w=new FightDiagnosticWindow(8);Assert.False(w.Add(double.NaN));Assert.False(w.Add(double.PositiveInfinity));Assert.False(w.Add(0));Assert.False(w.Add(-1));Assert.True(w.Add(.02));var s=w.Take();Assert.AreEqual(50,s.Fps,1e-8);Assert.AreEqual(20,s.P95Ms,1e-8);Assert.AreEqual(4,s.Rejected);}
  [Test]public void PercentileUsesNearestRankAndRealElapsed(){var w=new FightDiagnosticWindow(100);for(int i=0;i<95;i++)w.Add(.01);for(int i=0;i<5;i++)w.Add(.1);var s=w.Take();Assert.AreEqual(100/1.45,s.Fps,1e-8);Assert.AreEqual(10,s.P95Ms,1e-8);}
  [Test]public void OverflowIsExplicitAndCannotReportIncompleteP95(){var w=new FightDiagnosticWindow(2);w.Add(.01);w.Add(.02);w.Add(.03);var s=w.Take();Assert.True(s.Overflow);Assert.True(double.IsNaN(s.P95Ms));Assert.AreEqual(50,s.Fps,1e-8);Assert.AreEqual(3,s.Frames);}
  [Test]public void TakeResetsWindowAndBackgroundResetDropsLag(){var w=new FightDiagnosticWindow(4);w.Add(.01);w.Reset();Assert.AreEqual(0,w.Take().Frames);w.Add(.02);Assert.AreEqual(50,w.Take().Fps,1e-8);Assert.AreEqual(0,w.Take().Frames);}
  [Test]public void GpuRequiresUniquePositiveTimestampAndValue(){var w=new FightDiagnosticWindow(8);Assert.False(w.AddGpu(0,2));Assert.False(w.AddGpu(1,0));Assert.True(w.AddGpu(1,2));Assert.False(w.AddGpu(1,9));Assert.False(w.AddGpu(0,9));Assert.False(w.AddGpu(2,double.NaN));Assert.True(w.AddGpu(2,4));Assert.AreEqual(3,w.Take().GpuMs,1e-8);Assert.True(double.IsNaN(w.Take().GpuMs));Assert.False(w.AddGpu(2,4));}
  [Test]public void PauseWindowClearKeepsGpuWatermark(){var w=new FightDiagnosticWindow(4);Assert.True(w.AddGpu(7,2));w.ClearSamples();Assert.False(w.AddGpu(7,9));Assert.True(double.IsNaN(w.Take().GpuMs));Assert.True(w.AddGpu(8,3));Assert.AreEqual(3,w.Take().GpuMs);}
  [Test]public void CpuUsesCoreEquivalentsAndRejectsMissingOrResetCounters(){Assert.AreEqual(1.5,FightDiagnosticWindow.CpuCores(4,7,2),1e-8);Assert.True(double.IsNaN(FightDiagnosticWindow.CpuCores(-1,2,1)));Assert.True(double.IsNaN(FightDiagnosticWindow.CpuCores(4,3,1)));Assert.True(double.IsNaN(FightDiagnosticWindow.CpuCores(4,5,0)));}
  [Test,Category("UnityLifecycle")]public void InactiveOwnerDoesNotStartJournalAndDisableClosesIt(){
   var go=new GameObject("diagnostics-lifecycle");go.SetActive(false);var d=go.AddComponent<FightDiagnostics>();string path=null;
   try{Assert.AreEqual("",d.LogPath);go.SetActive(true);path=d.LogPath;Assert.That(File.Exists(path),Is.True);d.enabled=false;Assert.AreEqual("Diagnostics off",d.Summary);using(var file=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Assert.Greater(file.Length,0);}
   finally{UnityEngine.Object.DestroyImmediate(go);if(path!=null&&File.Exists(path))File.Delete(path);}
  }
  [Test,Category("UnityLifecycle")]public void PauseAndFocusResetSamplesWithoutRestartingJournal(){
   var go=new GameObject("diagnostics-background");go.SetActive(false);var d=go.AddComponent<FightDiagnostics>();string path=null;
   try{go.SetActive(true);path=d.LogPath;var field=typeof(FightDiagnostics).GetField("window",BindingFlags.Instance|BindingFlags.NonPublic);var w=(FightDiagnosticWindow)field.GetValue(d);w.Add(.5);typeof(FightDiagnostics).GetMethod("OnApplicationPause",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,new object[]{true});Assert.AreEqual(0,w.Take().Frames);Assert.AreEqual(path,d.LogPath);typeof(FightDiagnostics).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,new object[]{true});Assert.True((bool)typeof(FightDiagnostics).GetField("suspended",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(d),"Focus must not undo active application pause");}
   finally{UnityEngine.Object.DestroyImmediate(go);if(path!=null&&File.Exists(path))File.Delete(path);}
  }
#if !UNITY_IOS || UNITY_EDITOR
  [Test,Category("UnityLifecycle")]public void DesktopNativeMetricsAreExplicitlyUnavailable(){
   var go=new GameObject("diagnostics-fallback");go.SetActive(false);var d=go.AddComponent<FightDiagnostics>();try{var n=typeof(FightDiagnostics).GetMethod("ReadNative",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);var t=n.GetType();Assert.AreEqual(-1d,t.GetField("cpu").GetValue(n));Assert.AreEqual(0ul,t.GetField("bytes").GetValue(n));Assert.AreEqual(-1,t.GetField("thermal").GetValue(n));Assert.AreEqual(-1,t.GetField("lowPower").GetValue(n));}finally{UnityEngine.Object.DestroyImmediate(go);}
  }
#endif
 }
}
