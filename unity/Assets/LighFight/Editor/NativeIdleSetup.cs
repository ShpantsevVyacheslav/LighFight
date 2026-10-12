using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace LighFight.Editor {
 public static class NativeIdleSetup {
  public static void Apply(){
   AssetDatabase.Refresh();NativeFighterImport.PrepareImporter((ModelImporter)AssetImporter.GetAtPath(NativeStanceReview.Source));
   var clip=AssetDatabase.LoadAllAssetsAtPath(NativeStanceReview.Source).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview"));
   const string path="Assets/LighFight/Art/Fighters/fighter-3.prefab";var actor=PrefabUtility.LoadPrefabContents(path);
   try{
    NativeFighterImport.ValidateClip(actor,clip);
    var source=AssetDatabase.LoadAssetAtPath<GameObject>(NativeStanceReview.Source).GetComponentInChildren<SkinnedMeshRenderer>();var current=actor.GetComponentInChildren<SkinnedMeshRenderer>();
    for(int i=0;i<current.bones.Length;i++){
     int j=Array.FindIndex(source.bones,b=>b.name==current.bones[i].name);if(j<0)throw new InvalidOperationException("Idle source uses different bones");
     for(int k=0;k<16;k++)if(Mathf.Abs(current.sharedMesh.bindposes[i][k]-source.sharedMesh.bindposes[j][k])>.0001f)throw new InvalidOperationException("Idle source uses different bind matrices");
    }
    var animator=actor.GetComponent<Animator>();var controller=(AnimatorController)animator.runtimeAnimatorController;controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Idle").state.motion=clip;EditorUtility.SetDirty(controller);
    var view=new SerializedObject(actor.GetComponent<LighFight.Runtime.FighterView>());var entries=view.FindProperty("nativeClips");int matches=0;
    for(int i=0;i<entries.arraySize;i++){var entry=entries.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("state").stringValue=="Idle"){entry.FindPropertyRelative("clip").objectReferenceValue=clip;matches++;}}
    if(matches!=1)throw new InvalidOperationException("Require exactly one existing Idle binding");view.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(actor,path);AssetDatabase.SaveAssets();
   }finally{PrefabUtility.UnloadPrefabContents(actor);}
  }
 }
}
