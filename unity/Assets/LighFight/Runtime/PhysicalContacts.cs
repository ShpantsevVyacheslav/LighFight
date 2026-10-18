using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime {
 // Capsule-proxy CCD at deterministic sampled poses. Source geometry/bones are never edited.
 public sealed class PhysicalContacts {
  private sealed class Body {
   public readonly Vector3[] now=new Vector3[13],before=new Vector3[13];public bool ready;public float frame,beforeFrame;
   public void Sample(FighterView view,Fighter fighter,Fighter other,bool history=true){
    if(history){for(int i=0;i<now.Length;i++)before[i]=now[i];beforeFrame=frame;}
    view.SampleContactPose(fighter,other);frame=view.ContactAttackFrame(fighter);
    now[0]=view.StrikeHandCenter(true);now[1]=view.StrikeHandCenter(false);
    now[2]=view.BonePoint("LeftToeBase",new Vector3(-.14f,.1f,.1f));now[3]=view.BonePoint("RightToeBase",new Vector3(.14f,.1f,.1f));
    now[4]=view.BonePoint("LeftForeArm",new Vector3(-.2f,1.25f,.2f));now[5]=view.BonePoint("RightForeArm",new Vector3(.2f,1.25f,.2f));
    now[6]=view.BonePoint("LeftLeg",new Vector3(-.14f,.5f,0));now[7]=view.BonePoint("RightLeg",new Vector3(.14f,.5f,0));
    now[8]=view.BonePoint("Head",new Vector3(0,1.7f,0));now[9]=view.BonePoint("Hips",new Vector3(0,.9f,0));
    now[11]=view.BonePoint("LeftArm",new Vector3(-.2f,1.45f,0));now[12]=view.BonePoint("RightArm",new Vector3(.2f,1.45f,0));now[10]=(now[11]+now[12])*.5f;
    if(!ready){for(int i=0;i<now.Length;i++)before[i]=now[i];beforeFrame=frame;ready=true;}
   }
  }
  static readonly float[] CalibratedReach={.85f,.85f,.7f,.7f,1,1,.85f,.6f,.7f,.6f,1,1,.85f,1,.85f,1,.85f,.6f,1,1,1,.7f,.85f,1,.7f};
  static readonly int[] CapsuleA={8,9,9,9,6,7,11},CapsuleB={8,10,6,7,2,3,12};
  // Existing body radii retained; upper torso follows actual shoulder bones instead of ending at lower Spine.
  static readonly float[] Radius={.14f,.20f,.115f,.115f,.09f,.09f,.115f};
  readonly Body a=new Body(),b=new Body();readonly FighterView viewA,viewB;readonly Match match;bool pendingA,pendingB;
  public Vector3 ImpactA{get;private set;}public Vector3 ImpactB{get;private set;}public int SampledPosePairs{get;private set;}public int ConfirmedPoseContacts{get;private set;}public int UnresolvedPoseContacts{get;private set;}
  public PhysicalContacts(Match value,FighterView player,FighterView opponent){match=value;viewA=player;viewB=opponent;value.EffectiveReach=(fighter,index)=>CalibratedReach[index];value.RequirePhysicalContact=true;value.BeforeContact=Sample;value.ContactValidator=Touches;}
  static readonly Unity.Profiling.ProfilerMarker SampleMarker=new Unity.Profiling.ProfilerMarker("LighFight.Contact.Sample");
  static bool NeedsSweep(Fighter f)=>f.Attack.HasValue&&f.Outcome==StrikeOutcome.None&&f.Frame<f.Attack.Value.Startup+f.Attack.Value.Active;
  void Sample(Match m){if(!NeedsSweep(m.A)&&!NeedsSweep(m.B)){a.ready=b.ready=false;pendingA=pendingB=false;viewA.ClearContactPoseSample();viewB.ClearContactPoseSample();return;}SampleBoth(m);}
  void SampleBoth(Match m){using(SampleMarker.Auto()){SampledPosePairs++;pendingA=pendingB=false;a.Sample(viewA,m.A,m.B);b.Sample(viewB,m.B,m.A);
   // Compute both proposals before either view changes so simultaneous trades use the same world pose.
   float ta,tb;bool ua,ub;int ia,ib;pendingA=Find(a,b,viewA,m.A,out ta,out ia,out ua);pendingB=Find(b,a,viewB,m.B,out tb,out ib,out ub);
   if(pendingA||ua){pendingA&=Stop(a,b,viewA,m.A,m.B,ta,ia);ImpactB=a.now[ia];}
   if(pendingB||ub){pendingB&=Stop(b,a,viewB,m.B,m.A,tb,ib);ImpactA=b.now[ib];}
  }}
  static Vector3 LimbPoint(FighterView view,int limb){switch(limb){case 0:return view.StrikeHandCenter(true);case 1:return view.StrikeHandCenter(false);case 2:return view.BonePoint("LeftToeBase",Vector3.zero);case 3:return view.BonePoint("RightToeBase",Vector3.zero);case 4:return view.BonePoint("LeftForeArm",Vector3.zero);case 5:return view.BonePoint("RightForeArm",Vector3.zero);case 6:return view.BonePoint("LeftLeg",Vector3.zero);default:return view.BonePoint("RightLeg",Vector3.zero);}}
  static float LimbRadius(string id)=>id.Contains("Kick")||id=="sweep"?.11f:id=="elbow"?.10f:id=="knee"?.12f:.095f;
  static bool AtBody(Vector3 point,float radius,Body target){for(int part=0;part<Radius.Length;part++){Vector3 c=target.now[CapsuleA[part]],d=target.now[CapsuleB[part]];float r=radius+Radius[part];if(HitGeometry.SegmentDistanceSquared(point.x,point.y,point.z,point.x,point.y,point.z,c.x,c.y,c.z,d.x,d.y,d.z)<=r*r)return true;}return false;}
  bool Stop(Body strike,Body target,FighterView view,Fighter fighter,Fighter other,float time,int limb){
   float from=strike.beforeFrame,to=strike.frame,radius=LimbRadius(fighter.Attack.Value.Id),low=from,high=from;bool found=false;
   // Validate a real sampled source pose, not merely the straight line between two poses.
   // Fixed bounded probes localize the candidate; they are not an exact nonlinear-mesh CCD certificate.
   for(int probe=0;probe<=8;probe++){float frame=probe==0?from:Mathf.Lerp(from,to,probe/8f);view.SampleContactPoseAtFrame(fighter,other,frame);if(AtBody(LimbPoint(view,limb),radius,target)){high=frame;found=true;break;}low=frame;}
   if(found){for(int pass=0;pass<10&&high-low>.00001f;pass++){float middle=(low+high)*.5f;view.SampleContactPoseAtFrame(fighter,other,middle);if(AtBody(LimbPoint(view,limb),radius,target))high=middle;else low=middle;}ConfirmedPoseContacts++;}
   else{low=from;UnresolvedPoseContacts++;}
   view.StopStrikeAtFrame(fighter,low);strike.Sample(view,fighter,other,false);return found;
  }
  static ContactVector V(Vector3 p)=>new ContactVector(p.x,p.y,p.z);
  static bool Find(Body strike,Body body,FighterView view,Fighter fighter,out float earliest,out int limb,out bool uncertain){earliest=1;limb=0;uncertain=false;if(!NeedsSweep(fighter)||fighter.Frame<1||view.StrikeStopped(fighter))return false;var id=fighter.Attack.Value.Id;int first=0,last=1;float radius=.095f;
   if(id.Contains("Kick")||id=="sweep"){first=2;last=3;radius=.11f;}else if(id=="elbow"){first=4;last=5;radius=.10f;}else if(id=="knee"){first=6;last=7;radius=.12f;}else if(id=="jab"||id=="hook"||id=="shortHook")first=last=0;else if(id=="cross"||id=="uppercut"||id=="backfist")first=last=1;
   int side=view.NativeStrikeSide(id);if(side>0&&id!="knee"){first=(id.Contains("Kick")||id=="sweep"?2:id=="elbow"?4:0)+side-1;last=first;}bool hit=false;
   for(int i=first;i<=last;i++)for(int part=0;part<Radius.Length;part++){int c=CapsuleA[part],d=CapsuleB[part];var sweep=SweptContact.Cast(V(strike.before[i]),V(strike.before[i]),V(strike.now[i]),V(strike.now[i]),V(body.before[c]),V(body.before[d]),V(body.now[c]),V(body.now[d]),radius+Radius[part]);if(sweep.Indeterminate){uncertain=true;if(sweep.Time<earliest){earliest=sweep.Time;limb=i;}}else if(sweep.Hit&&sweep.Time<=earliest){hit=true;earliest=sweep.Time;limb=i;}}
   // An unresolved earlier proxy cannot authorize damage at a later witness.
   return hit&&!uncertain;
  }
  bool Touches(Fighter attacker,Fighter defender)=>attacker==match.A?pendingA:pendingB;
 }
}
