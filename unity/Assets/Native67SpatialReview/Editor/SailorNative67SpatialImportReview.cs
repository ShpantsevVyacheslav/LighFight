using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
// Review-only. No menu/automatic postprocessor, no catalog writes or source mutations.
public static class SailorNative67SpatialImportReview {
 public const string ReviewRoot="Assets/Native67SpatialReview/";
 public static void ConfigureReviewImport(string assetPath){
  if(!assetPath.StartsWith(ReviewRoot,StringComparison.Ordinal)||!assetPath.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Owned review FBX only");
  var m=AssetImporter.GetAtPath(assetPath) as ModelImporter;if(m==null)throw new InvalidOperationException("Existing copied review FBX required");
  m.skinWeights=ModelImporterSkinWeights.Custom;m.maxBonesPerVertex=255;m.minBoneWeight=0;
  m.meshCompression=ModelImporterMeshCompression.Off;m.animationCompression=ModelImporterAnimationCompression.Off;
  m.animationType=ModelImporterAnimationType.Generic;m.optimizeGameObjects=false;m.isReadable=true;
  m.optimizeMeshPolygons=false;m.optimizeMeshVertices=false;m.weldVertices=false;
  m.importNormals=ModelImporterNormals.Import;m.importTangents=ModelImporterTangents.Import;
  m.resampleCurves=false;m.importAnimation=true;m.SaveAndReimport();
 }
 [Serializable]public sealed class ImportedStats {
  public string assetPath,assetGuid,skinWeights,meshCompression,animationCompression,animationType,weightAttributeFormat,indexAttributeFormat,rendererSkinQuality,globalSkinWeights;
  public int maxBonesPerVertex,unitySeamVertices,triangles,rendererBones,bindposes,coefficientCount,maximumObservedInfluences,zeroWeightEntries,unweightedVertices;
  public float minBoneWeight,smallestPositive=float.PositiveInfinity;public bool isReadable,optimizeGameObjects,optimizeMeshPolygons,optimizeMeshVertices,weldVertices;
 }
 public static ImportedStats ReadImportedStats(string path,SkinnedMeshRenderer skin){
  var m=AssetImporter.GetAtPath(path) as ModelImporter;if(m==null||skin==null||skin.sharedMesh==null)throw new ArgumentException();var mesh=skin.sharedMesh;
  var r=new ImportedStats{rendererSkinQuality=skin.quality.ToString(),globalSkinWeights=QualitySettings.skinWeights.ToString(),assetPath=path,assetGuid=AssetDatabase.AssetPathToGUID(path),skinWeights=m.skinWeights.ToString(),maxBonesPerVertex=m.maxBonesPerVertex,minBoneWeight=m.minBoneWeight,meshCompression=m.meshCompression.ToString(),animationCompression=m.animationCompression.ToString(),animationType=m.animationType.ToString(),isReadable=m.isReadable,optimizeGameObjects=m.optimizeGameObjects,optimizeMeshPolygons=m.optimizeMeshPolygons,optimizeMeshVertices=m.optimizeMeshVertices,weldVertices=m.weldVertices,unitySeamVertices=mesh.vertexCount,triangles=mesh.triangles.Length/3,rendererBones=skin.bones.Length,bindposes=mesh.bindposes.Length,weightAttributeFormat=mesh.GetVertexAttributeFormat(UnityEngine.Rendering.VertexAttribute.BlendWeight).ToString(),indexAttributeFormat=mesh.GetVertexAttributeFormat(UnityEngine.Rendering.VertexAttribute.BlendIndices).ToString()};
  using(var counts=mesh.GetBonesPerVertex())using(var weights=mesh.GetAllBoneWeights()){
   r.coefficientCount=weights.Length;int offset=0;for(int v=0;v<counts.Length;v++){r.maximumObservedInfluences=Math.Max(r.maximumObservedInfluences,counts[v]);if(counts[v]==0)r.unweightedVertices++;for(int k=0;k<counts[v];k++){var w=weights[offset++];if(w.boneIndex<0||w.boneIndex>=skin.bones.Length||float.IsNaN(w.weight)||float.IsInfinity(w.weight)||w.weight<0)throw new InvalidDataException("Invalid typed imported influence");if(w.weight==0)r.zeroWeightEntries++;else r.smallestPositive=Math.Min(r.smallestPositive,w.weight);}}if(offset!=weights.Length)throw new InvalidDataException("Typed shape mismatch");
  }return r;
 }
 public static void ConfigureReviewRenderer(SkinnedMeshRenderer skin){QualitySettings.skinWeights=SkinWeights.Unlimited;skin.quality=SkinQuality.Auto;}
 // Caller must first prove full original index/UV/triangle correspondence; seam counts need not match.
 public static void VerifyAllMappedPositiveInfluences(SkinnedMeshRenderer skin,int[] originalToUnity,byte[] sourceCounts,int[] sourceBoneIndices,float[] sourceWeights,string[] sourceBoneNames,out float maxNamedWeightDelta){
  if(originalToUnity.Length!=994452||sourceCounts.Length!=originalToUnity.Length||sourceBoneIndices.Length!=2922048||sourceWeights.Length!=sourceBoneIndices.Length)throw new InvalidDataException("Complete spatial original shape required");
  maxNamedWeightDelta=0;using(var c=skin.sharedMesh.GetBonesPerVertex())using(var w=skin.sharedMesh.GetAllBoneWeights()){
   var offsets=new int[c.Length];for(int i=1;i<c.Length;i++)offsets[i]=offsets[i-1]+c[i-1];int at=0;
   for(int v=0;v<originalToUnity.Length;v++){int mapped=originalToUnity[v];if(mapped<0||mapped>=c.Length||c[mapped]!=sourceCounts[v])throw new InvalidDataException("Original-index influence count mismatch "+v);
    var named=new Dictionary<string,float>(StringComparer.Ordinal);for(int k=0;k<c[mapped];k++){var iw=w[offsets[mapped]+k];string name=skin.bones[iw.boneIndex].name;if(named.ContainsKey(name))throw new InvalidDataException("Duplicate named target influence");named.Add(name,iw.weight);}
    for(int k=0;k<sourceCounts[v];k++){float expected=sourceWeights[at];string name=sourceBoneNames[sourceBoneIndices[at++]];if(!(expected>0)||!named.TryGetValue(name,out float actual)||!(actual>0))throw new InvalidDataException("Missing positive source influence "+v+" "+name);maxNamedWeightDelta=Math.Max(maxNamedWeightDelta,Math.Abs(actual-expected));named.Remove(name);}if(named.Count!=0)throw new InvalidDataException("Extra influence");
   }if(at!=sourceWeights.Length)throw new InvalidDataException("Incomplete source coefficients");
  }
 }
 [Serializable]public sealed class SourceIndexLayerContract {
  public string layerName;public string[] originalUVLayerNames,exportUVLayerOrder;
  public int originalVertexCount,denominator,manifestCandidateChannel;
  public float verifiedImportedV;
 }
 // Candidate channel derives from manifest layer order, then every actual GetUVs value is verified.
 // This only proves ID coverage; full triangle/cornerUV/source-position/named-weight gates remain mandatory.
 public static int[] ReadSourceVertexIds(Mesh mesh,SourceIndexLayerContract contract){
  if(mesh==null||contract==null||contract.layerName!="__LF_SOURCE_VERTEX_ID_V1"||contract.originalVertexCount!=994452||contract.denominator!=1048576||contract.exportUVLayerOrder==null||contract.originalUVLayerNames==null)throw new InvalidDataException("Explicit source-index UV contract required");
  int channel=Array.IndexOf(contract.exportUVLayerOrder,contract.layerName);
  if(channel<0||channel>7||channel!=contract.manifestCandidateChannel||Array.LastIndexOf(contract.exportUVLayerOrder,contract.layerName)!=channel)throw new InvalidDataException("Unsupported/ambiguous source-index UV channel");
  foreach(string name in contract.originalUVLayerNames)if(Array.IndexOf(contract.exportUVLayerOrder,name)<0)throw new InvalidDataException("Original UV layer omitted");
  if(contract.verifiedImportedV!=0&&contract.verifiedImportedV!=1)throw new InvalidDataException("Only independently verified fixed V convention accepted");
  var uv=new List<Vector4>();mesh.GetUVs(channel,uv);if(uv.Count!=mesh.vertexCount)throw new InvalidDataException("Incomplete source-index channel");
  var ids=new int[uv.Count];var covered=new bool[contract.originalVertexCount];int distinct=0;
  for(int i=0;i<ids.Length;i++){
   var value=uv[i];float scaled=value.x*contract.denominator;
   if(float.IsNaN(scaled)||float.IsInfinity(scaled)||scaled<0||scaled>=contract.originalVertexCount||scaled!=(float)Math.Floor(scaled)||value.x!=(float)(int)scaled/contract.denominator||value.y!=contract.verifiedImportedV||value.z!=0||value.w!=0)throw new InvalidDataException("Nonexact source-index UV at Unity seam vertex "+i);
   int id=(int)scaled;ids[i]=id;if(!covered[id]){covered[id]=true;distinct++;}
  }
  if(distinct!=contract.originalVertexCount)throw new InvalidDataException("Missing original vertex IDs; do not nearest-fill");
  return ids;
 }

}
