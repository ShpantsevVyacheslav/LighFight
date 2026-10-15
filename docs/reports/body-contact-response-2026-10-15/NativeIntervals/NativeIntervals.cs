using System;using System.IO;using System.IO.Compression;
namespace NativeInterval {
// Outward IEEE double interval arithmetic. Finite input required; overflow => unsupported.
public struct I {
 public double Lo,Hi;public I(double lo,double hi){if(double.IsNaN(lo)||double.IsNaN(hi)||double.IsInfinity(lo)||double.IsInfinity(hi)||lo>hi)throw new ArithmeticException("Finite interval required");Lo=lo;Hi=hi;}
 public static double Down(double x){if(double.IsNaN(x)||double.IsInfinity(x))throw new ArithmeticException();if(x==0)return -double.Epsilon;long bits=BitConverter.DoubleToInt64Bits(x);return BitConverter.Int64BitsToDouble(x>0?bits-1:bits+1);}
 public static double Up(double x)=>-Down(-x);public static I Point(double x)=>new I(x,x);
 public static I operator +(I a,I b)=>new I(Down(a.Lo+b.Lo),Up(a.Hi+b.Hi));
 public static I operator -(I a,I b)=>new I(Down(a.Lo-b.Hi),Up(a.Hi-b.Lo));
 public static I operator *(I a,I b){double p=a.Lo*b.Lo,q=a.Lo*b.Hi,r=a.Hi*b.Lo,s=a.Hi*b.Hi;return new I(Down(Math.Min(Math.Min(p,q),Math.Min(r,s))),Up(Math.Max(Math.Max(p,q),Math.Max(r,s))));}
 public static I operator /(I a,I b){if(b.Lo<=0&&b.Hi>=0)throw new ArithmeticException("Denominator can vanish");return a*new I(Down(1/b.Hi),Up(1/b.Lo));}
 public static I Hull(I a,I b)=>new I(Math.Min(a.Lo,b.Lo),Math.Max(a.Hi,b.Hi));
 public static I Sqr(I a)=>new I(a.Lo<=0&&a.Hi>=0?0:Down(Math.Min(a.Lo*a.Lo,a.Hi*a.Hi)),Up(Math.Max(a.Lo*a.Lo,a.Hi*a.Hi)));
 public static I Sqrt(I a){if(a.Lo<0)throw new ArithmeticException();return new I(a.Lo==0?0:Down(Math.Sqrt(a.Lo)),Up(Math.Sqrt(a.Hi)));}
 public bool Contains(double x)=>x>=Lo&&x<=Hi;
}
public struct Key {public double Time,Value,In,Out,InWeight,OutWeight;public int Weighted;}
public sealed class Curve {
 public int PreWrap,PostWrap;public Key[] Keys;
 // Supports finite unweighted cubic Hermite and weighted Bezier control hulls in key domain.
 // Weighted weights outside[0,1], stepped infinity tangents, wraps/extrapolation are explicitly unsupported, never sampled-clear.
 public bool Bound(double from,double to,out I result){result=I.Point(0);if(Keys==null||Keys.Length==0||!Finite(from)||!Finite(to)||from>to)return false;var keys=Keys;if(from<keys[0].Time||to>keys[keys.Length-1].Time)return false;
 bool found=false;for(int n=0;n<keys.Length;n++){var k=keys[n];if(!Finite(k.Time)||!Finite(k.Value)||!Finite(k.In)||!Finite(k.Out)||(k.Weighted<0||k.Weighted>3)||n>0&&k.Time<=keys[n-1].Time)return false;}
 if(keys.Length==1){result=I.Point(keys[0].Value);return from==to&&from==keys[0].Time;}
 for(int n=0;n<keys.Length-1;n++){var a=keys[n];var b=keys[n+1];if(to<a.Time||from>b.Time)continue;I dt=I.Point(b.Time)-I.Point(a.Time);double low=Math.Max(from,a.Time),high=Math.Min(to,b.Time);
 if((a.Weighted&2)!=0||(b.Weighted&1)!=0){double ow=(a.Weighted&2)!=0?a.OutWeight:1.0/3,iw=(b.Weighted&1)!=0?b.InWeight:1.0/3;if(!Finite(ow)||!Finite(iw)||ow<0||ow>1||iw<0||iw>1)return false;
 // Weighted time Bezier is monotone for weights in[0,1]. Taking the ENTIRE value
 // control hull is conservative independently of inverse-time solver behaviour in exact arithmetic.
 I p0=I.Point(a.Value),p1=p0+dt*I.Point(ow)*I.Point(a.Out),p3=I.Point(b.Value),p2=p3-dt*I.Point(iw)*I.Point(b.In);I valueHull=I.Hull(I.Hull(p0,p1),I.Hull(p2,p3));result=found?I.Hull(result,valueHull):valueHull;found=true;continue;
 }
var u=(new I(low,high)-I.Point(a.Time))/dt; // interval polynomial encloses every u, including overshoot
 I u2=u*u,u3=u2*u,one=I.Point(1),two=I.Point(2),three=I.Point(3);
 I value=(two*u3-three*u2+one)*I.Point(a.Value)+(u3-two*u2+u)*dt*I.Point(a.Out)+((I.Point(0)-two)*u3+three*u2)*I.Point(b.Value)+(u3-u2)*dt*I.Point(b.In);
 I control=I.Hull(I.Hull(I.Point(a.Value),I.Point(a.Value)+dt*I.Point(a.Out)/I.Point(3)),I.Hull(I.Point(b.Value),I.Point(b.Value)-dt*I.Point(b.In)/I.Point(3)));value=new I(Math.Max(value.Lo,control.Lo),Math.Min(value.Hi,control.Hi));result=found?I.Hull(result,value):value;found=true;
 }return found;
 }
 static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
}
public sealed class Clip {
 public Curve[] Curves;public int[][] JointChannels;
 public static Clip ReadDeflate(Stream stream){using(var zip=new DeflateStream(stream,CompressionMode.Decompress,true))return ReadRaw(zip);}
 public static Clip ReadRaw(Stream stream){using(var r=new BinaryReader(stream,System.Text.Encoding.UTF8,true)){
 if(r.ReadInt32()!=0x31434e4c)throw new InvalidDataException("LNC1 required");int count=Count(r,100000);var times=new double[count][];for(int t=0;t<count;t++){int n=Count(r,1000000);times[t]=new double[n];for(int k=0;k<n;k++)times[t][k]=r.ReadSingle();}var curves=new Curve[Count(r,100000)];for(int c=0;c<curves.Length;c++){int pre=r.ReadInt32(),post=r.ReadInt32(),table=r.ReadInt32();if(table<0||table>=times.Length)throw new InvalidDataException();var keys=new Key[times[table].Length];for(int k=0;k<keys.Length;k++)keys[k]=new Key{Time=times[table][k],Value=r.ReadSingle(),In=r.ReadSingle(),Out=r.ReadSingle(),InWeight=r.ReadSingle(),OutWeight=r.ReadSingle(),Weighted=r.ReadInt32()};curves[c]=new Curve{PreWrap=pre,PostWrap=post,Keys=keys};}var joints=new int[Count(r,100000)][];for(int j=0;j<joints.Length;j++){joints[j]=new int[10];for(int channel=0;channel<10;channel++){int id=r.ReadInt32();if(id< -1||id>=curves.Length)throw new InvalidDataException();joints[j][channel]=id;}}if(r.BaseStream.ReadByte()!=-1)throw new InvalidDataException("Trailing LNC1 bytes");return new Clip{Curves=curves,JointChannels=joints};}}
 static int Count(BinaryReader r,int max){int n=r.ReadInt32();if(n<0||n>max)throw new InvalidDataException("Bounded LNC1 table");return n;}
}
public sealed class Mat {
 public readonly I[] V=new I[12];public static Mat Identity(){var m=new Mat();m.V[0]=m.V[5]=m.V[10]=I.Point(1);return m;}
 public static Mat Multiply(Mat a,Mat b){var m=new Mat();for(int row=0;row<3;row++)for(int col=0;col<4;col++){I sum=I.Point(col==3? a.V[row*4+3].Lo:0);if(col==3)sum=a.V[row*4+3];for(int k=0;k<3;k++)sum=sum+a.V[row*4+k]*b.V[k*4+col];m.V[row*4+col]=sum;}return m;}
 public I[] Point(I x,I y,I z){var a=new I[3];for(int row=0;row<3;row++)a[row]=V[row*4]*x+V[row*4+1]*y+V[row*4+2]*z+V[row*4+3];return a;}
 public static Mat TRS(I[] channels){var norm=I.Sqrt(I.Sqr(channels[3])+I.Sqr(channels[4])+I.Sqr(channels[5])+I.Sqr(channels[6]));if(norm.Lo<=1e-12)throw new ArithmeticException("Quaternion normalization can be singular");I x=channels[3]/norm,y=channels[4]/norm,z=channels[5]/norm,w=channels[6]/norm,one=I.Point(1),two=I.Point(2);var m=new Mat();m.V[0]=(one-two*(I.Sqr(y)+I.Sqr(z)))*channels[7];m.V[1]=two*(x*y-z*w)*channels[8];m.V[2]=two*(x*z+y*w)*channels[9];m.V[4]=two*(x*y+z*w)*channels[7];m.V[5]=(one-two*(I.Sqr(x)+I.Sqr(z)))*channels[8];m.V[6]=two*(y*z-x*w)*channels[9];m.V[8]=two*(x*z-y*w)*channels[7];m.V[9]=two*(y*z+x*w)*channels[8];m.V[10]=(one-two*(I.Sqr(x)+I.Sqr(y)))*channels[9];m.V[3]=channels[0];m.V[7]=channels[1];m.V[11]=channels[2];return m;}
}
public struct Context {
 public bool SourceGeneric,Unblended,NoGrounding,NoRootAnchor,NoExtraIK,FixedHierarchy,FixedRoot;
 public bool Supported=>SourceGeneric&&Unblended&&NoGrounding&&NoRootAnchor&&NoExtraIK&&FixedHierarchy&&FixedRoot;
}
public sealed class FK {
 // Rest channels REQUIRED for absent curves, preserving original authored local transforms.
 public static bool Bound(Clip clip,int[] parents,double[][] rest,Mat root,double from,double to,Context context,out Mat[] world){world=null;if(!context.Supported||parents.Length!=clip.JointChannels.Length||rest.Length!=parents.Length)return false;try{var result=new Mat[parents.Length];for(int j=0;j<parents.Length;j++){if(parents[j]>=j||parents[j]< -1||rest[j].Length!=10||clip.JointChannels[j].Length!=10)return false;var channels=new I[10];for(int c=0;c<10;c++){int id=clip.JointChannels[j][c];if(id<0)channels[c]=I.Point(rest[j][c]);else if(!clip.Curves[id].Bound(from,to,out channels[c]))return false;}result[j]=Mat.Multiply(parents[j]<0?root:result[parents[j]],Mat.TRS(channels));}world=result;return true;}catch(ArithmeticException){return false;}}
}
}
