#if UNITY_EDITOR
using System;
using UnityEngine;
// Invoked explicitly by the review job; synthetic edge checks are NOT actual-143k evidence.
public static class DenseFootAdapterChecks {
 public static int Run(){
  int assertions=0;var root=new GameObject("review typed-foot checks");
  try{
   var ground=root.AddComponent<CandidateFootGrounding>();ground.enabled=false;var rig=new GameObject("offset").transform;rig.SetParent(root.transform,false);var bone=new GameObject("bone").transform;bone.SetParent(rig,false);
   ground.nativeRig=rig;ground.nativeRest=Vector3.zero;ground.bones=new[]{bone};
   ground.shoePoints=new[]{Point(0,new Vector3(0,-.25f,0)),Point(0,new Vector3(1,.5f,0))};
   using(var adapter=new DenseFootFeatureAdapter(ground,2)){
    DenseFootFeatureAdapter.Sample sample;Require(!adapter.TryMeasure(false,out sample),ref assertions);Require(adapter.RowCaptures==0&&adapter.FeatureQueries==0&&adapter.FullQueries==0,ref assertions);
    Require(adapter.TryMeasure(true,out sample)&&sample.Minimum==-.25f,ref assertions);Require(adapter.FeatureQueries==1&&adapter.FullQueries==1,ref assertions);
    Require(adapter.TryMeasure(true,out sample)&&sample.Held&&sample.Minimum==-.25f,ref assertions);Require(adapter.FeatureQueries==1&&adapter.FullQueries==1&&adapter.HeldHits==1,ref assertions);
    bone.localPosition=Vector3.up;Require(adapter.TryMeasure(true,out sample)&&!sample.Held&&sample.Minimum==.75f,ref assertions);Require(adapter.FeatureQueries==2&&adapter.FullQueries==2,ref assertions);
    adapter.Invalidate();Require(adapter.TryMeasure(true,out sample)&&!sample.Held,ref assertions);
    // Inactive path must not reset the rig, read rows, or allocate, even after a transform change.
    rig.localPosition=new Vector3(0,3,0);int captures=adapter.RowCaptures,queries=adapter.FeatureQueries;long before=GC.GetAllocatedBytesForCurrentThread();
    for(int i=0;i<128;i++)if(adapter.TryMeasure(false,out sample))throw new Exception("Inactive query ran");
    long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Require(adapter.RowCaptures==captures&&adapter.FeatureQueries==queries&&rig.localPosition.y==3,ref assertions);
    // A caught parity failure must not make the next same-row query return stale success.
    var original=ground.shoePoints[0].influences[0];var changed=original;changed.bindPoint.y=float.NaN;ground.shoePoints[0].influences[0]=changed;adapter.Invalidate();
    for(int attempt=0;attempt<2;attempt++){bool failed=false;try{adapter.TryMeasure(true,out sample);}catch(InvalidOperationException){failed=true;}Require(failed,ref assertions);}
    ground.shoePoints[0].influences[0]=original;
    ground.shoePoints=new[]{Point(0,Vector3.zero)};bool rejected=false;try{adapter.TryMeasure(true,out sample);}catch(InvalidOperationException){rejected=true;}Require(rejected,ref assertions);
   }
   bool mismatch=false;try{using(var unused=new DenseFootFeatureAdapter(ground,143168)){} }catch(ArgumentException){mismatch=true;}Require(mismatch,ref assertions);
   return assertions;
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 static CandidateFootGrounding.Point Point(int bone,Vector3 p)=>new CandidateFootGrounding.Point{influences=new[]{new CandidateFootGrounding.Influence{bone=bone,weight=1,bindPoint=p}}};
 static void Require(bool pass,ref int count){count++;if(!pass)throw new Exception("Typed foot adapter synthetic check "+count+" failed");}
}
#endif
