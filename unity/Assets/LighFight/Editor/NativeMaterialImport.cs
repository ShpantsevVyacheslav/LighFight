using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor {
 public static class NativeMaterialImport {
  // glTF: metallic = B, roughness = G. Unity Standard: metallic = R, smoothness = A.
  public static Color ConvertChannels(Color source) => new Color(source.b,0,0,1-source.g);
  public static void ApplySurfaceMode(Material material,bool doubleSided) {
   var shader=Shader.Find(doubleSided?"LighFight/Native PBR":"Standard");
   if(!shader || !shader.isSupported)throw new InvalidOperationException("Source material shader unavailable");
   material.shader=shader;material.doubleSidedGI=doubleSided;if(doubleSided)material.SetFloat("_Cull",0);
  }
  public static void MigrateKnownWebSources() {
   // Both retained GLBs explicitly declare material.doubleSided=true.
   foreach(var path in new[]{NativeIslandSetup.Folder+"/Pbr/native.mat","Assets/LighFight/Art/Drafts/IslandWebVisible/Pbr/native.mat"}) {
    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat)throw new InvalidOperationException("Missing source material "+path);
    ApplySurfaceMode(mat,true);EditorUtility.SetDirty(mat);
   }
   AssetDatabase.SaveAssets();
  }
  public static Material Create(string folder,string albedo,string normal,string metallicRoughness,bool doubleSided=false) {
   Directory.CreateDirectory(folder);
   File.Copy(albedo,folder+"/albedo.png",true);File.Copy(normal,folder+"/normal.png",true);
   var packed=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
   try {
    if(!packed.LoadImage(File.ReadAllBytes(metallicRoughness)))throw new InvalidOperationException("Invalid source metallic roughness texture");
    var pixels=packed.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=ConvertChannels(pixels[i]);packed.SetPixels(pixels);packed.Apply();File.WriteAllBytes(folder+"/metallic-smoothness.png",packed.EncodeToPNG());
   } finally{UnityEngine.Object.DestroyImmediate(packed);}
   AssetDatabase.Refresh();
   foreach(string name in new[]{"albedo","normal","metallic-smoothness"}) {
    var importer=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+name+".png");importer.textureType=name=="normal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=name=="albedo";importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
   }
   string path=folder+"/native.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
   ApplySurfaceMode(mat,doubleSided);
   mat.color=Color.white;mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/albedo.png");mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/normal.png"));mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/metallic-smoothness.png"));mat.SetFloat("_BumpScale",1);mat.SetFloat("_GlossMapScale",1);mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICGLOSSMAP");EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();return mat;
  }
 }
}
