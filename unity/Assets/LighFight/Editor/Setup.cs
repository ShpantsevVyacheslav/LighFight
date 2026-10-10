using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LighFight.Editor
{
    public static class Setup
    {
        private const string ScenePath = "Assets/LighFight/Scenes/Prototype.unity";

        [MenuItem("LighFight/Create prototype scene")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/LighFight/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Host Bridge").AddComponent<LighFight.Runtime.HostBridge>();
            new GameObject("LighFight Prototype").AddComponent<LighFight.Runtime.Prototype>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "LighChat";
            PlayerSettings.productName = "LighFight";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Legacy Input is used by the prototype (including multitouch).
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 0; settings.ApplyModifiedProperties(); }
            AssetDatabase.SaveAssets();
        }

        public static void ExportAndroid() => Export(BuildTarget.Android, "Builds/Android");
        public static void ExportIos() => Export(BuildTarget.iOS, "Builds/iOS");

        private static void IncludeRuntimeShaders()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var included = settings.FindProperty("m_AlwaysIncludedShaders");
            foreach (string name in new[] { "LighFight/OfficeLit", "LighFight/CharacterSkin", "Unlit/Color" })
            {
                var shader = Shader.Find(name);
                if (shader == null) throw new System.InvalidOperationException("Missing runtime shader: " + name);
                bool found = false;
                for (int i = 0; i < included.arraySize; i++) if (included.GetArrayElementAtIndex(i).objectReferenceValue == shader) found = true;
                if (!found) { int i = included.arraySize; included.arraySize++; included.GetArrayElementAtIndex(i).objectReferenceValue = shader; }
            }
            settings.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
        }

        private static void Export(BuildTarget target, string output)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new System.InvalidOperationException("Required Unity platform module is not installed: " + target);
            bool preview = System.Environment.GetEnvironmentVariable("LIGHFIGHT_EMBED_PROTOTYPE") == "1";
            string scenePath;
            if (System.Environment.GetEnvironmentVariable("LIGHFIGHT_EMBED_ART_REVIEW")=="1")
            {
                if(preview)throw new System.InvalidOperationException("Choose only one embedded preview profile");
                HostArtReviewSetup.Create();scenePath=HostArtReviewSetup.ScenePath;
            }
            else if (preview) { scenePath = ScenePath; if (!File.Exists(scenePath)) Create(); }
            else
            {
                if (string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("LIGHFIGHT_MANIFEST_URL")))
                    throw new System.InvalidOperationException("Set LIGHFIGHT_MANIFEST_URL for downloadable content; local preview requires LIGHFIGHT_EMBED_PROTOTYPE=1");
                ContentBuild.Bootstrap();scenePath = "Assets/LighFight/Scenes/Bootstrap.unity";
            }
            var addresses = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.GetSettings(true);
            addresses.BuildAddressablesWithPlayerBuild = UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            IncludeRuntimeShaders();
            Directory.CreateDirectory(output);
            if (target == BuildTarget.Android) EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { scenePath }, locationPathName = output,
                target = target, options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("LighFight export failed: " + report.summary.result);
        }
    }
}
