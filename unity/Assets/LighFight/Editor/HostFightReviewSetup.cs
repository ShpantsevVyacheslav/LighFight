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
        public const string ContentPath="Assets/LighFight/Art/Drafts/Rose/FightReviewContent.asset";
        public static GameContent SnapshotCatalog(GameContent source,string assetPath)
        {
            if(source==null || source.fighters==null || source.fighters.Length<2 || source.arenas==null || source.arenas.Length!=5)throw new InvalidOperationException("Approved content catalog missing");
            var content=AssetDatabase.LoadAssetAtPath<GameContent>(assetPath);
            if(content==null){content=ScriptableObject.CreateInstance<GameContent>();AssetDatabase.CreateAsset(content,assetPath);}
            content.fighters=(GameObject[])source.fighters.Clone();
            content.arenas=(GameObject[])source.arenas.Clone();
            content.fighterMetadata=source.fighterMetadata==null?new GameContent.FighterMetadata[0]:(GameContent.FighterMetadata[])source.fighterMetadata.Clone();
            EditorUtility.SetDirty(content);
            return content;
        }
        public static void Create()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
            var content=SnapshotCatalog(source,ContentPath);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("LighFight Host Bridge").AddComponent<HostBridge>();
            var prototype=new GameObject("LighFight Battle").AddComponent<Prototype>();prototype.content=content;
            var settings=new SerializedObject(prototype);settings.FindProperty("selectedFighter").intValue=3;settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            Debug.Log("Embedded fight review created from the current approved fighter and arena catalog");
        }
    }
}
