using System;using System.IO;using SourceContact;using LazySourceContact;using LighFight.Core;
namespace SourceContactReview {
 // Attach AFTER production PhysicalContacts installs its sampler. Observe it; do not replace validation or damage.
 public sealed class SourceSurfaceReviewTap:IDisposable {
  readonly SourceSurfaceReviewAdapter a,b;readonly CandidatePairInspection inspection=new CandidatePairInspection();readonly Match match;readonly Action<Match> callback;
  public Verdict LastStatus{get;private set;}public PairInspection LastPairInspection{get;private set;}public PairEvidence LastPairEvidence{get;private set;}public Witness LastWitness{get;private set;}public Exception Failure{get;private set;}public int Samples{get;private set;}public int Work{get;private set;}public int AttackFrameA{get;private set;}public int AttackFrameB{get;private set;}
  public SourceSurfaceReviewTap(Match simulation,SourceSurfaceReviewAdapter actorA,SourceSurfaceReviewAdapter actorB){if(simulation==null||simulation.BeforeContact==null||!actorA||!actorB||!actorA.IsWarm||!actorB.IsWarm)throw new InvalidOperationException("Install production pose sampler and await both actor warmups first");match=simulation;a=actorA;b=actorB;callback=Observe;match.BeforeContact+=callback;}
  void Observe(Match value){try{a.CaptureSampledPose();b.CaptureSampledPose();var budget=new WorkBudget(100000);PairEvidence evidence;LastPairInspection=inspection.Inspect(a.Surface,b.Surface,a.MainTopology,b.MainTopology,ref budget,out evidence);LastPairEvidence=evidence;LastStatus=LastPairInspection==PairInspection.SurfaceContact?Verdict.Contact:LastPairInspection==PairInspection.Indeterminate?Verdict.Indeterminate:Verdict.Clear;LastWitness=evidence.SurfaceWitness;Work=budget.Spent;AttackFrameA=value.A.Frame;AttackFrameB=value.B.Frame;Samples++;}catch(Exception error){Failure=error;LastStatus=Verdict.Indeterminate;LastPairInspection=PairInspection.Indeterminate;}}
  // QA-only file output. The caller chooses paths and emits these AFTER its bounded scenario reaches the requested frame.
  public void WriteContactPair(string path){using(var writer=new BinaryWriter(File.Create(path))){a.WriteCapturedPose(writer);b.WriteCapturedPose(writer);}}
  public void WriteLivePair(string path){using(var writer=new BinaryWriter(File.Create(path))){a.WriteLivePose(writer);b.WriteLivePose(writer);}}
  public void WriteBakedPair(string path){using(var writer=new BinaryWriter(File.Create(path))){a.WriteBakedWorldVertices(writer);b.WriteBakedWorldVertices(writer);}}
  public void Dispose(){match.BeforeContact-=callback;}
 }
}
