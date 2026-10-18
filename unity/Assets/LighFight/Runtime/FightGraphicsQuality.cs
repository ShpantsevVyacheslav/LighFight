using UnityEngine;
namespace LighFight.Runtime {
 // Runtime rendering budgets. Imported meshes, skin weights and texture data stay intact.
 public static class FightGraphicsQuality {
  public static int Tier(int value)=>Mathf.Clamp(value,0,2);
  public static int FrameRate(int value)=>value==30?30:60;
  public static float RenderScale(int tier)=>Tier(tier)==0?.65f:Tier(tier)==1?.85f:1f;
  public static int Antialiasing(int tier)=>Tier(tier)==0?0:Tier(tier)==1?2:4;
  public static void ApplyGlobal(int tier,int fps,bool shadows){
   tier=Tier(tier);
   QualitySettings.globalTextureMipmapLimit=0;QualitySettings.skinWeights=SkinWeights.Unlimited;QualitySettings.lodBias=4;
   QualitySettings.antiAliasing=Antialiasing(tier);QualitySettings.vSyncCount=0;Application.targetFrameRate=FrameRate(fps);
   QualitySettings.shadows=shadows?ShadowQuality.All:ShadowQuality.Disable;
   QualitySettings.shadowDistance=tier==0?10:tier==1?18:25;
   QualitySettings.shadowResolution=tier==0?ShadowResolution.Low:tier==1?ShadowResolution.Medium:ShadowResolution.High;
  }
  public static void ApplyLights(GameObject owner,int tier,bool shadows){
   foreach(var light in owner.GetComponentsInChildren<Light>(true)){
    light.shadows=!shadows?LightShadows.None:Tier(tier)==0?LightShadows.Hard:LightShadows.Soft;
    light.shadowStrength=.65f;light.shadowBias=.03f;light.shadowNormalBias=.2f;
   }
  }
 }
}
