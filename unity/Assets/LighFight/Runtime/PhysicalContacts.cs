using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime
{
 // Samples the same animation pose used by the simulation tick, not render-frame Physics callbacks.
 public sealed class PhysicalContacts
 {
  private sealed class Body
  {
   public readonly Vector3[] now=new Vector3[8],before=new Vector3[8];
   public Vector3 head,chest,hips,leftKnee,rightKnee,leftFoot,rightFoot;
   public bool ready;
   public void Sample(FighterView view,Fighter fighter,Fighter other)
   {
    view.SampleContactPose(fighter,other);
    for(int i=0;i<8;i++)before[i]=now[i];
    now[0]=view.BonePoint("LeftHand",new Vector3(-.2f,1.4f,.3f));now[1]=view.BonePoint("RightHand",new Vector3(.2f,1.4f,.3f));
    now[2]=view.BonePoint("LeftToeBase",new Vector3(-.14f,.1f,.1f));now[3]=view.BonePoint("RightToeBase",new Vector3(.14f,.1f,.1f));
    now[4]=view.BonePoint("LeftForeArm",new Vector3(-.2f,1.25f,.2f));now[5]=view.BonePoint("RightForeArm",new Vector3(.2f,1.25f,.2f));
    // Native rigs locate the palm using finger knuckles; wrist-only rigs use their existing offset.
    now[0]=view.StrikeHandCenter(true);now[1]=view.StrikeHandCenter(false);
    head=view.BonePoint("Head",new Vector3(0,1.7f,0));chest=view.BonePoint(view.UsesNativeMotion?"Spine2":"Spine",new Vector3(0,1.3f,0));hips=view.BonePoint("Hips",new Vector3(0,.9f,0));
    leftKnee=now[6]=view.BonePoint("LeftLeg",new Vector3(-.14f,.5f,0));rightKnee=now[7]=view.BonePoint("RightLeg",new Vector3(.14f,.5f,0));leftFoot=now[2];rightFoot=now[3];
    if(!ready){for(int i=0;i<8;i++)before[i]=now[i];ready=true;}
   }
  }
  private static readonly float[] CalibratedReach={.85f,.85f,.7f,.7f,1,1,.85f,.6f,.7f,.6f,1,1,.85f,1,.85f,1,.85f,.6f,1,1,1,.7f,.85f,1,.7f};
  private readonly Body a=new Body(),b=new Body();
  private readonly FighterView viewA,viewB;
  private readonly Match match;
  public Vector3 ImpactA {get;private set;} public Vector3 ImpactB {get;private set;}
  public PhysicalContacts(Match value,FighterView player,FighterView opponent){match=value;viewA=player;viewB=opponent;value.EffectiveReach=(fighter,index)=>CalibratedReach[index];value.RequirePhysicalContact=true;value.BeforeContact=Sample;value.ContactValidator=Touches;}
  private static readonly Unity.Profiling.ProfilerMarker SampleMarker=new Unity.Profiling.ProfilerMarker("LighFight.Contact.Sample");
  public int SampledPosePairs{get;private set;}
  private static bool NeedsSweep(Fighter fighter){if(!fighter.Attack.HasValue)return false;var move=fighter.Attack.Value;return fighter.Frame>=move.Startup-1&&fighter.Frame<move.Startup+move.Active;}
  private void Sample(Match value){if(!NeedsSweep(value.A)&&!NeedsSweep(value.B)){a.ready=b.ready=false;viewA.ClearContactPoseSample();viewB.ClearContactPoseSample();return;}SampleBoth(value);}
  private void SampleBoth(Match value){using(SampleMarker.Auto()){SampledPosePairs++;a.Sample(viewA,value.A,value.B);b.Sample(viewB,value.B,value.A);}}
  private bool Touches(Fighter attacker,Fighter defender)
  {
   var strike=attacker==match.A?a:b;var body=attacker==match.A?b:a;var id=attacker.Attack.Value.Id;
   int first=0,last=1;float radius=.095f;
   if(id.Contains("Kick")||id=="sweep"){first=2;last=3;radius=.11f;}
   else if(id=="elbow"){first=4;last=5;radius=.10f;}
   else if(id=="jab"||id=="hook"||id=="shortHook"){first=last=0;}
   else if(id=="cross"||id=="uppercut"||id=="backfist"){first=last=1;}
   if(id=="knee")return HitKnee(strike,body,attacker);
   int side=(attacker==match.A?viewA:viewB).NativeStrikeSide(id);if(side>0){first=(id.Contains("Kick")||id=="sweep"?2:id=="elbow"?4:0)+(side-1);last=first;}
   for(int i=first;i<=last;i++)if(Intersects(strike.before[i],strike.now[i],radius,body)){SetImpact(attacker,strike.now[i]);return true;}
   return false;
  }
  private bool HitKnee(Body strike,Body body,Fighter attacker)
  {if(Intersects(strike.before[6],strike.now[6],.12f,body)){SetImpact(attacker,strike.leftKnee);return true;}if(Intersects(strike.before[7],strike.now[7],.12f,body)){SetImpact(attacker,strike.rightKnee);return true;}return false;}
  private void SetImpact(Fighter attacker,Vector3 point){if(attacker==match.A)ImpactB=point;else ImpactA=point;}
  private static bool Intersects(Vector3 from,Vector3 to,float radius,Body target)
  {
   return Capsule(from,to,target.head,target.head,radius+.14f)||Capsule(from,to,target.hips,target.chest,radius+.20f)||
    Capsule(from,to,target.hips,target.leftKnee,radius+.115f)||Capsule(from,to,target.hips,target.rightKnee,radius+.115f)||
    Capsule(from,to,target.leftKnee,target.leftFoot,radius+.09f)||Capsule(from,to,target.rightKnee,target.rightFoot,radius+.09f);
  }
  private static bool Capsule(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float radius)=>HitGeometry.SegmentDistanceSquared(a.x,a.y,a.z,b.x,b.y,b.z,c.x,c.y,c.z,d.x,d.y,d.z)<=radius*radius;
 }
}
