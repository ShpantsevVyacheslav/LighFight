using UnityEngine;
using UnityEngine.Rendering;

namespace LighFight.Runtime
{
    /// <summary>Built-in camera-only environment scope. Configure once when selected actors change.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class SelectionStudioLighting : MonoBehaviour
    {
        public SelectionStudioLightingConfig configuration;
        public bool explicitPreviewEnabled;
        public Light ownedKeyLight;
        static SelectionStudioLighting activeScope, suspendedScope;
        Camera ownerCamera;
        Renderer[] selected = System.Array.Empty<Renderer>();
        LightProbeUsage[] priorLightProbes = System.Array.Empty<LightProbeUsage>();
        ReflectionProbeUsage[] priorReflectionProbes = System.Array.Empty<ReflectionProbeUsage>();
        AmbientMode priorAmbientMode;
        SphericalHarmonicsL2 priorProbe;
        Texture priorReflection;
        DefaultReflectionMode priorReflectionMode;
        float priorAmbientIntensity, priorReflectionIntensity;
        bool priorKeyEnabled, scoped, interrupted;
        Light scopedKey;
        public bool IsScoped => scoped;
        public int AppliedScopes { get; private set; }

        // Allocations occur only on roster rebuild, never during rendering.
        public void SetSelectedRenderers(Renderer[] renderers)
        {
            Restore();
            selected = renderers == null ? System.Array.Empty<Renderer>() : (Renderer[])renderers.Clone();
            priorLightProbes = new LightProbeUsage[selected.Length];
            priorReflectionProbes = new ReflectionProbeUsage[selected.Length];

        }
        public void SetPreviewEnabled(bool value)
        {
            explicitPreviewEnabled = value;
            if (!value) Restore();
        }
        void OnEnable()
        {
            ownerCamera = GetComponent<Camera>();
            Camera.onPreCull += BeforeAnyCamera;
        }
        void OnDisable() { Camera.onPreCull -= BeforeAnyCamera; Restore(); if (suspendedScope == this) suspendedScope = null; interrupted = false; }
        void OnDestroy() { Restore(); }
        void BeforeAnyCamera(Camera camera)
        {
            // A nested camera must see the original fight environment, never borrow the outer scope.
            if (activeScope && camera != activeScope.ownerCamera)
            {
                suspendedScope = activeScope;
                activeScope.interrupted = true;
                activeScope.Restore();
            }
            if (camera != ownerCamera || suspendedScope) return;
            if (scoped) { suspendedScope = this; interrupted = true; Restore(); return; }
            if (interrupted || !isActiveAndEnabled || !explicitPreviewEnabled || !configuration || !configuration.IsValid) return;
            Begin();
        }
        void Begin()
        {
            if (activeScope) return;
            priorAmbientMode = RenderSettings.ambientMode;
            priorProbe = RenderSettings.ambientProbe;
            priorAmbientIntensity = RenderSettings.ambientIntensity;
            priorReflection = RenderSettings.customReflectionTexture;
            priorReflectionMode = RenderSettings.defaultReflectionMode;
            priorReflectionIntensity = RenderSettings.reflectionIntensity;
            scopedKey = ownedKeyLight;
            if (scopedKey) { priorKeyEnabled = scopedKey.enabled; scopedKey.enabled = false; }
            for (int i = 0; i < selected.Length; i++) if (selected[i])
            {
                priorLightProbes[i] = selected[i].lightProbeUsage;
                priorReflectionProbes[i] = selected[i].reflectionProbeUsage;
                selected[i].lightProbeUsage = LightProbeUsage.Off;
                selected[i].reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            scoped = true;
            activeScope = this;
            RenderSettings.ambientMode = AmbientMode.Custom;
            RenderSettings.ambientProbe = configuration.DiffuseProbe;
            RenderSettings.ambientIntensity = 1;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = configuration.prefilteredReflection;
            RenderSettings.reflectionIntensity = 1;
            AppliedScopes++;
        }
        void OnPostRender() { Restore(); if (suspendedScope == this) suspendedScope = null; interrupted = false; }
        public void Restore()
        {
            if (!scoped) return;
            RenderSettings.ambientMode = priorAmbientMode;
            RenderSettings.ambientProbe = priorProbe;
            RenderSettings.ambientIntensity = priorAmbientIntensity;
            RenderSettings.defaultReflectionMode = priorReflectionMode;
            RenderSettings.customReflectionTexture = priorReflection;
            RenderSettings.reflectionIntensity = priorReflectionIntensity;
            if (scopedKey) scopedKey.enabled = priorKeyEnabled;
            for (int i = 0; i < selected.Length; i++) if (selected[i])
            {
                selected[i].lightProbeUsage = priorLightProbes[i];
                selected[i].reflectionProbeUsage = priorReflectionProbes[i];
            }
            scopedKey = null;
            scoped = false;
            if (activeScope == this) activeScope = null;
        }
    }
}
