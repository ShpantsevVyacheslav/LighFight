using UnityEngine;
namespace LighFight.Runtime {
 public sealed partial class Prototype {
  private FightDiagnostics fightDiagnostics;
  private GUIStyle fightDiagnosticsStyle;
  private ContentBootstrap diagnosticBootstrap;
  private bool FightDiagnosticsEnabled=>fightDiagnostics&&fightDiagnostics.isActiveAndEnabled;
  private void SetFightDiagnostics(bool enabled){
   if(!enabled){if(fightDiagnostics)fightDiagnostics.gameObject.SetActive(false);return;}
   if(!fightDiagnostics){var owner=new GameObject("Opt-in fight diagnostics");owner.SetActive(false);owner.transform.SetParent(transform,false);fightDiagnostics=owner.AddComponent<FightDiagnostics>();diagnosticBootstrap=FindFirstObjectByType<ContentBootstrap>();fightDiagnostics.ContextProvider=FightDiagnosticContext;}
   fightDiagnostics.gameObject.SetActive(true);
  }
  private string FightDiagnosticContext(){
   string stage=backgrounded?"background":optionsOpen?"options":selecting?"selection":paused||Time.timeScale==0?"paused":session!=null&&session.Phase==LighFight.Core.FightPhase.Complete?"victory":"fight";
   return "stage="+stage+"; fighter="+selectedFighter+"; opponent="+OpponentIndex+"; arena="+selectedArena+"; tier="+graphicsQuality+"; target="+frameRate+"; AA="+QualitySettings.antiAliasing+"; shadows="+QualitySettings.shadows+"; screen="+Screen.width+"x"+Screen.height+"; power="+SystemInfo.batteryStatus+"; content="+(diagnosticBootstrap?diagnosticBootstrap.ContentVersion??"unavailable":"inline")+"; unity="+Application.unityVersion+"; development="+Debug.isDebugBuild;
  }
  private void DrawFightDiagnostics(){
   if(!FightDiagnosticsEnabled)return;
   if(fightDiagnosticsStyle==null)fightDiagnosticsStyle=new GUIStyle(GUI.skin.label){fontSize=10,wordWrap=false,clipping=TextClipping.Clip,padding=new RectOffset(8,8,6,6),normal={textColor=Color.white}};
   // Labels and painted backdrop have no input control: gestures remain available through the HUD.
   var rect=new Rect(255,432,450,96);Card(rect,new Color(.02f,.025f,.04f,.82f));GUI.Label(rect,fightDiagnostics.Summary,fightDiagnosticsStyle);
  }
 }
}
