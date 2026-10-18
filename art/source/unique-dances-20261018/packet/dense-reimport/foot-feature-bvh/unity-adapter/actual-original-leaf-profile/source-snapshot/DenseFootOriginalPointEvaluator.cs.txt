using System.Runtime.CompilerServices;
// REVIEW ONLY. Literal production CandidateFootGrounding point expression and
// original influence order. A single compiled boundary is shared by both paths.
// Matching production's separately compiled full loop still requires actual proof.
public static class DenseFootOriginalPointEvaluator {
 [MethodImpl(MethodImplOptions.NoInlining)]
 public static float Evaluate(CandidateFootGrounding.Point point,float[] rows){
  float y=0;
  foreach(var inf in point.influences){int b=inf.bone*4;var p=inf.bindPoint;
   y+=(rows[b]*p.x+rows[b+1]*p.y+rows[b+2]*p.z+rows[b+3])*inf.weight;
  }
  return y;
 }
}
