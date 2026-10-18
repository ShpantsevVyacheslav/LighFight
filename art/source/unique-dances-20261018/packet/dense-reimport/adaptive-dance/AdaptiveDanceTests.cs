using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using DenseRigifyReview.Adaptive;
static class AdaptiveDanceTests
{
 static int checks;
 static void Check(bool valid,string message){checks++;if(!valid)throw new Exception(message);}
 static double[] Doubles(BinaryReader r,int n){var a=new double[n];for(int i=0;i<n;i++)a[i]=r.ReadDouble();return a;}
 static void IdentityAt(double[] a,int index){a[index]=a[index+5]=a[index+10]=a[index+15]=1;}
 static void Small()
 {
  var rest=new double[32];IdentityAt(rest,0);IdentityAt(rest,16);rest[16+7]=2;
  var key=new double[40];for(int k=0;k<2;k++)for(int b=0;b<2;b++){int i=(k*2+b)*10;key[i+3]=k==0?1:-1;key[i+7]=key[i+8]=key[i+9]=1;}
  key[20]=4;var data=new AdaptiveDanceData(new[]{"root","child"},new[]{-1,0},rest,MathD.Identity(),MathD.Identity(),new[]{0.0,2.0},key,2);
  var evaluator=new AdaptiveDanceEvaluator(data);evaluator.Evaluate(1);
  Check(Math.Abs(evaluator.WorldPose[3]-2)<1e-12,"linear translation");Check(Math.Abs(evaluator.WorldPose[16+7]-2)<1e-12,"FULL parent rest FK");Check(Math.Abs(evaluator.WorldPose[16+3]-2)<1e-12,"parent movement propagates");Check(Math.Abs(evaluator.WorldPose[0]-1)<1e-12,"hemisphere-equivalent q and -q do not collapse/flip");
  evaluator.Evaluate(-1);Check(evaluator.WorldPose[3]==0,"start clamps");evaluator.Evaluate(9);Check(evaluator.WorldPose[3]==4,"end clamps, no loop");
  bool rejected=false;try{evaluator.Evaluate(double.NaN);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid time rejects");
 }
 static double[] Polar(double[] m,int offset)
 {
  var a=new double[9];for(int r=0;r<3;r++)for(int c=0;c<3;c++)a[r*3+c]=m[offset+r*4+c];
  for(int iteration=0;iteration<32;iteration++)
  {
   double det=a[0]*(a[4]*a[8]-a[5]*a[7])-a[1]*(a[3]*a[8]-a[5]*a[6])+a[2]*(a[3]*a[7]-a[4]*a[6]);if(Math.Abs(det)<1e-16)throw new Exception("Singular O3 polar witness");
   var inverseTranspose=new[]{(a[4]*a[8]-a[5]*a[7])/det,(a[5]*a[6]-a[3]*a[8])/det,(a[3]*a[7]-a[4]*a[6])/det,(a[2]*a[7]-a[1]*a[8])/det,(a[0]*a[8]-a[2]*a[6])/det,(a[1]*a[6]-a[0]*a[7])/det,(a[1]*a[5]-a[2]*a[4])/det,(a[2]*a[3]-a[0]*a[5])/det,(a[0]*a[4]-a[1]*a[3])/det};
   double change=0;for(int i=0;i<9;i++){double next=(a[i]+inverseTranspose[i])*.5;change=Math.Max(change,Math.Abs(next-a[i]));a[i]=next;}if(change<1e-13)return a;
  }
  throw new Exception("Polar convergence not certified");
 }
 static double Angle(double[] a,int ai,double[] b,int bi)
 {
  var x=Polar(a,ai);var y=Polar(b,bi);var r=new double[9];for(int i=0;i<3;i++)for(int j=0;j<3;j++)for(int k=0;k<3;k++)r[i*3+j]+=x[k*3+i]*y[k*3+j];
  double sx=(r[7]-r[5])*.5,sy=(r[2]-r[6])*.5,sz=(r[3]-r[1])*.5,c=Math.Max(-1,Math.Min(1,(r[0]+r[4]+r[8]-1)*.5));return Math.Atan2(Math.Sqrt(sx*sx+sy*sy+sz*sz),c)*180/Math.PI;
 }
 static int Main(string[] args)
 {
  try
  {
   Small();AdaptiveDanceData data;using(var input=File.OpenRead(args[0]))data=AdaptiveDanceData.Read(input);Check(data.KeyCount==2535&&data.BoneCount==136,"ALL accepted keys/bones retained");var eval=new AdaptiveDanceEvaluator(data);var watch=Stopwatch.StartNew();
   using(var reader=new BinaryReader(File.OpenRead(args[1])))
   {
    Check(reader.ReadUInt32()==0x31434e44,"DNC1");int bones=reader.ReadInt32(),poses=reader.ReadInt32(),keyed=reader.ReadInt32();double duration=reader.ReadDouble(),maxSum=reader.ReadDouble();
    for(int i=0;i<bones;i++){string name=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));Check(name==data.BoneName(i),"named source order");}
    var corners=Doubles(reader,bones*8*4);var active=new bool[bones];for(int i=0;i<bones;i++)active[i]=reader.ReadByte()!=0;Doubles(reader,16);Doubles(reader,bones*16);
    double worstBound=0,worstAngle=0;int worstPose=-1;var expectedPose=new double[bones*16];var expectedSkin=new double[bones*16];
    for(int pose=0;pose<poses;pose++)
    {
     double phase=reader.ReadDouble(),seconds=reader.ReadDouble();bool isKey=reader.ReadByte()!=0;for(int i=0;i<expectedPose.Length;i++)expectedPose[i]=reader.ReadDouble();for(int i=0;i<expectedSkin.Length;i++)expectedSkin[i]=reader.ReadDouble();eval.Evaluate(seconds);double bound=0,angle=0;
     for(int b=0;b<bones;b++)
     {
      angle=Math.Max(angle,Angle(expectedPose,b*16,eval.WorldPose,b*16));if(!active[b])continue;
      for(int c=0;c<8;c++)
      {
       int p=(b*8+c)*4;double x=0,y=0,z=0;for(int k=0;k<4;k++){x+=(eval.WorldSkin[b*16+k]-expectedSkin[b*16+k])*corners[p+k];y+=(eval.WorldSkin[b*16+4+k]-expectedSkin[b*16+4+k])*corners[p+k];z+=(eval.WorldSkin[b*16+8+k]-expectedSkin[b*16+8+k])*corners[p+k];}bound=Math.Max(bound,Math.Sqrt(x*x+y*y+z*z));
      }
     }
     bound*=maxSum;if(bound>worstBound){worstBound=bound;worstPose=pose;}worstAngle=Math.Max(worstAngle,angle);Check(bound<=.0001&&angle<=.01,"native-frame original source gate pose="+pose+" phase="+phase+" seconds="+seconds+" keyed="+isKey+" boundM="+bound+" angleDeg="+angle);
    }
    Console.WriteLine("SOURCE ORIGINAL POSES="+poses+" KEYED_ENDPOINTS="+keyed+" HOLDOUTS="+(poses-keyed)+" MAX_BOUND_NATIVE_M="+worstBound+" MAX_BONE_DEG="+worstAngle+" WORST_POSE="+worstPose+" wallMs="+watch.Elapsed.TotalMilliseconds);
    var method=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",BindingFlags.Static|BindingFlags.Public);if(method!=null){var counter=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),method);eval.Evaluate(1);counter();long calibrationBefore=counter();var sentinel=new byte[65536];sentinel[123]=17;GC.KeepAlive(sentinel);long calibration=counter()-calibrationBefore;Console.WriteLine("KNOWN_ALLOC_65536_COUNTER="+calibration+" OBSERVED="+sentinel[123]);if(calibration>=sentinel.Length){long before=counter();for(int i=0;i<128;i++)eval.Evaluate(i*duration/127);long alloc=counter()-before;Check(alloc==0,"positively calibrated warm allocationfree evaluator");Console.WriteLine("WARM_EVALUATE128_MANAGED_ALLOC="+alloc);}else Console.WriteLine("ALLOCATION_UNQUALIFIED counter did not observe positive sentinel; no zero allocation claim");}
   }
   Console.WriteLine("PASS checks="+checks+" portable native-frame only; no Unity/player/root-scale/floor/skin-quality acceptance");return 0;
  }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}
 }
}
