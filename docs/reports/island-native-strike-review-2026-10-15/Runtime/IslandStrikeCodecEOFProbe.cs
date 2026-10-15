using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
// Pure codec/original-controller oracle, NOT a combat controller or contact acceptance test.
[DefaultExecutionOrder(10000)]
public sealed class IslandStrikeCodecEOFProbe:MonoBehaviour {
 public GameObject model;public RuntimeAnimatorController controller;public NativeSupportPoseData[] poses;
 GameObject source,target;Animator oracle;SkinnedMeshRenderer a,b;Transform[] bindings;Mesh ma,mb;StreamWriter log;int failures;float maxError;
 IEnumerator Start(){
  string output=Arg("-islandReviewOutput",Path.Combine(Application.persistentDataPath,"IslandStrikeEOF"));Directory.CreateDirectory(output);log=new StreamWriter(Path.Combine(output,"eof.csv"));log.WriteLine("take,frame,coreTicks,seconds,maxWorldVertexError");
  source=Instantiate(model);target=Instantiate(model);oracle=source.GetComponent<Animator>();oracle.runtimeAnimatorController=controller;oracle.applyRootMotion=false;oracle.cullingMode=AnimatorCullingMode.AlwaysAnimate;oracle.Rebind();target.GetComponent<Animator>().enabled=false;a=source.GetComponentInChildren<SkinnedMeshRenderer>();b=target.GetComponentInChildren<SkinnedMeshRenderer>();foreach(var r in source.GetComponentsInChildren<Renderer>())r.enabled=false;ma=new Mesh();mb=new Mesh();
  foreach(var pose in poses){using(pose.PinAndPrewarm()){bindings=pose.Bind(target.transform);int ticks=Mathf.CeilToInt(pose.duration*240);for(int i=0;i<=ticks;){int n=Mathf.Min(i%2==0?3:1,ticks-i+1);float t=0;for(int k=0;k<n;k++,i++){t=Mathf.Min(i/240f,pose.duration);oracle.enabled=true;oracle.Play(pose.sourceName,0,Mathf.Min(t/pose.duration,0.9999999f));oracle.Update(0);oracle.enabled=false;pose.Apply(bindings,t);}float before=Compare();yield return new WaitForEndOfFrame();float after=Compare();maxError=Mathf.Max(maxError,Mathf.Max(before,after));if(before>0.00001f||after>0.00001f||!float.IsFinite(after))failures++;log.WriteLine(FormattableString.Invariant($"{pose.sourceName},{Time.frameCount},{n},{t:R},{after:R}"));}}}
  log.Dispose();log=null;File.WriteAllText(Path.Combine(output,"verdict.txt"),"failures="+failures+"\nmaxWorldSurfaceError="+maxError.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"\nOriginal Generic controller vs packed original transforms. NOT physical contact/entry/return acceptance.\n");Debug.Log("ISLAND_CODEC_EOF failures="+failures+" max="+maxError);Application.Quit(failures==0?0:1);
 }
 float Compare(){a.BakeMesh(ma,true);b.BakeMesh(mb,true);var x=ma.vertices;var y=mb.vertices;if(x.Length!=y.Length)throw new Exception("Original surface mismatch");float error=0;for(int i=0;i<x.Length;i++){var p=a.transform.TransformPoint(x[i]);var q=b.transform.TransformPoint(y[i]);float d=Vector3.Distance(p,q);if(!float.IsFinite(d))return float.PositiveInfinity;error=Mathf.Max(error,d);}return error;}
 static string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
 void OnDestroy(){log?.Dispose();if(ma)Destroy(ma);if(mb)Destroy(mb);if(source)Destroy(source);if(target)Destroy(target);}
}
