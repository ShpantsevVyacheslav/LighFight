using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace LighFight.Tests {
 public sealed class GroundingPerformanceTests {
  GameObject actor;CandidateFootGrounding g;Transform joint;
  [SetUp]public void Setup(){actor=new GameObject("grounding-cache-test");var rig=new GameObject("rig").transform;rig.SetParent(actor.transform,false);joint=new GameObject("joint").transform;joint.SetParent(rig,false);g=actor.AddComponent<CandidateFootGrounding>();g.nativeRig=rig;g.nativeRest=Vector3.zero;g.bones=new[]{joint};g.shoePoints=new[]{Point(new Vector3(0,-.2f,0)),Point(new Vector3(.4f,.3f,-.2f))};g.maxCorrection=.5f;}
  static CandidateFootGrounding.Point Point(Vector3 p)=>new CandidateFootGrounding.Point{influences=new[]{new CandidateFootGrounding.Influence{bone=0,bindPoint=p,weight=1}}};
  [TearDown]public void Teardown(){UnityEngine.Object.DestroyImmediate(actor);}
  static int Evaluations(CandidateFootGrounding value){var p=typeof(CandidateFootGrounding).GetProperty("MinimumEvaluationCount");Assert.NotNull(p,"Ground support must expose diagnostic scan count to prove duplicate scans are eliminated");return (int)p.GetValue(value);}
  [Test]public void RepeatedIdenticalPoseScansSupportOnce(){g.ApplyForReview();int count=Evaluations(g);g.ApplyForReview();g.ApplyForReview();Assert.AreEqual(count,Evaluations(g));Assert.AreEqual(.2f,g.LastCorrection,.00001f);}
  [Test]public void BoneAndSupportSetChangesInvalidateCachedMinimum(){g.ApplyForReview();int count=Evaluations(g);joint.localPosition=Vector3.up*.1f;g.ApplyForReview();Assert.AreEqual(count+1,Evaluations(g));Assert.AreEqual(.1f,g.LastCorrection,.00001f);g.shoePoints=new[]{Point(new Vector3(0,-.4f,0))};g.ApplyForReview();Assert.AreEqual(count+2,Evaluations(g));Assert.AreEqual(.3f,g.LastCorrection,.00001f);}
  [Test]public void CorrectionLimitChangesUseSameRawFloorMeasurement(){g.ApplyForReview();int count=Evaluations(g);g.maxCorrection=.05f;g.ApplyForReview();Assert.AreEqual(count,Evaluations(g));Assert.AreEqual(.05f,g.LastCorrection,.00001f);Assert.AreEqual(-.2f,g.UncorrectedMinimum,.00001f);}
  [Test]public void WorldTransformChangesInvalidateCachedFloor(){g.ApplyForReview();int count=Evaluations(g);actor.transform.position=Vector3.up*.15f;g.ApplyForReview();Assert.AreEqual(count+1,Evaluations(g));Assert.AreEqual(.05f,g.LastCorrection,.00001f);}
  [Test]public void FloorMinimumMatchesOriginalWeightedXYZUnderTransforms(){
   var second=new GameObject("second").transform;second.SetParent(g.nativeRig,false);g.bones=new[]{joint,second};var random=new System.Random(281);
   Func<float> next=()=>(float)random.NextDouble();var points=new CandidateFootGrounding.Point[123];
   for(int n=0;n<points.Length;n++){float w=next();points[n]=new CandidateFootGrounding.Point{influences=new[]{new CandidateFootGrounding.Influence{bone=0,bindPoint=new Vector3(next(),next()-.8f,next()),weight=w},new CandidateFootGrounding.Influence{bone=1,bindPoint=new Vector3(next(),next()-.8f,next()),weight=1-w}}};}g.shoePoints=points;
   for(int phase=0;phase<80;phase++){
    actor.transform.position=new Vector3(next(),next()-.4f,next());joint.localRotation=Quaternion.Euler(next()*200,next()*200,next()*200);second.localPosition=new Vector3(next(),next(),next());second.localScale=Vector3.one*(.2f+next());
    g.ResetForSample();float minimum=float.PositiveInfinity;foreach(var point in points){Vector3 weighted=Vector3.zero;foreach(var influence in point.influences)weighted+=g.bones[influence.bone].localToWorldMatrix.MultiplyPoint3x4(influence.bindPoint)*influence.weight;minimum=Mathf.Min(minimum,weighted.y);}
    g.ApplyForReview();Assert.AreEqual(minimum,g.UncorrectedMinimum,.000001f,"Original weighted floor changed at pose "+phase);Assert.AreEqual(Mathf.Clamp(-minimum,0,g.maxCorrection),g.LastCorrection,.000001f);
   }
  }
 }
}
