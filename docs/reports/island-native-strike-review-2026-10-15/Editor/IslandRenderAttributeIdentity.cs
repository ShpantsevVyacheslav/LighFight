using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
// Editor review only. Hashes stored data; never recalculates normals/tangents or mutates assets.
public static class IslandRenderAttributeIdentity {
 static string SHA(byte[] bytes){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static void V(BinaryWriter w,Vector3 v){w.Write(v.x);w.Write(v.y);w.Write(v.z);}
 static void Bounds(BinaryWriter w,UnityEngine.Bounds b){V(w,b.center);V(w,b.extents);}
 public static string FullMeshHash(Mesh mesh){
  using(var memory=new MemoryStream()){using(var w=new BinaryWriter(memory,Encoding.UTF8,true)){
   w.Write("IslandFullRenderAttributes-v1");w.Write(mesh.vertexCount);w.Write((int)mesh.indexFormat);Bounds(w,mesh.bounds);
   var attributes=mesh.GetVertexAttributes();w.Write(attributes.Length);foreach(var a in attributes){w.Write((int)a.attribute);w.Write((int)a.format);w.Write(a.dimension);w.Write(a.stream);w.Write(mesh.GetVertexAttributeOffset(a.attribute));}
   // Exact vertex streams include positions, normals, tangent handedness, color representation,
   // all authored UV0..UV7 dimensions/formats and any additional stored vertex channels.
   using(var read=Mesh.AcquireReadOnlyMeshData(mesh)){var data=read[0];w.Write(data.vertexBufferCount);for(int stream=0;stream<data.vertexBufferCount;stream++){w.Write(data.GetVertexBufferStride(stream));var bytes=data.GetVertexData<byte>(stream);w.Write(bytes.Length);foreach(byte b in bytes)w.Write(b);}
    if(mesh.indexFormat==UnityEngine.Rendering.IndexFormat.UInt16){var indices=data.GetIndexData<ushort>();w.Write(indices.Length);foreach(var i in indices)w.Write(i);}else{var indices=data.GetIndexData<uint>();w.Write(indices.Length);foreach(var i in indices)w.Write(i);}
   }
   w.Write(mesh.subMeshCount);for(int s=0;s<mesh.subMeshCount;s++){var d=mesh.GetSubMesh(s);w.Write(d.indexStart);w.Write(d.indexCount);w.Write((int)d.topology);w.Write(d.baseVertex);w.Write(d.firstVertex);w.Write(d.vertexCount);Bounds(w,d.bounds);}
   var binds=mesh.bindposes;w.Write(binds.Length);foreach(var b in binds)for(int i=0;i<16;i++)w.Write(b[i]);using(var counts=mesh.GetBonesPerVertex()){w.Write(counts.Length);foreach(var c in counts)w.Write(c);}using(var weights=mesh.GetAllBoneWeights()){w.Write(weights.Length);foreach(var b in weights){w.Write(b.boneIndex);w.Write(b.weight);}}
   w.Write(mesh.blendShapeCount);if(mesh.blendShapeCount>0){var p=new Vector3[mesh.vertexCount];var n=new Vector3[mesh.vertexCount];var t=new Vector3[mesh.vertexCount];for(int b=0;b<mesh.blendShapeCount;b++){w.Write(mesh.GetBlendShapeName(b));int frames=mesh.GetBlendShapeFrameCount(b);w.Write(frames);for(int f=0;f<frames;f++){w.Write(mesh.GetBlendShapeFrameWeight(b,f));mesh.GetBlendShapeFrameVertices(b,f,p,n,t);for(int i=0;i<p.Length;i++){V(w,p[i]);V(w,n[i]);V(w,t[i]);}}}}
  }return SHA(memory.ToArray());}
 }
 [Serializable] public sealed class FileProof {public string path,sha256;public long bytes;}
 [Serializable] sealed class Manifest {public string status;public FileProof[] files;public string[] builtinDependencies;}
 public sealed class FrozenAssets:IDisposable {
  readonly FileProof[] before;readonly string[] builtin;readonly string output;bool disposed;
  internal FrozenAssets(string[] paths,string[] builtinDependencies,string evidence){output=evidence;builtin=builtinDependencies;before=paths.Select(Read).ToArray();Write("before","READ_ONLY_SNAPSHOT",before);}
  static FileProof Read(string path){if(!File.Exists(path))throw new IOException("Frozen dependency file missing: "+path);var bytes=File.ReadAllBytes(path);return new FileProof{path=path,bytes=bytes.LongLength,sha256=SHA(bytes)};}
  void Write(string suffix,string status,FileProof[] files){File.WriteAllText(Path.Combine(output,"asset-dependencies-"+suffix+".json"),JsonUtility.ToJson(new Manifest{status=status,files=files,builtinDependencies=builtin},true));}
  public void Dispose(){if(disposed)return;disposed=true;var after=before.Select(x=>Read(x.path)).ToArray();bool equal=before.Zip(after,(a,b)=>a.path==b.path&&a.bytes==b.bytes&&a.sha256==b.sha256).All(x=>x);Write("after",equal?"UNCHANGED":"BLOCKED_DEPENDENCY_MUTATION",after);if(!equal)throw new InvalidOperationException("SDK fist/PBR material/texture or dependency meta bytes changed during review");}
 }
 public static FrozenAssets Freeze(string sdkPath,Material material,string output){
  var paths=new HashSet<string>(StringComparer.Ordinal);var builtin=new HashSet<string>(StringComparer.Ordinal);
  Action<string> add=p=>{if(string.IsNullOrEmpty(p))return;if(File.Exists(p)){paths.Add(p);if(File.Exists(p+".meta"))paths.Add(p+".meta");}else builtin.Add(p);};
  add(sdkPath);string mp=AssetDatabase.GetAssetPath(material);if(string.IsNullOrEmpty(mp)||!File.Exists(mp))throw new InvalidOperationException("Required source PBR material must be a saved asset");add(mp);
  foreach(string prop in material.GetTexturePropertyNames()){var texture=material.GetTexture(prop);if(texture){string texturePath=AssetDatabase.GetAssetPath(texture);if(string.IsNullOrEmpty(texturePath))throw new InvalidOperationException("Source PBR texture has no saved dependency path: "+prop);add(texturePath);}}
  foreach(string path in AssetDatabase.GetDependencies(mp,true)){var type=AssetDatabase.GetMainAssetTypeAtPath(path);if(type!=null&&typeof(Texture).IsAssignableFrom(type))add(path);}
  if(material.shader)add(AssetDatabase.GetAssetPath(material.shader));
  return new FrozenAssets(paths.OrderBy(x=>x,StringComparer.Ordinal).ToArray(),builtin.OrderBy(x=>x,StringComparer.Ordinal).ToArray(),output);
 }
}
