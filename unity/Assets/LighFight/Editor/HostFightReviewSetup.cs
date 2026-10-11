using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Editor
{
    public static class HostFightReviewSetup
    {
        public const string ScenePath="Assets/LighFight/Scenes/HostFightReview.unity";
        public static void Create()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
            if(source==null || source.fighters.Length!=8 || source.arenas.Length!=5)throw new InvalidOperationException("Starter content missing");
            var rose=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-4.prefab");
            if(rose==null || !rose.GetComponent<FighterView>().IsReady)throw new InvalidOperationException("Rose is not ready");
            var content=UnityEngine.Object.Instantiate(source);
            content.fighters=(GameObject[])source.fighters.Clone();content.fighters[4]=rose;
            var island=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-3.prefab");if(island!=null)content.fighters[3]=island;
            const string asset="Assets/LighFight/Art/Drafts/Rose/FightReviewContent.asset";
            AssetDatabase.DeleteAsset(asset);AssetDatabase.CreateAsset(content,asset);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("LighFight Host Bridge").AddComponent<HostBridge>();
            var prototype=new GameObject("LighFight Battle").AddComponent<Prototype>();prototype.content=content;
            var settings=new SerializedObject(prototype);settings.FindProperty("selectedFighter").intValue=3;settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            Debug.Log("Embedded fight review created with native Island, Rose and 25 attacks");
        }
    }
}
