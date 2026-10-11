using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor {
 public static class SourceDetailRestore {
  public static void Run(){Restore(4,"rose");Restore(3,"island");AssetDatabase.SaveAssets();Compact();}
  public static void Compact(){
   foreach(string kind in new[]{"rose","island"}){
    string path="Assets/LighFight/Art/Drafts/RestoredDetail/"+kind+"/source-detail.asset";
    var mesh=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Mesh>(path));
    UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[]{mesh},path,false);UnityEngine.Object.DestroyImmediate(mesh);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
    int id=kind=="rose"?4:3;string prefabPath=$"Assets/LighFight/Art/Fighters/fighter-{id}.prefab";var obj=PrefabUtility.LoadPrefabContents(prefabPath);try{obj.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);PrefabUtility.SaveAsPrefabAsset(obj,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(obj);}
   }
   AssetDatabase.Refresh();
  }

  static Texture2D Texture(string path,bool normal=false,bool readable=false){
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new Exception("Missing source map "+path);
   importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.isReadable=readable;importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
   importer.sRGBTexture=!normal&&!readable;foreach(string platform in new[]{"iPhone","Android"})importer.ClearPlatformTextureSettings(platform);importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  static void Restore(int id,string kind){
   string folder="Assets/LighFight/Art/Drafts/RestoredDetail/"+kind,fbx=folder+"/restored.fbx";AssetDatabase.Refresh();
   var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);if(importer==null)throw new Exception("Missing restored model "+kind);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=true;importer.importAnimation=false;importer.SaveAndReimport();
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(fbx).GetComponentInChildren<SkinnedMeshRenderer>();
   var obj=PrefabUtility.LoadPrefabContents($"Assets/LighFight/Art/Fighters/fighter-{id}.prefab");
   try{
    var target=obj.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=UnityEngine.Object.Instantiate(source.sharedMesh);mesh.name=kind+"-source-detail";
    var map=source.bones.Select(b=>Array.FindIndex(target.bones,t=>t.name==b.name)).ToArray();if(map.Any(i=>i<0))throw new Exception("Source rig mismatch");
    var weights=mesh.boneWeights;for(int i=0;i<weights.Length;i++){var w=weights[i];w.boneIndex0=map[w.boneIndex0];w.boneIndex1=map[w.boneIndex1];w.boneIndex2=map[w.boneIndex2];w.boneIndex3=map[w.boneIndex3];if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f)throw new Exception("Unnormalized skin");weights[i]=w;}mesh.boneWeights=weights;mesh.bindposes=target.sharedMesh.bindposes;
    if(mesh.triangles.Length/3<300000)throw new Exception("Source detail lost");if(Mathf.Abs(mesh.bounds.size.y-target.sharedMesh.bounds.size.y)>.03f)throw new Exception("Model scale changed");
    string meshPath=folder+"/source-detail.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(existing==null){AssetDatabase.CreateAsset(mesh,meshPath);existing=mesh;}else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);}
    var material=new Material(Shader.Find("Standard"));material.name=kind+"-source-PBR";material.mainTexture=Texture(folder+"/albedo.png");material.SetTexture("_BumpMap",Texture(folder+"/normal.png",true));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",1);
    var packed=Texture(folder+"/metallic-roughness.png",false,true);var pixels=packed.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(pixels[i].b,0,0,1-pixels[i].g);var smooth=new Texture2D(packed.width,packed.height,TextureFormat.RGBA32,false,true);smooth.SetPixels(pixels);smooth.Apply();File.WriteAllBytes(folder+"/metallic-smoothness.png",smooth.EncodeToPNG());UnityEngine.Object.DestroyImmediate(smooth);AssetDatabase.Refresh();material.SetTexture("_MetallicGlossMap",Texture(folder+"/metallic-smoothness.png",false,true));material.EnableKeyword("_METALLICGLOSSMAP");material.SetFloat("_GlossMapScale",1);
    string matPath=folder+"/source-PBR.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(mat==null){AssetDatabase.CreateAsset(material,matPath);mat=material;}else{EditorUtility.CopySerialized(material,mat);UnityEngine.Object.DestroyImmediate(material);}
    // Blender joins source body first, then existing hand/watch material slots.
    var old=kind=="rose"?new[]{AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/Rose/rose.mat")}:new[]{AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/character-skin.mat"),AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/watch-dark.mat")};var materials=new Material[existing.subMeshCount];materials[0]=mat;for(int i=1;i<materials.Length;i++)materials[i]=old[Math.Min(i-1,old.Length-1)];target.sharedMesh=existing;target.sharedMaterials=materials;target.localBounds=existing.bounds;
    PrefabUtility.SaveAsPrefabAsset(obj,$"Assets/LighFight/Art/Fighters/fighter-{id}.prefab");Debug.Log($"Restored {kind}: {existing.triangles.Length/3} triangles, {existing.subMeshCount} material slots, original PBR maps");
   }finally{PrefabUtility.UnloadPrefabContents(obj);}
  }
 }
}
