using System;using System.IO;
static class PreviousArgminChecks {
 static int[] Ints(BinaryReader r,int n){var a=new int[n];for(int i=0;i<n;i++)a[i]=r.ReadInt32();return a;}
 static float[] Floats(BinaryReader r,int n){var a=new float[n];for(int i=0;i<n;i++)a[i]=r.ReadSingle();return a;}
 static int Bits(float f)=>BitConverter.ToInt32(BitConverter.GetBytes(f),0);
 static int Main(string[] args){using(var r=new BinaryReader(File.OpenRead(args[0]))){if(r.ReadUInt32()!=0x31465346)throw new Exception();int count=r.ReadInt32(),bones=r.ReadInt32(),poses=r.ReadInt32();var b=Ints(r,count*4);var w=Floats(r,count*4);var p=Floats(r,count*12);var all=Floats(r,bones*4*poses);
 Func<int,float[],float> exact=(i,rows)=>{float y=0;for(int k=0;k<4;k++){int q=i*4+k;if(w[q]==0)continue;int d=b[q]*4,n=q*3;y+=(rows[d]*p[n]+rows[d+1]*p[n+1]+rows[d+2]*p[n+2]+rows[d+3])*w[q];}return y;};
 var tree=new ExactFootFeatureBounds(bones,b,w,p,64,exact);var rowsNow=new float[bones*4];var rng=new Random(5391);int previous=-1,checks=0;long baselineWork=0,hintWork=0;
 for(int pose=0;pose<129;pose++){
  if(pose<65)Array.Copy(all,pose*rowsNow.Length,rowsNow,0,rowsNow.Length);else for(int k=0;k<rowsNow.Length;k++)rowsNow[k]=(float)((rng.NextDouble()*2-1)*20);
  float full=float.PositiveInfinity;for(int i=0;i<count;i++){float y=exact(i,rowsNow);if(y<full)full=y;}
  ExactFootFeatureBounds.Statistics original,hinted;float originalMin=tree.Minimum(rowsNow,out original),hintedMin=tree.Minimum(rowsNow,previous,out hinted);
  if(Bits(originalMin)!=Bits(full)||Bits(hintedMin)!=Bits(full)||Bits(exact(hinted.WinningPoint,rowsNow))!=Bits(full))throw new Exception("Parity pose "+pose);
  if(pose<65){baselineWork+=original.ExactPoints;hintWork+=hinted.ExactPoints;}
  foreach(int hint in new[]{count-1,0,-2,count,int.MaxValue}){ExactFootFeatureBounds.Statistics st;float min=tree.Minimum(rowsNow,hint,out st);if(Bits(min)!=Bits(full)||Bits(exact(st.WinningPoint,rowsNow))!=Bits(full)||st.UsedHint!=(hint>=0&&hint<count))throw new Exception("Stale/invalid hint "+pose);checks++;}
  previous=hinted.WinningPoint;checks+=3;
 }
 Console.WriteLine("PASS points="+count+" sourcePoses="+poses+" sequentialFirst65=65 signedRandom=64 checks="+checks+" sequentialOriginalExactEvaluations="+baselineWork+" sequentialHintExactEvaluations="+hintWork+"; work counters only, not timing/Unity acceptance");return 0;
 }}
}
