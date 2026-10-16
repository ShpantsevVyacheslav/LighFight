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
  [UnityTest]public IEnumerator RealUpdatesWriteOneSecondSamplesAndPreserveRenderSettings(){
   int aa=QualitySettings.antiAliasing,fps=Application.targetFrameRate;bool timing=FrameTimingManager.IsFeatureEnabled();var go=new GameObject("diagnostics-frame-smoke");go.SetActive(false);var diagnostic=go.AddComponent<FightDiagnostics>();string path=null;int contextCalls=0;
   try{diagnostic.ContextProvider=()=>{contextCalls++;return "stage=fight; label=\"a,b\"";};go.SetActive(true);path=diagnostic.LogPath;typeof(FightDiagnostics).GetMethod("OnApplicationFocus",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(diagnostic,new object[]{true});yield return new WaitForSecondsRealtime(1.2f);StringAssert.StartsWith("FPS ",diagnostic.Summary);Assert.That(contextCalls,Is.InRange(1,3));diagnostic.enabled=false;var rows=File.ReadAllLines(path);Assert.GreaterOrEqual(rows.Length,2);StringAssert.Contains("\"\"a,b\"\"",rows[1]);Assert.AreEqual(aa,QualitySettings.antiAliasing);Assert.AreEqual(fps,Application.targetFrameRate);Assert.AreEqual(timing,FrameTimingManager.IsFeatureEnabled());}
   finally{Object.DestroyImmediate(go);if(path!=null&&File.Exists(path))File.Delete(path);}
  }
 }
}
