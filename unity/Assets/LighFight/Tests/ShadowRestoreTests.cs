using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using LighFight.Runtime;
public class ShadowRestoreTests {
 [Test] public void ShadowSwitchRestoresAuthoredPbrWithoutLosingTextures(){
  var root=new GameObject("Shadow restore");var material=new Material(Shader.Find("LighFight/Native PBR"));var albedo=new Texture2D(2,2);var normal=new Texture2D(2,2);
  try{
   var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;material.mainTexture=albedo;material.SetTexture("_BumpMap",normal);material.EnableKeyword("_NORMALMAP");material.color=new Color(.7f,.8f,.9f,1);material.SetFloat("_Cull",0);
   var mode=root.AddComponent<ShadowlessFighterMaterials>();
   for(int i=0;i<3;i++){
    mode.SetShadows(false);Assert.AreEqual(ShadowCastingMode.Off,renderer.shadowCastingMode);Assert.IsFalse(renderer.receiveShadows);Assert.AreEqual("LighFight/Shadowless Character",renderer.sharedMaterial.shader.name);
    mode.SetShadows(true);Assert.AreSame(material,renderer.sharedMaterial);Assert.AreSame(albedo,renderer.sharedMaterial.mainTexture);Assert.AreSame(normal,renderer.sharedMaterial.GetTexture("_BumpMap"));Assert.IsTrue(renderer.sharedMaterial.IsKeywordEnabled("_NORMALMAP"));Assert.AreEqual(0,renderer.sharedMaterial.GetFloat("_Cull"));Assert.AreEqual(ShadowCastingMode.On,renderer.shadowCastingMode);Assert.IsTrue(renderer.receiveShadows);
   }
  }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(material);Object.DestroyImmediate(albedo);Object.DestroyImmediate(normal);}
 }
}
