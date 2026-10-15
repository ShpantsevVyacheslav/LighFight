using System;
using UnityEditor;
using UnityEditor.Build;

namespace LighFight.Editor
{
    // Explicit policy shared by ordinary exports; diagnostic helper remains opt-in.
    public sealed class PhoneBuildPolicy : IDisposable
    {
        public const string Variable = "LIGHFIGHT_BUILD_CONFIGURATION";
        public static bool ParseDevelopment(string value)
        {
            if (value == null || value == "Release") return false;
            if (value == "Development") return true;
            throw new ArgumentException(Variable + " must be Release or Development (unset defaults to Release)");
        }
        public static bool FromEnvironment() => ParseDevelopment(Environment.GetEnvironmentVariable(Variable));
        public readonly BuildOptions Options;
        private readonly NamedBuildTarget target;
        private readonly bool controlsCompiler;
        private readonly Il2CppCompilerConfiguration previousCompiler;
        private readonly Il2CppCodeGeneration previousGeneration;
        public PhoneBuildPolicy(BuildTarget buildTarget, bool development)
        {
            target = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(buildTarget));
            // iOS compiles generated C++ in Xcode; Unity's setter is unsupported there.
            // This policy is used by Android/iOS exports: only Android owns this setting.
            controlsCompiler = buildTarget == BuildTarget.Android;
            if (controlsCompiler) previousCompiler = PlayerSettings.GetIl2CppCompilerConfiguration(target);
            previousGeneration = PlayerSettings.GetIl2CppCodeGeneration(target);
            Options = development ? BuildOptions.Development : BuildOptions.None;
            try
            {
                if (controlsCompiler) PlayerSettings.SetIl2CppCompilerConfiguration(target, development ? Il2CppCompilerConfiguration.Debug : Il2CppCompilerConfiguration.Release);
                PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSpeed);
            }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            try { if (controlsCompiler) PlayerSettings.SetIl2CppCompilerConfiguration(target, previousCompiler); }
            finally { PlayerSettings.SetIl2CppCodeGeneration(target, previousGeneration); }
        }
    }
}
