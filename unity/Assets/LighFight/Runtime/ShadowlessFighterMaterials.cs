using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
namespace LighFight.Runtime {
 // Compatibility name retained for existing prefabs/callers. Shadows never change authored materials.
 public sealed class ShadowlessFighterMaterials:MonoBehaviour {
  struct AuthoredFlags {public ShadowCastingMode casting;public bool receiving;}
  readonly Dictionary<Renderer,AuthoredFlags> overrides=new Dictionary<Renderer,AuthoredFlags>();
  public void SetShadows(bool enabled){if(enabled)Restore();else Apply();}
  public void Apply(){
   foreach(var renderer in GetComponentsInChildren<Renderer>(true)){
    if(!overrides.TryGetValue(renderer,out var authored))authored=new AuthoredFlags{casting=renderer.shadowCastingMode,receiving=renderer.receiveShadows};
    else {if(renderer.shadowCastingMode!=ShadowCastingMode.Off)authored.casting=renderer.shadowCastingMode;if(renderer.receiveShadows)authored.receiving=true;}
    overrides[renderer]=authored;
    renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
   }
  }
  public void Restore(){
   foreach(var pair in overrides){
    var renderer=pair.Key;if(!renderer)continue;
    // Release only values still owned by this override; preserve explicit external updates.
    if(renderer.shadowCastingMode==ShadowCastingMode.Off)renderer.shadowCastingMode=pair.Value.casting;
    if(!renderer.receiveShadows)renderer.receiveShadows=pair.Value.receiving;
   }
   overrides.Clear();
  }
  void OnDisable(){Restore();}
  void OnDestroy(){Restore();}
 }
}
