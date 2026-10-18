using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using UnityEngine.Rendering;

// Review-only. No catalog assignments and no edits outside this owned folder.
public static partial class DenseRigifyReimportReview
{
    const string Root = "Assets/DenseRigifyReview";
    const string BaseOutput = "../art/source/unique-dances-20261018/audit/dense-reimport";
    static string Output=BaseOutput;
    const float PositionQuantum = .000002f, UVQuantum = .000002f, WeightQuantum = .000002f;
    static Quaternion reviewBoneLocalAxis=Quaternion.identity;
    static Matrix4x4[] reviewBoneConventions;

    [Serializable] public sealed class Config
    {
        public string model; // Combined mesh + all takes, inside owned folder.
        public string oracle; // Absolute DRQ1 QA sidecar, outside Assets.
        public string baseColor, normal, roughness, metallic;
        public float metallicFactor, roughnessFactor, normalScale;
        public float baseR, baseG, baseB, baseA;
        public bool originalMaterialFactorsVerified;
        public bool restoreOriginalSourceWeights;
    }
    sealed class Pose { public string clip; public double phase, frame; public Vector3[] points; public Matrix4x4[] bones; }
    sealed class Source
    {
        public float[] positions, uv, weights; public int[] triangles, joints, samples;
        public Matrix4x4[] rest; public Matrix4x4 meshWorld, rigWorld;
        public string[] names; public Pose[] poses;
        public bool importerBasis;
        public Vector3 Point(int i) {var p=meshWorld.MultiplyPoint3x4(new Vector3(positions[i*3],positions[i*3+1],positions[i*3+2]));return importerBasis?Basis(p):new Vector3(p.x,p.z,-p.y);}
        public static Source Read(string path,bool importerBasis=true)
        {
            using(var r = new BinaryReader(File.OpenRead(path)))
            {
                if(Encoding.ASCII.GetString(r.ReadBytes(4))!="DRQ1")throw new Exception("Wrong dense QA oracle magic");
                var s=new Source{importerBasis=importerBasis};s.positions=Floats(Array(r,"<f4"));s.triangles=Ints(Array(r,"<i4"));s.uv=Floats(Array(r,"<f4"));
                s.joints=Ints(Array(r,"<i4"));s.weights=Floats(Array(r,"<f4"));s.rest=Matrices(Array(r,"<f8"));
                s.meshWorld=Matrices(Array(r,"<f8"))[0];s.rigWorld=Matrices(Array(r,"<f8"))[0];
                s.names=new string[r.ReadInt32()];for(int i=0;i<s.names.Length;i++)s.names[i]=Text(r);
                s.samples=new int[r.ReadInt32()];for(int i=0;i<s.samples.Length;i++)s.samples[i]=r.ReadInt32();
                s.poses=new Pose[r.ReadInt32()];for(int k=0;k<s.poses.Length;k++)
                {
                    var p=new Pose{clip=Text(r),phase=r.ReadDouble(),frame=r.ReadDouble(),points=new Vector3[s.samples.Length],bones=new Matrix4x4[s.names.Length]};
                    for(int i=0;i<p.points.Length;i++){var value=new Vector3((float)r.ReadDouble(),(float)r.ReadDouble(),(float)r.ReadDouble());p.points[i]=importerBasis?Reflect(value):value;}
                    for(int i=0;i<p.bones.Length;i++){var value=Matrix(r);p.bones[i]=importerBasis?Reflection*value*Reflection:value;}s.poses[k]=p;
                }
                if(r.BaseStream.Position!=r.BaseStream.Length)throw new Exception("Trailing oracle bytes");return s;
            }
        }
        static string Text(BinaryReader r) => Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));
        static byte[] Array(BinaryReader r,string type){if(Text(r)!=type)throw new Exception("Oracle array type");int rank=r.ReadInt32();long count=1;for(int i=0;i<rank;i++)count*=r.ReadInt32();long bytes=r.ReadInt64();int size=type=="<f8"?8:4;if(bytes!=count*size||bytes>int.MaxValue)throw new Exception("Oracle array shape");var b=r.ReadBytes((int)bytes);if(b.Length!=bytes)throw new EndOfStreamException();return b;}
        static float[] Floats(byte[] b){var a=new float[b.Length/4];Buffer.BlockCopy(b,0,a,0,b.Length);return a;}
        static int[] Ints(byte[] b){var a=new int[b.Length/4];Buffer.BlockCopy(b,0,a,0,b.Length);return a;}
        static Matrix4x4 Matrix(BinaryReader r){var m=new Matrix4x4();for(int row=0;row<4;row++)for(int col=0;col<4;col++)m[row,col]=(float)r.ReadDouble();return m;}
        static Matrix4x4[] Matrices(byte[] b){using(var r=new BinaryReader(new MemoryStream(b))){var a=new Matrix4x4[b.Length/128];for(int i=0;i<a.Length;i++)a[i]=Matrix(r);return a;}}
    }

    // Original corner equivalence includes location, UV and canonical bone/weight
    // tuple. Seam duplicates with identical tuples are intentionally equivalent.
    readonly struct VertexKey : IEquatable<VertexKey>
    {
        readonly int x,y,z,b0,b1,b2,b3,w0,w1,w2,w3;
        public VertexKey(Vector3 p,int[] b,float[] w){x=BitConverter.SingleToInt32Bits(p.x);y=BitConverter.SingleToInt32Bits(p.y);z=BitConverter.SingleToInt32Bits(p.z);b0=b[0];b1=b[1];b2=b[2];b3=b[3];w0=BitConverter.SingleToInt32Bits(w[0]);w1=BitConverter.SingleToInt32Bits(w[1]);w2=BitConverter.SingleToInt32Bits(w[2]);w3=BitConverter.SingleToInt32Bits(w[3]);}
        public bool Equals(VertexKey o)=>x==o.x&&y==o.y&&z==o.z&&b0==o.b0&&b1==o.b1&&b2==o.b2&&b3==o.b3&&w0==o.w0&&w1==o.w1&&w2==o.w2&&w3==o.w3;
        public override bool Equals(object o)=>o is VertexKey k&&Equals(k);
        public override int GetHashCode(){unchecked{int h=x;h=h*397^y;h=h*397^z;h=h*397^b0;h=h*397^b1;h=h*397^b2;h=h*397^b3;h=h*397^w0;h=h*397^w1;h=h*397^w2;return h*397^w3;}}
    }
    readonly struct Cell : IEquatable<Cell>
    {
        public readonly int x,y,z;public Cell(Vector3 p){x=(int)Math.Floor(p.x/PositionQuantum);y=(int)Math.Floor(p.y/PositionQuantum);z=(int)Math.Floor(p.z/PositionQuantum);}public Cell(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
        public bool Equals(Cell o)=>x==o.x&&y==o.y&&z==o.z;public override bool Equals(object o)=>o is Cell c&&Equals(c);public override int GetHashCode()=>unchecked((x*397^y)*397^z);
    }
    readonly struct Corner : IEquatable<Corner>, IComparable<Corner>
    {
        readonly int vertex,u,v;
        public Corner(int vertex,Vector2 uv){this.vertex=vertex;u=BitConverter.SingleToInt32Bits(uv.x);v=BitConverter.SingleToInt32Bits(uv.y);}
        public int CompareTo(Corner o){int n=vertex.CompareTo(o.vertex);if(n!=0)return n;n=u.CompareTo(o.u);return n!=0?n:v.CompareTo(o.v);}
        public bool Equals(Corner o)=>vertex==o.vertex&&u==o.u&&v==o.v;
        public override bool Equals(object o)=>o is Corner c&&Equals(c);
        public override int GetHashCode()=>unchecked((vertex*397^u)*397^v);
    }
    readonly struct Face : IEquatable<Face>
    {
        readonly Corner a,b,c;
        public Face(Corner x,Corner y,Corner z){ // Cyclic rotation only: winding is retained.
            if(x.CompareTo(y)<=0&&x.CompareTo(z)<=0){a=x;b=y;c=z;}else if(y.CompareTo(z)<=0){a=y;b=z;c=x;}else{a=z;b=x;c=y;}
        }
        public bool Equals(Face o)=>a.Equals(o.a)&&b.Equals(o.b)&&c.Equals(o.c);
        public override bool Equals(object o)=>o is Face f&&Equals(f);
        public override int GetHashCode()=>unchecked((a.GetHashCode()*397^b.GetHashCode())*397^c.GetHashCode());
    }
    readonly struct UVCell : IEquatable<UVCell>
    {
        public readonly int vertex,u,v;public UVCell(int vertex,int u,int v){this.vertex=vertex;this.u=u;this.v=v;}public UVCell(int vertex,Vector2 p):this(vertex,(int)Math.Floor(p.x/UVQuantum),(int)Math.Floor(p.y/UVQuantum)){}
        public bool Equals(UVCell o)=>vertex==o.vertex&&u==o.u&&v==o.v;public override bool Equals(object o)=>o is UVCell k&&Equals(k);public override int GetHashCode()=>unchecked((vertex*397^u)*397^v);
    }
    sealed class UVLookup
    {
        readonly Dictionary<UVCell,Vector2> cells=new Dictionary<UVCell,Vector2>();readonly Dictionary<UVCell,List<Vector2>> collisions=new Dictionary<UVCell,List<Vector2>>();
        public void Add(int vertex,Vector2 uv){var cell=new UVCell(vertex,uv);if(!cells.TryGetValue(cell,out var prior)){cells.Add(cell,uv);return;}if(prior.x==uv.x&&prior.y==uv.y)return;if(!collisions.TryGetValue(cell,out var list)){list=new List<Vector2>();collisions.Add(cell,list);}if(!list.Any(p=>p.x==uv.x&&p.y==uv.y))list.Add(uv);}
        public Corner Match(int vertex,Vector2 uv)
        {
            Vector2 best=default;double distance=double.PositiveInfinity;bool tie=false;var cell=new UVCell(vertex,uv);
            void Test(Vector2 original){if(Mathf.Abs(original.x-uv.x)>.000001f||Mathf.Abs(original.y-uv.y)>.000001f)return;double dx=(double)original.x-uv.x,dy=(double)original.y-uv.y,d=dx*dx+dy*dy;if(d<distance){distance=d;best=original;tie=false;}else if(d==distance&&(best.x!=original.x||best.y!=original.y))tie=true;}
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++){var key=new UVCell(vertex,cell.u+x,cell.v+y);if(cells.TryGetValue(key,out var original))Test(original);if(collisions.TryGetValue(key,out var list))foreach(var p in list)Test(p);}
            if(double.IsInfinity(distance)||tie)throw new Exception("Missing/ambiguous original UV correspondence at vertex "+vertex+" UV="+uv);return new Corner(vertex,best);
        }
    }
    static int Q(float value,float quantum)=>checked((int)Math.Round(value/quantum));
    // Fixed importer conversion proved independently by all136 bind joints and
    // named-weight vertex witnesses. Blender RHS -> FBX -> Unity LHS; no fit.
    static Vector3 Reflect(Vector3 p)=>new Vector3(-p.x,p.y,p.z);
    static Matrix4x4 Reflection=>Matrix4x4.Scale(new Vector3(-1,1,1));
    static Vector3 Basis(Vector3 p)=>new Vector3(-p.x,p.z,-p.y);
    static Matrix4x4 CanonicalBasis {get{var b=Matrix4x4.identity;b.SetColumn(1,new Vector4(0,0,-1,0));b.SetColumn(2,new Vector4(0,1,0,0));return b;}}
    static Matrix4x4 BasisMatrix=>Reflection*CanonicalBasis;
    static void Canonical(int[] indices,float[] weights){for(int i=0;i<4;i++)if(weights[i]==0)indices[i]=int.MaxValue;System.Array.Sort(indices,weights);}
    static Config LoadConfig(){var c=JsonUtility.FromJson<Config>(File.ReadAllText(Root+"/review-config.json"));if(!c.model.StartsWith(Root+"/",StringComparison.Ordinal))throw new Exception("Model outside owned folder");return c;}
    public static void ConfigureAndRun(){ConfigureImport();Run();}
    public static void RestoreAndRun()
    {
        var c=LoadConfig();c.restoreOriginalSourceWeights=true;File.WriteAllText(Root+"/review-config.json",JsonUtility.ToJson(c,true));
        var importer=(ModelImporter)AssetImporter.GetAtPath(c.model);importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();Run();
    }
    public static void RestoreReadbackAuditOnly()
    {
        Directory.CreateDirectory(Output);var c=LoadConfig();var s=Source.Read(c.oracle);var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(c.model));Mesh clone=null;
        try{clone=RestoreWeights(s,actor.GetComponentInChildren<SkinnedMeshRenderer>(),new List<string>(),out _);Debug.Log("AGENT_RESULT fullsource mapping/namedinfluence readback audited; no poses/captures or animation acceptance");}
        finally{UnityEngine.Object.DestroyImmediate(actor);if(clone)UnityEngine.Object.DestroyImmediate(clone);}
    }
    public static void CoordinateWitness()
    {
        var c=LoadConfig();var s=Source.Read(c.oracle,false);var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(c.model));
        try{var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var rows=new List<string>{"kind,index,name,x,y,z,b0,w0,b1,w1,b2,w2,b3,w3"};
        var importer=(ModelImporter)AssetImporter.GetAtPath(c.model);rows.Add("rootDeterminant,"+actor.transform.localToWorldMatrix.determinant.ToString("R"));rows.Add("rendererDeterminant,"+skin.transform.localToWorldMatrix.determinant.ToString("R"));rows.Add("bakeAxisConversion,"+importer.bakeAxisConversion);rows.Add("useFileScale,"+importer.useFileScale);rows.Add("globalScale,"+importer.globalScale.ToString("R"));
        for(int i=0;i<32;i++){var p=skin.transform.TransformPoint(vertices[i]);var w=weights[i];rows.Add(string.Join(",","import",i,"vertex",p.x.ToString("R"),p.y.ToString("R"),p.z.ToString("R"),skin.bones[w.boneIndex0].name,w.weight0.ToString("R"),skin.bones[w.boneIndex1].name,w.weight1.ToString("R"),skin.bones[w.boneIndex2].name,w.weight2.ToString("R"),skin.bones[w.boneIndex3].name,w.weight3.ToString("R")));var q=s.Point(i);rows.Add(string.Join(",","sourceB",i,"vertex",q.x.ToString("R"),q.y.ToString("R"),q.z.ToString("R"),s.names[s.joints[i*4]],s.weights[i*4].ToString("R"),s.names[s.joints[i*4+1]],s.weights[i*4+1].ToString("R"),s.names[s.joints[i*4+2]],s.weights[i*4+2].ToString("R"),s.names[s.joints[i*4+3]],s.weights[i*4+3].ToString("R")));}
        for(int i=0;i<skin.bones.Length;i++){var actual=skin.transform.localToWorldMatrix*mesh.bindposes[i].inverse;int j=System.Array.IndexOf(s.names,skin.bones[i].name);var expected=CanonicalBasis*s.rigWorld*s.rest[j]*CanonicalBasis.inverse;rows.Add("importBind,"+i+","+skin.bones[i].name+","+actual[0,3].ToString("R")+","+actual[1,3].ToString("R")+","+actual[2,3].ToString("R"));rows.Add("sourceBind,"+i+","+s.names[j]+","+expected[0,3].ToString("R")+","+expected[1,3].ToString("R")+","+expected[2,3].ToString("R"));}
        var triangles=mesh.triangles;for(int i=0;i<24;i++){var p=skin.transform.TransformPoint(vertices[triangles[i]]);rows.Add("importTriangleCorner,"+i+","+triangles[i]+","+p.x.ToString("R")+","+p.y.ToString("R")+","+p.z.ToString("R"));var q=s.Point(s.triangles[i]);rows.Add("sourceTriangleCorner,"+i+","+s.triangles[i]+","+q.x.ToString("R")+","+q.y.ToString("R")+","+q.z.ToString("R"));}
        Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/unity-coordinate-witness.csv",rows);Debug.Log("AGENT_RESULT coordinate witness only; no parity acceptance");}finally{UnityEngine.Object.DestroyImmediate(actor);}
    }
    public static void Vertex302Witness()
    {
        Directory.CreateDirectory(Output);QualitySettings.skinWeights=SkinWeights.Unlimited;var c=LoadConfig();var s=Source.Read(c.oracle);var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(c.model));
        try
        {
            var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();skin.quality=SkinQuality.Auto;var mesh=skin.sharedMesh;var importer=(ModelImporter)AssetImporter.GetAtPath(c.model);var v=mesh.vertices;var bw=mesh.boneWeights;int at=302;Vector3 p=skin.transform.TransformPoint(v[at]);
            var rows=new List<string>{"Imported vertex302 world="+p.ToString("R"),"QualitySettings.skinWeights="+QualitySettings.skinWeights,"renderer.quality="+skin.quality,"importer.skinWeights="+importer.skinWeights,"importer.maxBonesPerVertex="+importer.maxBonesPerVertex,"importer.minBoneWeight="+importer.minBoneWeight.ToString("R"),"importer.meshCompression="+importer.meshCompression,"vertices="+mesh.vertexCount+" bones="+skin.bones.Length+" bindposes="+mesh.bindposes.Length,"normals="+mesh.normals.Length+" tangents="+mesh.tangents.Length};
            var legacy=bw[at];int[] bi={legacy.boneIndex0,legacy.boneIndex1,legacy.boneIndex2,legacy.boneIndex3};float[] ww={legacy.weight0,legacy.weight1,legacy.weight2,legacy.weight3};for(int k=0;k<4;k++)rows.Add("Legacy4 "+skin.bones[bi[k]].name+" "+ww[k].ToString("R")+Lattice(ww[k]));
            var counts=mesh.GetBonesPerVertex();var all=mesh.GetAllBoneWeights();try{int offset=0;for(int i=0;i<at;i++)offset+=counts[i];rows.Add("EffectiveBonesPerVertex302="+counts[at]);for(int k=0;k<counts[at];k++){var weight=all[offset+k];rows.Add("BoneWeight1 "+skin.bones[weight.boneIndex].name+" "+weight.weight.ToString("R")+Lattice(weight.weight));}}finally{counts.Dispose();all.Dispose();}
            var nearest=new List<KeyValuePair<float,int>>();for(int i=0;i<s.positions.Length/3;i++){float distance=Vector3.Distance(p,s.Point(i));if(nearest.Count<5||distance<nearest[nearest.Count-1].Key){nearest.Add(new KeyValuePair<float,int>(distance,i));nearest.Sort((a,b)=>a.Key.CompareTo(b.Key));if(nearest.Count>5)nearest.RemoveAt(5);}}
            foreach(var pair in nearest){int i=pair.Value;rows.Add("SourceCandidate "+i+" distanceM="+pair.Key.ToString("R")+" world="+s.Point(i).ToString("R"));for(int k=0;k<4;k++)rows.Add("SourceWeight "+s.names[s.joints[i*4+k]]+" "+s.weights[i*4+k].ToString("R")+Lattice(s.weights[i*4+k]));}
            File.WriteAllLines(Output+"/unity-vertex302-witness.txt",rows);Debug.Log("AGENT_RESULT vertex302 namedweight/position/effectiveinfluence witness only; no acceptance");
        }
        finally{UnityEngine.Object.DestroyImmediate(actor);}
    }
    public static void WeightRestorationSemantics()
    {
        var c=LoadConfig();var s=Source.Read(c.oracle);var original=AssetDatabase.LoadAssetAtPath<GameObject>(c.model).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
        var mesh=new Mesh{name="Source302WeightAPIProbeOnly"};mesh.vertices=new[]{Vector3.zero};mesh.bindposes=original.bindposes;
        var counts=new NativeArray<byte>(1,Allocator.Temp);var values=new NativeArray<BoneWeight1>(3,Allocator.Temp);counts[0]=3;
        // Full source tuple, deliberately including the importer-pruned toe.
        var sourcePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(c.model);var bones=sourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>().bones.Select(t=>t.name).ToArray();var list=new List<BoneWeight1>();for(int k=0;k<4;k++)if(s.weights[302*4+k]>0)list.Add(new BoneWeight1{boneIndex=System.Array.IndexOf(bones,s.names[s.joints[302*4+k]]),weight=s.weights[302*4+k]});list.Sort((a,b)=>b.weight.CompareTo(a.weight));for(int i=0;i<3;i++)values[i]=list[i];
        var rows=new List<string>{"Review-only onevertex Mesh.SetBoneWeights source302 precision probe; no original mesh/source changes."};
        try{mesh.SetBoneWeights(counts,values);var actualCounts=mesh.GetBonesPerVertex();var actual=mesh.GetAllBoneWeights();try{rows.Add("EffectiveCount="+actualCounts[0]);if(actualCounts[0]!=3)throw new Exception("SetBoneWeights pruned original source influence");for(int i=0;i<3;i++){rows.Add(bones[actual[i].boneIndex]+" source="+values[i].weight.ToString("R")+" readback="+actual[i].weight.ToString("R")+" delta="+((double)actual[i].weight-values[i].weight).ToString("R")+Lattice(actual[i].weight));if(actual[i].boneIndex!=values[i].boneIndex||BitConverter.SingleToInt32Bits(actual[i].weight)!=BitConverter.SingleToInt32Bits(values[i].weight))throw new Exception("Engine weight readback differs; independent surface error gate required");}rows.Add("PASS_BITEXACT_CPU_READBACK; GPU/player surface remains separate.");}finally{actualCounts.Dispose();actual.Dispose();}}
        finally{counts.Dispose();values.Dispose();UnityEngine.Object.DestroyImmediate(mesh);Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/unity-setboneweights-precision.txt",rows);}
        Debug.Log("AGENT_RESULT source302 SetBoneWeights no-pruning CPU-readback gate passed; not GPU/player acceptance");
    }
    static string Lattice(float weight){double integer=Math.Round((double)weight*65535),nearest=integer/65535;return " k65535="+integer+" latticeResidual="+((double)weight-nearest).ToString("R");}
    static double Angle(Quaternion a,Quaternion b){double dot=(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z+(double)a.w*b.w;double aa=(double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z+(double)a.w*a.w,bb=(double)b.x*b.x+(double)b.y*b.y+(double)b.z*b.z+(double)b.w*b.w;return 2*Math.Acos(Math.Min(1,Math.Abs(dot)/Math.Sqrt(aa*bb)))*180/Math.PI;}

    public static void ConfigureImport()
    {
        var c=LoadConfig();var m=(ModelImporter)AssetImporter.GetAtPath(c.model);
        m.animationType=ModelImporterAnimationType.Generic;m.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        m.meshCompression=ModelImporterMeshCompression.Off;m.meshOptimizationFlags=0;m.optimizeBones=false;m.optimizeGameObjects=false;
        m.weldVertices=false;m.isReadable=true;m.importBlendShapes=true;m.importNormals=ModelImporterNormals.Import;m.importTangents=ModelImporterTangents.Import;
        m.skinWeights=ModelImporterSkinWeights.Custom;m.maxBonesPerVertex=4;m.minBoneWeight=0;
        m.importAnimation=true;m.animationCompression=ModelImporterAnimationCompression.Off;m.resampleCurves=false;
        var takes=m.defaultClipAnimations;foreach(var take in takes){take.loopTime=false;take.loopPose=false;}m.clipAnimations=takes;
        m.materialImportMode=ModelImporterMaterialImportMode.None;m.SaveAndReimport();
        foreach(var path in new[]{c.baseColor,c.normal,c.roughness,c.metallic})
        {
            if(!path.StartsWith(Root+"/",StringComparison.Ordinal))throw new Exception("Texture outside owned folder");
            var t=(TextureImporter)AssetImporter.GetAtPath(path);t.maxTextureSize=4096;t.textureCompression=TextureImporterCompression.Uncompressed;t.crunchedCompression=false;t.isReadable=true;
            t.sRGBTexture=path==c.baseColor;t.textureType=path==c.normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            t.SaveAndReimport();
        }
        Debug.Log("AGENT_RESULT DenseRigify scoped Generic import configured; no parity/visual acceptance claimed");
    }

    static IEnumerator pendingReview;
    static int reviewTicks;
    public static void RunFramedReview()
    {
        if(pendingReview!=null)throw new Exception("Review already running");Output=BaseOutput+"/framed-gpu-review-v1";pendingReview=RunSteps(true);reviewTicks=0;EditorApplication.update+=PumpReview;EditorApplication.QueuePlayerLoopUpdate();
    }
    static void PumpReview()
    {
        try{reviewTicks++;if(pendingReview.MoveNext()){EditorApplication.QueuePlayerLoopUpdate();return;}((IDisposable)pendingReview).Dispose();pendingReview=null;EditorApplication.update-=PumpReview;EditorApplication.Exit(0);}
        catch(Exception error){Debug.LogException(error);(pendingReview as IDisposable)?.Dispose();pendingReview=null;EditorApplication.update-=PumpReview;EditorApplication.Exit(1);}
    }
    public static void Run(){Output=BaseOutput;var e=RunSteps(false);try{while(e.MoveNext()){} }finally{(e as IDisposable)?.Dispose();}}
    static IEnumerator RunSteps(bool framed)
    {
        Directory.CreateDirectory(Output);if(framed)AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);var c=LoadConfig();var source=Source.Read(c.oracle);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(c.model);var scene=EditorSceneManager.NewPreviewScene();var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);Mesh restoredMesh=null;int[] restoredMapping=null;
        try
        {
            QualitySettings.skinWeights=SkinWeights.Unlimited;
            var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);if(skins.Length!=1)throw new Exception("Review currently requires exactly one original skin");var skin=skins[0];skin.quality=SkinQuality.Auto;skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;
            var report=new List<string>{"Combined model: "+c.model,"Source vertices: "+source.positions.Length/3,"Source triangles: "+source.triangles.Length/3,"Imported vertices: "+skin.sharedMesh.vertexCount,"Original indexed source geometry, no density reduction."};
            if(c.restoreOriginalSourceWeights){restoredMesh=RestoreWeights(source,skin,report,out restoredMapping);skin.sharedMesh=restoredMesh;}
            int[] originalToImported=Geometry(source,skin,report);File.WriteAllLines(Output+"/unity-geometry-bind.txt",report);
            // A RED geometry gate aborts before any animation/visual conclusion.
            var clips=AssetDatabase.LoadAllAssetsAtPath(c.model).OfType<AnimationClip>().Where(a=>!a.name.StartsWith("__preview__")).ToArray();
            var animator=actor.GetComponentInChildren<Animator>();if(!animator)animator=actor.AddComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.updateMode=AnimatorUpdateMode.Normal;animator.applyRootMotion=false;
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/DenseReview.controller");if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/DenseReview.controller");var sm=controller.layers[0].stateMachine;foreach(var clip in clips){var state=sm.states.FirstOrDefault(x=>x.state.name==clip.name).state;if(!state)state=sm.AddState(clip.name);state.motion=clip;}EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();animator.runtimeAnimatorController=controller;animator.Rebind();
            bool materialReady=false;try{skin.sharedMaterial=CreateOriginalMaterial(c);materialReady=true;report.Add("Original4K material review setup PASS; separate linear scalar pack, source bytes retained.");}catch(Exception e){report.Add("MATERIAL_BLOCKED "+e.Message);Debug.LogWarning("Material gate blocked independently: "+e.Message);}
            if(framed&&materialReady)
            {
                yield return null;yield return null;
                var probe=ProbeMaterialGPU(skin.sharedMaterial);try{while(probe.MoveNext())yield return null;}finally{(probe as IDisposable)?.Dispose();}
                ShaderUtil.CompilePass(skin.sharedMaterial,0,true);yield return null;
            }
            var rows=new List<string>{"clip,phase,seconds,surfaceMaxM,surfaceRmsM,boneJointMaxM,boneAngleMaxDeg"};var weightRows=new List<string>{"clip,phase,fullImportedVertices,weightOnlySurfaceMaxM,weightOnlySurfaceRmsM"};
            foreach(var p in source.poses)
            {
                var clip=clips.Single(a=>a.name.IndexOf(p.clip,StringComparison.OrdinalIgnoreCase)>=0);float seconds=(float)(p.phase*clip.length);animator.Play(Animator.StringToHash(clip.name),0,(float)p.phase);animator.Update(0);
                if(framed){yield return null;yield return null;animator.Play(Animator.StringToHash(clip.name),0,(float)p.phase);animator.Update(0);yield return null;}
                var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*skin.sharedMesh.bindposes[i]).ToArray();var verts=skin.sharedMesh.vertices;var bw=ReadTypedWeights(skin.sharedMesh);double max=0,sum=0;
                if(restoredMapping!=null){var error=WeightOnlySurfaceError(source,skin,restoredMapping,matrices,verts,bw);weightRows.Add(string.Join(",",p.clip,p.phase,verts.Length,error.x,error.y));File.WriteAllLines(Output+"/unity-restored-weight-fullpose-bound.csv",weightRows);if(error.x>.0001f)report.Add("WEIGHT_SURFACE_RED "+p.clip+" phase="+p.phase+" maxM="+error.x);}
                for(int i=0;i<source.samples.Length;i++){int j=originalToImported[source.samples[i]];var w=bw[j];var v=verts[j];Vector3 q=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;double e=Vector3.Distance(q,p.points[i]);max=Math.Max(max,e);sum+=e*e;}
                double joint=0,angle=0;for(int i=0;i<source.names.Length;i++){var b=skin.bones.Single(t=>t.name==source.names[i]);var predicted=p.bones[i]*reviewBoneConventions[i];joint=Math.Max(joint,Vector3.Distance(b.position,predicted.GetColumn(3)));angle=Math.Max(angle,Angle(b.rotation,predicted.rotation));}
                rows.Add(string.Join(",",p.clip,p.phase,seconds,max,Math.Sqrt(sum/source.samples.Length),joint,angle));File.WriteAllLines(Output+"/unity-keyed-skin.csv",rows);
                if(max>.0001||angle>.01)report.Add("KEYED_RED "+p.clip+" phase="+p.phase+" surface="+max+" angle="+angle);
                if(materialReady&&p.clip=="dance_01"&&p.phase>.2&&p.phase<.8)Capture(skin,p.phase,report);
            }
            File.WriteAllLines(Output+"/unity-review-status.txt",report.Concat(new[]{"Off-key original evaluator parity: OPEN unless separately supplied.","Full-motion player/camera/floor/entry-return: OPEN.","No catalog promotion or phone claim."}));
            Debug.Log("AGENT_RESULT DenseRigify geometry/bind and keyed review executed; inspect separate gates; no promotion");
        }
        finally{UnityEngine.Object.DestroyImmediate(actor);if(restoredMesh)UnityEngine.Object.DestroyImmediate(restoredMesh);EditorSceneManager.ClosePreviewScene(scene);}
    }
    static BoneWeight[] ReadTypedWeights(Mesh mesh)
    {
        var counts=mesh.GetBonesPerVertex();var all=mesh.GetAllBoneWeights();try{var result=new BoneWeight[counts.Length];int at=0;for(int i=0;i<counts.Length;i++){if(counts[i]>4)throw new Exception("Source max4 proof invalidated");var w=new BoneWeight();for(int k=0;k<counts[i];k++){var b=all[at++];if(k==0){w.boneIndex0=b.boneIndex;w.weight0=b.weight;}else if(k==1){w.boneIndex1=b.boneIndex;w.weight1=b.weight;}else if(k==2){w.boneIndex2=b.boneIndex;w.weight2=b.weight;}else{w.boneIndex3=b.boneIndex;w.weight3=b.weight;}}result[i]=w;}return result;}finally{counts.Dispose();all.Dispose();}
    }
    static Vector2 WeightOnlySurfaceError(Source source,SkinnedMeshRenderer skin,int[] mapped,Matrix4x4[] matrices,Vector3[] vertices,BoneWeight[] readback)
    {
        var lookup=source.names.Select(name=>System.Array.FindIndex(skin.bones,t=>t.name==name)).ToArray();double maximum=0,sum=0;
        // Same unchanged imported positions/matrices on both sides isolates only
        // original source desired weights versus engine readback. No Animator
        // sampling/interpolation error is included in this independent gate.
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 v=vertices[i],desired=Vector3.zero;int id=mapped[i];for(int k=0;k<4;k++)if(source.weights[id*4+k]>0)desired+=matrices[lookup[source.joints[id*4+k]]].MultiplyPoint3x4(v)*source.weights[id*4+k];
            var w=readback[i];var actual=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;double error=Vector3.Distance(desired,actual);maximum=Math.Max(maximum,error);sum+=error*error;
        }
        return new Vector2((float)maximum,(float)Math.Sqrt(sum/vertices.Length));
    }

    static Mesh RestoreWeights(Source s,SkinnedMeshRenderer skin,List<string> report,out int[] sourceMapping)
    {
        sourceMapping=null;var original=skin.sharedMesh;var positions=original.vertices;var uv=original.uv;var legacy=ReadTypedWeights(original);int n=s.positions.Length/3;
        var canonical=new int[n];var next=new int[n];var identities=new Dictionary<VertexKey,int>();var cells=new Dictionary<Cell,int>();var b=new int[4];var w=new float[4];
        for(int i=0;i<n;i++){for(int k=0;k<4;k++){b[k]=s.joints[i*4+k];w[k]=s.weights[i*4+k];}Canonical(b,w);var p=s.Point(i);var key=new VertexKey(p,b,w);if(!identities.TryGetValue(key,out int id)){id=i;identities.Add(key,id);}canonical[i]=id;var cell=new Cell(p);next[i]=cells.TryGetValue(cell,out int head)?head:-1;cells[cell]=i;}
        var uvLookup=new UVLookup();for(int i=0;i<s.triangles.Length;i++)uvLookup.Add(canonical[s.triangles[i]],new Vector2(s.uv[i*2],s.uv[i*2+1]));
        var mapped=new int[positions.Length];float maxPosition=0;
        for(int i=0;i<positions.Length;i++)
        {
            Vector3 p=skin.transform.TransformPoint(positions[i]);var cell=new Cell(p);int chosen=-1;
            for(int dz=-1;dz<=1;dz++)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(cells.TryGetValue(new Cell(cell.x+dx,cell.y+dy,cell.z+dz),out int candidate))for(;candidate>=0;candidate=next[candidate])
            {
                if(Vector3.Distance(p,s.Point(candidate))>.000001f)continue;int id=canonical[candidate];try{uvLookup.Match(id,uv[i]);}catch{continue;}
                if(chosen>=0&&chosen!=id)throw new Exception("Indeterminate source weight ownership: coincident position/UV with different original weights at imported "+i);chosen=id;
            }
            if(chosen<0)throw new Exception("Cannot restore weights without verified position+UV ownership at imported "+i);mapped[i]=chosen;maxPosition=Mathf.Max(maxPosition,Vector3.Distance(p,s.Point(chosen)));
        }
        // Verify every original corner/face BEFORE changing any weight.
        VerifyFaces(s,canonical,mapped,uv,original.triangles,uvLookup);
        var importedBoneNames=skin.bones.Select(t=>t.name).ToArray();var sourceToImported=s.names.Select(name=>System.Array.IndexOf(importedBoneNames,name)).ToArray();if(sourceToImported.Any(i=>i<0))throw new Exception("Original named bone missing");
        int influences=0,prunedVertices=0,missingInfluences=0,changedVertices=0;float minimum=float.PositiveInfinity;
        for(int i=0;i<mapped.Length;i++)for(int k=0;k<4;k++)if(s.weights[mapped[i]*4+k]>0)influences++;
        var counts=new NativeArray<byte>(mapped.Length,Allocator.Temp);var values=new NativeArray<BoneWeight1>(influences,Allocator.Temp);Mesh clone=null;
        try
        {
            int offset=0;for(int i=0;i<mapped.Length;i++)
            {
                int id=mapped[i];var q=legacy[i];int[] oldBones={q.boneIndex0,q.boneIndex1,q.boneIndex2,q.boneIndex3};float[] oldWeights={q.weight0,q.weight1,q.weight2,q.weight3};var list=new List<BoneWeight1>(4);bool pruned=false,changed=false;
                for(int k=0;k<4;k++){float weight=s.weights[id*4+k];if(weight<=0)continue;int bone=sourceToImported[s.joints[id*4+k]];minimum=Mathf.Min(minimum,weight);list.Add(new BoneWeight1{boneIndex=bone,weight=weight});int at=System.Array.FindIndex(oldBones,j=>j==bone);if(at<0||oldWeights[at]<=0){missingInfluences++;pruned=true;changed=true;}else if(BitConverter.SingleToInt32Bits(oldWeights[at])!=BitConverter.SingleToInt32Bits(weight))changed=true;}
                if(pruned)prunedVertices++;if(changed)changedVertices++;list.Sort((a,bb)=>bb.weight.CompareTo(a.weight));counts[i]=(byte)list.Count;foreach(var value in list)values[offset++]=value;
            }
            clone=UnityEngine.Object.Instantiate(original);clone.name="DenseOriginalSourceWeights_ReviewOnly";clone.SetBoneWeights(counts,values);
            int lost=0,unexpected=0,countMismatch=0,changedFloatCount=0;double maxDelta=0,maxLattice=0,maxNormalization=0;float readbackMin=float.PositiveInfinity;
            var actualCounts=clone.GetBonesPerVertex();var actual=clone.GetAllBoneWeights();try
            {
                int desiredOffset=0,actualOffset=0;for(int vertex=0;vertex<counts.Length;vertex++)
                {
                    if(actualCounts[vertex]!=counts[vertex])countMismatch++;double sum=0;for(int k=0;k<counts[vertex];k++)sum+=values[desiredOffset+k].weight;
                    for(int k=0;k<counts[vertex];k++){var desired=values[desiredOffset+k];int found=-1;for(int j=0;j<actualCounts[vertex];j++)if(actual[actualOffset+j].boneIndex==desired.boneIndex){found=actualOffset+j;break;}if(found<0||actual[found].weight<=0){lost++;continue;}float read=actual[found].weight;readbackMin=Mathf.Min(readbackMin,read);double delta=Math.Abs((double)read-desired.weight);if(BitConverter.SingleToInt32Bits(read)!=BitConverter.SingleToInt32Bits(desired.weight))changedFloatCount++;maxDelta=Math.Max(maxDelta,delta);maxLattice=Math.Max(maxLattice,Math.Abs(read-Math.Round((double)read*65535)/65535));maxNormalization=Math.Max(maxNormalization,Math.Abs(read-desired.weight/sum));}
                    for(int j=0;j<actualCounts[vertex];j++){bool found=false;for(int k=0;k<counts[vertex];k++)if(actual[actualOffset+j].boneIndex==values[desiredOffset+k].boneIndex)found=true;if(!found&&actual[actualOffset+j].weight>0)unexpected++;}
                    desiredOffset+=counts[vertex];actualOffset+=actualCounts[vertex];
                }
            }
            finally{actualCounts.Dispose();actual.Dispose();}
            var audit=new List<string>{"REVIEW ONLY: original desired write and engine readback separate; raw source/FBX unchanged.","Source mapped via fullposition+UV+corner topology before any mutation; maxPositionM="+maxPosition,"Imported vertices="+mapped.Length+" desired nonzero influences="+influences+" sourceMinimumPositive="+minimum.ToString("R"),"ImporterPrunedVertices="+prunedVertices+" importerMissingInfluences="+missingInfluences+" importerChangedVertices="+changedVertices,"Readback lostNamedPositiveInfluences="+lost+" unexpectedNamedInfluences="+unexpected+" verticesWithCountMismatch="+countMismatch,"Readback changedFloatInfluences="+changedFloatCount+" maxAbsoluteWeightDelta="+maxDelta.ToString("R")+" maxNearest65535LatticeResidual="+maxLattice.ToString("R")+" maxNormalizeSourceSumResidual="+maxNormalization.ToString("R")+" minimumPositiveReadback="+readbackMin.ToString("R"),"Imported vertex attributes="+string.Join(";",original.GetVertexAttributes().Select(a=>a.attribute+":"+a.format+":"+a.dimension)),"Restored vertex attributes="+string.Join(";",clone.GetVertexAttributes().Select(a=>a.attribute+":"+a.format+":"+a.dimension))};
            File.WriteAllLines(Output+"/unity-original-weight-restoration.txt",audit);
            if(lost>0||unexpected>0||countMismatch>0)throw new Exception("Engine readback lost/changed original named influence presence; audit retained");
            if(!clone.vertices.SequenceEqual(original.vertices)||!clone.normals.SequenceEqual(original.normals)||!clone.tangents.SequenceEqual(original.tangents)||!clone.uv.SequenceEqual(original.uv)||!clone.triangles.SequenceEqual(original.triangles)||!clone.bindposes.SequenceEqual(original.bindposes))throw new Exception("Restoration altered a nonweight buffer");
            audit.Add("PASS no pruning/unexpected named weights; nonweight positions/normals/tangents/UV/triangles/binds bitexact clone retention. Engine normalization roundoff quantified above; independent fullpose bound required. GPU/player remains OPEN.");File.WriteAllLines(Output+"/unity-original-weight-restoration.txt",audit);sourceMapping=mapped;
            report.Add("Original source desired weight write retained all named influences; engine readback normalization measured separately; nonweight buffers retained.");return clone;
        }
        catch{if(clone)UnityEngine.Object.DestroyImmediate(clone);throw;}
        finally{counts.Dispose();values.Dispose();}
    }

    static Material CreateOriginalMaterial(Config c)
    {
        if(!c.originalMaterialFactorsVerified)throw new Exception("Original material factors/channel provenance not provided; no guessed material acceptance");
        if(c.metallicFactor!=1||c.roughnessFactor!=1)throw new Exception("Nonunit source scalar factors require a float shader/packing extension; no silent8bit factor quantization");
        var rough=AssetDatabase.LoadAssetAtPath<Texture2D>(c.roughness);var metal=AssetDatabase.LoadAssetAtPath<Texture2D>(c.metallic);
        foreach(var texture in new[]{rough,metal,AssetDatabase.LoadAssetAtPath<Texture2D>(c.baseColor),AssetDatabase.LoadAssetAtPath<Texture2D>(c.normal)})if(texture.width!=4096||texture.height!=4096)throw new Exception("Original4K texture resized");
        var rr=rough.GetPixels32();var mm=metal.GetPixels32();var pack=new Texture2D(4096,4096,TextureFormat.RGBA32,false,true);var pixels=new Color32[rr.Length];
        // Separate scalar maps: R(metal)*factor, A=1-R(rough)*factor.
        // Importers above are linear for both data maps; no additional decode.
        for(int i=0;i<pixels.Length;i++){if(rr[i].r!=rr[i].g||rr[i].r!=rr[i].b||mm[i].r!=mm[i].g||mm[i].r!=mm[i].b)throw new Exception("Non-grayscale scalar source requires explicit Color-to-Value conversion proof");pixels[i]=new Color32(mm[i].r,0,0,(byte)(255-rr[i].r));}
        pack.SetPixels32(pixels);pack.Apply(false,false);string path=Root+"/DenseOriginalLinearMetalSmooth.png";var encoded=pack.EncodeToPNG();bool changed=!File.Exists(path)||!File.ReadAllBytes(path).SequenceEqual(encoded);if(changed)File.WriteAllBytes(path,encoded);UnityEngine.Object.DestroyImmediate(pack);if(changed)AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer.sRGBTexture||!importer.isReadable||importer.maxTextureSize!=4096||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.crunchedCompression){importer.sRGBTexture=false;importer.isReadable=true;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.crunchedCompression=false;importer.SaveAndReimport();}
        var packedTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var actual=packedTexture.GetPixels32();if(actual.Length!=pixels.Length)throw new Exception("Packed texture resized");for(int i=0;i<actual.Length;i++)if(!actual[i].Equals(pixels[i]))throw new Exception("Linear shader packing readback changed byte at "+i);
        File.WriteAllLines(Output+"/unity-material-packing.txt",new[]{"PASS full16777216 decoded pixels: both separate scalar sources grayscale; packedR=originalMetalR; packedA=255-originalRoughR; import readback byteexact.","Original source factors1, normalStrength1; base sRGB; normal/rough/metal/derived pack linear; actual4096x4096; compressionOff; no extra gamma decode.","Standard shader review material; this verifies maps/packing, not exact cross-renderer BRDF parity."});
        string materialPath=Root+"/DenseOriginal4K.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,materialPath);}mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(c.baseColor);mat.color=new Color(c.baseR,c.baseG,c.baseB,c.baseA);mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(c.normal));mat.SetFloat("_BumpScale",c.normalScale);mat.SetTexture("_MetallicGlossMap",packedTexture);mat.SetFloat("_GlossMapScale",1);mat.SetFloat("_Metallic",1);mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICGLOSSMAP");EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();return mat;
    }

    static IEnumerator ProbeMaterialGPU(Material sourceMaterial)
    {
        var texture=(Texture2D)sourceMaterial.GetTexture("_MetallicGlossMap");var shader=Shader.Find("DenseRigifyReview/ScalarGPUProbe");if(!shader||!shader.isSupported)throw new Exception("GPU scalar probe shader unavailable");var material=new Material(shader);material.SetTexture("_ScalarMap",texture);
        var rt=new RenderTexture(1,1,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);rt.Create();var rows=new List<string>{"u,v,attempt,cpuMetalR,gpuMetalR,cpuSmoothA,gpuSmoothA,maxAbsError,editorTick,frameCount,renderedFrameCount"};
        try{foreach(var requestedUV in new[]{new Vector2(.1f,.1f),new Vector2(.25f,.75f),new Vector2(.5f,.5f),new Vector2(.9f,.8f)})
        {
            int x=Mathf.Clamp(Mathf.FloorToInt(requestedUV.x*texture.width),0,texture.width-1),y=Mathf.Clamp(Mathf.FloorToInt(requestedUV.y*texture.height),0,texture.height-1);var uv=new Vector2((x+.5f)/texture.width,(y+.5f)/texture.height);var expected=texture.GetPixel(x,y);double deadline=EditorApplication.timeSinceStartup+15;int attempt=0;
            while(true)
            {
                attempt++;material.SetVector("_ProbeUV",new Vector4(uv.x,uv.y,0,0));var previous=RenderTexture.active;Graphics.Blit(Texture2D.blackTexture,rt,material);RenderTexture.active=previous;var request=AsyncGPUReadback.Request(rt,0,TextureFormat.RGBAFloat);
                while(!request.done){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("GPU scalar fence timeout");yield return null;}if(request.hasError)throw new Exception("GPU scalar fence readback failed");var actual=request.GetData<Color>()[0];float error=Mathf.Max(Mathf.Abs(actual.r-expected.r),Mathf.Abs(actual.g-expected.a));rows.Add(string.Join(",",uv.x,uv.y,attempt,expected.r,actual.r,expected.a,actual.g,error,reviewTicks,Time.frameCount,Time.renderedFrameCount));File.WriteAllLines(Output+"/unity-gpu-scalar-readback.csv",rows);
                if(error<=.00002f)break;if(EditorApplication.timeSinceStartup>deadline)throw new Exception("GPU scalar material inputs did not converge to CPU: "+error);yield return null;
            }
            yield return null;
        }}finally{rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(material);}
    }

    static void MaskAgreement(SkinnedMeshRenderer skin,Camera camera,RenderTexture rt,Vector3[] vertices,BoneWeight[] weights,Matrix4x4[] matrices,double phase,int side)
    {
        Vector2 cpuMin=Vector2.one*float.PositiveInfinity,cpuMax=Vector2.one*float.NegativeInfinity;for(int i=0;i<vertices.Length;i++){var w=weights[i];var v=vertices[i];var p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;var q=camera.WorldToViewportPoint(p);cpuMin=Vector2.Min(cpuMin,new Vector2(q.x,q.y));cpuMax=Vector2.Max(cpuMax,new Vector2(q.x,q.y));}
        var original=skin.sharedMaterial;var background=camera.backgroundColor;var mask=new Material(Shader.Find("Unlit/Color"));mask.color=Color.white;ShaderUtil.CompilePass(mask,0,true);Texture2D snapshot=null;
        try{skin.sharedMaterial=mask;camera.backgroundColor=Color.black;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;snapshot=new Texture2D(1024,1024,TextureFormat.RGB24,false);snapshot.ReadPixels(new Rect(0,0,1024,1024),0,0);snapshot.Apply();RenderTexture.active=old;var pixels=snapshot.GetPixels32();int minX=1024,minY=1024,maxX=-1,maxY=-1;for(int y=0;y<1024;y++)for(int x=0;x<1024;x++)if(pixels[y*1024+x].r>127){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}float difference=Mathf.Max(Mathf.Abs(minX-cpuMin.x*1024),Mathf.Abs(minY-cpuMin.y*1024),Mathf.Abs(maxX-cpuMax.x*1024),Mathf.Abs(maxY-cpuMax.y*1024));var local=skin.localBounds;string line=string.Join(",",phase,side,cpuMin.x,cpuMin.y,cpuMax.x,cpuMax.y,minX,minY,maxX,maxY,difference,local.center.x,local.center.y,local.center.z,local.extents.x,local.extents.y,local.extents.z,skin.updateWhenOffscreen,skin.forceMatrixRecalculationPerRender,reviewTicks,Time.frameCount,Time.renderedFrameCount,camera.aspect,camera.projectionMatrix.m00,camera.projectionMatrix.m11);
            if(!File.Exists(Output+"/unity-render-mask-agreement.csv"))File.WriteAllText(Output+"/unity-render-mask-agreement.csv","phase,side,cpuMinX,cpuMinY,cpuMaxX,cpuMaxY,gpuMinPxX,gpuMinPxY,gpuMaxPxX,gpuMaxPxY,maxDifferencePx,localCenterX,localCenterY,localCenterZ,localExtentX,localExtentY,localExtentZ,updateWhenOffscreen,forceMatrices,editorTick,frameCount,renderedFrameCount,aspect,projection00,projection11"+Environment.NewLine);File.AppendAllText(Output+"/unity-render-mask-agreement.csv",line+Environment.NewLine);File.WriteAllBytes(Output+"/unity-dance-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"-"+side+"-mask.png",snapshot.EncodeToPNG());if(maxX<0||difference>3)throw new Exception("GPU silhouette vs CPU fullvertex projection RED: "+difference+"px; no automatic framing shrink");}
        finally{skin.sharedMaterial=original;camera.backgroundColor=background;if(snapshot)UnityEngine.Object.DestroyImmediate(snapshot);UnityEngine.Object.DestroyImmediate(mask);}
    }

    static void BakeAgreement(SkinnedMeshRenderer skin,Vector3[] vertices,BoneWeight[] weights,Matrix4x4[] matrices,double phase)
    {
        var baked=new Mesh();try
        {
            skin.BakeMesh(baked,false);var without=baked.vertices;skin.BakeMesh(baked,true);var with=baked.vertices;if(with.Length!=vertices.Length||without.Length!=vertices.Length)throw new Exception("BakeMesh vertex correspondence lost");var rigid=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);double maxFalse=0,maxTrue=0,maxTrueRigid=0;
            for(int i=0;i<vertices.Length;i++){var w=weights[i];var v=vertices[i];var expected=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;maxFalse=Math.Max(maxFalse,Vector3.Distance(expected,skin.transform.TransformPoint(without[i])));maxTrue=Math.Max(maxTrue,Vector3.Distance(expected,skin.transform.TransformPoint(with[i])));maxTrueRigid=Math.Max(maxTrueRigid,Vector3.Distance(expected,rigid.MultiplyPoint3x4(with[i])));}
            var scale=skin.transform.lossyScale;if(!File.Exists(Output+"/unity-bakemesh-coordinate-audit.csv"))File.WriteAllText(Output+"/unity-bakemesh-coordinate-audit.csv","phase,vertices,scaleX,scaleY,scaleZ,bakeFalseFullTRSMaxM,bakeTrueFullTRSMaxM,bakeTrueScaleFreeTRSMaxM,editorTick,frameCount"+Environment.NewLine);File.AppendAllText(Output+"/unity-bakemesh-coordinate-audit.csv",string.Join(",",phase,vertices.Length,scale.x,scale.y,scale.z,maxFalse,maxTrue,maxTrueRigid,reviewTicks,Time.frameCount)+Environment.NewLine);
        }
        finally{UnityEngine.Object.DestroyImmediate(baked);}
    }

    static void NoNormalDiagnostic(SkinnedMeshRenderer skin,Camera camera,RenderTexture rt,double phase)
    {
        var original=skin.sharedMaterial;var diagnostic=new Material(original);diagnostic.name="DiagnosticOnly_NoNormal";diagnostic.SetTexture("_BumpMap",null);diagnostic.DisableKeyword("_NORMALMAP");ShaderUtil.CompilePass(diagnostic,0,true);Texture2D snapshot=null;
        try{skin.sharedMaterial=diagnostic;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;snapshot=new Texture2D(1024,1024,TextureFormat.RGB24,false);snapshot.ReadPixels(new Rect(0,0,1024,1024),0,0);snapshot.Apply();RenderTexture.active=old;File.WriteAllBytes(Output+"/unity-dance-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"-positiveZ-neck-DIAGNOSTIC-no-normal.png",snapshot.EncodeToPNG());}
        finally{skin.sharedMaterial=original;if(snapshot)UnityEngine.Object.DestroyImmediate(snapshot);UnityEngine.Object.DestroyImmediate(diagnostic);}
    }

    static void IgnoredUniformDiagnostic(SkinnedMeshRenderer skin,Camera camera,RenderTexture rt)
    {
        var original=skin.sharedMaterial;var diagnostic=new Material(original);diagnostic.SetFloat("_Metallic",0);diagnostic.SetFloat("_Glossiness",0);ShaderUtil.CompilePass(diagnostic,0,true);Texture2D first=null,second=null;
        Texture2D Read(){var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1024,1024,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1024,1024),0,0);t.Apply();RenderTexture.active=old;return t;}
        try{camera.Render();first=Read();skin.sharedMaterial=diagnostic;camera.Render();second=Read();var a=first.GetPixels32();var b=second.GetPixels32();int max=0,changed=0;for(int i=0;i<a.Length;i++){int error=Math.Max(Math.Abs(a[i].r-b[i].r),Math.Max(Math.Abs(a[i].g-b[i].g),Math.Abs(a[i].b-b[i].b)));max=Math.Max(max,error);if(error>0)changed++;}File.WriteAllLines(Output+"/unity-standard-ignored-uniform-ab.txt",new[]{"Diagnostic only: _METALLICGLOSSMAP variant should ignore _Metallic/_Glossiness uniforms. Source material/factors unchanged.","VariantKeywords="+string.Join("|",original.shaderKeywords),"MetallicUniform1Versus0_MaxRGBByteDelta="+max+" changedPixels="+changed});File.WriteAllBytes(Output+"/unity-dance-0.50-positiveZ-neck-DIAGNOSTIC-uniforms0.png",second.EncodeToPNG());}
        finally{skin.sharedMaterial=original;if(first)UnityEngine.Object.DestroyImmediate(first);if(second)UnityEngine.Object.DestroyImmediate(second);UnityEngine.Object.DestroyImmediate(diagnostic);}
    }

    static void Capture(SkinnedMeshRenderer skin,double phase,List<string> report)
    {
        var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=ReadTypedWeights(mesh);var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();var bounds=new Bounds();bool first=true;float minimum=float.PositiveInfinity;
        for(int i=0;i<vertices.Length;i++){var w=weights[i];var v=vertices[i];var p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);minimum=Mathf.Min(minimum,p.y);}
        BakeAgreement(skin,vertices,weights,matrices,phase);
        var cameraObject=new GameObject("DenseRigifyReviewCamera");var lightObject=new GameObject("DenseRigifyReviewLight");SceneManager.MoveGameObjectToScene(cameraObject,skin.gameObject.scene);SceneManager.MoveGameObjectToScene(lightObject,skin.gameObject.scene);var camera=cameraObject.AddComponent<Camera>();camera.scene=skin.gameObject.scene;var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;lightObject.transform.rotation=Quaternion.Euler(35,-25,0);var oldAmbient=RenderSettings.ambientLight;RenderSettings.ambientLight=new Color(.35f,.35f,.35f);
        var rt=new RenderTexture(1024,1024,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.18f,.21f);camera.orthographic=true;camera.aspect=1;camera.nearClipPlane=.01f;camera.farClipPlane=20;
        try{foreach(int side in new[]{1,-1})foreach(bool neck in new[]{false,true})
        {
            Vector3 center=bounds.center;if(neck){var shoulder=skin.bones.Where(t=>t.name=="DEF-shoulder.L"||t.name=="DEF-shoulder.R").ToArray();if(shoulder.Length!=2)throw new Exception("Shoulder framing witness missing");center=(shoulder[0].position+shoulder[1].position)*.5f+Vector3.up*.055f;}
            camera.orthographicSize=neck?bounds.size.y*.205f:Mathf.Max(bounds.extents.y,bounds.extents.x)*1.2f;camera.transform.position=center+new Vector3(0,0,side*3f);camera.transform.LookAt(center);camera.ResetProjectionMatrix();camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var png=new Texture2D(1024,1024,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1024,1024),0,0);png.Apply();RenderTexture.active=old;File.WriteAllBytes(Output+"/unity-dance-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"-"+(side==1?"positiveZ":"negativeZ")+"-"+(neck?"neck":"fullbody")+".png",png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);
            if(neck&&side==1&&Math.Abs(phase-.5)<.00001){NoNormalDiagnostic(skin,camera,rt,phase);IgnoredUniformDiagnostic(skin,camera,rt);}
            if(!neck)MaskAgreement(skin,camera,rt,vertices,weights,matrices,phase,side);
            if(!neck){float margin=1;for(int k=0;k<8;k++){Vector3 p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1));Vector3 q=camera.WorldToViewportPoint(p);margin=Mathf.Min(margin,q.x,q.y,1-q.x,1-q.y);}report.Add("DANCE_CAPTURE phase="+phase+" side="+side+" fullSourceMinimumY="+minimum+" conservativeBoundsViewportMargin="+margin);if(margin<.05f)throw new Exception("Fullbody camera margin RED");}
        }}finally{RenderSettings.ambientLight=oldAmbient;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(lightObject);}
    }

    static int[] Geometry(Source s,SkinnedMeshRenderer skin,List<string> report)
    {
        var mesh=skin.sharedMesh;var v=mesh.vertices;var uv=mesh.uv;var bw=ReadTypedWeights(mesh);var sourceIds=new Dictionary<VertexKey,int>();var cells=new Dictionary<Cell,int>();int n=s.positions.Length/3;var canonical=new int[n];var next=new int[n];var b=new int[4];var w=new float[4];
        for(int i=0;i<n;i++){for(int k=0;k<4;k++){b[k]=s.joints[i*4+k];w[k]=s.weights[i*4+k];}Canonical(b,w);Vector3 point=s.Point(i);var key=new VertexKey(point,b,w);if(!sourceIds.TryGetValue(key,out int id)){id=i;sourceIds.Add(key,id);}canonical[i]=id;var cell=new Cell(point);next[i]=cells.TryGetValue(cell,out int head)?head:-1;cells[cell]=i;}
        var boneMap=skin.bones.Select(t=>System.Array.IndexOf(s.names,t.name)).ToArray();if(boneMap.Any(i=>i<0))throw new Exception("Unexpected imported weighted bone");
        var importedIds=new int[v.Length];var originalToImported=Enumerable.Repeat(-1,n).ToArray();float maxPosition=0,maxWeight=0;
        for(int i=0;i<v.Length;i++)
        {
            var q=bw[i];b[0]=boneMap[q.boneIndex0];b[1]=boneMap[q.boneIndex1];b[2]=boneMap[q.boneIndex2];b[3]=boneMap[q.boneIndex3];w[0]=q.weight0;w[1]=q.weight1;w[2]=q.weight2;w[3]=q.weight3;Canonical(b,w);Vector3 pos=skin.transform.TransformPoint(v[i]);
            int id=-1;var cell=new Cell(pos);for(int dz=-1;dz<=1&&id<0;dz++)for(int dy=-1;dy<=1&&id<0;dy++)for(int dx=-1;dx<=1&&id<0;dx++)if(cells.TryGetValue(new Cell(cell.x+dx,cell.y+dy,cell.z+dz),out int candidate))for(;candidate>=0;candidate=next[candidate])
            {
                if(Vector3.Distance(pos,s.Point(candidate))>.000001f)continue;var cb=new int[4];var cw=new float[4];for(int k=0;k<4;k++){cb[k]=s.joints[candidate*4+k];cw[k]=s.weights[candidate*4+k];}Canonical(cb,cw);bool match=true;for(int k=0;k<4;k++)if(cb[k]!=b[k]||Mathf.Abs(cw[k]-w[k])>.000001f)match=false;if(match){id=canonical[candidate];break;}
            }
            if(id<0)
            {
                float nearest=float.PositiveInfinity;int at=-1;for(int k=0;k<n;k++){float distance=Vector3.Distance(pos,s.Point(k));if(distance<nearest){nearest=distance;at=k;}}
                var details=new List<string>{"Imported vertex="+i,"Imported world="+pos.ToString("R"),"Nearest original index="+at,"Nearest original world="+s.Point(at).ToString("R"),"DistanceM="+nearest.ToString("R")};
                for(int k=0;k<4;k++)details.Add("Imported influence "+(b[k]==int.MaxValue?"zero":s.names[b[k]])+" "+w[k].ToString("R"));for(int k=0;k<4;k++)details.Add("Original influence "+s.names[s.joints[at*4+k]]+" "+s.weights[at*4+k].ToString("R"));
                File.WriteAllLines(Output+"/unity-correspondence-red.txt",details);throw new Exception("Geometry/weights correspondence RED at imported vertex "+i+". See unity-correspondence-red.txt; no nearest-only acceptance or silent basis correction.");
            }
            importedIds[i]=id;originalToImported[id]=i;maxPosition=Mathf.Max(maxPosition,Vector3.Distance(pos,s.Point(id)));
            var sb=new int[4];var sw=new float[4];for(int k=0;k<4;k++){sb[k]=s.joints[id*4+k];sw[k]=s.weights[id*4+k];}Canonical(sb,sw);for(int k=0;k<4;k++)maxWeight=Mathf.Max(maxWeight,Mathf.Abs(w[k]-sw[k]));
        }
        for(int i=0;i<n;i++){originalToImported[i]=originalToImported[canonical[i]];if(originalToImported[i]<0)throw new Exception("Source vertex missing "+i);}
        var uvCells=new UVLookup();for(int i=0;i<s.triangles.Length;i++)uvCells.Add(canonical[s.triangles[i]],new Vector2(s.uv[i*2],s.uv[i*2+1]));
        VerifyFaces(s,canonical,importedIds,uv,mesh.triangles,uvCells);
        File.WriteAllLines(Output+"/unity-full-geometry-uv-weights.txt",new[]{"PASS full source indexed vertex/corner triangle winding/UV/namedweights correspondence under fixedC importer conversion.","Source vertices="+n+" imported seam vertices="+v.Length+" original triangles="+s.triangles.Length/3,"PositionMaxM="+maxPosition.ToString("R")+" weightMax="+maxWeight.ToString("R"),"Original float tuples remain exact; importer positions/UV/weights matched at1e-6. Seam duplicates permitted only for original equivalent tuples. Bind/originalmotion/material gates separate."});
        float bindMax=0;double angleMax=0;reviewBoneConventions=new Matrix4x4[s.names.Length];var gaugeRows=new List<string>{"bone,K00,K01,K02,K03,K10,K11,K12,K13,K20,K21,K22,K23,K30,K31,K32,K33"};var bindRows=new List<string>{"bone,jointErrorM,rawOrientationDifferenceDeg,fixedLocalAxisResidualDeg"};var basis=BasisMatrix;
        for(int i=0;i<skin.bones.Length;i++){int j=boneMap[i];var expected=basis*s.rigWorld*s.rest[j]*basis.inverse;var actual=skin.transform.localToWorldMatrix*mesh.bindposes[i].inverse;var K=expected.inverse*actual;reviewBoneConventions[j]=K;var cellsK=new List<string>{skin.bones[i].name};for(int row=0;row<4;row++)for(int col=0;col<4;col++)cellsK.Add(K[row,col].ToString("R",System.Globalization.CultureInfo.InvariantCulture));gaugeRows.Add(string.Join(",",cellsK));if(i==0)reviewBoneLocalAxis=(Quaternion.Inverse(expected.rotation)*actual.rotation).normalized;float distance=Vector3.Distance(expected.GetColumn(3),actual.GetColumn(3));double raw=Angle(expected.rotation,actual.rotation),angle=Angle(expected.rotation*reviewBoneLocalAxis,actual.rotation);bindMax=Mathf.Max(bindMax,distance);angleMax=Math.Max(angleMax,angle);bindRows.Add(string.Join(",",skin.bones[i].name,distance,raw,angle));}File.WriteAllLines(Output+"/unity-bind-correspondence.csv",bindRows);File.WriteAllLines(Output+"/unity-frozen-bind-local-conventions.csv",gaugeRows);
        File.WriteAllLines(Output+"/unity-fixed-bone-axis.txt",new[]{"Fixed FBX bone-local convention derived from FIRST true inverse-bind only, validated against ALL136 unchanged true binds. Not a moving-pose fit.","QuaternionXYZW="+reviewBoneLocalAxis.ToString("R"),"maxJointM="+bindMax+" maxAllBoneLocalAxisResidualDeg="+angleMax,"Full K_i=inverse(sourceBindWorld)*UnityBindWorld from true inversebind only; predictedUnityPose=sourcePoseWorld*K_i. Each fixedK including unit scale archived; never fit moving poses. Deformation Mpose*inverse(Mbind) cancels fixed bone-local convention. Source points/geometry untouched; raw orientation mismatch retained in CSV."});
        if(bindMax>.0001f||angleMax>.01)throw new Exception("Bind convention correspondence RED joint="+bindMax+" fixedAxisResidual="+angleMax+"; separate full geometry gate retained.");
        report.Add("Full vertex/triangle/cornerUV/weights correspondence PASS: source tuples bit-exact, imported positions/weights/UV matched within1e-6; seam-equivalent duplicates allowed, winding retained. Spatial cells are candidate lookup only.");report.Add("Position max m="+maxPosition+"; weight max="+maxWeight+"; bind joint max m="+bindMax+"; candidatePositionCell="+PositionQuantum+"; candidateUVCell="+UVQuantum);return originalToImported;
    }
    static void VerifyFaces(Source s,int[] canonical,int[] importedIds,Vector2[] uv,int[] triangles,UVLookup uvCells)
    {
        // ReflectX changes handedness: Unity FBX reverses triangle winding.
        var faces=new Dictionary<Face,int>();for(int t=0;t<s.triangles.Length;t+=3){var f=new Face(new Corner(canonical[s.triangles[t]],new Vector2(s.uv[t*2],s.uv[t*2+1])),new Corner(canonical[s.triangles[t+2]],new Vector2(s.uv[(t+2)*2],s.uv[(t+2)*2+1])),new Corner(canonical[s.triangles[t+1]],new Vector2(s.uv[(t+1)*2],s.uv[(t+1)*2+1])));faces.TryGetValue(f,out int count);faces[f]=count+1;}
        if(triangles.Length!=s.triangles.Length)throw new Exception("Triangle count changed");var importedCorners=new Corner[importedIds.Length];for(int i=0;i<importedIds.Length;i++)importedCorners[i]=uvCells.Match(importedIds[i],uv[i]);for(int t=0;t<triangles.Length;t+=3){int a=triangles[t],bb=triangles[t+1],c=triangles[t+2];var f=new Face(importedCorners[a],importedCorners[bb],importedCorners[c]);if(!faces.TryGetValue(f,out int count)||count==0)throw new Exception("Corner topology/UV/winding RED at triangle "+t/3);faces[f]=count-1;}if(faces.Values.Any(i=>i!=0))throw new Exception("Source triangle corners missing");
    }

}
