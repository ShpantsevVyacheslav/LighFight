using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Editor {
 public static class OriginalIslandSetup {
  public const string Folder="Assets/LighFight/Art/Drafts/OriginalIsland";
  public static void PrepareAndCapture(){Prepare();OriginalIslandReview.Capture();}
  public static void Prepare(){
   foreach(var file in Directory.GetFiles("../art/source/original-island","*.png"))File.Copy(file,Folder+"/"+Path.GetFileName(file),true);
   AssetDatabase.Refresh();string path=Folder+"/island-motions.fbx";
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importAnimation=true;importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();
   var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
   try{
    var anim=obj.GetComponent<Animator>();anim.applyRootMotion=false;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();var mat=new Material(Shader.Find("Standard"));mat.name="Original source PBR";mat.mainTexture=Texture("albedo");mat.SetTexture("_BumpMap",Texture("normal",true));mat.EnableKeyword("_NORMALMAP");
    var rough=Texture("roughness",false,true);var metallic=Texture("metallic",false,true);var r=rough.GetPixels();var m=metallic.GetPixels();for(int i=0;i<m.Length;i++)m[i]=new Color(m[i].r,0,0,1-r[i].r);var packed=new Texture2D(metallic.width,metallic.height,TextureFormat.RGBA32,false,true);packed.SetPixels(m);packed.Apply();File.WriteAllBytes(Folder+"/metallic-smoothness.png",packed.EncodeToPNG());UnityEngine.Object.DestroyImmediate(packed);AssetDatabase.Refresh();mat.SetTexture("_MetallicGlossMap",Texture("metallic-smoothness",false,true));mat.EnableKeyword("_METALLICGLOSSMAP");
    AssetDatabase.DeleteAsset(Folder+"/body.mat");AssetDatabase.CreateAsset(mat,Folder+"/body.mat");
    var hand=new Material(Shader.Find("Standard"));hand.mainTexture=Texture("hand-albedo");hand.SetFloat("_Glossiness",.45f);AssetDatabase.DeleteAsset(Folder+"/hand.mat");AssetDatabase.CreateAsset(hand,Folder+"/hand.mat");skin.sharedMaterials=skin.sharedMaterials.Select(m=>m.name.Contains("UnifiedHand")?hand:m.name.Contains("Watch")?AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/watch-dark.mat"):mat).ToArray();
    var reference=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-3.prefab").GetComponent<FighterView>();var old=(AnimatorController)reference.GetComponent<Animator>().runtimeAnimatorController;var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
    string cp=Folder+"/original.controller";AssetDatabase.DeleteAsset(cp);var controller=AnimatorController.CreateAnimatorControllerAtPath(cp);
    var guard=MeshyMotionSetup.CreatePoseClip(clips.Single(c=>c.name.EndsWith("|Left_Jab_from_Guard")),Array.Find(reference.CopyContactPoses(),p=>p.moveId=="_guard").phase,Folder+"/guard.anim");
    foreach(var state in old.layers[0].stateMachine.states){var source=(AnimationClip)state.state.motion;string key=source.name.Split('|').Last();var clip=state.state.name=="Guard"||state.state.name=="Idle"?guard:clips.Single(c=>c.name.EndsWith("|"+key));controller.AddMotion(clip).name=state.state.name;}
    anim.runtimeAnimatorController=controller;obj.AddComponent<FighterView>().ConfigureAnimation(anim,reference.CopyContactPoses());PrefabUtility.SaveAsPrefabAsset(obj,Folder+"/candidate.prefab");AssetDatabase.SaveAssets();
   }finally{UnityEngine.Object.DestroyImmediate(obj);}
  }
  static Texture2D Texture(string name,bool normal=false,bool linear=false){string p=Folder+"/"+name+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(p);imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.isReadable=true;imp.sRGBTexture=!normal&&!linear;imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(p);}
 }
}
