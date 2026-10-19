using System;
using UnityEngine;
// Review-only original-weight steady Dance. Finite4759 poses, not continuous/entry/return coverage.
public sealed class DenseFiniteDanceBounds : MonoBehaviour
{
 public enum FloorEnvelope { Ungrounded, UpOnly, Signed }
 public DenseAdaptivePoseBridge bridge;
 public FloorEnvelope floorEnvelope;
 [Min(0)] public float upwardCap=.2f;
 [Min(0)] public float downwardCap=.2f;
 public const string SourceKeysSHA256="296ea14ea4c70fc41d20f1e013d43cebd635daff06b61d3f881be2cf31ac67e4";
 public const string OriginalMeshSHA256="83c3bf5070bd8e73e755c659789a29beebc997e765fdd4a9575a3ebd13c6b437";
 public const int FinitePoses=4759;
 // Output already C=ReflectX*B; applying C again is incorrect. K_i is fixed bind-only gauge.
 public Bounds NativeBounds {
  get {
   // Outward-rounded float limits retain the measured .025m native camera margin.
   var lo=new Vector3(-.995787f,-.042100f,-.648709f);
   var hi=new Vector3(.931090f,1.255259f,.829446f);
   if(floorEnvelope!=FloorEnvelope.Ungrounded)hi.y+=Mathf.Min(ValidCap(upwardCap),.017100f);
   if(floorEnvelope==FloorEnvelope.Signed)lo.y-=Mathf.Min(ValidCap(downwardCap),.016214f);
   var b=new Bounds();b.SetMinMax(lo,hi);return b;
  }
 }
 static float ValidCap(float v){if(float.IsNaN(v)||float.IsInfinity(v)||v<0)throw new InvalidOperationException("Finite nonnegative native cap required");return v;}
 // Returned camera-space AABB can fit the full landscape canvas; no portrait-cell assumptions.
 public bool TryGetBounds(Matrix4x4 worldToSpace,out Bounds bounds) {
  bounds=default(Bounds);if(!bridge||!bridge.actorAnchor)return false;
  bounds=TransformCorners(NativeBounds,worldToSpace*bridge.actorAnchor.localToWorldMatrix);return true;
 }
 public static Bounds TransformCorners(Bounds native,Matrix4x4 matrix) {
  var lo=native.min;var hi=native.max;var result=new Bounds();
  for(int i=0;i<8;i++){var p=matrix.MultiplyPoint3x4(new Vector3((i&1)==0?lo.x:hi.x,(i&2)==0?lo.y:hi.y,(i&4)==0?lo.z:hi.z));if(!Finite(p))throw new InvalidOperationException("Nonfinite actor transform");if(i==0)result=new Bounds(p,Vector3.zero);else result.Encapsulate(p);}return result;
 }
 static bool Finite(Vector3 p){return !(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z));}
 public static float FullCanvasOrthographicSize(Bounds cameraSpace,float aspect,float extraFraction=.02f) {
  if(!(aspect>0)||float.IsInfinity(aspect)||extraFraction<0||float.IsNaN(extraFraction)||float.IsInfinity(extraFraction))throw new ArgumentException("Finite canvas fit");
  return Mathf.Max(cameraSpace.extents.y,cameraSpace.extents.x/aspect)*(1+extraFraction);
 }
}
