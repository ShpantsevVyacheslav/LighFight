using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Editor {
 public static class NativeIslandSetup {
  public const string Folder="Assets/LighFight/Art/Drafts/MeshyWebIsland";
  static readonly string[] Map={"jab:Left_Jab_from_Guard","cross:Right_Jab_from_Guard","hook:Left_Hook_from_Guard","body:body","frontKick:Spartan_Kick","lowKick:lowKick","sweep:Sweep_Kick","uppercut:Right_Uppercut_from_Guard","knee:Boxing_Guard_Step_Knee_Strike","elbow:Elbow_Strike","spinKick:spinKick","jumpKick:Rising_Flying_Kick","backfist:backfist","axeKick:axeKick","sideKick:sideKick","roundhouseKick:Roundhouse_Kick","hammer:hammerReach","palm:palmReach","heelKick:heelKick","switchKick:switchKick","highKick:High_Kick","shortHook:Left_Short_Hook_from_Guard","doublePunch:Punch_Forward_with_Both_Fists","stepKick:Step_in_High_Kick","counterstrike:Counterstrike","Idle:Boxing_Guard","Guard:Boxing_Guard","Walk:Walk_Fight_Forward","WalkBack:Walk_Fight_Back","Crouch:air_squat","Hit:Hit_Reaction_to_Waist","Fall:falling_down","GetUp:Stand_Up1","Dance:Hip_Hop_Dance_1"};
  public static void Build() { MeshyWebReview.PrepareAndCapture();Promote(); }
  public static void Prepare() {
   NativeMaterialImport.Create(Folder+"/Pbr","../art/source/original-island/web-albedo.png","../art/source/original-island/web-normal.png","../art/source/original-island/web-metallic-roughness.png",true);
   var clips=Clips();var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/web-motions.fbx");
   foreach(var clip in clips)NativeFighterImport.ValidateClip(model,clip);
   Debug.Log("Verified native source clips: "+clips.Length);
  }
  static AnimationClip[] Clips()=>AssetDatabase.LoadAllAssetsAtPath(Folder+"/web-motions.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
  static float ReferenceHeadHeight(){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-4.prefab");var obj=UnityEngine.Object.Instantiate(prefab);try{var view=obj.GetComponent<FighterView>();view.Pose(new Fighter(),1,0);return view.BonePoint("Head",Vector3.zero).y;}finally{UnityEngine.Object.DestroyImmediate(obj);}}
  public static void Promote() {
   Prepare();var clips=Clips();var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/web-motions.fbx");
   var states=Map.Select(row=>{var parts=row.Split(':');var clip=clips.Single(c=>c.name=="SmartRigArmature|"+parts[1]+"_SmartRigArmature");return new NativeFighterImport.State{state=parts[0],clip=clip.name};}).ToArray();
   float targetHeight=ReferenceHeadHeight();var obj=UnityEngine.Object.Instantiate(model);var audit=new List<string>();
   try {
    var bones=obj.GetComponentsInChildren<Transform>();Func<string,Transform> bone=n=>bones.Single(t=>t.name.EndsWith(":"+n));
    foreach(var state in states.Where(s=>Moves.All.Any(m=>m.Id==s.state))) {
     var clip=clips.Single(c=>c.name==state.clip);var move=Moves.All.Single(m=>m.Id==state.state);bool kick=state.state.Contains("Kick")||state.state=="sweep";
     var ends=state.state=="knee"?new[]{bone("LeftLeg"),bone("RightLeg")}:state.state=="elbow"?new[]{bone("LeftForeArm"),bone("RightForeArm")}:kick?new[]{bone("LeftFoot"),bone("RightFoot")}:new[]{bone("LeftHand"),bone("RightHand")};
     float maximum=float.NegativeInfinity,phase=.5f;int side=0;for(int f=3;f<=57;f++){clip.SampleAnimation(obj,clip.length*f/60);for(int index=0;index<ends.Length;index++){float reach=Vector3.Dot(ends[index].position-bone("Hips").position,obj.transform.forward);if(reach>maximum){maximum=reach;side=index+1;}}}maximum=float.NegativeInfinity;
     for(int f=3;f<=57;f++){clip.SampleAnimation(obj,clip.length*f/60);for(int index=side-1;index<side;index++){float height=ends[index].position.y;bool heightMatches=move.Level==Level.High?height>=targetHeight-.15f&&height<=targetHeight+.13f:move.Level==Level.Low?height<=targetHeight-.3f:height>=.7f&&height<=targetHeight+.13f;if(!heightMatches)continue;float reach=Vector3.Dot(ends[index].position-bone("Hips").position,obj.transform.forward);if(reach>maximum){maximum=reach;phase=f/60f;side=index+1;}}}
     if(float.IsNegativeInfinity(maximum))throw new System.InvalidOperationException("Native strike never reaches target height: "+state.state);clip.SampleAnimation(obj,clip.length*phase);var aim=ends[side-1].position-bone("Hips").position;state.headingOffset=-Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg;state.contactPhase=phase;state.strikeSide=side;audit.Add(state.state+": "+state.clip+" contact="+phase+" side="+side+" source reach="+maximum);
    }
   }finally{UnityEngine.Object.DestroyImmediate(obj);}
   const string path="Assets/LighFight/Art/Fighters/fighter-3.prefab";
   var prefab=NativeFighterImport.Import(new NativeFighterImport.Manifest{modelPath=Folder+"/web-motions.fbx",materialPath=Folder+"/Pbr/native.mat",outputPath=path,states=states});
   var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");content.fighters[3]=prefab;EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();
   File.WriteAllLines("../art/source/original-island/native-gameplay-map.txt",audit);
   HostFightReviewSetup.Create();
  }
 }
}
