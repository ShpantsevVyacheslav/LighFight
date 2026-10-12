using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Editor {
 // Every state must explicitly name a clip on this model's native rig.
 public static class NativeFighterImport {
  [Serializable] public class Manifest { public string modelPath,materialPath,outputPath;public State[] states; }
  [Serializable] public class State {public string state,clip;public float contactPhase,start,end=1;public int strikeSide;public float headingOffset;}
  public static void ImportManifest() {
   var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-nativeManifest");
   if(index<0 || index+1>=args.Length)throw new ArgumentException("Pass -nativeManifest with a JSON file");
   Import(JsonUtility.FromJson<Manifest>(File.ReadAllText(args[index+1])));
  }
  public static GameObject Import(Manifest manifest) {
   if(manifest==null || manifest.states==null)throw new ArgumentException("Missing native motion manifest");
   string[] required=Moves.All.Select(m=>m.Id).Concat(new[]{"Idle","Guard","Walk","WalkBack","Crouch","Hit","Fall","GetUp"}).ToArray();
   foreach(string state in required)if(manifest.states.Count(s=>s.state==state)!=1)throw new InvalidOperationException("Require explicit native clip for "+state);
   if(manifest.states.Select(s=>s.state).Distinct().Count()!=manifest.states.Length)throw new InvalidOperationException("Duplicate native state");
   if(manifest.states.Any(s=>!required.Contains(s.state)&&!new[]{"Dance","Greeting","Victory"}.Contains(s.state) || s.start<0 || s.end>1 || s.start>=s.end || s.contactPhase<0 || s.contactPhase>1))throw new InvalidOperationException("Invalid native state mapping");
   var importer=AssetImporter.GetAtPath(manifest.modelPath) as ModelImporter;
   if(!importer)throw new InvalidOperationException("Native character must be an imported model");
   PrepareImporter(importer);
   var material=AssetDatabase.LoadAssetAtPath<Material>(manifest.materialPath);if(!material)throw new InvalidOperationException("Missing source material");
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(manifest.modelPath);if(!model)throw new InvalidOperationException("Missing native rig");
   var clips=AssetDatabase.LoadAllAssetsAtPath(manifest.modelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
   var selected=manifest.states.Select(s=>new{state=s,clip=clips.Single(c=>c.name==s.clip)}).ToArray();
   foreach(var entry in selected)ValidateClip(model,entry.clip);
   string cp=Path.ChangeExtension(manifest.outputPath,"controller");Directory.CreateDirectory(Path.GetDirectoryName(cp));
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(cp);
   if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(cp);
   foreach(var state in controller.layers[0].stateMachine.states)controller.layers[0].stateMachine.RemoveState(state.state);
   foreach(var entry in selected){var state=controller.AddMotion(entry.clip);state.name=entry.state.state;if(state.name=="Idle")controller.layers[0].stateMachine.defaultState=state;}
   var obj=UnityEngine.Object.Instantiate(model);
   try {
    var animator=obj.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.sharedMaterials=skin.sharedMaterials.Select(m=>material).ToArray();skin.quality=SkinQuality.Auto;}
    var view=obj.AddComponent<FighterView>();view.ConfigureAnimation(animator,manifest.states.Where(s=>s.contactPhase>0).Select(s=>new FighterView.ContactPose{moveId=s.state,phase=s.contactPhase,start=s.start,end=s.end,strikeSide=s.strikeSide,headingOffset=s.headingOffset}).ToArray());
    view.UseNativeMotion(selected.Select(e=>new FighterView.NativeClip{state=e.state.state,clip=e.clip}).ToArray());
    var prefab=PrefabUtility.SaveAsPrefabAsset(obj,manifest.outputPath);AssetDatabase.SaveAssets();return prefab;
   } finally {UnityEngine.Object.DestroyImmediate(obj);}
  }
  public static void PreserveKnownNativeIsland() {
   var importer=(ModelImporter)AssetImporter.GetAtPath(NativeIslandSetup.Folder+"/web-motions.fbx");
   if(!importer)throw new InvalidOperationException("Missing existing native Island source");
   PrepareImporter(importer);AssetDatabase.SaveAssets();
  }
  public static void PrepareImporter(ModelImporter importer) {
   if(importer.skinWeights!=ModelImporterSkinWeights.Custom||importer.avatarSetup!=ModelImporterAvatarSetup.CreateFromThisModel||importer.animationType!=ModelImporterAnimationType.Generic||importer.animationCompression!=ModelImporterAnimationCompression.Off||importer.meshCompression!=ModelImporterMeshCompression.Off||importer.maxBonesPerVertex!=8||importer.minBoneWeight!=0||!importer.isReadable||importer.resampleCurves||importer.weldVertices){
    importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.meshCompression=ModelImporterMeshCompression.Off;importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=8;importer.minBoneWeight=0;importer.isReadable=true;importer.resampleCurves=false;importer.weldVertices=false;importer.SaveAndReimport();
   }
  }
  public static void ValidateClip(GameObject model,AnimationClip clip) {
   if(clip.length<=0)throw new InvalidOperationException("Empty native clip");
   float variation=0;
   foreach(var b in AnimationUtility.GetCurveBindings(clip)) {
    if(b.type!=typeof(Transform))continue;
    if(!model.transform.Find(b.path)&&b.path!="")throw new InvalidOperationException("Clip uses another rig: "+b.path);
    bool euler=b.propertyName.StartsWith("localEulerAngles");
    if(!b.propertyName.StartsWith("m_LocalRotation")&&!euler)continue;
    var curve=AnimationUtility.GetEditorCurve(clip,b);var values=Enumerable.Range(0,61).Select(i=>curve.Evaluate(clip.length*i/60f)).ToArray();variation=Mathf.Max(variation,(values.Max()-values.Min())*(euler?Mathf.Deg2Rad:1));
   }
   if(variation<.002f)throw new InvalidOperationException("Frozen native motion: "+clip.name);
  }
 }
}
