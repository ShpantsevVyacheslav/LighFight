using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using LighFight.Runtime;
using Object = UnityEngine.Object;

namespace LighFight.Tests
{
    // This is an owner/lifecycle integration gate, not a second shader/material oracle.
    // The companion SelectionStudioLightingTests exercises actual Renderer GPU pixels.
    public sealed class SelectionStudioPrototypeIntegrationTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static T Read<T>(object owner, string field) => (T)owner.GetType().GetField(field, Private).GetValue(owner);
        static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Private).SetValue(owner, value);
        static void Call(object owner, string method, params object[] values) => owner.GetType().GetMethod(method, Private).Invoke(owner, values);

        [UnityTest]
        public IEnumerator RealBeginFightRebuildAndRosterRestoreArmedPreviewScope()
        {
            using (var fixture = new Fixture())
            {
                var game = fixture.Create();
                Call(game, "Start"); // real startup, disabled UI owner; no automatic second Start
                var scope = Read<SelectionStudioLighting>(game, "selectionLighting");
                Assert.That(scope.configuration, Is.Not.Null);
                Assert.That(scope.configuration.IsValid, Is.True);
                Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));
                game.SelectRosterFighter(0);
                Call(game, "UpdateSelection");
                var fighter = Read<FighterView>(game, "player");
                var renderer = fighter.GetComponentInChildren<Renderer>(true);
                var before = new LightingSnapshot(scope.ownedKeyLight, renderer);
                Arm(scope, Read<Camera>(game, "fightCamera"), renderer);
                game.BeginFight();
                Assert.That(scope.explicitPreviewEnabled, Is.False);
                Assert.That(scope.IsScoped, Is.False);
                before.AssertRestored(scope.ownedKeyLight, renderer);
                Assert.That(Read<bool>(game, "selecting"), Is.False);

                // Exercise the real roster rebuild while a scope is armed, so flag-only cleanup
                // cannot pass. The old renderer must be restored before it is destroyed/replaced.
                Set(game, "selecting", true);
                game.SelectRosterFighter(1);
                Call(game, "UpdateSelection");
                renderer = Read<FighterView>(game, "player").GetComponentInChildren<Renderer>(true);
                before = new LightingSnapshot(scope.ownedKeyLight, renderer);
                Arm(scope, Read<Camera>(game, "fightCamera"), renderer);
                game.SelectRosterFighter(2);
                Assert.That(scope.explicitPreviewEnabled, Is.False);
                Assert.That(scope.IsScoped, Is.False);
                before.AssertRestored(scope.ownedKeyLight, renderer);

                Call(game, "UpdateSelection");
                renderer = Read<FighterView>(game, "player").GetComponentInChildren<Renderer>(true);
                before = new LightingSnapshot(scope.ownedKeyLight, renderer);
                Arm(scope, Read<Camera>(game, "fightCamera"), renderer);
                Set(game, "rosterOpen", true);
                Call(game, "UpdateSelection");
                Assert.That(scope.explicitPreviewEnabled, Is.False);
                Assert.That(scope.IsScoped, Is.False);
                before.AssertRestored(scope.ownedKeyLight, renderer);
                Assert.That(Read<FighterView>(game, "player").gameObject.activeSelf, Is.False);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealStreamedStartDefersActorsAndBindsLoadedSelectionBeforeFight()
        {
            using (var fixture = new Fixture())
            {
                var game = fixture.Create(true);
                Assert.DoesNotThrow(() => Call(game, "Start"), "Streamed startup has no player until selected assets finish");
                var scope = Read<SelectionStudioLighting>(game, "selectionLighting");
                Assert.That(game.ActorsLoading, Is.True);
                Assert.That(Read<FighterView>(game, "player"), Is.Null);
                Assert.That(scope.explicitPreviewEnabled, Is.False);
                yield return Wait(() => fixture.provider.operations.Count == 3, "selected loads issued");
                foreach (var operation in fixture.provider.operations) operation.done = true;
                yield return Wait(() => !game.ActorsLoading, "selected actor readiness");
                Call(game, "UpdateSelection"); // initial roster is opaque, so still no studio render
                Assert.That(scope.explicitPreviewEnabled, Is.False);
                Set(game, "rosterOpen", false);
                Call(game, "UpdateSelection");
                var renderer = Read<FighterView>(game, "player").GetComponentInChildren<Renderer>(true);
                var before = new LightingSnapshot(scope.ownedKeyLight, renderer);
                Arm(scope, Read<Camera>(game, "fightCamera"), renderer);
                game.BeginFight();
                Assert.That(scope.explicitPreviewEnabled, Is.False);
                Assert.That(scope.IsScoped, Is.False);
                before.AssertRestored(scope.ownedKeyLight, renderer);
                Assert.That(fixture.provider.operations.All(o => o.releases == 0), Is.True, "Fight retains selected actor leases");
            }
            yield return null; yield return null; yield return null;
        }

        static void Arm(SelectionStudioLighting scope, Camera camera, Renderer renderer)
        {
            Assert.That(scope.explicitPreviewEnabled, Is.True);
            // Invoke the actual render callback at a controlled boundary. BeginFight/rebuild
            // must release immediately even if invoked before OnPostRender. No engine lifecycle
            // subscription is faked; actual camera render/GPU behavior is covered separately.
            Call(scope, "BeforeAnyCamera", camera);
            Assert.That(scope.IsScoped, Is.True);
            Assert.That(scope.ownedKeyLight.enabled, Is.False);
            Assert.That(renderer.lightProbeUsage, Is.EqualTo(LightProbeUsage.Off));
            Assert.That(renderer.reflectionProbeUsage, Is.EqualTo(ReflectionProbeUsage.Off));
            Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Custom));
        }
        static IEnumerator Wait(Func<bool> ready, string stage)
        {
            float deadline = Time.realtimeSinceStartup + 8;
            while (!ready() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(ready(), Is.True, "Bounded readiness: " + stage);
        }
        sealed class LightingSnapshot
        {
            readonly AmbientMode ambient = RenderSettings.ambientMode;
            readonly SphericalHarmonicsL2 probe = RenderSettings.ambientProbe;
            readonly DefaultReflectionMode reflectionMode = RenderSettings.defaultReflectionMode;
            readonly Texture reflection = RenderSettings.customReflectionTexture;
            readonly float ambientIntensity = RenderSettings.ambientIntensity, reflectionIntensity = RenderSettings.reflectionIntensity;
            readonly bool keyEnabled;
            readonly LightProbeUsage lightUsage;
            readonly ReflectionProbeUsage reflectionUsage;
            public LightingSnapshot(Light key, Renderer renderer)
            { keyEnabled = key.enabled; lightUsage = renderer.lightProbeUsage; reflectionUsage = renderer.reflectionProbeUsage; }
            public void AssertRestored(Light key, Renderer renderer)
            {
                Assert.That(RenderSettings.ambientMode, Is.EqualTo(ambient));
                Assert.That(RenderSettings.ambientIntensity, Is.EqualTo(ambientIntensity));
                Assert.That(RenderSettings.reflectionIntensity, Is.EqualTo(reflectionIntensity));
                Assert.That(RenderSettings.defaultReflectionMode, Is.EqualTo(reflectionMode));
                Assert.That(RenderSettings.customReflectionTexture, Is.SameAs(reflection));
                for (int c = 0; c < 3; c++) for (int i = 0; i < 9; i++) Assert.That(RenderSettings.ambientProbe[c,i], Is.EqualTo(probe[c,i]));
                Assert.That(key.enabled, Is.EqualTo(keyEnabled));
                Assert.That(renderer.lightProbeUsage, Is.EqualTo(lightUsage));
                Assert.That(renderer.reflectionProbeUsage, Is.EqualTo(reflectionUsage));
            }
        }
        sealed class Operation : ISelectedAssetLoad
        {
            public bool done; public GameObject prefab; public int releases;
            public bool IsDone => done; public GameObject Asset => prefab; public string Error => null;
            public void Dispose() { releases++; }
        }
        sealed class Provider : ISelectedAssetProvider
        {
            public readonly List<Operation> operations = new List<Operation>();
            public GameObject fighter, arena;
            public ISelectedAssetLoad Load(string key)
            { var operation = new Operation { prefab = key.StartsWith("a") ? arena : fighter }; operations.Add(operation); return operation; }
        }
        sealed class Fixture : IDisposable
        {
            readonly string[] ints = {"lf.shadows","lf.swipes","lf.graphics","lf.fps","lf.sound","lf.shake","lf.left","lf.diagnostics","lf.attackButton.0","lf.attackButton.1","lf.attackButton.2","lf.attackButton.3"};
            readonly string[] floats = {"lf.controlScale","lf.controlOpacity"};
            readonly Dictionary<string,float> preferences = new Dictionary<string,float>();
            readonly HashSet<string> existing = new HashSet<string>();
            readonly AmbientMode mode = RenderSettings.ambientMode;
            readonly SphericalHarmonicsL2 probe = RenderSettings.ambientProbe;
            readonly Color ambient = RenderSettings.ambientLight;
            readonly Texture reflection = RenderSettings.customReflectionTexture;
            readonly DefaultReflectionMode reflectionMode = RenderSettings.defaultReflectionMode;
            readonly float ambientIntensity = RenderSettings.ambientIntensity, reflectionIntensity = RenderSettings.reflectionIntensity;
            readonly int aa = QualitySettings.antiAliasing, vsync = QualitySettings.vSyncCount, fps = Application.targetFrameRate, mips = QualitySettings.globalTextureMipmapLimit;
            readonly float lod = QualitySettings.lodBias, distance = QualitySettings.shadowDistance;
            readonly SkinWeights weights = QualitySettings.skinWeights;
            readonly ShadowQuality shadows = QualitySettings.shadows;
            readonly ShadowResolution resolution = QualitySettings.shadowResolution;
            GameObject root, fighter, arena; GameContent catalog;
            public Provider provider;
            public Fixture()
            {
                foreach (string key in ints) { if (PlayerPrefs.HasKey(key)) existing.Add(key); preferences[key] = PlayerPrefs.GetInt(key); }
                foreach (string key in floats) { if (PlayerPrefs.HasKey(key)) existing.Add(key); preferences[key] = PlayerPrefs.GetFloat(key); }
                PlayerPrefs.SetInt("lf.diagnostics", 0);
            }
            public Prototype Create(bool streamed = false)
            {
                root = new GameObject("real Prototype studio owner integration");
                var game = root.AddComponent<Prototype>(); game.enabled = false;
                if (streamed)
                {
                    fighter = FighterView.Create("streamed lighting fixture", Color.gray, false).gameObject; fighter.SetActive(false);
                    arena = OfficeArena.Create(OfficeLocation.Yard).gameObject; arena.SetActive(false);
                    catalog = ScriptableObject.CreateInstance<GameContent>(); catalog.streamed = true;
                    catalog.fighterAddresses = new[] {"f0","f1"}; catalog.arenaAddresses = new[] {"a0"};
                    provider = new Provider {fighter=fighter,arena=arena}; game.content=catalog; game.SelectedAssetProvider=provider;
                }
                return game;
            }
            public void Dispose()
            {
                if (root) Object.DestroyImmediate(root); if (fighter) Object.DestroyImmediate(fighter); if (arena) Object.DestroyImmediate(arena); if (catalog) Object.DestroyImmediate(catalog);
                RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambient;RenderSettings.ambientProbe=probe;RenderSettings.ambientIntensity=ambientIntensity;
                RenderSettings.customReflectionTexture=reflection;RenderSettings.defaultReflectionMode=reflectionMode;RenderSettings.reflectionIntensity=reflectionIntensity;
                QualitySettings.antiAliasing=aa;QualitySettings.vSyncCount=vsync;Application.targetFrameRate=fps;QualitySettings.globalTextureMipmapLimit=mips;
                QualitySettings.lodBias=lod;QualitySettings.skinWeights=weights;QualitySettings.shadowDistance=distance;QualitySettings.shadows=shadows;QualitySettings.shadowResolution=resolution;
                foreach(string key in ints)if(existing.Contains(key))PlayerPrefs.SetInt(key,(int)preferences[key]);else PlayerPrefs.DeleteKey(key);
                foreach(string key in floats)if(existing.Contains(key))PlayerPrefs.SetFloat(key,preferences[key]);else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
}
