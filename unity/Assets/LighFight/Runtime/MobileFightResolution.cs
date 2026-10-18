using UnityEngine;
namespace LighFight.Runtime {
 // Scale the mobile player framebuffer, independently of the unchanged source assets.
 // Baseline is the player resolution on first use, so Maximum restores that resolution.
 [DisallowMultipleComponent]public sealed class MobileFightResolution:MonoBehaviour {
  Vector2Int baseline;float scale=1;bool landscape,configured;
  public Vector2Int RequestedSize{get;private set;}
  public static Vector2Int Size(Vector2Int baseline,float scale,bool landscape){
   int longest=Mathf.Max(baseline.x,baseline.y),shortest=Mathf.Min(baseline.x,baseline.y);
   int width=landscape?longest:shortest,height=landscape?shortest:longest;
   return new Vector2Int(Mathf.Max(1,Mathf.RoundToInt(width*scale)),Mathf.Max(1,Mathf.RoundToInt(height*scale)));
  }
  public void Configure(float value){
   if(!Application.isMobilePlatform||Application.isEditor)return;
   if(!configured){baseline=new Vector2Int(Screen.width,Screen.height);landscape=Screen.width>=Screen.height;configured=true;}
   scale=Mathf.Clamp(value,.1f,1);Apply();
  }
  void Update(){if(!configured)return;bool current=Screen.width>=Screen.height;if(current!=landscape){landscape=current;Apply();}}
  void Apply(){var size=Size(baseline,scale,landscape);RequestedSize=size;if(size.x!=Screen.width||size.y!=Screen.height)Screen.SetResolution(size.x,size.y,Screen.fullScreenMode);}
 }
}
