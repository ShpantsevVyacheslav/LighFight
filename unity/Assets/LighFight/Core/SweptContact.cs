using System;
namespace LighFight.Core {
 public struct ContactVector {
  public float X,Y,Z;public ContactVector(float x,float y,float z){X=x;Y=y;Z=z;}
  public static ContactVector operator +(ContactVector a,ContactVector b)=>new ContactVector(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
  public static ContactVector operator -(ContactVector a,ContactVector b)=>new ContactVector(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
  public static ContactVector operator *(ContactVector a,float t)=>new ContactVector(a.X*t,a.Y*t,a.Z*t);
  public float Length=>(float)Math.Sqrt(X*X+Y*Y+Z*Z);
 }
 public struct ContactSweep {public bool Hit,Indeterminate;public float Time;}
 // Continuous for linearly moving capsule axes, not a proof for nonlinear skinned mesh surfaces.
 public static class SweptContact {
  static ContactVector At(ContactVector a,ContactVector b,float t)=>a+(b-a)*t;
  static bool Finite(ContactVector p)=>!(float.IsNaN(p.X)||float.IsNaN(p.Y)||float.IsNaN(p.Z)||float.IsInfinity(p.X)||float.IsInfinity(p.Y)||float.IsInfinity(p.Z));
  public static ContactSweep Cast(ContactVector a0,ContactVector b0,ContactVector a1,ContactVector b1,ContactVector c0,ContactVector d0,ContactVector c1,ContactVector d1,float radius,int iterations=64){
   if(!Finite(a0)||!Finite(b0)||!Finite(a1)||!Finite(b1)||!Finite(c0)||!Finite(d0)||!Finite(c1)||!Finite(d1)||float.IsNaN(radius)||float.IsInfinity(radius)||radius<0)return new ContactSweep{Indeterminate=true};
   float speed=Math.Max((a1-a0).Length,(b1-b0).Length)+Math.Max((c1-c0).Length,(d1-d0).Length),time=0;
   for(int i=0;i<iterations;i++){var a=At(a0,a1,time);var b=At(b0,b1,time);var c=At(c0,c1,time);var d=At(d0,d1,time);float distance=(float)Math.Sqrt(HitGeometry.SegmentDistanceSquared(a.X,a.Y,a.Z,b.X,b.Y,b.Z,c.X,c.Y,c.Z,d.X,d.Y,d.Z));
    // 10 micrometre solver convergence tolerance, not a gameplay reach/radius adjustment.
    if(distance<=radius+.00001f)return new ContactSweep{Hit=true,Time=time};
    if(speed<.0000001f)return new ContactSweep{Time=1};float advance=(distance-radius)/speed;
    if(advance>1-time)return new ContactSweep{Time=1};if(advance<=0||time+advance<=time)return new ContactSweep{Indeterminate=true,Time=time};time=Math.Min(1,time+advance);
   }
   return new ContactSweep{Indeterminate=true,Time=time};
  }
 }
}
