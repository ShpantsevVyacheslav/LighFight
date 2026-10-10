using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Runtime;

namespace LighFight.Editor
{
    public static class HostArtReviewSetup
    {
        public const string ScenePath = "Assets/LighFight/Scenes/HostArtReview.unity";
        const string SourceScene = "Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/FingerArtReview.unity";

        public static void Create()
        {
            StaticFistAudit.Verify();
            var scene = EditorSceneManager.OpenScene(SourceScene);
            new GameObject("LighFightHostBridge").AddComponent<HostBridge>();
            var review = UnityEngine.Object.FindFirstObjectByType<MeshyArtReview>();
            review.embeddedHost = true;
            review.startWithCandidate = true;
            ValidateScene();
            Directory.CreateDirectory("Assets/LighFight/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        public static void Verify()
        {
            Create();
            UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindFirstObjectByType<HostBridge>().gameObject);
            bool caught = false;
            try { ValidateScene(); }
            catch (InvalidOperationException error) when (error.Message == "Embedded review needs one host bridge") { caught = true; }
            finally { EditorSceneManager.OpenScene(ScenePath); }
            if (!caught) throw new InvalidOperationException("Missing-bridge mutation escaped detection");
            ValidateScene();
            File.WriteAllText("../.tools/reports/host-art-review-scene.txt", "Embedded review scene validated; missing bridge mutation detected; original isolated scene preserved.");
        }

        static void ValidateScene()
        {
            if (UnityEngine.Object.FindObjectsByType<HostBridge>(FindObjectsSortMode.None).Length != 1)
                throw new InvalidOperationException("Embedded review needs one host bridge");
            var review = UnityEngine.Object.FindFirstObjectByType<MeshyArtReview>();
            if (review == null || !review.embeddedHost || !review.startWithCandidate || review.candidate == null || review.candidateClip == null)
                throw new InvalidOperationException("Embedded review configuration is incomplete");
            if (UnityEngine.Object.FindFirstObjectByType<ContentBootstrap>() != null || UnityEngine.Object.FindFirstObjectByType<Prototype>() != null)
                throw new InvalidOperationException("Art review must not start a battle or download starter content");
        }
    }
}
