using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

static class FootFeatureTests
{
    static int assertions;
    static void Check(bool yes,string message){assertions++;if(!yes)throw new Exception(message);}
    static float Brute(int count,int[] bones,float[] weights,float[] points,float[] rows)
    {
        float best=float.PositiveInfinity;
        for(int i=0;i<count;i++)
        {
            float sum=0;
            for(int k=0;k<4;k++)
            {
                int q=i*4+k;if(weights[q]==0)continue;int r=bones[q]*4,p=q*3;
                // Separate reference full-support loop; original order and float operations.
                sum+=(rows[r]*points[p]+rows[r+1]*points[p+1]+rows[r+2]*points[p+2]+rows[r+3])*weights[q];
            }
            if(sum<best)best=sum;
        }
        return best;
    }
    static void SmallParity()
    {
        var random=new Random(41827);
        for(int suite=0;suite<24;suite++)
        {
            int n=1+suite*17,boneCount=4;var bones=new int[n*4];var weights=new float[n*4];var points=new float[n*12];
            for(int i=0;i<n;i++)
            {
                float sum=0;for(int k=0;k<4;k++){int q=i*4+k;bones[q]=k;weights[q]=(float)random.NextDouble();sum+=weights[q];for(int d=0;d<3;d++)points[q*3+d]=(float)(random.NextDouble()*6-3);}
                for(int k=0;k<4;k++)weights[i*4+k]/=sum;
            }
            var tree=new ExactFootFeatureBounds(boneCount,bones,weights,points,3);
            for(int pose=0;pose<80;pose++)
            {
                var rows=new float[boneCount*4];for(int d=0;d<rows.Length;d++)rows[d]=(float)(random.NextDouble()*8-4);
                ExactFootFeatureBounds.Statistics stats;float expected=Brute(n,bones,weights,points,rows),actual=tree.Minimum(rows,out stats);
                Check(actual==expected,"small all-point parity suite="+suite+" pose="+pose+" actual="+actual+" expected="+expected);
            }
        }
        // Extreme lower point is not a centroid/sample seed; an unsound midpoint bound must miss it.
        var b=new int[512*4];var w=new float[512*4];var p=new float[512*12];
        for(int i=0;i<512;i++){w[i*4]=1;p[i*12+1]=i==511?-100:(i%2==0?1:9);}
        var witness=new ExactFootFeatureBounds(1,b,w,p,4);ExactFootFeatureBounds.Statistics s;
        Check(witness.Minimum(new float[]{0,1,0,0},out s)==-100,"extreme original support point cannot disappear");
        bool rejected=false;try{witness.Minimum(new float[]{0,float.NaN,0,0},out s);}catch(ArgumentException){rejected=true;}
        Check(rejected,"NaN cannot become a floor-clear result");
        var huge = new ExactFootFeatureBounds(1,new int[4],new float[]{1e-30f,0,0,0},new float[]{1e30f,0,0,0,0,0,0,0,0,0,0,0});
        rejected=false;try{huge.Minimum(new float[]{1e30f,0,0,0},out s);}catch(ArithmeticException){rejected=true;}
        Check(rejected,"Tiny weight cannot hide unweighted intermediate float overflow");
    }
    static int[] Ints(BinaryReader reader,int count){var a=new int[count];for(int i=0;i<count;i++)a[i]=reader.ReadInt32();return a;}
    static float[] Floats(BinaryReader reader,int count){var a=new float[count];for(int i=0;i<count;i++)a[i]=reader.ReadSingle();return a;}
    static double Percentile(List<double> values,double q){values.Sort();return values[(int)Math.Ceiling((values.Count-1)*q)];}
    static int Main(string[] args)
    {
        try
        {
            SmallParity();
            using(var reader=new BinaryReader(File.OpenRead(args[0])))
            {
                Check(reader.ReadUInt32()==0x31465346,"FSF1");int count=reader.ReadInt32(),boneCount=reader.ReadInt32(),poseCount=reader.ReadInt32();
                var bones=Ints(reader,count*4);var weights=Floats(reader,count*4);var points=Floats(reader,count*12);
                var poses=new float[poseCount][];for(int i=0;i<poseCount;i++)poses[i]=Floats(reader,boneCount*4);
                Check(reader.BaseStream.Position==reader.BaseStream.Length,"source fixture complete");
                var build=Stopwatch.StartNew();var tree=new ExactFootFeatureBounds(boneCount,bones,weights,points,64);build.Stop();
                long evaluated=0,bounds=0;var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var wall=Stopwatch.StartNew();
                for(int i=0;i<poseCount;i++)
                {
                    ExactFootFeatureBounds.Statistics stats;float actual=tree.Minimum(poses[i],out stats),expected=Brute(count,bones,weights,points,poses[i]);
                    Check(actual==expected,"ALL source points/pose parity pose="+i+" actual="+actual+" expected="+expected);
                    evaluated+=stats.ExactPoints;bounds+=stats.BoundsTested;
                }
                // Random per-bone affine vertical rows include signed/reflected scales and large
                // root translations; they are a stronger algebraic bound test, not new source poses.
                var random=new Random(9817);
                for(int i=0;i<64;i++)
                {
                    var rows=new float[boneCount*4];float translation=(float)(random.NextDouble()*20000-10000);
                    for(int j=0;j<boneCount;j++){for(int k=0;k<3;k++)rows[j*4+k]=(float)(random.NextDouble()*8-4);rows[j*4+3]=translation+(float)random.NextDouble();}
                    ExactFootFeatureBounds.Statistics stats;Check(tree.Minimum(rows,out stats)==Brute(count,bones,weights,points,rows),"random signed affine full-source parity "+i);
                }
                wall.Stop();process.Refresh();Console.WriteLine("SOURCE count="+count+" poses="+poseCount+" groups="+tree.GroupCount+" nodes="+tree.NodeCount+" buildWallMs="+build.Elapsed.TotalMilliseconds.ToString("F3"));
                Console.WriteLine("PARITY sourceExactPointAverage="+(evaluated/(double)poseCount).ToString("F1")+" boundsAverage="+(bounds/(double)poseCount).ToString("F1")+" sourceOriginalPoints="+count+" wallMs="+wall.Elapsed.TotalMilliseconds.ToString("F3")+" processCpuMs="+(process.TotalProcessorTime-cpu).TotalMilliseconds.ToString("F3"));
                var accelerated=new List<double>();var brute=new List<double>();
                for(int i=0;i<160;i++)
                {
                    var rows=poses[(i*13)%poseCount];ExactFootFeatureBounds.Statistics stats;var timer=Stopwatch.StartNew();tree.Minimum(rows,out stats);timer.Stop();if(i>=32)accelerated.Add(timer.Elapsed.TotalMilliseconds);
                    timer.Restart();Brute(count,bones,weights,points,rows);timer.Stop();if(i>=32)brute.Add(timer.Elapsed.TotalMilliseconds);
                }
                Console.WriteLine("PORTABLE HOST-CONTENDED ONLY acceleratedMedianMs="+Percentile(accelerated,.5).ToString("F6")+" p95="+Percentile(accelerated,.95).ToString("F6")+" max="+Percentile(accelerated,1).ToString("F6")+" bruteMedianMs="+Percentile(brute,.5).ToString("F6")+" bruteP95="+Percentile(brute,.95).ToString("F6")+" bruteMax="+Percentile(brute,1).ToString("F6"));
            }
            Console.WriteLine("PASS assertions="+assertions+"; no Unity/renderer/production adapter or phone performance claim");return 0;
        }
        catch(Exception error){Console.Error.WriteLine("FAIL "+error);return 1;}
    }
}
