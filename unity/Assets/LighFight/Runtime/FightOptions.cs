using UnityEngine;
namespace LighFight.Runtime {
 public sealed partial class Prototype {
  private bool optionsOpen;
  private readonly AttackButtonBindings attackBindings=new AttackButtonBindings();
  private int bindingSlot=-1;
  private int graphicsQuality=2,frameRate=60;
  private bool soundEnabled=true,cameraShake=true,leftHanded,shadowsEnabled=true;
  private float controlsScale=1,controlsOpacity=.75f;
  private Rect ControlRect(Rect rect){var center=rect.center;rect.width*=controlsScale;rect.height*=controlsScale;rect.center=center;if(leftHanded)rect.x=960-rect.xMax;return rect;}
  private void LoadOptions(){LoadOptionsForDevice(Application.isMobilePlatform&&!Application.isEditor);}
  private void LoadOptionsForDevice(bool mobile){attackBindings.Load("lf.attackButton.");shadowsEnabled=PlayerPrefs.GetInt("lf.shadows",1)==1;controlsScale=PlayerPrefs.GetFloat("lf.controlScale",1);controlsOpacity=PlayerPrefs.GetFloat("lf.controlOpacity",.75f);gesturesEnabled=PlayerPrefs.GetInt("lf.swipes",1)==1;graphicsQuality=PlayerPrefs.GetInt("lf.graphics",mobile?1:2);frameRate=PlayerPrefs.GetInt("lf.fps",mobile?30:60);soundEnabled=PlayerPrefs.GetInt("lf.sound",1)==1;cameraShake=PlayerPrefs.GetInt("lf.shake",1)==1;leftHanded=PlayerPrefs.GetInt("lf.left",0)==1;ApplyOptions();SetFightDiagnostics(PlayerPrefs.GetInt("lf.diagnostics",0)==1);}
  private void ApplyVisualQuality(){
   FightGraphicsQuality.ApplyGlobal(graphicsQuality,frameRate,shadowsEnabled);
   FightGraphicsQuality.ApplyLights(gameObject,graphicsQuality,shadowsEnabled);
   foreach(var materials in GetComponentsInChildren<ShadowlessFighterMaterials>(true))materials.SetShadows(shadowsEnabled);
   if(arena)foreach(var renderer in arena.GetComponentsInChildren<Renderer>(true)){renderer.receiveShadows=shadowsEnabled;renderer.shadowCastingMode=shadowsEnabled?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;}
   if(Application.isMobilePlatform&&!Application.isEditor){var resolution=GetComponent<MobileFightResolution>();if(!resolution)resolution=gameObject.AddComponent<MobileFightResolution>();resolution.Configure(FightGraphicsQuality.RenderScale(graphicsQuality));}
  }
  private void ApplyOptions(){graphicsQuality=FightGraphicsQuality.Tier(graphicsQuality);frameRate=FightGraphicsQuality.FrameRate(frameRate);ApplyVisualQuality();var audio=GetComponentsInChildren<AudioSource>(true);foreach(var source in audio)source.mute=!soundEnabled;
   attackBindings.Save("lf.attackButton.");PlayerPrefs.SetFloat("lf.controlScale",controlsScale);PlayerPrefs.SetFloat("lf.controlOpacity",controlsOpacity);PlayerPrefs.SetInt("lf.swipes",gesturesEnabled?1:0);PlayerPrefs.SetInt("lf.graphics",graphicsQuality);PlayerPrefs.SetInt("lf.fps",frameRate);PlayerPrefs.SetInt("lf.sound",soundEnabled?1:0);PlayerPrefs.SetInt("lf.shadows",shadowsEnabled?1:0);PlayerPrefs.SetInt("lf.shake",cameraShake?1:0);PlayerPrefs.SetInt("lf.left",leftHanded?1:0);PlayerPrefs.Save();}
  private void OpenOptions(){ClearPlayerGuard();bindingSlot=-1;optionsOpen=true;paused=true;pendingAttack=-1;stickInput=Vector2.zero;stickCrouch=false;stickFinger=-1;strikeGesture.Cancel();accumulator=0;}
  private void OptionsGUI(){SelectionResources();Card(new Rect(0,0,960,540),new Color(0,0,0,.8f));Card(new Rect(190,35,580,470),selectionPanel);Card(new Rect(208,96,544,96),new Color(.13f,.135f,.20f));Card(new Rect(208,198,544,109),new Color(.13f,.135f,.20f));Card(new Rect(208,310,544,116),new Color(.13f,.135f,.20f));Text(new Rect(220,48,390,42),bindingSlot>=0?T("Удар для кнопки ","Button strike ")+(bindingSlot+1):T("Настройки","Settings"),28);
   DrawFightDiagnosticsStatus();
   if(bindingSlot>=0){BindingOptionsGUI();return;}
   if(Pill(new Rect(420,50,226,34),T("Диагностика: ","Diagnostics: ")+(FightDiagnosticsEnabled?"ON":"OFF"),FightDiagnosticsEnabled)){SetFightDiagnostics(!FightDiagnosticsEnabled);PlayerPrefs.SetInt("lf.diagnostics",FightDiagnosticsEnabled?1:0);PlayerPrefs.Save();}
   if(Pill(new Rect(656,50,85,34),T("Закрыть","Close"))){optionsOpen=false;paused=false;ApplyOptions();}
   Text(new Rect(220,112,190,30),T("Графика","Graphics"));string[] labels={T("Экономия","Battery"),T("Высокая","High"),T("Максимум","Maximum")};for(int i=0;i<3;i++)if(Pill(new Rect(420+i*106,110,100,34),labels[i],graphicsQuality==i)){graphicsQuality=i;ApplyOptions();}
   Text(new Rect(220,158,190,30),T("Частота кадров","Frame rate"));if(Pill(new Rect(420,156,100,34),"30 FPS",frameRate==30)){frameRate=30;ApplyOptions();}if(Pill(new Rect(528,156,100,34),"60 FPS",frameRate==60)){frameRate=60;ApplyOptions();}if(Pill(new Rect(636,156,100,34),T("Тени ","Shadows ")+(shadowsEnabled?"ON":"OFF"),shadowsEnabled)){shadowsEnabled=!shadowsEnabled;ApplyOptions();}
   if(Pill(new Rect(220,208,246,38),T("Звук: ","Sound: ")+(soundEnabled?"ON":"OFF"),soundEnabled)){soundEnabled=!soundEnabled;ApplyOptions();}
   if(Pill(new Rect(482,208,246,38),T("Тряска камеры: ","Camera shake: ")+(cameraShake?"ON":"OFF"),cameraShake)){cameraShake=!cameraShake;ApplyOptions();}
   if(Pill(new Rect(220,260,246,38),T("Свайпы: ","Swipes: ")+(gesturesEnabled?"ON":"OFF"),gesturesEnabled))gesturesEnabled=!gesturesEnabled;
   if(Pill(new Rect(482,260,246,38),T("Левая рука: ","Left handed: ")+(leftHanded?"ON":"OFF"),leftHanded)){leftHanded=!leftHanded;ApplyOptions();}
   Text(new Rect(220,317,160,26),T("Размер кнопок","Control size"));controlsScale=GUI.HorizontalSlider(new Rect(420,327,300,18),controlsScale,.8f,1.25f);
   Text(new Rect(220,352,160,26),T("Прозрачность","Opacity"));controlsOpacity=GUI.HorizontalSlider(new Rect(420,362,300,18),controlsOpacity,.3f,1);
   for(int i=0;i<4;i++)if(Pill(new Rect(220+i*130,391,122,34),(i+1)+" · "+T(MoveNames[attackBindings[i]],LighFight.Core.Moves.All[attackBindings[i]].Id))){bindingSlot=i;strikeGesture.Cancel();}
   if(!selecting){if(Pill(new Rect(220,445,150,42),T("Повторить бой","Rematch"))){optionsOpen=false;Reset();}if(Pill(new Rect(380,445,160,42),T("Выбор бойца","Fighter selection"))){optionsOpen=false;paused=false;selecting=true;rosterOpen=true;advancedMoves=false;}}
   if(Pill(new Rect(550,445,178,42),T("В Game Hub","Game Hub")))bridge?.Exit("");
  }
  private void BindingOptionsGUI(){
   if(Pill(new Rect(656,50,85,34),T("Назад","Back"))){bindingSlot=-1;ApplyOptions();return;}
   for(int i=0;i<LighFight.Core.Moves.All.Length;i++)if(Pill(new Rect(220+i%5*104,110+i/5*58,98,48),T(MoveNames[i],LighFight.Core.Moves.All[i].Id),attackBindings[bindingSlot]==i)){attackBindings.Assign(bindingSlot,i);ApplyOptions();}
   if(Pill(new Rect(220,445,246,38),T("Сбросить назначения","Reset bindings"))){attackBindings.Reset();ApplyOptions();}
  }
 }
}
