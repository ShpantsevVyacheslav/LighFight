using System;
using System.IO;

namespace DenseRigifyReview.Adaptive
{
 public sealed class AdaptiveDanceData
 {
  internal readonly int[] parents;internal readonly double[] rest,armature,mesh,times,trs;
  readonly string[] names;
  public int BoneCount {get{return parents.Length;}}public int KeyCount{get{return times.Length;}}public double Duration{get;private set;}
  public string BoneName(int i){return names[i];}public int Parent(int i){return parents[i];}
  public static AdaptiveDanceData Read(Stream stream)
  {
   using(var reader=new BinaryReader(stream,System.Text.Encoding.UTF8,true))
   {
    if(reader.ReadUInt32()!=0x3141444e||reader.ReadInt32()!=1)throw new InvalidDataException("NDA1/version");
    int keys=reader.ReadInt32(),bones=reader.ReadInt32();double duration=reader.ReadDouble();if(keys<2||keys>100000||bones<1||bones>1024)throw new InvalidDataException("Bounded shape");
    var names=new string[bones];for(int i=0;i<bones;i++){int bytes=reader.ReadInt32();if(bytes<1||bytes>512)throw new InvalidDataException("Bone name");names[i]=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(bytes));}
    var parents=new int[bones];for(int i=0;i<bones;i++)parents[i]=reader.ReadInt32();var rest=ReadD(reader,bones*16);var armature=ReadD(reader,16);var mesh=ReadD(reader,16);var times=ReadD(reader,keys);var trs=ReadD(reader,checked(keys*bones*10));
    if(stream.CanSeek&&stream.Position!=stream.Length)throw new InvalidDataException("Trailing key bytes");return new AdaptiveDanceData(names,parents,rest,armature,mesh,times,trs,duration);
   }
  }
  static double[] ReadD(BinaryReader r,int count){var a=new double[count];for(int i=0;i<count;i++)a[i]=r.ReadDouble();return a;}
  public AdaptiveDanceData(string[] names,int[] parents,double[] rest,double[] armature,double[] mesh,double[] times,double[] trs,double duration)
  {
   if(names==null||parents==null||names.Length!=parents.Length||rest==null||rest.Length!=parents.Length*16||armature==null||armature.Length!=16||mesh==null||mesh.Length!=16||times==null||times.Length<2||trs==null||trs.Length!=times.Length*parents.Length*10||!MathD.Finite(duration)||duration<=0)throw new ArgumentException("Complete source-key shape required");
   for(int i=0;i<parents.Length;i++)if(parents[i]<-1||parents[i]>=i||string.IsNullOrEmpty(names[i]))throw new ArgumentException("Parent-first FULL hierarchy required");
   if(times[0]!=0||Math.Abs(times[times.Length-1]-duration)>1e-12)throw new ArgumentException("Full author endpoints required");
   for(int i=0;i<times.Length;i++)if(!MathD.Finite(times[i])||(i>0&&times[i]<=times[i-1]))throw new ArgumentException("Strict source time order");
   foreach(var a in new[]{rest,armature,mesh,trs})for(int i=0;i<a.Length;i++)if(!MathD.Finite(a[i]))throw new ArgumentException("Nonfinite source");
   for(int i=0;i<trs.Length;i+=10){double norm=0;for(int j=3;j<7;j++)norm+=trs[i+j]*trs[i+j];if(norm<=1e-24)throw new ArgumentException("Zero source quaternion");}
   // Data fields are internal read-only-by-contract. Evaluation never rewrites source arrays.
   this.names=names;this.parents=parents;this.rest=rest;this.armature=armature;this.mesh=mesh;this.times=times;this.trs=trs;Duration=duration;
  }
 }
 public sealed class AdaptiveDanceEvaluator
 {
  readonly AdaptiveDanceData data;readonly double[] restLocal,skinBind,armaturePose,basis=new double[16],intermediate=new double[16];
  // Exposed arrays are read-only-by-contract, updated in place; one evaluator per actor/thread.
  public readonly double[] WorldPose,WorldSkin;
  public AdaptiveDanceEvaluator(AdaptiveDanceData data)
  {
   this.data=data;int n=data.BoneCount;restLocal=new double[n*16];skinBind=new double[n*16];armaturePose=new double[n*16];WorldPose=new double[n*16];WorldSkin=new double[n*16];var inverseRest=new double[n*16];var inverseArmature=new double[16];MathD.Inverse(data.armature,0,inverseArmature,0);
   for(int i=0;i<n;i++)MathD.Inverse(data.rest,i*16,inverseRest,i*16);
   for(int i=0;i<n;i++)
   {
    int p=data.parents[i];if(p<0)Array.Copy(data.rest,i*16,restLocal,i*16,16);else MathD.Multiply(inverseRest,p*16,data.rest,i*16,restLocal,i*16);
    MathD.Multiply(inverseRest,i*16,inverseArmature,0,intermediate,0);MathD.Multiply(intermediate,0,data.mesh,0,skinBind,i*16);
   }
  }
  public void Evaluate(double seconds)
  {
   if(!MathD.Finite(seconds))throw new ArgumentException("Nonfinite time, no pose claim");
   int a,b;double u;
   if(seconds<=0){a=b=0;u=0;}else if(seconds>=data.Duration){a=b=data.KeyCount-1;u=0;}
   else {int lo=0,hi=data.KeyCount-1;while(hi-lo>1){int m=(lo+hi)/2;if(data.times[m]<=seconds)lo=m;else hi=m;}a=lo;b=hi;u=(seconds-data.times[a])/(data.times[b]-data.times[a]);}
   int n=data.BoneCount;
   for(int i=0;i<n;i++)
   {
    int ai=(a*n+i)*10,bi=(b*n+i)*10;ComposeInterpolated(data.trs,ai,bi,u,basis);
    MathD.Multiply(restLocal,i*16,basis,0,intermediate,0);
    if(data.parents[i]<0)Array.Copy(intermediate,0,armaturePose,i*16,16);else MathD.Multiply(armaturePose,data.parents[i]*16,intermediate,0,armaturePose,i*16);
    MathD.Multiply(data.armature,0,armaturePose,i*16,WorldPose,i*16);MathD.Multiply(WorldPose,i*16,skinBind,i*16,WorldSkin,i*16);
   }
  }
  static void ComposeInterpolated(double[] trs,int a,int b,double u,double[] m)
  {
   double aw=trs[a+3],ax=trs[a+4],ay=trs[a+5],az=trs[a+6],bw=trs[b+3],bx=trs[b+4],by=trs[b+5],bz=trs[b+6];
   double an=Math.Sqrt(aw*aw+ax*ax+ay*ay+az*az),bn=Math.Sqrt(bw*bw+bx*bx+by*by+bz*bz);aw/=an;ax/=an;ay/=an;az/=an;bw/=bn;bx/=bn;by/=bn;bz/=bn;
   if(aw*bw+ax*bx+ay*by+az*bz<0){bw=-bw;bx=-bx;by=-by;bz=-bz;}
   double w=aw*(1-u)+bw*u,x=ax*(1-u)+bx*u,y=ay*(1-u)+by*u,z=az*(1-u)+bz*u;
   double norm=Math.Sqrt(w*w+x*x+y*y+z*z);if(norm<=1e-12)throw new ArithmeticException("Degenerate normalized-linear quaternion");w/=norm;x/=norm;y/=norm;z/=norm;
   // curve_core.compose normalizes again, even after interpolate normalized the quaternion.
   norm=Math.Sqrt(w*w+x*x+y*y+z*z);w/=norm;x/=norm;y/=norm;z/=norm;
   double sx=trs[a+7]*(1-u)+trs[b+7]*u,sy=trs[a+8]*(1-u)+trs[b+8]*u,sz=trs[a+9]*(1-u)+trs[b+9]*u;
   m[0]=(1-2*(y*y+z*z))*sx;m[1]=2*(x*y-z*w)*sy;m[2]=2*(x*z+y*w)*sz;m[3]=trs[a]*(1-u)+trs[b]*u;
   m[4]=2*(x*y+z*w)*sx;m[5]=(1-2*(x*x+z*z))*sy;m[6]=2*(y*z-x*w)*sz;m[7]=trs[a+1]*(1-u)+trs[b+1]*u;
   m[8]=2*(x*z-y*w)*sx;m[9]=2*(y*z+x*w)*sy;m[10]=(1-2*(x*x+y*y))*sz;m[11]=trs[a+2]*(1-u)+trs[b+2]*u;
   m[12]=m[13]=m[14]=0;m[15]=1;
  }
 }
 public static class MathD
 {
  public static bool Finite(double v){return !double.IsNaN(v)&&!double.IsInfinity(v);}
  public static double[] Identity(){return new double[]{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1};}
  public static void Multiply(double[] a,int ai,double[] b,int bi,double[] output,int oi)
  {
   for(int r=0;r<4;r++)for(int c=0;c<4;c++){double sum=0;for(int k=0;k<4;k++)sum+=a[ai+r*4+k]*b[bi+k*4+c];output[oi+r*4+c]=sum;}
  }
  public static void Inverse(double[] input,int start,double[] output,int target)
  {
   var a=new double[32];for(int r=0;r<4;r++)for(int c=0;c<4;c++){a[r*8+c]=input[start+r*4+c];a[r*8+4+c]=r==c?1:0;}
   for(int c=0;c<4;c++)
   {
    int pivot=c;for(int r=c+1;r<4;r++)if(Math.Abs(a[r*8+c])>Math.Abs(a[pivot*8+c]))pivot=r;
    if(Math.Abs(a[pivot*8+c])<1e-14)throw new ArgumentException("Singular source rest");
    if(pivot!=c)for(int j=0;j<8;j++){double temp=a[c*8+j];a[c*8+j]=a[pivot*8+j];a[pivot*8+j]=temp;}
    double d=a[c*8+c];for(int j=0;j<8;j++)a[c*8+j]/=d;
    for(int r=0;r<4;r++)if(r!=c){double f=a[r*8+c];for(int j=0;j<8;j++)a[r*8+j]-=f*a[c*8+j];}
   }
   for(int r=0;r<4;r++)for(int c=0;c<4;c++)output[target+r*4+c]=a[r*8+4+c];
  }
 }
}
