using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Editor
{
    public static class IslandCombatSetup
    {
        public static void VerifyAndPrepare(){Run();RoseCombatAudit.Run();}
        public static void Run()
        {
            AssetDatabase.Refresh();const string folder="Assets/LighFight/Art/Drafts/IslandCombat";string path=folder+"/island-motions.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=true;importer.isReadable=true;importer.SaveAndReimport();
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));var animator=obj.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();skin.sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/character-skin.mat"),AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/watch-dark.mat")};
            string controllerPath=folder+"/island.controller";AssetDatabase.DeleteAsset(controllerPath);var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var reference=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-4.prefab").GetComponent<FighterView>();var rose=(AnimatorController)reference.GetComponent<Animator>().runtimeAnimatorController;
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
            var guardSource=clips.Single(c=>c.name.EndsWith("|Left_Jab_from_Guard"));var guardPhase=Array.Find(reference.CopyContactPoses(),p=>p.moveId=="_guard").phase;var guard=MeshyMotionSetup.CreatePoseClip(guardSource,guardPhase,folder+"/guard.anim");
            foreach(var state in rose.layers[0].stateMachine.states)
            {
                var source=(AnimationClip)state.state.motion;string key=source.name.Split('|').Last();var clip=state.state.name=="Guard" || state.state.name=="Idle"?guard:clips.Single(c=>c.name.EndsWith("|"+key));controller.AddMotion(clip).name=state.state.name;
            }
            animator.runtimeAnimatorController=controller;var view=obj.AddComponent<FighterView>();view.ConfigureAnimation(animator,reference.CopyContactPoses());var correction=obj.AddComponent<JointCorrectives>();correction.referenceAngles=new[]{135.99724f,111.25128f,73.56264f};
            PrefabUtility.SaveAsPrefabAsset(obj,"Assets/LighFight/Art/Fighters/fighter-3.prefab");UnityEngine.Object.DestroyImmediate(obj);AssetDatabase.SaveAssets();Debug.Log("Island fighter upgraded with 31 shared motions");
            HostFightReviewSetup.Create();
        }
    }
}
