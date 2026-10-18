using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using LighFight.Runtime;

namespace LighFight.Tests
{
    public sealed class SelectionStudioLightingTests
    {
        [UnityTest]
        public IEnumerator ActualRendererPreviewRestoresFightEnvironmentAndDisabledDoesNoWork()
        {
            var oldMode = RenderSettings.ambientMode;
            var oldProbe = RenderSettings.ambientProbe;
            var oldReflection = RenderSettings.customReflectionTexture;
            var oldDefault = RenderSettings.defaultReflectionMode;
            float oldAmbient = RenderSettings.ambientIntensity, oldReflectionIntensity = RenderSettings.reflectionIntensity;
            var oldRT = RenderTexture.active;
            GameObject cameraObject = null, card = null, keyObject = null, nestedObject = null;
            Camera.CameraCallback observer = null;
            Material material = null;
            RenderTexture target = null;
            Texture2D pixel = null;
            Cubemap cube = null;
            SelectionStudioLightingConfig config = null;
            try
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientIntensity = .73f;
                var fightSH = new SphericalHarmonicsL2();
                RenderSettings.ambientProbe = fightSH;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
                RenderSettings.customReflectionTexture = null;
                RenderSettings.reflectionIntensity = .37f;
                cameraObject = new GameObject("Scoped studio camera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(10000, 10000, 10000);
                camera.orthographic = true; camera.orthographicSize = 1;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                camera.cullingMask = 1 << 30;
                target = new RenderTexture(16, 16, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                target.Create(); camera.targetTexture = target;
                card = GameObject.CreatePrimitive(PrimitiveType.Quad);
                card.layer = 30; card.transform.position = camera.transform.position + Vector3.forward * 2;
                var renderer = card.GetComponent<Renderer>();
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                material = new Material(Shader.Find("Hidden/LighFight/SelectionStudioProbe"));
                material.SetVector("_ProbeDir", Vector3.up); renderer.sharedMaterial = material;
                keyObject = new GameObject("Owned fight key");
                var key = keyObject.AddComponent<Light>(); key.enabled = true;
                cube = new Cubemap(128, TextureFormat.RGBAHalf, true);
                var pixels = new Color[128 * 128];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(.25f, .25f, .25f, 1);
                for (int f = 0; f < 6; f++) cube.SetPixels(pixels, (CubemapFace)f);
                cube.Apply(true, false);
                config = ScriptableObject.CreateInstance<SelectionStudioLightingConfig>();
                config.prefilteredReflection = cube;
                var studioSH = new SphericalHarmonicsL2(); studioSH.AddAmbientLight(new Color(.18f, .18f, .18f));
                for (int c = 0; c < 3; c++) for (int i = 0; i < 9; i++) config.nativeDiffuseSH[c * 9 + i] = studioSH[c, i];
                var scope = cameraObject.AddComponent<SelectionStudioLighting>();
                scope.configuration = config; scope.ownedKeyLight = key; scope.SetSelectedRenderers(new[] { renderer });
                pixel = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
                yield return null; yield return null;
                camera.Render();
                float fight = Read(target, pixel);
                fightSH = RenderSettings.ambientProbe; // actual Flat engine-generated state at snapshot boundary
                scope.SetPreviewEnabled(true);
                camera.Render();
                float preview = Read(target, pixel);
                Assert.That(scope.AppliedScopes, Is.EqualTo(1), "Camera lifecycle subscription must execute before interpreting GPU output");
                Assert.That(preview, Is.EqualTo(.18f).Within(.004f), "Actual ForwardBase renderer must receive native global SH");
                Assert.That(preview, Is.GreaterThan(fight + .1f));
                Assert.That(scope.AppliedScopes, Is.EqualTo(1));
                Assert.That(scope.IsScoped, Is.False);
                Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));
                Assert.That(RenderSettings.ambientIntensity, Is.EqualTo(.73f));
                Assert.That(RenderSettings.reflectionIntensity, Is.EqualTo(.37f));
                Assert.That(RenderSettings.defaultReflectionMode, Is.EqualTo(DefaultReflectionMode.Skybox));
                Assert.That(RenderSettings.customReflectionTexture, Is.Null);
                Assert.That(key.enabled, Is.True);
                Assert.That(renderer.lightProbeUsage, Is.EqualTo(LightProbeUsage.BlendProbes));
                Assert.That(renderer.reflectionProbeUsage, Is.EqualTo(ReflectionProbeUsage.BlendProbes));
                for (int c = 0; c < 3; c++) for (int i = 0; i < 9; i++) Assert.That(RenderSettings.ambientProbe[c, i], Is.EqualTo(fightSH[c, i]));
                material.SetFloat("_Mode", 1);
                camera.Render();
                Assert.That(Read(target, pixel), Is.EqualTo(.25f).Within(.004f), "Actual renderer must use scoped global prefiltered reflection");
                material.SetFloat("_Mode", 0);
                int appliedAfterPreview = scope.AppliedScopes;
                scope.SetPreviewEnabled(false); camera.Render();
                Assert.That(Read(target, pixel), Is.EqualTo(fight).Within(.001f));
                Assert.That(scope.AppliedScopes, Is.EqualTo(appliedAfterPreview));
                scope.enabled = false; camera.Render();
                Assert.That(scope.AppliedScopes, Is.EqualTo(appliedAfterPreview));
                scope.enabled = true; scope.SetPreviewEnabled(true);
                nestedObject = new GameObject("Nested fight camera");
                var nested = nestedObject.AddComponent<Camera>(); nested.enabled = false;
                nested.cullingMask = 0; nested.targetTexture = target;
                bool nestedRendered = false, observedPreview = false;
                observer = current =>
                {
                    if (current == camera && !nestedRendered)
                    {
                        observedPreview = true;
                        Assert.That(key.enabled, Is.False);
                        Assert.That(renderer.lightProbeUsage, Is.EqualTo(LightProbeUsage.Off));
                        Assert.That(renderer.reflectionProbeUsage, Is.EqualTo(ReflectionProbeUsage.Off));
                        Assert.That(RenderSettings.customReflectionTexture, Is.SameAs(cube));
                        nestedRendered = true;
                        nested.Render();
                        Assert.That(scope.IsScoped, Is.False, "Nested camera must interrupt outer scope");
                    }
                    if (current == nested)
                    {
                        Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));
                        Assert.That(key.enabled, Is.True);
                    }
                };
                Camera.onPreRender += observer;
                camera.Render();
                Camera.onPreRender -= observer; observer = null;
                Assert.That(observedPreview && nestedRendered, Is.True);
                Assert.That(scope.IsScoped, Is.False);
                observer = current => { if (current == camera) scope.enabled = false; };
                Camera.onPreRender += observer;
                camera.Render();
                Camera.onPreRender -= observer; observer = null;
                Assert.That(scope.IsScoped, Is.False, "Disable during render must restore immediately");
                Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));
                Assert.That(key.enabled, Is.True);
            }
            finally
            {
                if (observer != null) Camera.onPreRender -= observer;
                if (nestedObject) Object.DestroyImmediate(nestedObject);
                if (cameraObject) Object.DestroyImmediate(cameraObject);
                if (card) Object.DestroyImmediate(card);
                if (keyObject) Object.DestroyImmediate(keyObject);
                if (material) Object.DestroyImmediate(material);
                if (config) Object.DestroyImmediate(config);
                if (cube) Object.DestroyImmediate(cube);
                if (pixel) Object.DestroyImmediate(pixel);
                RenderTexture.active = oldRT;
                if (target) { target.Release(); Object.DestroyImmediate(target); }
                RenderSettings.ambientMode = oldMode; RenderSettings.ambientProbe = oldProbe;
                RenderSettings.ambientIntensity = oldAmbient; RenderSettings.reflectionIntensity = oldReflectionIntensity;
                RenderSettings.defaultReflectionMode = oldDefault; RenderSettings.customReflectionTexture = oldReflection;
            }
        }
        static float Read(RenderTexture target, Texture2D pixel)
        {
            RenderTexture.active = target;
            pixel.ReadPixels(new Rect(8, 8, 1, 1), 0, 0); pixel.Apply();
            return pixel.GetPixel(0, 0).r;
        }
    }
}
