using UnityEngine;
namespace LighFight.Runtime {
 public sealed partial class Prototype {
  private bool optionsOpen;
  private int graphicsQuality=2,frameRate=60;
  private bool soundEnabled=true,cameraShake=true,leftHanded;
  private float controlsScale=1,controlsOpacity=.75f;
  private Rect ControlRect(Rect rect){var center=rect.center;rect.width*=controlsScale;rect.height*=controlsScale;rect.center=center;if(leftHanded)rect.x=960-rect.xMax;return rect;}
  private Rect CrouchArea=>ControlRect(new Rect(244,430,78,78));
  private void LoadOptions(){controlsScale=PlayerPrefs.GetFloat("lf.controlScale",1);controlsOpacity=PlayerPrefs.GetFloat("lf.controlOpacity",.75f);gesturesEnabled=PlayerPrefs.GetInt("lf.swipes",1)==1;graphicsQuality=PlayerPrefs.GetInt("lf.graphics",2);frameRate=PlayerPrefs.GetInt("lf.fps",60);soundEnabled=PlayerPrefs.GetInt("lf.sound",1)==1;cameraShake=PlayerPrefs.GetInt("lf.shake",1)==1;leftHanded=PlayerPrefs.GetInt("lf.left",0)==1;ApplyOptions();}
  private void ApplyOptions(){QualitySettings.globalTextureMipmapLimit=0;QualitySettings.skinWeights=SkinWeights.Unlimited;QualitySettings.lodBias=graphicsQuality==2?4:2;QualitySettings.antiAliasing=graphicsQuality==2?4:2;QualitySettings.shadows=graphicsQuality==0?ShadowQuality.Disable:ShadowQuality.All;QualitySettings.shadowResolution=graphicsQuality==2?ShadowResolution.VeryHigh:ShadowResolution.Medium;Application.targetFrameRate=frameRate;var audio=GetComponentsInChildren<AudioSource>();foreach(var source in audio)source.mute=!soundEnabled;
   PlayerPrefs.SetFloat("lf.controlScale",controlsScale);PlayerPrefs.SetFloat("lf.controlOpacity",controlsOpacity);PlayerPrefs.SetInt("lf.swipes",gesturesEnabled?1:0);PlayerPrefs.SetInt("lf.graphics",graphicsQuality);PlayerPrefs.SetInt("lf.fps",frameRate);PlayerPrefs.SetInt("lf.sound",soundEnabled?1:0);PlayerPrefs.SetInt("lf.shake",cameraShake?1:0);PlayerPrefs.SetInt("lf.left",leftHanded?1:0);PlayerPrefs.Save();}
  private void OpenOptions(){optionsOpen=true;paused=true;pendingAttack=-1;stickInput=Vector2.zero;stickFinger=-1;strikeGesture.Cancel();accumulator=0;}
  private void OptionsGUI(){SelectionResources();Card(new Rect(0,0,960,540),new Color(0,0,0,.8f));Card(new Rect(190,35,580,470),selectionPanel);Text(new Rect(220,48,390,42),T("Настройки","Settings"),28);
   if(Pill(new Rect(656,50,85,34),T("Закрыть","Close"))){optionsOpen=false;paused=false;ApplyOptions();}
   Text(new Rect(220,112,190,30),T("Графика","Graphics"));string[] labels={T("Экономия","Battery"),T("Высокая","High"),T("Максимум","Maximum")};for(int i=0;i<3;i++)if(Pill(new Rect(420+i*106,110,100,34),labels[i],graphicsQuality==i)){graphicsQuality=i;ApplyOptions();}
   Text(new Rect(220,158,190,30),T("Частота кадров","Frame rate"));if(Pill(new Rect(420,156,150,34),"30 FPS",frameRate==30)){frameRate=30;ApplyOptions();}if(Pill(new Rect(578,156,150,34),"60 FPS",frameRate==60)){frameRate=60;ApplyOptions();}
   if(Pill(new Rect(220,208,246,38),T("Звук: ","Sound: ")+(soundEnabled?"ON":"OFF"),soundEnabled)){soundEnabled=!soundEnabled;ApplyOptions();}
   if(Pill(new Rect(482,208,246,38),T("Тряска камеры: ","Camera shake: ")+(cameraShake?"ON":"OFF"),cameraShake)){cameraShake=!cameraShake;ApplyOptions();}
   if(Pill(new Rect(220,260,246,38),T("Свайпы: ","Swipes: ")+(gesturesEnabled?"ON":"OFF"),gesturesEnabled))gesturesEnabled=!gesturesEnabled;
   if(Pill(new Rect(482,260,246,38),T("Левая рука: ","Left handed: ")+(leftHanded?"ON":"OFF"),leftHanded)){leftHanded=!leftHanded;ApplyOptions();}
   Text(new Rect(220,317,160,26),T("Размер кнопок","Control size"));controlsScale=GUI.HorizontalSlider(new Rect(420,327,300,18),controlsScale,.8f,1.25f);
   Text(new Rect(220,352,160,26),T("Прозрачность","Opacity"));controlsOpacity=GUI.HorizontalSlider(new Rect(420,362,300,18),controlsOpacity,.3f,1);
   if(!selecting){if(Pill(new Rect(220,405,150,42),T("Повторить бой","Rematch"))){optionsOpen=false;Reset();}if(Pill(new Rect(380,405,160,42),T("Выбор бойца","Fighter selection"))){optionsOpen=false;paused=false;selecting=true;}}
   if(Pill(new Rect(550,405,178,42),T("В Game Hub","Game Hub")))bridge?.Exit("");
  }
 }
}
