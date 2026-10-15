using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
// Review-only; intentionally no FighterView, support catalog or shipping prefab mutation.
public static class IslandNativeStrikeReviewJob {
 const string Folder="Assets/IslandNativeStrikeReview";
 const string SHA="5cc8b7208f3267cb1e20909bcff4eb04a9b9bf58ce7f28ebb77bbdb516c41a24";
 static readonly string[] Names={"box_01","box_02","front_kick_01"};
 static readonly float[] Durations={53f/24,67f/24,60f/24};
 static readonly string[] Fields={"px","py","pz","qx","qy","qz","qw","sx","sy","sz"};
 static string Arg(string key,string fallback=null){var a=Environment.GetCommandLineArgs();int n=Array.IndexOf(a,key);return n>=0&&n+1<a.Length?a[n+1]:fallback;}
 static string Hash(byte[] bytes){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static void OwnedProject(){string root=Path.GetFullPath(Path.GetDirectoryName(Application.dataPath));if(!root.Replace('\\','/').Contains("/.tools/tripo-humanoid-review-unity/"))throw new Exception("Island job is restricted to its owned isolated QA project, never main");Directory.SetCurrentDirectory(root);}
 public static void Prepare(){
  OwnedProject();
  string input=Arg("-islandNativeSource");if(string.IsNullOrEmpty(input)||Hash(File.ReadAllBytes(input))!=SHA)throw new Exception("Exact Island native FBX SHA gate failed");
  string path=Folder+"/Source/model.fbx";Directory.CreateDirectory(Folder+"/Source");Directory.CreateDirectory(Folder+"/Poses");Directory.CreateDirectory("IslandStrikeEvidence");
  if(File.Exists(path)&&Hash(File.ReadAllBytes(path))!=SHA)throw new Exception("Review input already exists with different bytes");
  if(!File.Exists(path))File.Copy(input,path);AssetDatabase.Refresh();
  var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.meshCompression=ModelImporterMeshCompression.Off;importer.isReadable=true;importer.optimizeGameObjects=false;importer.weldVertices=false;importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=8;importer.minBoneWeight=0;importer.SaveAndReimport();
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var mesh=model.GetComponentInChildren<SkinnedMeshRenderer>();if(!mesh||mesh.bones.Length!=67||mesh.sharedMesh.triangles.Length/3!=100000)throw new Exception("Unexpected Island imported geometry/bone inventory");
  string geometry=MeshHash(mesh.sharedMesh);var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/original-generic.controller");if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/original-generic.controller");var sm=controller.layers[0].stateMachine;foreach(var s in sm.states)sm.RemoveState(s.state);
  var poses=new List<NativeSupportPoseData>();
  for(int i=0;i<Names.Length;i++){
   var clip=clips.Single(c=>c.name==Names[i]);if(clip.humanMotion||Mathf.Abs(clip.length-Durations[i])>1e-5f)throw new Exception("Raw Generic duration/type gate "+clip.name);
   var state=sm.AddState(clip.name);state.motion=clip;state.writeDefaultValues=true;var pose=Export(clip,Folder+"/Poses/"+clip.name+".pose.asset");IslandStrikeLosslessPack.PackOnly();poses.Add(pose);Measure(model,clip,pose);
  }
  AssetDatabase.SaveAssets();if(MeshHash(mesh.sharedMesh)!=geometry||Hash(File.ReadAllBytes(input))!=SHA||Hash(File.ReadAllBytes(path))!=SHA)throw new Exception("Source geometry or FBX changed");
  File.WriteAllText("IslandStrikeEvidence/import.txt","sourceSHA="+SHA+"\nmeshSHA="+geometry+"\nvertices="+mesh.sharedMesh.vertexCount+"\ntriangles="+mesh.sharedMesh.triangles.Length/3+"\nbones="+mesh.bones.Length+"\nmaxWeights="+MaxWeights(mesh.sharedMesh)+"\nNo material/image writes. Raw imported Generic original channels.\n");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var host=new GameObject("Island native codec EOF review ONLY");var probe=host.AddComponent<IslandStrikeCodecEOFProbe>();probe.model=model;probe.controller=controller;probe.poses=poses.ToArray();EditorSceneManager.SaveScene(scene,Folder+"/codec-eof-review.unity");AssetDatabase.SaveAssets();Debug.Log("ISLAND_REVIEW prepared; full trajectories exported. NO STRIKE SEGMENT ACCEPTED.");
 }
 public static void BuildCodecProbe(){OwnedProject();string output=Arg("-islandReviewPlayer");if(string.IsNullOrEmpty(output))throw new Exception("Supply owned -islandReviewPlayer app path");var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Folder+"/codec-eof-review.unity"},locationPathName=output,target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Island review player build failed");}
 static int MaxWeights(Mesh m){int max=0;using(var c=m.GetBonesPerVertex())foreach(var n in c)max=Math.Max(max,n);return max;}
 public static string MeshHash(Mesh m){using(var b=new MemoryStream()){using(var w=new BinaryWriter(b,Encoding.UTF8,true)){foreach(var v in m.vertices){w.Write(v.x);w.Write(v.y);w.Write(v.z);}foreach(var v in m.uv){w.Write(v.x);w.Write(v.y);}foreach(int t in m.triangles)w.Write(t);foreach(var a in m.bindposes)for(int i=0;i<16;i++)w.Write(a[i]);using(var c=m.GetBonesPerVertex())foreach(var n in c)w.Write(n);using(var a=m.GetAllBoneWeights())foreach(var n in a){w.Write(n.boneIndex);w.Write(n.weight);}}return Hash(b.ToArray());}}
 static NativeSupportPoseData Export(AnimationClip clip,string path){
  var d=AssetDatabase.LoadAssetAtPath<NativeSupportPoseData>(path);if(!d){d=ScriptableObject.CreateInstance<NativeSupportPoseData>();AssetDatabase.CreateAsset(d,path);}var bs=AnimationUtility.GetCurveBindings(clip);if(bs.Any(b=>b.type!=typeof(Transform)))throw new Exception("Nontransform original binding");var js=new List<NativeSupportPoseData.Joint>();
  using(var memory=new MemoryStream()){using(var w=new BinaryWriter(memory,Encoding.UTF8,true))foreach(string p in bs.Select(b=>b.path).Distinct().OrderBy(p=>p,StringComparer.Ordinal)){var j=new NativeSupportPoseData.Joint{path=p};foreach(var b in bs.Where(b=>b.path==p).OrderBy(b=>b.propertyName,StringComparer.Ordinal)){string f=b.propertyName.StartsWith("m_LocalPosition.")?"p"+b.propertyName.Last():b.propertyName.StartsWith("m_LocalRotation.")?"q"+b.propertyName.Last():b.propertyName.StartsWith("m_LocalScale.")?"s"+b.propertyName.Last():null;if(f==null||!Fields.Contains(f))throw new Exception("Unsupported original curve "+b.propertyName);var c=AnimationUtility.GetEditorCurve(clip,b);typeof(NativeSupportPoseData.Joint).GetField(f).SetValue(j,new AnimationCurve(c.keys){preWrapMode=c.preWrapMode,postWrapMode=c.postWrapMode});w.Write(b.path);w.Write(b.propertyName);w.Write((int)c.preWrapMode);w.Write((int)c.postWrapMode);w.Write(c.length);foreach(var k in c.keys){w.Write(k.time);w.Write(k.value);w.Write(k.inTangent);w.Write(k.outTangent);w.Write(k.inWeight);w.Write(k.outWeight);w.Write((int)k.weightedMode);}}js.Add(j);}d.sourceCurveHash=Hash(memory.ToArray());}
  d.sourceName=clip.name;d.duration=clip.length;d.joints=js.ToArray();EditorUtility.SetDirty(d);return d;
 }
 static void Measure(GameObject model,AnimationClip clip,NativeSupportPoseData pose){
  var source=UnityEngine.Object.Instantiate(model);var replay=UnityEngine.Object.Instantiate(model);var a=source.GetComponent<Animator>();if(a)a.enabled=false;var b=replay.GetComponent<Animator>();if(b)b.enabled=false;var sourceSkin=source.GetComponentInChildren<SkinnedMeshRenderer>();var replaySkin=replay.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();var other=new Mesh();var targets=pose.Bind(replay.transform);string before=MeshHash(sourceSkin.sharedMesh);float maxError=0;
  try{using(var log=new StreamWriter("IslandStrikeEvidence/"+clip.name+".trajectory.csv")){log.WriteLine("seconds,joint,x,y,z,fullSurfaceMinY,codecSurfaceMaxError");int count=Mathf.CeilToInt(clip.length*240);for(int i=0;i<=count;i++){float t=Mathf.Min(i/240f,clip.length);clip.SampleAnimation(source,t);pose.Apply(targets,t);sourceSkin.BakeMesh(baked,true);replaySkin.BakeMesh(other,true);var vs=baked.vertices;var rs=other.vertices;if(vs.Length!=rs.Length)throw new Exception("Codec mesh length mismatch");float floor=float.PositiveInfinity,error=0;for(int v=0;v<vs.Length;v++){var x=sourceSkin.transform.TransformPoint(vs[v]);var y=replaySkin.transform.TransformPoint(rs[v]);if(!Finite(x)||!Finite(y))throw new Exception("Nonfinite original/codec surface");floor=Mathf.Min(floor,x.y);error=Mathf.Max(error,Vector3.Distance(x,y));}maxError=Mathf.Max(maxError,error);foreach(var bone in source.GetComponentsInChildren<Transform>()){var p=source.transform.InverseTransformPoint(bone.position);log.WriteLine(FormattableString.Invariant($"{t:R},{bone.name},{p.x:R},{p.y:R},{p.z:R},{floor:R},{error:R}"));}}}if(maxError>0.00001f)throw new Exception("Generic direct-channel/packed fullsurface parity >.01mm: "+maxError);if(MeshHash(sourceSkin.sharedMesh)!=before)throw new Exception("Mesh mutated");File.WriteAllText("IslandStrikeEvidence/"+clip.name+".source-gate.txt","fullSurfaceCodecMaxError="+maxError.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"\ncurveSHA="+pose.sourceCurveHash+"\n");}
  finally{UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(other);UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(replay);pose.ReleaseDecoded();}
 }
 static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
}
