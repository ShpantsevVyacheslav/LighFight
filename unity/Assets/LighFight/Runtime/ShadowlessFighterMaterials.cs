using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
namespace LighFight.Runtime {
 public sealed class ShadowlessFighterMaterials:MonoBehaviour {
  readonly Dictionary<Material,Material> copies=new Dictionary<Material,Material>();
  public void Apply(){
   var shader=Shader.Find("LighFight/Shadowless Character");if(!shader)throw new System.InvalidOperationException("Missing shadowless character shader");
   foreach(var renderer in GetComponentsInChildren<Renderer>(true)){
    var materials=renderer.sharedMaterials;
    for(int i=0;i<materials.Length;i++){
     var source=materials[i];if(!source||source.shader==shader)continue;
     if(!copies.TryGetValue(source,out var flat)){
      flat=new Material(shader){name=source.name+" (shadowless)",mainTexture=source.mainTexture,color=source.HasProperty("_Color")?source.color:Color.white};
      flat.mainTextureScale=source.mainTextureScale;flat.mainTextureOffset=source.mainTextureOffset;flat.SetFloat("_Cull",source.HasProperty("_Cull")?source.GetFloat("_Cull"):2);
      if(source.HasProperty("_DetailSkinTex")&&source.GetTexture("_DetailSkinTex")){flat.SetTexture("_DetailSkinTex",source.GetTexture("_DetailSkinTex"));flat.SetFloat("_UseDetailSkin",1);}
      copies.Add(source,flat);
     }
     materials[i]=flat;
    }
    renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
   }
  }
  void OnDestroy(){foreach(var material in copies.Values)if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);copies.Clear();}
 }
}
