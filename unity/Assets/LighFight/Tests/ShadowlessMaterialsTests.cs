using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using LighFight.Runtime;
public class ShadowlessMaterialsTests {
 [Test] public void RenderKeepsAuthoredHandMaskAndIgnoresDirectionalLight(){
  var root=new GameObject("Shadowless pixel test");var source=new Material(Shader.Find("LighFight/CharacterSkin"));var body=new Texture2D(2,2);var hand=new Texture2D(2,2);var mesh=new Mesh();var rt=new RenderTexture(64,64,24);var image=new Texture2D(64,64);var previous=RenderTexture.active;bool asynchronous=UnityEditor.ShaderUtil.allowAsyncCompilation;
  try{
   UnityEditor.ShaderUtil.allowAsyncCompilation=false;root.layer=31;body.SetPixels(new[]{Color.red,Color.red,Color.red,Color.red});body.Apply();hand.SetPixels(new[]{Color.green,Color.green,Color.green,Color.green});hand.Apply();source.mainTexture=body;source.SetTexture("_DetailSkinTex",hand);
   mesh.vertices=new[]{new Vector3(-.9f,-.9f),new Vector3(-.05f,-.9f),new Vector3(-.05f,.9f),new Vector3(-.9f,.9f),new Vector3(.05f,-.9f),new Vector3(.9f,-.9f),new Vector3(.9f,.9f),new Vector3(.05f,.9f)};
   mesh.triangles=new[]{0,1,2,0,2,3,4,5,6,4,6,7};mesh.uv=new Vector2[8];mesh.uv2=new Vector2[8];mesh.colors=new[]{Color.black,Color.black,Color.black,Color.black,Color.red,Color.red,Color.red,Color.red};root.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=source;root.AddComponent<ShadowlessFighterMaterials>().Apply();renderer.sharedMaterial.SetFloat("_Cull",0);
   var camObject=new GameObject("Pixel camera");camObject.transform.SetParent(root.transform);var camera=camObject.AddComponent<Camera>();camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.position=Vector3.back*2;camera.transform.LookAt(Vector3.zero);camera.targetTexture=rt;
   var lightObject=new GameObject("Pixel light");lightObject.transform.SetParent(root.transform);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<31;light.intensity=0;
   camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,64,64),0,0);image.Apply();var firstBody=image.GetPixel(16,32);var firstHand=image.GetPixel(48,32);Assert.Greater(firstBody.r,firstBody.g+.5f,"Body samples the wrong atlas");Assert.Greater(firstHand.g,firstHand.r+.5f,"Replacement hand mask disappeared");
   light.intensity=4;camera.Render();image.ReadPixels(new Rect(0,0,64,64),0,0);image.Apply();Assert.Less(Vector4.Distance(firstBody,image.GetPixel(16,32)),.01f);Assert.Less(Vector4.Distance(firstHand,image.GetPixel(48,32)),.01f);
   camera.targetTexture=null;
  }finally{RenderTexture.active=previous;UnityEditor.ShaderUtil.allowAsyncCompilation=asynchronous;Object.DestroyImmediate(root);Object.DestroyImmediate(source);Object.DestroyImmediate(body);Object.DestroyImmediate(hand);Object.DestroyImmediate(mesh);Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
 }
 [Test] public void ShadowlessPresentationPreservesBodyAndReplacementHandTextures(){
  var root=new GameObject("Shadowless material test");var source=new Material(Shader.Find("LighFight/CharacterSkin"));var body=new Texture2D(2,2);var hand=new Texture2D(2,2);
  try{
   source.mainTexture=body;source.SetTexture("_DetailSkinTex",hand);source.color=new Color(.8f,.7f,.6f,1);source.mainTextureScale=new Vector2(2,3);source.mainTextureOffset=new Vector2(.1f,.2f);
   var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=source;root.AddComponent<ShadowlessFighterMaterials>().Apply();
   var flat=renderer.sharedMaterial;Assert.AreEqual("LighFight/Shadowless Character",flat.shader.name);Assert.AreNotSame(source,flat);Assert.AreSame(body,flat.mainTexture);Assert.AreSame(hand,flat.GetTexture("_DetailSkinTex"));Assert.AreEqual(1,flat.GetFloat("_UseDetailSkin"));Assert.AreEqual(source.color,flat.color);Assert.AreEqual(source.mainTextureScale,flat.mainTextureScale);Assert.AreEqual(source.mainTextureOffset,flat.mainTextureOffset);
   Assert.AreEqual(ShadowCastingMode.Off,renderer.shadowCastingMode);Assert.IsFalse(renderer.receiveShadows);
   root.GetComponent<ShadowlessFighterMaterials>().Apply();Assert.AreSame(flat,renderer.sharedMaterial,"Repeated options must not allocate more materials");Assert.AreEqual("LighFight/CharacterSkin",source.shader.name,"Imported source material must remain intact");
  }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(source);Object.DestroyImmediate(body);Object.DestroyImmediate(hand);}
 }
 [Test] public void NativePresentationRetainsDoubleSidedSurface(){
  var root=new GameObject("Native shadowless test");var source=new Material(Shader.Find("LighFight/Native PBR"));
  try{source.SetFloat("_Cull",0);var renderer=root.AddComponent<SkinnedMeshRenderer>();renderer.sharedMaterial=source;root.AddComponent<ShadowlessFighterMaterials>().Apply();Assert.AreEqual("LighFight/Shadowless Character",renderer.sharedMaterial.shader.name);Assert.AreEqual(0,renderer.sharedMaterial.GetFloat("_Cull"));Assert.AreEqual(0,renderer.sharedMaterial.GetFloat("_UseDetailSkin"));}
  finally{Object.DestroyImmediate(root);Object.DestroyImmediate(source);}
 }
}
