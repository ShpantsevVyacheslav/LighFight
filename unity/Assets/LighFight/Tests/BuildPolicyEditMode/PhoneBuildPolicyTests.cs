using System;
using NUnit.Framework;
using LighFight.Editor;

public class PhoneBuildPolicyTests
{
    [TestCase(null, false)]
    [TestCase("Release", false)]
    [TestCase("Development", true)]
    public void OnlyExplicitDevelopmentOptsIn(string value, bool expected)
        => Assert.AreEqual(expected, PhoneBuildPolicy.ParseDevelopment(value));

    [TestCase("")][TestCase("Debug")][TestCase("release")][TestCase(" Development ")]
    public void UnknownConfigurationsFailClosed(string value)
        => Assert.Throws<ArgumentException>(() => PhoneBuildPolicy.ParseDevelopment(value));
    [TestCase(UnityEditor.BuildTarget.iOS, false)]
    [TestCase(UnityEditor.BuildTarget.iOS, true)]
    [TestCase(UnityEditor.BuildTarget.Android, false)]
    [TestCase(UnityEditor.BuildTarget.Android, true)]
    public void BuildScopePinsSupportedSettingsAndRestoresOnFailure(UnityEditor.BuildTarget buildTarget, bool development)
    {
        var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(UnityEditor.BuildPipeline.GetBuildTargetGroup(buildTarget));
        var compiler = UnityEditor.PlayerSettings.GetIl2CppCompilerConfiguration(target);
        var generation = UnityEditor.PlayerSettings.GetIl2CppCodeGeneration(target);
        Assert.Throws<InvalidOperationException>(() => {
            using (var policy = new PhoneBuildPolicy(buildTarget, development))
            {
                Assert.AreEqual(development ? UnityEditor.BuildOptions.Development : UnityEditor.BuildOptions.None, policy.Options);
                // iOS must leave the unsupported compiler field untouched, not silently request Debug.
                Assert.AreEqual(buildTarget == UnityEditor.BuildTarget.iOS ? compiler :
                    (development ? UnityEditor.Il2CppCompilerConfiguration.Debug : UnityEditor.Il2CppCompilerConfiguration.Release),
                    UnityEditor.PlayerSettings.GetIl2CppCompilerConfiguration(target));
                Assert.AreEqual(UnityEditor.Build.Il2CppCodeGeneration.OptimizeSpeed,
                    UnityEditor.PlayerSettings.GetIl2CppCodeGeneration(target));
                throw new InvalidOperationException("simulated failed build");
            }
        });
        Assert.AreEqual(compiler, UnityEditor.PlayerSettings.GetIl2CppCompilerConfiguration(target));
        Assert.AreEqual(generation, UnityEditor.PlayerSettings.GetIl2CppCodeGeneration(target));
    }
}
