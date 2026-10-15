using System;using System.IO;using System.Diagnostics;using System.Runtime.InteropServices;using SourceContact;using LazySourceContact;using SourceResponse;
class SourceRootProfile {
 [DllImport("/usr/lib/libSystem.B.dylib")]static extern int getrusage(int who,IntPtr result);static readonly IntPtr usage=Marshal.AllocHGlobal(256);
 static double Cpu(){if(getrusage(0,usage)!=0)throw new Exception();return Marshal.ReadInt64(usage,0)*1000.0+Marshal.ReadInt32(usage,8)*.001+Marshal.ReadInt64(usage,16)*1000.0+Marshal.ReadInt32(usage,24)*.001;}
 static CompactSurface Read(string path,Surface source){using(var r=new BinaryReader(File.OpenRead(path))){int n=r.ReadInt32(),o=r.ReadInt32(),v=r.ReadInt32();var nodes=new CompactNode[n];for(int i=0;i<n;i++)nodes[i]=new CompactNode{Left=r.ReadInt32(),Right=r.ReadInt32(),Start=r.ReadInt32(),Count=r.ReadInt32(),VertexStart=r.ReadInt32(),VertexCount=r.ReadInt32(),OriginalNode=r.ReadInt32()};var order=new int[o];var vertices=new int[v];for(int i=0;i<o;i++)order[i]=r.ReadInt32();for(int i=0;i<v;i++)vertices[i]=r.ReadInt32();return new CompactSurface(source,nodes,order,vertices);}}
 static CompactSurface Reference(Surface source){var nodes=new CompactNode[source.Nodes.Length];var order=new int[source.Triangles.Length/3];for(int i=0;i<order.Length;i++)order[i]=i;for(int n=0;n<nodes.Length;n++){var a=source.Nodes[n];nodes[n]=new CompactNode{Left=a.Left,Right=a.Right,Start=a.Triangle,Count=a.Triangle>=0?1:0,VertexStart=a.Triangle*3,VertexCount=a.Triangle>=0?3:0,OriginalNode=n};}return new CompactSurface(source,nodes,order,source.Triangles);}
 static double Error(V a,V b)=>Math.Max(Math.Abs(a.X-b.X),Math.Max(Math.Abs(a.Y-b.Y),Math.Abs(a.Z-b.Z)));
 static string dumpPath;
 static long HitHash(Surface a,Surface b,long[] keys,int count,Query q,out int hits){long hash=17;hits=0;var dump=dumpPath==null?null:new StreamWriter(dumpPath);dumpPath=null;for(int i=0;i<count;i++){int x=(int)(keys[i]>>32),y=(int)keys[i];if(q.Triangle(a.Vertex(x,0),a.Vertex(x,1),a.Vertex(x,2),b.Vertex(y,0),b.Vertex(y,1),b.Vertex(y,2))==Verdict.Contact){hits++;if(dump!=null)dump.WriteLine(x+","+y);hash=unchecked(hash*31+keys[i]);}}if(dump!=null)dump.Dispose();return hash;}
 static void Main(string[] args){System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;string root=args[0],packet=args[1];V[] p;int[] bones,tris;double[] weights;int nb;
 using(var r=new BinaryReader(File.OpenRead(Path.Combine(root,"original-mesh.bin")))){int nv=r.ReadInt32(),nt=r.ReadInt32();nb=r.ReadInt32();p=new V[nv];for(int i=0;i<nv;i++)p[i]=new V(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());bones=new int[nv*4];weights=new double[nv*4];for(int i=0;i<nv;i++){for(int k=0;k<4;k++)bones[i*4+k]=r.ReadInt32();for(int k=0;k<4;k++)weights[i*4+k]=r.ReadSingle();}for(int i=0;i<nb*16;i++)r.ReadSingle();tris=new int[nt];for(int i=0;i<nt;i++)tris[i]=r.ReadInt32();}
 Node[] old;using(var r=new BinaryReader(File.OpenRead(Path.Combine(packet,"sailor-topology.bin")))){old=new Node[r.ReadInt32()];for(int i=0;i<old.Length;i++)old[i]=new Node{Left=r.ReadInt32(),Right=r.ReadInt32(),Triangle=r.ReadInt32()};}
 var a=new Surface(p,tris,old,bones,weights){Scalar=true};var b=new Surface(p,tris,old,bones,weights){Scalar=true};
#if NET6_INTRINSICS
 a.Simd=b.Simd=true;
#endif
 var ra=Reference(a);var rb=Reference(b);var cq=new CompactQuery(128);var predicate=new Query(128,4096);var expected=new long[100000];var actual=new long[100000];var ma=new double[nb*16];var mb=new double[nb*16];long version=0;

 NodeHull[] hulls;BoneHull[] boxes;using(var r=new BinaryReader(File.OpenRead(Path.Combine(packet,"../LazyExact/sailor-lazy-all-influences.bin")))){int nn=r.ReadInt32(),bc=r.ReadInt32();r.ReadInt32();r.ReadInt32();hulls=new NodeHull[nn];boxes=new BoneHull[bc];for(int i=0;i<nn;i++)hulls[i]=new NodeHull{Start=r.ReadInt32(),Count=r.ReadInt32(),SumMin=r.ReadDouble(),SumMax=r.ReadDouble()};for(int i=0;i<bc;i++)boxes[i]=new BoneHull{Bone=r.ReadInt32(),Min=new V(r.ReadDouble(),r.ReadDouble(),r.ReadDouble()),Max=new V(r.ReadDouble(),r.ReadDouble(),r.ReadDouble())};}
 var compact=Read(Path.Combine(packet,"compact-16.bin"),a);var la=new LazySurface(p,tris,bones,weights,compact.Nodes,compact.Order,compact.LeafVertices,hulls,boxes,nb);var lb=new LazySurface(p,tris,bones,weights,compact.Nodes,compact.Order,compact.LeafVertices,hulls,boxes,nb);var lazy=new LazyQuery(128);


 var sweep=new TranslationSweep(128);var response=new RootMotionResponse();
 foreach(string name in new[]{"production-jab-0.65-0.pose.bin","production-jab-0.65-7.pose.bin","production-cross-0.9-14.pose.bin"}){
 using(var r=new BinaryReader(File.OpenRead(Path.Combine(root,name)))){for(int i=0;i<ma.Length;i++)ma[i]=r.ReadSingle();for(int i=0;i<mb.Length;i++)mb[i]=r.ReadSingle();}la.SetPose(ma);lb.SetPose(mb);var budget=new WorkBudget(100000);double begin=Cpu();var result=sweep.Query(la,lb,new V(),new V(),ref budget);if(result.Status!=SweepStatus.Contact||result.Time!=0)throw new Exception("Frozen overlap not detected "+name);Console.WriteLine(name+" frozenReadyRED="+result.Status+" t="+result.Time+" witness="+result.FaceA+","+result.FaceB+" cpuMs="+(Cpu()-begin)+" work="+budget.Spent);
 // Rigid common translation is exact, does not alter source poses or vertex arrays.
 for(int bone=0;bone<nb;bone++)mb[bone*16+3]+=4;lb.SetPose(mb);budget=new WorkBudget(100000);var miss=sweep.Query(la,lb,new V(),new V(),ref budget);if(miss.Status!=SweepStatus.Clear)throw new Exception("Far fixture miss failed");Console.WriteLine("translated4m fixture="+miss.Status+" work="+budget.Spent);
 }
 }
}
