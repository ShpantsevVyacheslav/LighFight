using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor {
 public static class NativeMaterialImport {
  // glTF: metallic = B, roughness = G. Unity Standard: metallic = R, smoothness = A.
  public static Color ConvertChannels(Color source) => new Color(source.b,0,0,1-source.g);
  public static Material Create(string folder,string albedo,string normal,string metallicRoughness) {
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
   mat.color=Color.white;mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/albedo.png");mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/normal.png"));mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/metallic-smoothness.png"));mat.SetFloat("_BumpScale",1);mat.SetFloat("_GlossMapScale",1);mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICGLOSSMAP");EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();return mat;
  }
 }
}
