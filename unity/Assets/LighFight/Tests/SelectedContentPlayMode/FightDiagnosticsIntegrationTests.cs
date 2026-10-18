using System.IO;
using System.Collections;
using UnityEngine.TestTools;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class FightDiagnosticsIntegrationTests {
  [Test]public void DiagnosticsAreLazyAndOwnerToggleClosesJournal(){
   var go=new GameObject("optional diagnostics owner");var game=go.AddComponent<Prototype>();string path=null;
   try{Assert.IsNull(go.GetComponentInChildren<FightDiagnostics>(true));var toggle=typeof(Prototype).GetMethod("SetFightDiagnostics",BindingFlags.NonPublic|BindingFlags.Instance);toggle.Invoke(game,new object[]{false});Assert.IsNull(go.GetComponentInChildren<FightDiagnostics>(true));toggle.Invoke(game,new object[]{true});var diagnostic=go.GetComponentInChildren<FightDiagnostics>(true);Assert.IsTrue(diagnostic.isActiveAndEnabled);path=diagnostic.LogPath;Assert.IsTrue(File.Exists(path));StringAssert.Contains("content=inline",diagnostic.ContextProvider());toggle.Invoke(game,new object[]{false});Assert.IsFalse(diagnostic.gameObject.activeSelf);using(var file=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Assert.Greater(file.Length,0);}
   finally{Object.DestroyImmediate(go);if(path!=null&&File.Exists(path))File.Delete(path);}
  }
  const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
  static void Set(FightDiagnostics d,string field,object value)=>typeof(FightDiagnostics).GetField(field,Private).SetValue(d,value);
  static void Call(FightDiagnostics d,string method,params object[] args)=>typeof(FightDiagnostics).GetMethod(method,Private).Invoke(d,args);
  static void Emit(FightDiagnostics d){Call(d,"OnApplicationFocus",true);Set(d,"skipNextFrame",false);Set(d,"lastRefresh",Time.realtimeSinceStartupAsDouble-1.1);Call(d,"Update");}
  [Test]public void EnabledJournalCanBeReadImmediatelyAfterEachSample(){
   var go=new GameObject("read live journal");go.SetActive(false);var d=go.AddComponent<FightDiagnostics>();string path=null;
   try{go.SetActive(true);path=d.LogPath;Assert.AreEqual(1,File.ReadAllLines(path).Length,"Header must be visible before first sample");
    Emit(d);Assert.IsTrue(d.isActiveAndEnabled);Assert.AreEqual(2,File.ReadAllLines(path).Length);Assert.AreEqual(1,d.SampleCount);Assert.AreEqual(1,d.JournalSampleCount);StringAssert.Contains("CSV recording",d.JournalStatus);
    Emit(d);Assert.AreEqual(3,File.ReadAllLines(path).Length);Assert.AreEqual(2,d.JournalSampleCount);
   }finally{Object.DestroyImmediate(go);if(path!=null&&File.Exists(path))File.Delete(path);}
  }
  [Test]public void SuspendedStatusAndAgeDoNotDependOnUpdate(){
   var go=new GameObject("suspended status");go.SetActive(false);var d=go.AddComponent<FightDiagnostics>();string path=null;
   try{go.SetActive(true);path=d.LogPath;Emit(d);Set(d,"lastSample",Time.realtimeSinceStartupAsDouble-8);
    Call(d,"OnApplicationPause",true);Call(d,"OnApplicationFocus",false);
    StringAssert.Contains("application pause + focus lost",d.JournalStatus);Assert.GreaterOrEqual(d.LastSampleAgeSeconds,8);int count=d.SampleCount;Call(d,"Update");Assert.AreEqual(count,d.SampleCount);
    Call(d,"OnApplicationFocus",true);Assert.AreEqual("application pause",d.SuspensionReason);StringAssert.Contains("suspended",d.JournalStatus);
    Call(d,"OnApplicationPause",false);Assert.AreEqual("none",d.SuspensionReason);StringAssert.Contains("warming",d.JournalStatus);
    Set(d,"skipNextFrame",false);StringAssert.Contains("cause unknown",d.JournalStatus);
   }finally{Object.DestroyImmediate(go);if(path!=null&&File.Exists(path))File.Delete(path);}
  }
  [Test]public void RowAndTimeLimitsAreVisibleAndToggleStartsFreshJournal(){
   var go=new GameObject("journal limit restart");go.SetActive(false);var d=go.AddComponent<FightDiagnostics>();string first=null,second=null;
   try{go.SetActive(true);first=d.LogPath;Set(d,"rows",1199);Emit(d);Assert.AreEqual(1200,d.JournalSampleCount);StringAssert.Contains("limit reached",d.JournalStatus);
    using(var file=new FileStream(first,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Assert.Greater(file.Length,0);
    go.SetActive(false);go.SetActive(true);second=d.LogPath;Assert.AreNotEqual(first,second);Assert.AreEqual(0,d.SampleCount);Assert.AreEqual(0,d.JournalSampleCount);StringAssert.Contains("CSV recording",d.JournalStatus);Emit(d);Assert.AreEqual(2,File.ReadAllLines(second).Length);
    Set(d,"sessionStart",Time.realtimeSinceStartupAsDouble-1201);Call(d,"OnApplicationPause",true);StringAssert.Contains("limit reached",d.JournalStatus);Call(d,"OnApplicationPause",false);Emit(d);Assert.AreEqual(1,d.JournalSampleCount);StringAssert.Contains("limit reached",d.JournalStatus);
   }finally{Object.DestroyImmediate(go);foreach(var path in new[]{first,second})if(path!=null&&File.Exists(path))File.Delete(path);}
  }
  [Test]public void JournalErrorIsVisibleWithoutAnotherSample(){
   var go=new GameObject("journal error status");go.SetActive(false);var d=go.AddComponent<FightDiagnostics>();string path=null;
   try{go.SetActive(true);path=d.LogPath;LogAssert.Expect(LogType.Warning,"LF_DIAG journal unavailable: IOException");Call(d,"FailJournal",new IOException("test"));StringAssert.Contains("CSV error: IOException",d.JournalStatus);Assert.AreEqual(0,d.JournalSampleCount);
   }finally{Object.DestroyImmediate(go);if(path!=null&&File.Exists(path))File.Delete(path);}
  }
  [UnityTest]public IEnumerator RealUpdatesWriteOneSecondSamplesAndPreserveRenderSettings(){
   int aa=QualitySettings.antiAliasing,fps=Application.targetFrameRate;bool timing=FrameTimingManager.IsFeatureEnabled();var go=new GameObject("diagnostics-frame-smoke");go.SetActive(false);var diagnostic=go.AddComponent<FightDiagnostics>();string path=null;int contextCalls=0;
   try{diagnostic.ContextProvider=()=>{contextCalls++;return "stage=fight; label=\"a,b\"";};go.SetActive(true);path=diagnostic.LogPath;typeof(FightDiagnostics).GetMethod("OnApplicationFocus",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(diagnostic,new object[]{true});yield return new WaitForSecondsRealtime(1.2f);StringAssert.StartsWith("FPS ",diagnostic.Summary);Assert.That(contextCalls,Is.InRange(1,3));diagnostic.enabled=false;var rows=File.ReadAllLines(path);Assert.GreaterOrEqual(rows.Length,2);StringAssert.Contains("\"\"a,b\"\"",rows[1]);Assert.AreEqual(aa,QualitySettings.antiAliasing);Assert.AreEqual(fps,Application.targetFrameRate);Assert.AreEqual(timing,FrameTimingManager.IsFeatureEnabled());}
   finally{Object.DestroyImmediate(go);if(path!=null&&File.Exists(path))File.Delete(path);}
  }
 }
}
