using UnityEngine;using UnityEngine.Rendering;using UnityEditor;
// Explicit diffuse sky irradiance on review instances; no unlit/emission brightness workaround.
public static class CourtyardRendererIrradianceReview {
 public static void Apply(GameObject root){var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.40f,.43f,.46f));sh.AddDirectionalLight(Vector3.up,new Color(.25f,.28f,.32f),1);foreach(var r in root.GetComponentsInChildren<Renderer>(true)){r.lightProbeUsage=LightProbeUsage.CustomProvided;var p=new MaterialPropertyBlock();r.GetPropertyBlock(p);p.CopySHCoefficientArraysFrom(new[]{sh});p.CopyProbeOcclusionArrayFrom(new[]{Vector4.one});r.SetPropertyBlock(p);}Debug.Log("COURTYARD_IRRADIANCE CustomProvided SH: diffuseambient .40/.43/.46 + uppersky .25/.28/.32; instance-only, GPU proof pending");}
}
