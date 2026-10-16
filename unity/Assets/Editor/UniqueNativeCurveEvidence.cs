#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Diagnostics;using System.Security.Cryptography;using UnityEditor;using UnityEngine;
public static class UniqueNativeCurveEvidence {
 const string Packet="/Users/macbook1/Documents/ChatGPT/LighFight/.tools/thermal-sustain-review/UniqueCurveReview";
 static readonly string[] Roles={"Bone","Witch","Master","Island","Rose","Ranger","Gala","Cobalt","Retro","Sailor","Summer"};
 public static void Run(){Directory.CreateDirectory(Packet+"/Evidence");var lines=new List<string>{"asset,poses,referenceEvaluationsPerApply,candidateEvaluationsPerApply,referenceMedianUs,candidateMedianUs,referenceAllocatedBytes,candidateAllocatedBytes"};int assets=0,poses=0;var roleCount=new Dictionary<string,int>();var hashes=new Dictionary<string,string>();string failure=null;try{
  var diagnostic=typeof(NativeSupportPoseData).GetProperty("LastCurveEvaluationCount");if(diagnostic==null)throw new Exception("RED: reviewed unique-curve candidate not compiled; main baseline has no counter");
  foreach(string role in Roles){roleCount[role]=0;string folder="Assets/NativeSupport/"+role;if(!AssetDatabase.IsValidFolder(folder))throw new Exception("Missing real source role "+role);
   foreach(string path in AssetDatabase.FindAssets("t:NativeSupportPoseData",new[]{folder}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(x=>x,StringComparer.Ordinal)){
    var data=AssetDatabase.LoadAssetAtPath<NativeSupportPoseData>(path);if(!data)throw new Exception("Pose asset failed "+path);HashInput(path,hashes);if(data.PackedAsset)HashInput(AssetDatabase.GetAssetPath(data.PackedAsset),hashes);int usersBefore=data.ActiveUsers;GameObject a=null,b=null;
    using(data.PinAndPrewarm())try{
     a=Rig(data.JointPaths,"reference");b=Rig(data.JointPaths,"candidate");var ta=data.Bind(a.transform);var tb=data.Bind(b.transform);var js=data.joints;int refCount=Count(js);int clipPoses=0;
     // Exact phases, endpoints, repeated identical seconds, outside-range wrap; no phase epsilon.
     for(int n=-1;n<=129;n++){float seconds=n<0?-.125f:n==129?data.duration*1.125f:data.duration*(n/128f);Reference(js,ta,seconds);data.Apply(tb,seconds);Equal(ta,tb,path,seconds);Reference(js,ta,seconds);data.Apply(tb,seconds);Equal(ta,tb,path,seconds);clipPoses+=2;}
     // Re-evaluation must overwrite external pose contamination even at the same seconds.
     for(int i=0;i<tb.Length;i++){ta[i].localPosition=tb[i].localPosition=new Vector3(.123f,.234f,.345f);ta[i].localRotation=tb[i].localRotation=Quaternion.Euler(13,27,39);ta[i].localScale=tb[i].localScale=Vector3.one*1.234f;}Reference(js,ta,.5f*data.duration);data.Apply(tb,.5f*data.duration);Equal(ta,tb,path,.5f*data.duration);clipPoses++;
     int unique=(int)diagnostic.GetValue(data);double[] rt=new double[6],ct=new double[6];long ra=0,ca=0;
     for(int warm=0;warm<16;warm++){Reference(js,ta,data.duration*.37f);data.Apply(tb,data.duration*.37f);}
     for(int round=0;round<6;round++){Action reference=()=>{for(int i=0;i<64;i++)Reference(js,ta,data.duration*((i%17)/16f));};Action candidate=()=>{for(int i=0;i<64;i++)data.Apply(tb,data.duration*((i%17)/16f));};if(round%2==0){rt[round]=Time(reference,out long alloc);ra+=alloc;ct[round]=Time(candidate,out alloc);ca+=alloc;}else{ct[round]=Time(candidate,out long alloc);ca+=alloc;rt[round]=Time(reference,out alloc);ra+=alloc;}}
     Array.Sort(rt);Array.Sort(ct);lines.Add(string.Join(",",path,clipPoses,refCount,unique,N((rt[2]+rt[3])/2/64),N((ct[2]+ct[3])/2/64),ra,ca));assets++;poses+=clipPoses;roleCount[role]++;
    }finally{if(a)UnityEngine.Object.DestroyImmediate(a);if(b)UnityEngine.Object.DestroyImmediate(b);}
    if(data.ActiveUsers!=usersBefore)throw new Exception("Lease count changed "+path);
   }if(roleCount[role]==0)throw new Exception("No real assets for "+role);
  }
 foreach(var input in hashes)if(Hash(input.Key)!=input.Value)throw new Exception("Source input changed "+input.Key);
 }catch(Exception e){failure=e.ToString();throw;}finally{File.WriteAllLines(Packet+"/Evidence/clips.csv",lines);File.WriteAllLines(Packet+"/Evidence/source-inputs.sha256",hashes.Select(x=>x.Value+" "+x.Key));File.WriteAllText(Packet+"/Evidence/result.txt","passed="+(failure==null)+"\nassets="+assets+"\nposes="+poses+"\nroles="+string.Join(";",roleCount.Select(x=>x.Key+":"+x.Value))+"\nfailure="+failure+"\nscope=Editor exact curve/transform proof and warmed CPU; not player/phone/heat proof\n");}UnityEngine.Debug.Log("AGENT_RESULT unique native curves PASS assets="+assets+" poses="+poses);
 }
 static void HashInput(string p,Dictionary<string,string> output){output[p]=Hash(p);if(File.Exists(p+".meta"))output[p+".meta"]=Hash(p+".meta");}
 static string Hash(string p){using(var h=SHA256.Create())using(var f=File.OpenRead(p))return BitConverter.ToString(h.ComputeHash(f));}
 static string N(double v)=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
 static double Time(Action work,out long allocation){long before=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();work();long end=Stopwatch.GetTimestamp();allocation=GC.GetAllocatedBytesForCurrentThread()-before;return (end-start)*1e6/Stopwatch.Frequency;}
 static GameObject Rig(string[] paths,string name){var go=new GameObject(name);foreach(string path in paths){var t=go.transform;if(path.Length==0)continue;foreach(string piece in path.Split('/')){var child=t.Find(piece);if(!child){child=new GameObject(piece).transform;child.SetParent(t,false);}t=child;}}return go;}
 static int Count(NativeSupportPoseData.Joint[] js){int count=0;foreach(var j in js){if(j.px!=null)count+=3;if(j.qx!=null)count+=4;if(j.sx!=null)count+=3;}return count;}
 public static void Reference(NativeSupportPoseData.Joint[] js,Transform[] targets,float seconds){for(int i=0;i<js.Length;i++){var j=js[i];var t=targets[i];if(j.px!=null)t.localPosition=new Vector3(j.px.Evaluate(seconds),j.py.Evaluate(seconds),j.pz.Evaluate(seconds));if(j.qx!=null)t.localRotation=Quaternion.Normalize(new Quaternion(j.qx.Evaluate(seconds),j.qy.Evaluate(seconds),j.qz.Evaluate(seconds),j.qw.Evaluate(seconds)));if(j.sx!=null)t.localScale=new Vector3(j.sx.Evaluate(seconds),j.sy.Evaluate(seconds),j.sz.Evaluate(seconds));}}
 static bool Bits(float a,float b)=>BitConverter.SingleToInt32Bits(a)==BitConverter.SingleToInt32Bits(b);
 static void Equal(Transform[] a,Transform[] b,string path,float seconds){for(int i=0;i<a.Length;i++){var p=a[i].localPosition;var q=b[i].localPosition;var r=a[i].localRotation;var s=b[i].localRotation;var u=a[i].localScale;var v=b[i].localScale;bool equal=Bits(p.x,q.x)&&Bits(p.y,q.y)&&Bits(p.z,q.z)&&Bits(r.x,s.x)&&Bits(r.y,s.y)&&Bits(r.z,s.z)&&Bits(r.w,s.w)&&Bits(u.x,v.x)&&Bits(u.y,v.y)&&Bits(u.z,v.z);var m=a[i].localToWorldMatrix;var n=b[i].localToWorldMatrix;for(int k=0;k<16;k++)equal&=Bits(m[k],n[k]);if(!equal)throw new Exception("Strict bit pose mismatch "+path+" joint="+i+" seconds="+seconds);}}
}
#endif
