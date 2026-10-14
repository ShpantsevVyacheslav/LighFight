using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using LighFight.Runtime;
namespace LighFight.Editor {
 public static class MobileSourceTextureSetup {
  [Serializable] public class Row {public string path;public int sourceWidth,sourceHeight,maxSize;public bool sourcePixelsPreserved,cpuReadback;public string format;}
  [Serializable] public class Report {public Row[] textures;public string scope="iPhone only: original dimensions retained; ASTC4x4 at maximum encoder quality; desktop/source pixels unchanged. No device performance claim.";}
  public static void Configure(){
   var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");if(!content)throw new Exception("Content missing");
   var paths=new HashSet<string>();
   foreach(var prefab in content.fighters.Concat(content.arenas))foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials){if(!material)continue;foreach(var property in material.GetTexturePropertyNames()){var texture=material.GetTexture(property);if(texture is Texture2D){var path=AssetDatabase.GetAssetPath(texture);if(!string.IsNullOrEmpty(path)&&AssetImporter.GetAtPath(path) is TextureImporter)paths.Add(path);}}}
   var rows=new List<Row>();
   foreach(var path in paths.OrderBy(p=>p)){
    var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.GetSourceTextureWidthAndHeight(out int width,out int height);var before=File.ReadAllBytes(path);
    var settings=importer.GetPlatformTextureSettings("iPhone");settings.name="iPhone";settings.overridden=true;settings.maxTextureSize=Mathf.NextPowerOfTwo(Mathf.Max(width,height));settings.format=TextureImporterFormat.ASTC_4x4;settings.compressionQuality=100;settings.textureCompression=TextureImporterCompression.CompressedHQ;settings.crunchedCompression=false;importer.SetPlatformTextureSettings(settings);importer.isReadable=false;importer.SaveAndReimport();
    bool preserved=before.SequenceEqual(File.ReadAllBytes(path));if(!preserved)throw new Exception("Source pixels changed "+path);
    rows.Add(new Row{path=path,sourceWidth=width,sourceHeight=height,maxSize=settings.maxTextureSize,sourcePixelsPreserved=preserved,format=settings.format.ToString(),cpuReadback=importer.isReadable});
   }
   Directory.CreateDirectory("../docs/reports");File.WriteAllText("../docs/reports/mobile-source-textures-2026-10-14.json",JsonUtility.ToJson(new Report{textures=rows.ToArray()},true));AssetDatabase.SaveAssets();Debug.Log("AGENT_RESULT iPhone original-size ASTC4x4 configured textures="+rows.Count);
  }
 }
}
