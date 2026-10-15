using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
public static class CombatStanceRosterEOFBuild {
 [Serializable]sealed class Row{public int index=0;public string name=null,stanceSource=null,stanceOutput=null;}
 [Serializable]sealed class Rows{public Row[] rows=null;}
 [Serializable]sealed class Gate{public bool passed=false;public int samples=0;}
 static string Arg(string key,string value){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);return i>=0&&i+1<a.Length?a[i+1]:value;}
 public static void Build(){
  string projectRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));string allowed=Path.Combine(projectRoot,".tools/stance-roster-player");string output=Path.GetFullPath(Arg("-stancePlayer",Path.Combine(allowed,"Review.app")));if(!output.StartsWith(allowed+Path.DirectorySeparatorChar,StringComparison.Ordinal)||!output.EndsWith(".app",StringComparison.Ordinal))throw new Exception("Build output must remain .tools/stance-roster-player/*.app");
  string inputs=Arg("-stanceRosterInputs",Path.Combine(projectRoot,".tools/combat-stance-source-review/CandidatePacket/all11-review-inputs.json"));var rows=JsonUtility.FromJson<Rows>("{\"rows\":"+File.ReadAllText(inputs)+"}").rows;if(rows==null||rows.Length!=11||rows.Select(x=>x.index).Distinct().Count()!=11)throw new Exception("Expected11 unique roster inputs");
  var setup=EditorSceneManager.GetSceneManagerSetup();try{
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var probe=new GameObject("Actual all11 EOF probe").AddComponent<CombatStanceEOFProbe>();probe.roster=new CombatStanceEOFProbe.ActorPair[rows.Length];
   for(int i=0;i<rows.Length;i++){var r=rows[i];if(!r.stanceSource.StartsWith("Assets/LighFight/")||r.stanceSource.Contains("..")||!r.stanceOutput.StartsWith("Assets/CombatStanceSourceReview/")||r.stanceOutput.Contains(".."))throw new Exception("Invalid review source/output");var gate=JsonUtility.FromJson<Gate>(File.ReadAllText(r.stanceOutput+"/animator-only-gates.json"));if(!gate.passed||gate.samples<=0)throw new Exception("Strict source/state/phase gates required: "+r.name);
    var original=AssetDatabase.LoadAssetAtPath<GameObject>(r.stanceSource);var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(r.stanceOutput+"/IslandStanceCandidate.UNACCEPTED.prefab");if(!original||!candidate)throw new Exception("Missing pair "+r.name);var a=original.GetComponentInChildren<Animator>();var b=candidate.GetComponentInChildren<Animator>();var sourceController=a.runtimeAnimatorController as AnimatorController;if(!sourceController||gate.samples!=(sourceController.layers[0].stateMachine.states.Length+1)*6)throw new Exception("Incomplete per-state sample coverage "+r.name);if(a.avatar!=b.avatar||!a.avatar.isHuman||!a.avatar.isValid)throw new Exception("Own Avatar differs "+r.name);var sa=original.GetComponentsInChildren<SkinnedMeshRenderer>();var sb=candidate.GetComponentsInChildren<SkinnedMeshRenderer>();if(sa.Length!=sb.Length)throw new Exception("Renderer layout differs "+r.name);for(int j=0;j<sa.Length;j++)if(sa[j].sharedMesh!=sb[j].sharedMesh||!sa[j].sharedMaterials.SequenceEqual(sb[j].sharedMaterials))throw new Exception("Original render references differ "+r.name);
    probe.roster[i]=new CombatStanceEOFProbe.ActorPair{id=r.index.ToString("D2")+"-"+r.name,original=original,candidate=candidate};
   }
   var cam=new GameObject("Fixed same pair camera").AddComponent<Camera>();cam.transform.position=new Vector3(0,1.3f,-5.5f);cam.transform.LookAt(new Vector3(0,.95f,0));cam.fieldOfView=40;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.21f,.28f);
   var key=new GameObject("Daylight").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1;key.transform.rotation=Quaternion.Euler(40,-30,0);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;
   var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.localScale=new Vector3(2,1,1);UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());string path="Assets/CombatStanceSourceReview/RosterEOFReview.unity";EditorSceneManager.SaveScene(scene,path);Directory.CreateDirectory(allowed);
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName=output,target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});Debug.Log("STANCE_ROSTER_BUILD_RESULT "+report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings+" output="+output+" productName="+PlayerSettings.productName);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Roster player build failed");
  }finally{if(setup!=null&&setup.Length>0&&setup.Any(x=>x.isLoaded)&&setup.Count(x=>x.isActive&&x.isLoaded)==1&&setup.All(x=>!string.IsNullOrEmpty(x.path)&&File.Exists(x.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else Debug.Log("STANCE_ROSTER_RESTORE_SKIPPED invalid original batch setup");}
 }
}
