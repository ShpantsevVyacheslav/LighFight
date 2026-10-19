using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class Native67MotionOracleReview {
 [Serializable]public class Report{public string status="RUNNING_NO_ACCEPTANCE",error,unity,assetSHA256;public int samples,bones,vertices,triangles;public string scope="Actual uncorrected Generic AnimationClip.SampleAnimation world transforms, original6816 finite schedule, source-name order. No shoulder correction/DQS, production writes, surface raster or phone acceptance.";}
 static T[] Read<T>(string p,int stride)where T:struct{var b=File.ReadAllBytes(p);var a=new T[b.Length/stride];System.Buffer.BlockCopy(b,0,a,0,b.Length);return a;}
 static void Write(BinaryWriter w,Matrix4x4 m){for(int r=0;r<4;r++)for(int c=0;c<4;c++)w.Write(m[r,c]);}
 public static void Run(){var r=new Report{unity=Application.unityVersion};GameObject actor=null;int exit=1;string output=Path.GetDirectoryName(Environment.GetEnvironmentVariable("LF_NATIVE67_REPORT"));try{
 EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);string dir=Environment.GetEnvironmentVariable("LF_NATIVE67_SCHEDULE");var times=Read<double>(dir+"/times.bin",8);var ids=Read<int>(dir+"/clipIndex.bin",4);var clipsNames=File.ReadAllLines(dir+"/clips.txt");var boneNames=File.ReadAllLines(Environment.GetEnvironmentVariable("LF_NATIVE67_PACKET")+"/bones.txt");if(times.Length!=6816||ids.Length!=6816||boneNames.Length!=67)throw new Exception("Frozen schedule/source bone set required");
 string asset="Assets/Native67SpatialReview/SailorReview.fbx";using(var h=System.Security.Cryptography.SHA256.Create())r.assetSHA256=BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(asset))).Replace("-","").ToLowerInvariant();actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(asset));var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();skin.enabled=false;var byName=skin.bones.Select((b,i)=>new{b.name,i}).ToDictionary(b=>b.name,b=>b.i);var order=boneNames.Select(n=>byName[n]).ToArray();r.bones=skin.bones.Length;r.vertices=skin.sharedMesh.vertexCount;r.triangles=skin.sharedMesh.triangles.Length/3;
 using(var w=new BinaryWriter(File.Create(output+"/meshWorldBind-f32.bin")))Write(w,skin.transform.localToWorldMatrix);
 using(var w=new BinaryWriter(File.Create(output+"/bindposes-source-order-f32.bin")))foreach(int b in order)Write(w,skin.sharedMesh.bindposes[b]);File.WriteAllLines(output+"/bones.txt",boneNames);
 var clips=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();if(clips.Length!=8)throw new Exception("Exact source8 clip set");var selected=clipsNames.Select(n=>clips.Single(c=>c.name.EndsWith("|"+n))).ToArray();
 using(var w=new BinaryWriter(File.Create(output+"/actual-world-poses-f32.bin"))){for(int i=0;i<times.Length;i++){selected[ids[i]].SampleAnimation(actor,(float)Math.Min(times[i],selected[ids[i]].length));foreach(int b in order)Write(w,skin.bones[b].localToWorldMatrix);r.samples++;if(i%512==0)Debug.Log("Native67 source oracle capture "+i);}}
 r.status="CAPTURED_ACTUAL_UNITY_POSES_PENDING_OFFLINE_ORACLE";exit=0;
 }catch(Exception e){r.status="FAILED_NO_PROMOTION";r.error=e.ToString();Debug.LogException(e);}finally{if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);File.WriteAllText(Environment.GetEnvironmentVariable("LF_NATIVE67_REPORT"),JsonUtility.ToJson(r,true));Debug.Log("AGENT_RESULT "+r.status);EditorApplication.Exit(exit);}}
}
