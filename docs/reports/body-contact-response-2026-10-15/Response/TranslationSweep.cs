using System;using SourceContact;using LazySourceContact;
namespace SourceResponse {
public enum SweepStatus {Clear,Contact,Indeterminate}
public struct SweepResult {public SweepStatus Status;public double Time;public int FaceA,FaceB;}
// Exact original sampled triangles under constant linear ROOT TRANSLATION only.
// Does not interpolate rotations, skin matrices, source animation or certify solid regularity.
public sealed class TranslationSweep {
 readonly int[] left,right;readonly V[] axes=new V[17];
 public TranslationSweep(int stackCapacity){left=new int[stackCapacity];right=new int[stackCapacity];}
 static bool Finite(V x)=>!double.IsNaN(x.X)&&!double.IsNaN(x.Y)&&!double.IsNaN(x.Z)&&!double.IsInfinity(x.X)&&!double.IsInfinity(x.Y)&&!double.IsInfinity(x.Z);
 static double Size(V a,V b){var d=b-a;return V.Dot(d,d);}
 static bool Overlap(V al,V ah,V bl,V bh)=>al.X<=bh.X&&ah.X>=bl.X&&al.Y<=bh.Y&&ah.Y>=bl.Y&&al.Z<=bh.Z&&ah.Z>=bl.Z;
 static void Swept(ref V lo,ref V hi,V delta,double limit){var shift=delta*limit;lo=V.Min(lo,lo+shift);hi=V.Max(hi,hi+shift);}
 // Continuous separating-axis test for two convex triangles. Numerical uncertainty stops conservatively.
 public SweepStatus Triangles(V a,V b,V c,V d,V e,V f,V delta,out double time){
 time=0;if(!Finite(a)||!Finite(b)||!Finite(c)||!Finite(d)||!Finite(e)||!Finite(f)||!Finite(delta))return SweepStatus.Indeterminate;
 var ab=b-a;var bc=c-b;var ca=a-c;var de=e-d;var ef=f-e;var fd=d-f;var na=V.Cross(ab,bc);var nb=V.Cross(de,ef);double la=Math.Sqrt(V.Dot(na,na)),lb=Math.Sqrt(V.Dot(nb,nb));if(la<1e-14||lb<1e-14)return SweepStatus.Indeterminate;
 axes[0]=na;axes[1]=nb;axes[2]=V.Cross(ab,de);axes[3]=V.Cross(ab,ef);axes[4]=V.Cross(ab,fd);axes[5]=V.Cross(bc,de);axes[6]=V.Cross(bc,ef);axes[7]=V.Cross(bc,fd);axes[8]=V.Cross(ca,de);axes[9]=V.Cross(ca,ef);axes[10]=V.Cross(ca,fd);
 // These extra valid axes also handle coplanar triangles (normal/edge axes alone collapse).
 axes[11]=V.Cross(na,ab);axes[12]=V.Cross(na,bc);axes[13]=V.Cross(na,ca);axes[14]=V.Cross(nb,de);axes[15]=V.Cross(nb,ef);axes[16]=V.Cross(nb,fd);
 double enter=0,exit=1;bool uncertain=false;
 for(int i=0;i<axes.Length;i++){var n=axes[i];double length=Math.Sqrt(V.Dot(n,n));if(length<1e-18)continue;n=n*(1/length);double pa=V.Dot(a,n),pb=V.Dot(b,n),pc=V.Dot(c,n),pd=V.Dot(d,n),pe=V.Dot(e,n),pf=V.Dot(f,n);double amin=Math.Min(pa,Math.Min(pb,pc)),amax=Math.Max(pa,Math.Max(pb,pc)),bmin=Math.Min(pd,Math.Min(pe,pf)),bmax=Math.Max(pd,Math.Max(pe,pf)),speed=V.Dot(delta,n);
 const double roundoff=1e-10; // pruning tolerance only; cannot authorize damage
 if(Math.Abs(speed)<1e-15){double gap=Math.Max(bmin-amax,amin-bmax);if(gap>roundoff)return SweepStatus.Clear;if(gap>0)uncertain=true;continue;}
 double t0=(bmin-amax)/speed,t1=(bmax-amin)/speed;if(t0>t1){double swap=t0;t0=t1;t1=swap;}enter=Math.Max(enter,t0);exit=Math.Min(exit,t1);if(enter>exit+roundoff)return SweepStatus.Clear;if(enter>exit)uncertain=true;
 }
 time=Math.Max(0,Math.Min(1,enter));return uncertain?SweepStatus.Indeterminate:SweepStatus.Contact;
 }
 bool Push(ref int top,int a,int b){if(top==left.Length)return false;left[top]=a;right[top++]=b;return true;}
 public SweepResult Query(LazySurface a,LazySurface b,V deltaA,V deltaB,ref WorkBudget budget){
 var result=new SweepResult{Status=SweepStatus.Clear,Time=1,FaceA=-1,FaceB=-1};if(!Finite(deltaA)||!Finite(deltaB))return new SweepResult{Status=SweepStatus.Indeterminate};var delta=deltaA-deltaB;int top=0;if(!Push(ref top,0,0))return new SweepResult{Status=SweepStatus.Indeterminate};bool uncertain=false;
 while(top>0){if(!budget.Take(1))return new SweepResult{Status=SweepStatus.Indeterminate};int x=left[--top],y=right[top];V al,ah,bl,bh;if(!a.Bound(x,ref budget,out al,out ah)||!b.Bound(y,ref budget,out bl,out bh))return new SweepResult{Status=SweepStatus.Indeterminate};Swept(ref al,ref ah,delta,result.Time);if(!Overlap(al,ah,bl,bh))continue;var p=a.Nodes[x];var q=b.Nodes[y];
 if(p.Count>0){if(!a.ExactLeaf(x,ref budget,out al,out ah))return new SweepResult{Status=SweepStatus.Indeterminate};Swept(ref al,ref ah,delta,result.Time);if(!Overlap(al,ah,bl,bh))continue;}if(q.Count>0){if(!b.ExactLeaf(y,ref budget,out bl,out bh))return new SweepResult{Status=SweepStatus.Indeterminate};if(!Overlap(al,ah,bl,bh))continue;}
 if(p.Count>0&&q.Count>0){for(int i=p.Start;i<p.Start+p.Count;i++)for(int j=q.Start;j<q.Start+q.Count;j++){if(!budget.Take(1))return new SweepResult{Status=SweepStatus.Indeterminate};int ta=a.Order[i],tb=b.Order[j];double time;var status=Triangles(a.Vertex(ta,0),a.Vertex(ta,1),a.Vertex(ta,2),b.Vertex(tb,0),b.Vertex(tb,1),b.Vertex(tb,2),delta,out time);if(status==SweepStatus.Indeterminate)uncertain=true;else if(status==SweepStatus.Contact&&time<=result.Time){result=new SweepResult{Status=SweepStatus.Contact,Time=time,FaceA=ta,FaceB=tb};if(time==0)return result;}}}
 else if(p.Count==0&&(q.Count>0||Size(al,ah)>=Size(bl,bh))){if(!Push(ref top,p.Left,y)||!Push(ref top,p.Right,y))return new SweepResult{Status=SweepStatus.Indeterminate};}else if(!Push(ref top,x,q.Left)||!Push(ref top,x,q.Right))return new SweepResult{Status=SweepStatus.Indeterminate};
 }
 if(uncertain)return new SweepResult{Status=SweepStatus.Indeterminate};return result;
 }
}
}
