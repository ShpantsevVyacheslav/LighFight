#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
// Explicit review job only. Requires the REAL typed SourceBox assembler output, never reconstructs NPZ.
public static class DenseFootAdapterReview {
 const string Prefab="Assets/DenseRigifyReview/PlayableReview/SailorDenseBOXReview.prefab";
 const string Packet="art/source/unique-dances-20261018/packet/dense-reimport/foot-feature-bvh/unity-adapter/actual-original-leaf-validation";
 [Serializable]sealed class Report {
  public string status="not run",prefab=Prefab,scope="Unity Editor review only; no phone/player/thermal acceptance; profiler heap deltas are global, not isolated native allocations";
  public int originalPoints,sourceVertices,syntheticAssertions,poses,heldHits,inactiveQueries,featureQueries,fullQueries,nodes;
  public long meshRuntimeNativeBytes,supportRuntimeNativeBytes,referencedTextureRuntimeNativeBytes,assetLoadTicks,assetLoadAllocatedBytes,instantiateTicks,instantiateAllocatedBytes,compressedSupportBytes,warmupAllocatedBytes,warmupTicks,adapterBuildAllocatedBytes,adapterBuildTicks,warmQueryAllocatedBytes,heldAllocatedBytes,inactiveAllocatedBytes,globalProfilerBytesDelta;
  public double rowMedianMs,rowP95Ms,featureMedianMs,featureP95Ms,featureMaxMs,fullMedianMs,fullP95Ms,fullMaxMs;
  public float worstBakeSelectedError,worstSupportAboveWholeMeshFloor,maxSelectedWorldPositionError,maxFalseRigidWorldPositionError;
  public int selectedWorldPositionComparisons;public Vector3 rendererLossyScale;
  public string bakeContract="BakeMesh(useScale:true) + full renderer.localToWorldMatrix; false + rigidTRS independently checked once";
  public string failure;public string allocationQualification="UNSUPPORTED thread counter (known1MiB allocation reports0); byte fields are raw unusable readings, not zero-allocation acceptance";
 }
 static bool profiling;
 public static void RunProfile(){profiling=true;Run();}
 [MenuItem("LighFight/Review/Measure Actual Restored Foot Features")]
 public static void Run(){
  string project=Directory.GetParent(Application.dataPath).Parent.FullName;string output=Path.Combine(project,profiling?Packet.Replace("validation","profile"):Packet);Directory.CreateDirectory(output);
  var report=new Report();GameObject actor=null;DenseFootFeatureAdapter adapter=null;Mesh baked=null;
  var lines=new List<string>{"pose,rowsMs,featureMs,fullTypedMs,exactPoints,boundsTested,prunedPoints,queryManagedBytes,minimum"};
  try{
   report.syntheticAssertions=DenseFootAdapterChecks.Run();
   long assetBefore=GC.GetAllocatedBytesForCurrentThread(),assetStart=Stopwatch.GetTimestamp();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);report.assetLoadTicks=Stopwatch.GetTimestamp()-assetStart;report.assetLoadAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-assetBefore;if(!prefab)throw new InvalidOperationException("Actual typed-restored SourceBox prefab not built yet. Do not substitute the portable fixture or sparse support.");
   assetBefore=GC.GetAllocatedBytesForCurrentThread();assetStart=Stopwatch.GetTimestamp();actor=UnityEngine.Object.Instantiate(prefab);actor.SetActive(false);report.instantiateTicks=Stopwatch.GetTimestamp()-assetStart;report.instantiateAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-assetBefore;
   var owner=actor.GetComponent<DenseRigifySupportOwner>();if(!owner)throw new InvalidOperationException("Typed support owner missing");
   long memory=Profiler.GetTotalAllocatedMemoryLong(),before=GC.GetAllocatedBytesForCurrentThread(),started=Stopwatch.GetTimestamp();
   var warmup=owner.Warmup();try{while(warmup.MoveNext()){if(warmup.Current!=null)throw new InvalidOperationException("Unexpected nested loading yield");}}finally{(warmup as IDisposable)?.Dispose();}
   report.warmupTicks=Stopwatch.GetTimestamp()-started;report.warmupAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-before;
   if(!owner.IsWarm||owner.PointCount!=143168||owner.grounding.shoePoints==null||owner.grounding.shoePoints.Length!=143168)throw new InvalidOperationException("Actual full-point restoration incomplete");
   report.originalPoints=owner.PointCount;report.sourceVertices=owner.sourceVertices;report.compressedSupportBytes=new FileInfo(AssetDatabase.GetAssetPath(owner.supportBytes)).Length;
   adapter=new DenseFootFeatureAdapter(owner.grounding,143168);report.adapterBuildTicks=adapter.BuildTicks;report.adapterBuildAllocatedBytes=adapter.BuildAllocatedBytes;report.nodes=adapter.NodeCount;
   var animator=actor.GetComponentInChildren<Animator>(true);var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>(true);if(!animator||!skin)throw new InvalidOperationException("Actual restored rig/skin missing");
   report.meshRuntimeNativeBytes=Profiler.GetRuntimeMemorySizeLong(skin.sharedMesh);report.supportRuntimeNativeBytes=Profiler.GetRuntimeMemorySizeLong(owner.supportBytes);
   var textures=new HashSet<Texture>();foreach(var material in skin.sharedMaterials)if(material)foreach(string property in material.GetTexturePropertyNames()){var texture=material.GetTexture(property);if(texture)textures.Add(texture);}foreach(var texture in textures)report.referencedTextureRuntimeNativeBytes+=Profiler.GetRuntimeMemorySizeLong(texture);
   // No automatic animation, grounding or provider frame callbacks in this manual review.
   foreach(var behaviour in actor.GetComponentsInChildren<Behaviour>(true))behaviour.enabled=false;
   var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/DenseRigifyReview/PlayableReview/SourceGenericBOX.anim").OfType<AnimationClip>().ToArray();
   if(clips.Length!=1)throw new InvalidOperationException("Qualified source BOX clip required");var clip=clips[0];baked=new Mesh();
   var rows=new List<long>();var features=new List<long>();var full=new List<long>();DenseFootFeatureAdapter.Sample sample;
   for(int i=0;i<65;i++){
    clip.SampleAnimation(animator.gameObject,clip.length*i/64f);adapter.Invalidate();
    if(!adapter.TryMeasure(true,out sample))throw new Exception("Active sample missing");
    owner.grounding.InvalidateMinimumCache();float candidate=owner.grounding.MeasureMinimum();if(candidate!=sample.Minimum)throw new Exception("Existing CandidateFootGrounding full loop differs");
    if(i<8)continue; // warm JIT/runtime bindings; measured poses retain their true row capture cost
    rows.Add(sample.RowTicks);features.Add(sample.FeatureTicks);full.Add(sample.FullTicks);report.warmQueryAllocatedBytes+=sample.ManagedAllocatedBytes;report.poses++;
    lines.Add(string.Join(",",i,Ms(sample.RowTicks),Ms(sample.FeatureTicks),Ms(sample.FullTicks),sample.Statistics.ExactPoints,sample.Statistics.BoundsTested,sample.Statistics.PrunedPoints,sample.ManagedAllocatedBytes,sample.Minimum.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
    if(i%16==0){
     skin.BakeMesh(baked,true);var vertices=baked.vertices;report.rendererLossyScale=skin.transform.lossyScale;
     float positionError=CompareAllSelectedWorldPositions(owner,vertices,skin.transform.localToWorldMatrix);
     report.maxSelectedWorldPositionError=Mathf.Max(report.maxSelectedWorldPositionError,positionError);report.selectedWorldPositionComparisons+=owner.PointCount;
     if(positionError>.0001f)throw new Exception("All selected full typed/BakeMesh true+full world positions differ (>0.1mm)");
     float selected=float.PositiveInfinity,whole=float.PositiveInfinity;
     foreach(var p in vertices)whole=Mathf.Min(whole,skin.transform.TransformPoint(p).y);
     foreach(int n in owner.ImportedFootIndices){if(n<0||n>=vertices.Length)throw new Exception("Original imported support index invalid");selected=Mathf.Min(selected,skin.transform.TransformPoint(vertices[n]).y);}
     report.worstBakeSelectedError=Mathf.Max(report.worstBakeSelectedError,Mathf.Abs(selected-sample.Minimum));report.worstSupportAboveWholeMeshFloor=Mathf.Max(report.worstSupportAboveWholeMeshFloor,sample.Minimum-whole);
     if(Mathf.Abs(selected-sample.Minimum)>.0001f)throw new Exception("BakeMesh original selected-shoe floor mismatch (>0.1mm)");
     if(i==16){skin.BakeMesh(baked,false);float rigidError=CompareAllSelectedWorldPositions(owner,baked.vertices,Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one));report.maxFalseRigidWorldPositionError=rigidError;if(rigidError>.0001f)throw new Exception("BakeMesh false+rigidTRS independent coordinate contract differs (>0.1mm)");}
    }
   }
   report.rowMedianMs=Percentile(rows,.5);report.rowP95Ms=Percentile(rows,.95);report.featureMedianMs=Percentile(features,.5);report.featureP95Ms=Percentile(features,.95);report.featureMaxMs=Percentile(features,1);report.fullMedianMs=Percentile(full,.5);report.fullP95Ms=Percentile(full,.95);report.fullMaxMs=Percentile(full,1);
   // Warm held path before measuring; no forced invalidation or transforms changed.
   adapter.TryMeasure(true,out sample);int featureCount=adapter.FeatureQueries,fullCount=adapter.FullQueries;
   before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<128;i++){if(!adapter.TryMeasure(true,out sample)||!sample.Held)throw new Exception("Held pose cache missed");}report.heldAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-before;
   if(featureCount!=adapter.FeatureQueries||fullCount!=adapter.FullQueries)throw new Exception("Held pose performed a full/BVH scan");report.heldHits=adapter.HeldHits;
   int captures=adapter.RowCaptures;before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<128;i++){if(adapter.TryMeasure(false,out sample))throw new Exception("Inactive work executed");}report.inactiveAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-before;report.inactiveQueries=128;
   if(captures!=adapter.RowCaptures||featureCount!=adapter.FeatureQueries||fullCount!=adapter.FullQueries)throw new Exception("Inactive path captured rows or scanned");
   report.featureQueries=adapter.FeatureQueries;report.fullQueries=adapter.FullQueries;report.globalProfilerBytesDelta=Profiler.GetTotalAllocatedMemoryLong()-memory;
   // Allocation counter failed calibration: do not accept/reject allocation based on it.
   report.status="PASS actual restored scalar/Bake/cache validation; Editor only; no allocation acceptance";
  }catch(Exception e){report.status="FAILED/NOT ACCEPTED";report.failure=e.ToString();throw;}
  finally{adapter?.Dispose();if(baked)UnityEngine.Object.DestroyImmediate(baked);if(actor)UnityEngine.Object.DestroyImmediate(actor);File.WriteAllLines(Path.Combine(output,"queries.csv"),lines);File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));}
  UnityEngine.Debug.Log("AGENT_RESULT DenseFootAdapterReview "+report.status);
 }
 // Independent 3D original-influence LBS, not adapter/tree/Y-min arithmetic.
 static float CompareAllSelectedWorldPositions(DenseRigifySupportOwner owner,Vector3[] baked,Matrix4x4 bakedToWorld){
  var points=owner.grounding.shoePoints;var indices=owner.ImportedFootIndices;if(points.Length!=143168||indices==null||indices.Length!=points.Length)throw new Exception("Actual selected point/index count differs");
  var matrices=new Matrix4x4[owner.grounding.bones.Length];for(int b=0;b<matrices.Length;b++)matrices[b]=owner.grounding.bones[b].localToWorldMatrix;
  float max=0;for(int p=0;p<points.Length;p++){
   if(indices[p]<0||indices[p]>=baked.Length)throw new Exception("Original full-foot index outside restored mesh");Vector3 typed=Vector3.zero;
   foreach(var influence in points[p].influences)typed+=matrices[influence.bone].MultiplyPoint3x4(influence.bindPoint)*influence.weight;
   var actual=bakedToWorld.MultiplyPoint3x4(baked[indices[p]]);if(!FiniteVector(actual)||!FiniteVector(typed))throw new Exception("Nonfinite baked selected position");max=Mathf.Max(max,Vector3.Distance(typed,actual));
  }return max;
 }
 static bool FiniteVector(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
 static double Ms(long ticks)=>ticks*1000d/Stopwatch.Frequency;
 static double Percentile(List<long> values,double q){values.Sort();return Ms(values[Math.Max(0,(int)Math.Ceiling(values.Count*q)-1)]);}
}
#endif
