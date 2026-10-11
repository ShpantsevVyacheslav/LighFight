using System;
namespace LighFight.Core
{
 // Allocation-free segment distance used for swept striking limbs and body capsules.
 public static class HitGeometry
 {
  public static float SegmentDistanceSquared(float ax,float ay,float az,float bx,float by,float bz,float cx,float cy,float cz,float dx,float dy,float dz)
  {
   float ux=bx-ax,uy=by-ay,uz=bz-az,vx=dx-cx,vy=dy-cy,vz=dz-cz,wx=ax-cx,wy=ay-cy,wz=az-cz;
   float a=ux*ux+uy*uy+uz*uz,b=ux*vx+uy*vy+uz*vz,c=vx*vx+vy*vy+vz*vz,d=ux*wx+uy*wy+uz*wz,e=vx*wx+vy*wy+vz*wz;
   float s=0,t=0,den=a*c-b*b;
   if(a<1e-8f){t=c<1e-8f?0:Clamp(e/c);}
   else if(c<1e-8f){s=Clamp(-d/a);}
   else {s=den>1e-8f?Clamp((b*e-c*d)/den):0;t=(b*s+e)/c;if(t<0){t=0;s=Clamp(-d/a);}else if(t>1){t=1;s=Clamp((b-d)/a);}}
   float x=wx+s*ux-t*vx,y=wy+s*uy-t*vy,z=wz+s*uz-t*vz;return x*x+y*y+z*z;
  }
  private static float Clamp(float v)=>Math.Max(0,Math.Min(1,v));
 }
}
