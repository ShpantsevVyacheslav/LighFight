using System;using SourceContact;using LazySourceContact;
namespace SourceResponse {
public enum RootOutcome {Accepted,StoppedBeforeContact,ConservativeStop,InvalidStart}
public struct RootDecision {public RootOutcome Outcome;public double AcceptedFraction;public V DeltaA,DeltaB;public SweepResult Evidence;}
public sealed class RootMotionResponse {
 readonly TranslationSweep sweep;public RootMotionResponse(int stackCapacity=128){sweep=new TranslationSweep(stackCapacity);}
 // Caller supplies current sampled pair observation. This remains a candidate surface response,
 // not certified solid safety: ProvisionalOutsideBoth explicitly carries that limitation.
 public RootDecision Resolve(LazySurface a,LazySurface b,PairInspection start,V desiredA,V desiredB,ref WorkBudget budget,int bisections=16){
 if(start!=PairInspection.ProvisionalOutsideBoth)return new RootDecision{Outcome=RootOutcome.InvalidStart};var initial=sweep.Query(a,b,new V(),new V(),ref budget);if(initial.Status!=SweepStatus.Clear)return new RootDecision{Outcome=initial.Status==SweepStatus.Contact?RootOutcome.InvalidStart:RootOutcome.ConservativeStop,Evidence=initial};var evidence=sweep.Query(a,b,desiredA,desiredB,ref budget);
 if(evidence.Status==SweepStatus.Indeterminate)return new RootDecision{Outcome=RootOutcome.ConservativeStop,Evidence=evidence};
 if(evidence.Status==SweepStatus.Clear)return new RootDecision{Outcome=RootOutcome.Accepted,AcceptedFraction=1,DeltaA=desiredA,DeltaB=desiredB,Evidence=evidence};
 double low=0,high=evidence.Time; // Only a continuously clear prefix can be accepted.
 for(int i=0;i<bisections&&high-low>1e-9;i++){double mid=(low+high)*.5;var prefix=sweep.Query(a,b,desiredA*mid,desiredB*mid,ref budget);if(prefix.Status==SweepStatus.Indeterminate)return new RootDecision{Outcome=RootOutcome.ConservativeStop,Evidence=prefix};if(prefix.Status==SweepStatus.Clear)low=mid;else high=mid;}
 return new RootDecision{Outcome=RootOutcome.StoppedBeforeContact,AcceptedFraction=low,DeltaA=desiredA*low,DeltaB=desiredB*low,Evidence=evidence};
 }
}
public enum ImpactAnatomy {LegalHeadOrTorso,PassiveLimb,Other}
public enum AttackState {Advancing,Held,Returning,Finished}
public enum PhaseInterval {ProvenClear,Contact,Indeterminate}
// A temporal oracle must cover the ENTIRE source-animation interval, including unchanged roots.
// Endpoint-only sampled meshes MUST NOT implement ProvenClear. Source interpolation bounds/CCD pending.
public interface ISourcePhaseOracle {PhaseInterval Inspect(double from,double to,out double firstContact,out ImpactAnatomy anatomy);}
public struct AttackDecision {public double Phase;public AttackState State;public bool EmitImpact,BeginReaction,ConservativeStop;public ImpactAnatomy Anatomy;}
public sealed class AttackResponse {
 public AttackState State{get;private set;}public double Phase{get;private set;}double heldSeconds,returnElapsed,returnStart;bool impactIssued;readonly double holdDuration,returnDuration;
 public AttackResponse(double phase=0,double holdSeconds=.035,double returnSeconds=.18){if(!Finite(phase)||phase<0||phase>1||!Finite(holdSeconds)||!Finite(returnSeconds)||holdSeconds<0||returnSeconds<=0)throw new ArgumentException();Phase=phase;holdDuration=holdSeconds;returnDuration=returnSeconds;State=AttackState.Advancing;}
 static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
 public AttackDecision Tick(double requestedPhase,double dt,bool legalCombatWindow,ISourcePhaseOracle oracle){
 if(!Finite(dt)||dt<0||!Finite(requestedPhase)||requestedPhase<0||requestedPhase>1||oracle==null)throw new ArgumentException();var result=new AttackDecision{Phase=Phase,State=State};
 if(State==AttackState.Finished)return result;
 if(State==AttackState.Held){heldSeconds+=dt;if(heldSeconds<holdDuration)return result;State=AttackState.Returning;returnStart=Phase;returnElapsed=0;}
 double next;if(State==AttackState.Returning){double elapsed=Math.Min(returnDuration,returnElapsed+dt),t=elapsed/returnDuration;next=returnStart*(1-t*t*(3-2*t)); // source phase only, no bone edits
 double first;ImpactAnatomy anatomy;var status=oracle.Inspect(Phase,next,out first,out anatomy);if(status!=PhaseInterval.ProvenClear){result.ConservativeStop=true;result.State=State;return result;}returnElapsed=elapsed;Phase=next;if(elapsed>=returnDuration)State=AttackState.Finished;
 }else{if(requestedPhase<Phase)throw new ArgumentException("Forward attack phases must be monotonic");double first;ImpactAnatomy anatomy;var status=oracle.Inspect(Phase,requestedPhase,out first,out anatomy);
 if(status==PhaseInterval.Indeterminate){result.ConservativeStop=true;return result;}
 if(status==PhaseInterval.Contact){if(!Finite(first)||first<Phase||first>requestedPhase){result.ConservativeStop=true;return result;} // first is LAST SAFE phase before the contact, qualified by oracle
 Phase=first;State=AttackState.Held;heldSeconds=0;result.Anatomy=anatomy;if(!impactIssued&&legalCombatWindow&&anatomy==ImpactAnatomy.LegalHeadOrTorso){result.EmitImpact=result.BeginReaction=true;impactIssued=true;}
 }else Phase=requestedPhase;
 }
 result.Phase=Phase;result.State=State;return result;
 }
}
}
