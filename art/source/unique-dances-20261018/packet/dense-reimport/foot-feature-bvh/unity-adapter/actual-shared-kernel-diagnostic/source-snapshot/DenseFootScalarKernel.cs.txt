using System.Runtime.CompilerServices;
// REVIEW ONLY. Deliberate float ABI boundaries prevent a JIT from carrying a
// widened dot product directly into the weighted accumulation. These calls have
// a performance cost: measure in Unity before considering any production use.
public static class DenseFootScalarKernel {
 [MethodImpl(MethodImplOptions.NoInlining)]
 static float Multiply(float a,float b) { return a*b; }
 [MethodImpl(MethodImplOptions.NoInlining)]
 static float Add(float a,float b) { return a+b; }
 [MethodImpl(MethodImplOptions.NoInlining)]
 public static float Accumulate(float sum,float rx,float ry,float rz,float rt,float x,float y,float z,float weight) {
  float dot=Add(Add(Add(Multiply(rx,x),Multiply(ry,y)),Multiply(rz,z)),rt);
  return Add(sum,Multiply(dot,weight));
 }
}
