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

        private static void Export(BuildTarget target, string output)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new System.InvalidOperationException("Required Unity platform module is not installed: " + target);
            if (!File.Exists(ScenePath)) Create();
            Directory.CreateDirectory(output);
            if (target == BuildTarget.Android) EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = target, options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("LighFight export failed: " + report.summary.result);
        }
    }
}
