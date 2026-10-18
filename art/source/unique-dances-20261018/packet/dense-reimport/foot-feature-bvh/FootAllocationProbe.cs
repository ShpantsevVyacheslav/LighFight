using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
static class FootAllocationProbe
{
 static int[] Ints(BinaryReader r,int n){var a=new int[n];Buffer.BlockCopy(r.ReadBytes(n*4),0,a,0,n*4);return a;}
 static float[] Floats(BinaryReader r,int n){var a=new float[n];Buffer.BlockCopy(r.ReadBytes(n*4),0,a,0,n*4);return a;}
 static int Main(string[] args)
 {
  try
  {
   using(var r=new BinaryReader(File.OpenRead(args[0])))
   {
    r.ReadUInt32();int n=r.ReadInt32(),b=r.ReadInt32(),poses=r.ReadInt32();
    var bones=Ints(r,n*4);var weights=Floats(r,n*4);var points=Floats(r,n*12);var rows=new float[32][];
    for(int i=0;i<rows.Length;i++)rows[i]=Floats(r,b*4);
    var tree=new ExactFootFeatureBounds(b,bones,weights,points);
    ExactFootFeatureBounds.Statistics stats;for(int i=0;i<32;i++)tree.Minimum(rows[i],out stats);
    MethodInfo method=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",BindingFlags.Static|BindingFlags.Public);
    if(method==null){Console.WriteLine("UNAVAILABLE thread allocation counter; no zero-GC claim");return 2;}
    var counter=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),method);
    counter();long calibrationBefore=counter();var sentinel=new byte[65536];sentinel[123]=17;GC.KeepAlive(sentinel);long calibration=counter()-calibrationBefore;Console.WriteLine("KNOWN_ALLOC_65536_COUNTER="+calibration+" OBSERVED="+sentinel[123]);if(calibration<sentinel.Length){Console.WriteLine("UNQUALIFIED thread counter did not observe known positive allocation; no zero-GC claim");return 2;}
    long before=counter();float sink=0;
    for(int i=0;i<128;i++)sink+=tree.Minimum(rows[i%32],out stats);
    long allocated=counter()-before;
    Console.WriteLine("PORTABLE_MONO_WARM_QUERY_GC allocatedBytes="+allocated+" iterations=128 sink="+sink+" originalPoints="+n+" totalSourcePoses="+poses);
    return allocated==0?0:1;
   }
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
