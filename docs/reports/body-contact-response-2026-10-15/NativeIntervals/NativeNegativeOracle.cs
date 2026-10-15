using SourceResponse;
namespace NativeInterval {
// Native mathematical negative certificate. NOT a Unity-evaluator equivalence certificate.
// It may allow truly separated intervals, never invents a hit from overlapping bounds.
public sealed class NativeNegativeOracle:ISourcePhaseOracle {
 readonly Clip clip;readonly int[] parents;readonly double[][] rest;readonly Mat root;readonly Context context;readonly IntervalSurface moving,target;readonly IntervalPair pair;readonly double start,end;readonly int workLimit;
 public NativeNegativeOracle(Clip source,int[] sourceParents,double[][] sourceRest,Mat fixedRoot,Context exactContext,IntervalSurface sourceSurface,IntervalSurface fixedTarget,double startSeconds,double endSeconds,int workBudget=100000,int stack=128){if(double.IsNaN(startSeconds)||double.IsNaN(endSeconds)||double.IsInfinity(startSeconds)||double.IsInfinity(endSeconds)||endSeconds<startSeconds)throw new System.ArgumentException("Finite original source seconds required");clip=source;parents=sourceParents;rest=sourceRest;root=fixedRoot;context=exactContext;moving=sourceSurface;target=fixedTarget;start=startSeconds;end=endSeconds;workLimit=workBudget;pair=new IntervalPair(stack);}
 public PhaseInterval Inspect(double from,double to,out double lastSafe,out ImpactAnatomy anatomy){lastSafe=from;anatomy=ImpactAnatomy.Other;if(double.IsNaN(from)||double.IsNaN(to)||from<0||from>1||to<0||to>1||end<start)return PhaseInterval.Indeterminate;double a=start+(end-start)*from,b=start+(end-start)*to;Mat[] world;if(!FK.Bound(clip,parents,rest,root,System.Math.Min(a,b),System.Math.Max(a,b),context,out world)||!moving.SetInterval(world))return PhaseInterval.Indeterminate;var budget=new LazySourceContact.WorkBudget(workLimit);if(pair.Inspect(moving,target,ref budget)==IntervalVerdict.ProvenSeparated){lastSafe=to;return PhaseInterval.ProvenClear;}return PhaseInterval.Indeterminate;}
}
}
