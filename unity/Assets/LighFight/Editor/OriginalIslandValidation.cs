using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor {
 public static class OriginalIslandValidation {
  public static void Verify(){
   var method=typeof(RoseCombatAudit).GetMethod("VerifyCharacter",BindingFlags.Static|BindingFlags.NonPublic);method.Invoke(null,new object[]{3});
  }
  public static void Promote(){
   const string path="Assets/LighFight/Art/Fighters/fighter-3.prefab";var original=File.ReadAllBytes(path);
   File.Copy(OriginalIslandSetup.Folder+"/candidate.prefab",path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
   try{Verify();}catch{File.WriteAllBytes(path,original);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);throw;}
   AssetDatabase.SaveAssets();
  }
 }
}
