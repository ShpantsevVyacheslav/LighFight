using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class Native67ShoulderSequenceReview {
 [Serializable]public class ClipReport{public string clip;public int samples;public float maxWristShiftM,maxClavicleLengthErrorM,maxRotationErrorDeg,maxNeckShiftM,maxCorrectionVelocityMps,maxResampleRepeatErrorM;}
 [Serializable]public class Report{public string status="RUNNING_NO_ACCEPTANCE",error,unity;public int samples;public List<ClipReport> clips=new List<ClipReport>();public string scope="All8 native clips, original independent shifted120Hz+endpoints finite schedule. Pose-only; no surface/camera/floor/entry-return/phone acceptance. MUST resample authored pose before each Apply; Apply is not idempotent without fresh baseline. C1 activation <=16deg, source bones/curves/assets unchanged.";}
 static T[] Read<T>(string p,int stride)where T:struct{var b=File.ReadAllBytes(p);var a=new T[b.Length/stride];System.Buffer.BlockCopy(b,0,a,0,b.Length);return a;}
 public static void Run(){var r=new Report{unity=Application.unityVersion};GameObject actor=null;int exit=1;try{
 string dir=Environment.GetEnvironmentVariable("LF_NATIVE67_SCHEDULE");var times=Read<double>(dir+"/times.bin",8);var ids=Read<int>(dir+"/clipIndex.bin",4);var names=File.ReadAllLines(dir+"/clips.txt");if(times.Length!=6816||times.Length!=ids.Length||names.Length!=8)throw new Exception("Frozen original schedule shape");string asset="Assets/Native67SpatialReview/SailorReview.fbx";actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(asset));var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();skin.enabled=false;var bones=skin.bones;var boneMap=bones.ToDictionary(b=>b.name,b=>b);var clips=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
 for(int c=0;c<names.Length;c++){var clip=clips.Single(a=>a.name.EndsWith("|"+names[c]));var cr=new ClipReport{clip=names[c]};r.clips.Add(cr);Vector3[] previous=null;double previousTime=0;
  for(int i=0;i<times.Length;i++)if(ids[i]==c){float t=(float)Math.Min(times[i],clip.length);clip.SampleAnimation(actor,t);var before=new[]{boneMap["Left_Hand"].position,boneMap["Right_Hand"].position};var proofs=Native67ShoulderPoseReview.Apply(bones);var after=new[]{boneMap["Left_Hand"].position,boneMap["Right_Hand"].position};var delta=new[]{after[0]-before[0],after[1]-before[1]};
   for(int j=0;j<2;j++){var p=proofs[j];cr.maxWristShiftM=Math.Max(cr.maxWristShiftM,p.wristShiftM);cr.maxClavicleLengthErrorM=Math.Max(cr.maxClavicleLengthErrorM,Math.Abs(p.clavicleLengthAfterM-p.clavicleLengthBeforeM));cr.maxRotationErrorDeg=Math.Max(cr.maxRotationErrorDeg,p.upperArmRotationChangeDeg);cr.maxNeckShiftM=Math.Max(cr.maxNeckShiftM,p.neckShiftM);if(previous!=null&&times[i]>previousTime)cr.maxCorrectionVelocityMps=Math.Max(cr.maxCorrectionVelocityMps,(float)((delta[j]-previous[j]).magnitude/(times[i]-previousTime)));}
   if(cr.samples%128==0){clip.SampleAnimation(actor,t);Native67ShoulderPoseReview.Apply(bones);cr.maxResampleRepeatErrorM=Math.Max(cr.maxResampleRepeatErrorM,Math.Max(Vector3.Distance(after[0],boneMap["Left_Hand"].position),Vector3.Distance(after[1],boneMap["Right_Hand"].position)));}
   previous=delta;previousTime=times[i];cr.samples++;r.samples++;
  }
  if(cr.maxResampleRepeatErrorM>1e-6f||cr.maxCorrectionVelocityMps>.75f)throw new Exception("Sequence finite gate "+JsonUtility.ToJson(cr));Debug.Log("Shoulder sequence "+JsonUtility.ToJson(cr));
 }
 r.status="FINITE8_CLIP_POSE_GATES_PASS_NOT_VISUAL_OR_RUNTIME_ACCEPTANCE";exit=0;
 }catch(Exception e){r.status="FAILED_NO_PROMOTION";r.error=e.ToString();Debug.LogException(e);}finally{if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);File.WriteAllText(Environment.GetEnvironmentVariable("LF_NATIVE67_REPORT"),JsonUtility.ToJson(r,true));Debug.Log("AGENT_RESULT "+r.status);EditorApplication.Exit(exit);}}
}
