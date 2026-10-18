using UnityEngine;
using UnityEngine.Rendering;

namespace LighFight.Runtime
{
    /// <summary>Small, authored environment only. Actor materials are never modified.</summary>
    [CreateAssetMenu(menuName = "LighFight/Selection Studio Lighting")]
    public sealed class SelectionStudioLightingConfig : ScriptableObject
    {
        public Cubemap prefilteredReflection;
        [Tooltip("Unity-native RGB SH coefficients, channel-major; not raw orthonormal Ylm.")]
        public float[] nativeDiffuseSH = new float[27];
        public string sourceSHA256;
        public bool IsValid
        {
            get
            {
                if (!prefilteredReflection || prefilteredReflection.width != 128 ||
                    prefilteredReflection.mipmapCount != 8 || nativeDiffuseSH == null || nativeDiffuseSH.Length != 27)
                    return false;
                for (int i = 0; i < 27; i++)
                    if (float.IsNaN(nativeDiffuseSH[i]) || float.IsInfinity(nativeDiffuseSH[i])) return false;
                return true;
            }
        }
        public SphericalHarmonicsL2 DiffuseProbe
        {
            get
            {
                var value = new SphericalHarmonicsL2();
                for (int c = 0; c < 3; c++) for (int i = 0; i < 9; i++) value[c, i] = nativeDiffuseSH[c * 9 + i];
                return value;
            }
        }
    }
}
