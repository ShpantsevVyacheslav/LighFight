using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime
{
 public sealed partial class Prototype
 {
  private int targetWins=2,roundSeconds=60,difficulty=1,previewMove=-1;
  private int selectedOpponent=-1;
  private bool opponentOpen;
  private bool rulesOpen,arenaOpen;
  public void OpenRules(){arenaOpen=false;rulesOpen=true;dragging=false;}
  public void OpenArenas(){rulesOpen=false;arenaOpen=true;dragging=false;}
  private float previewYaw=15,previewDistance=2.65f,previewClock,previewFrame;
  private Vector2 dragPrevious;
  private bool dragging;
  private Fighter previewFighter=new Fighter{Guard=false};
  private GUIStyle selectionText;
  private readonly Color selectionAccent=new Color(.28f,.82f,1),selectionPanel=new Color(.045f,.085f,.15f,.96f);
  private Rect PreviewArea=>new Rect(245,54,430,378);
  private RenderTexture selectionRender;
  public static Vector2Int SelectionPreviewSize(Vector2 safePixels,int quality)
  {
   float scale=quality==0?.65f:quality==1?.85f:1f;
   var size=new Vector2(430*Mathf.Max(1,safePixels.x)/960,378*Mathf.Max(1,safePixels.y)/540)*scale;
   size*=Mathf.Min(1,2048/Mathf.Max(size.x,size.y));
   return new Vector2Int(Mathf.Max(1,Mathf.RoundToInt(size.x)),Mathf.Max(1,Mathf.RoundToInt(size.y)));
  }
  private Texture2D selectionBackdrop,selectionHalo;
  private Texture2D[] arenaImages,portraits;
  private void ReleaseSelectionRender(){if(fightCamera!=null){fightCamera.targetTexture=null;fightCamera.clearFlags=CameraClearFlags.Skybox;}if(selectionRender!=null){selectionRender.Release();Destroy(selectionRender);selectionRender=null;}}

  private void UpdateSelection()
  {
   opponent.gameObject.SetActive(false);arena.gameObject.SetActive(false);
   var previewSize=SelectionPreviewSize(UiArea.size,graphicsQuality);
   if(selectionRender!=null && (selectionRender.width!=previewSize.x || selectionRender.height!=previewSize.y || selectionRender.antiAliasing!=(graphicsQuality==2?4:2)))ReleaseSelectionRender();
   if(selectionRender==null && SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null){selectionRender=new RenderTexture(previewSize.x,previewSize.y,24,RenderTextureFormat.ARGB32){antiAliasing=graphicsQuality==2?4:2};selectionRender.Create();}
   fightCamera.targetTexture=selectionRender;fightCamera.rect=new Rect(0,0,1,1);
   fightCamera.clearFlags=CameraClearFlags.SolidColor;fightCamera.backgroundColor=Color.clear;
   var center=new Vector3(match.A.X,1,0);
   fightCamera.transform.position=center+new Vector3(0,.18f,-previewDistance);fightCamera.transform.LookAt(center);
   previewClock+=Time.deltaTime;
   previewFighter.X=match.A.X;previewFighter.Guard=false;
   if(previewMove>=0){if(!previewFighter.Attack.HasValue)previewFighter.Start(previewMove);previewFrame+=Time.deltaTime*60;previewFighter.Frame=(int)previewFrame;if(previewFighter.Frame>=previewFighter.Attack.Value.Duration){previewFrame=0;previewFighter.Attack=null;}}
   else {previewFighter.Attack=null;previewFrame=0;}
   player.Pose(previewFighter,1,Time.deltaTime);if(previewMove==-2)player.PreviewNativeMotion("Dance",previewClock);else if(previewMove==-1&&player.HasNativeMotion("Greeting"))player.PreviewNativeMotion("Greeting",previewClock);player.transform.rotation=Quaternion.Euler(0,(player.UsesNativeMotion?180:90)+previewYaw,0);
   if(previewMove==-1)player.transform.position+=Vector3.up*Mathf.Sin(previewClock*2)*.004f;
   var renderers=player.GetComponentsInChildren<Renderer>();
   if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
    float vertical=Mathf.Tan(fightCamera.fieldOfView*Mathf.Deg2Rad*.5f);
    float fit=Mathf.Max(bounds.extents.y/vertical,bounds.extents.x/(vertical*previewSize.x/previewSize.y))+bounds.extents.z;
    float distance=Mathf.Max(.3f,fit*1.15f*(previewDistance/2.65f));
    fightCamera.transform.position=bounds.center+Vector3.back*distance;fightCamera.transform.LookAt(bounds.center);
   }

   if(rulesOpen||arenaOpen||opponentOpen||optionsOpen){dragging=false;return;}
   UpdateStrikeGestures();
   if(Input.touchCount>=2)
   {
    var a=Input.GetTouch(0);var b=Input.GetTouch(1);
    if(PreviewArea.Contains(Position(a.position))&&PreviewArea.Contains(Position(b.position)))
    {float old=Vector2.Distance(a.position-a.deltaPosition,b.position-b.deltaPosition);float current=Vector2.Distance(a.position,b.position);previewDistance=Mathf.Clamp(previewDistance+(old-current)*.006f,1.7f,4.5f);}dragging=false;
   }
   else if(Input.touchCount==1)
   {
    var t=Input.GetTouch(0);if(t.phase==TouchPhase.Began){dragging=PreviewArea.Contains(Position(t.position));dragPrevious=t.position;}
    if(dragging&&t.phase==TouchPhase.Moved){previewYaw+=(t.position.x-dragPrevious.x)*.4f;dragPrevious=t.position;}
    if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)dragging=false;
   }
   else
   {
    if(Input.GetMouseButtonDown(0)){dragging=PreviewArea.Contains(Position(Input.mousePosition));dragPrevious=Input.mousePosition;}
    if(dragging&&Input.GetMouseButton(0)){Vector2 current=Input.mousePosition;previewYaw+=(current.x-dragPrevious.x)*.4f;dragPrevious=current;}
    if(Input.GetMouseButtonUp(0))dragging=false;
    if(PreviewArea.Contains(Position(Input.mousePosition)))previewDistance=Mathf.Clamp(previewDistance-Input.mouseScrollDelta.y*.15f,1.7f,4.5f);
   }
  }
  private void SelectionResources()
  {
   if(selectionText==null)selectionText=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NotoSans-Regular"),fontSize=16,alignment=TextAnchor.MiddleLeft,wordWrap=true};
   if(selectionBackdrop!=null)return;
   arenaImages=new Texture2D[5];portraits=new Texture2D[8];
   for(int i=0;i<5;i++)arenaImages[i]=Resources.Load<Texture2D>("Selection/arena-"+i);
   for(int i=0;i<8;i++)portraits[i]=Resources.Load<Texture2D>("Selection/fighter-"+i);
   selectionBackdrop=new Texture2D(256,144,TextureFormat.RGBA32,false);
   for(int y=0;y<144;y++)for(int x=0;x<256;x++){float d=Vector2.Distance(new Vector2(x/256f,y/144f),new Vector2(.48f,.45f));selectionBackdrop.SetPixel(x,y,Color.Lerp(new Color(.14f,.20f,.32f),new Color(.025f,.045f,.085f),Mathf.Clamp01(d*2)));}selectionBackdrop.Apply();
   selectionHalo=new Texture2D(512,512,TextureFormat.RGBA32,false);
   for(int y=0;y<512;y++)for(int x=0;x<512;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(256,256));float ring=Mathf.Clamp01(1-Mathf.Abs(r-232)/2);float inner=Mathf.Clamp01(1-Mathf.Abs(r-217)/1.2f)*.3f;float glow=Mathf.Exp(-Mathf.Abs(r-232)/14)*.14f;selectionHalo.SetPixel(x,y,new Color(1,.48f,.16f,Mathf.Max(ring*.45f,inner)+glow));}selectionHalo.Apply();
  }
  private void Card(Rect rect,Color color){GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,Mathf.Min(14,rect.height*.5f));}
  private void Text(Rect rect,string text,int size=16,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft)
  {selectionText.fontStyle=size>=25?FontStyle.BoldAndItalic:FontStyle.Normal;selectionText.fontSize=size;selectionText.alignment=align;selectionText.normal.textColor=color??Color.white;GUI.Label(rect,text,selectionText);}
  private bool Pill(Rect rect,string label,bool active=false,bool primary=false)
  {
   Card(rect,primary?new Color(1,.62f,.16f):active?new Color(.07f,.26f,.36f):selectionPanel);
   if(label=="⚙"||label=="♫"||label=="☰") DrawControlIcon(rect,label);
   else Text(rect,label,15,primary?new Color(.10f,.06f,.02f):active?selectionAccent:Color.white,TextAnchor.MiddleCenter);
   return GUI.Button(rect,GUIContent.none,GUIStyle.none);
  }
  private static readonly System.Collections.Generic.Dictionary<string,Texture2D> controlIcons=new System.Collections.Generic.Dictionary<string,Texture2D>();
  private void DrawControlIcon(Rect rect,string name)
  {
   if(!controlIcons.TryGetValue(name,out var icon)){
    icon=new Texture2D(64,64,TextureFormat.RGBA32,false);icon.hideFlags=HideFlags.HideAndDontSave;
    for(int y=0;y<64;y++)for(int x=0;x<64;x++){
     float dx=x-31.5f,dy=y-31.5f,r=Mathf.Sqrt(dx*dx+dy*dy),angle=Mathf.Atan2(dy,dx);
     bool ink=name=="⚙" ? (r>12&&r<20)||(r>=20&&r<25&&Mathf.Cos(angle*8)>.4f) :
      name=="☰" ? x>=13&&x<=50&&(Mathf.Abs(y-18)<2||Mathf.Abs(y-32)<2||Mathf.Abs(y-46)<2) :
      ((x-22)*(x-22)/64f+(y-18)*(y-18)/25f<1)||((x-44)*(x-44)/64f+(y-23)*(y-23)/25f<1)||
      (x>=26&&x<=29&&y>=18&&y<=48)||(x>=48&&x<=51&&y>=23&&y<=53)||(x>=26&&x<=51&&Mathf.Abs(y-(48+(x-26)*.2f))<2);
     icon.SetPixel(x,y,ink?Color.white:Color.clear);
    }icon.Apply();controlIcons.Add(name,icon);
   }
   var tint=GUI.color;GUI.color=Color.white;GUI.DrawTexture(new Rect(rect.center.x-10,rect.center.y-10,20,20),icon);GUI.color=tint;
  }
  private void SelectionGUI()
  {
   SelectionResources();
   bool previousEnabled=GUI.enabled;GUI.enabled=previousEnabled && !rulesOpen && !arenaOpen && !opponentOpen && !optionsOpen;
   GUI.DrawTexture(new Rect(0,0,960,540),selectionBackdrop);
   GUI.DrawTexture(new Rect(267,69,365,365),selectionHalo);
   var platformColor=GUI.color;GUI.color=new Color(.28f,.78f,1,.65f);GUI.DrawTexture(new Rect(275,388,350,30),selectionHalo);GUI.color=platformColor;
   if(selectionRender!=null)GUI.DrawTexture(PreviewArea,selectionRender,ScaleMode.StretchToFill,true);
   Card(new Rect(0,0,960,48),new Color(.01f,.025f,.05f,.95f));
   if(Pill(new Rect(16,7,73,34),T("‹ Назад","‹ Back")))bridge?.Exit("");
   Text(new Rect(109,2,155,43),"LIGH",25,Color.white);Text(new Rect(168,2,140,43),"FIGHT",25,new Color(1,.68f,.28f));
   Text(new Rect(642,4,220,38),T("ПОДГОТОВКА БОЯ","FIGHT SETUP"),11,new Color(.60f,.70f,.82f),TextAnchor.MiddleRight);
   if(Pill(new Rect(880,7,62,34),"⚙"))OpenOptions();
   Text(new Rect(30,65,212,24),T("ВЫБЕРИ БОЙЦА","CHOOSE FIGHTER"),11,selectionAccent);
   Text(new Rect(30,96,212,92),T(FighterNames[selectedFighter],FighterEnglish[selectedFighter]).ToUpperInvariant(),30);
   int available=0;foreach(var move in Moves.All)if(!player.UsesNativeMotion||player.HasNativeMotion(move.Id))available++;
   Text(new Rect(30,190,210,32),available+T(" приёмов · серии до 4 ударов"," moves · up to 4-hit chains"),11,new Color(.60f,.70f,.82f));
   Text(new Rect(30,233,210,22),T("ЗДОРОВЬЕ","HEALTH"),11,new Color(.60f,.70f,.82f));
   Text(new Rect(30,255,210,49),"150",38,new Color(1,.89f,.54f));
   string[] labels={T("Урон ударов","Damage"),T("Дальность","Reach"),T("Серия","Chain")};string[] values={"6–25","0.6–1.2 м","4"};float[] fills={.76f,.65f,1};
   for(int i=0;i<3;i++){float y=320+i*41;Text(new Rect(30,y,130,24),labels[i],12);Text(new Rect(158,y,78,24),values[i],12,new Color(.76f,.91f,.98f),TextAnchor.MiddleRight);Card(new Rect(30,y+28,205,4),new Color(.12f,.19f,.29f));Card(new Rect(30,y+28,205*fills[i],4),selectionAccent);}
   Text(new Rect(279,415,360,22),T("Вращай · Приближай двумя пальцами","Rotate · Pinch to zoom"),10,new Color(.60f,.73f,.84f),TextAnchor.MiddleCenter);
   Card(new Rect(300,442,265,36),selectionPanel);Text(new Rect(300,442,265,36),T("↑  ←  Свайп: показать удар  →  ↓","↑  ←  Swipe to preview  →  ↓"),11,selectionAccent,TextAnchor.MiddleCenter);
   if(Pill(new Rect(578,442,36,36),"♫",previewMove==-2)){if(player.HasNativeMotion("Dance")){previewMove=previewMove==-2?-1:-2;previewClock=0;}else previewMessageTime=3;}
   if(previewMessageTime>0)Text(new Rect(265,382,400,30),T("Танец пока не добавлен","Dance not available yet"),12,selectionAccent,TextAnchor.MiddleCenter);
   Text(new Rect(713,100,214,24),T("АРЕНА","ARENA"),11,new Color(.60f,.70f,.82f));
   Rect arenaCard=new Rect(710,133,220,126);Card(arenaCard,selectionPanel);
   if(arenaImages[selectedArena]!=null){var old=GUI.color;GUI.color=new Color(.62f,.68f,.76f);GUI.DrawTexture(arenaCard,arenaImages[selectedArena],ScaleMode.ScaleAndCrop,true,0,Color.white,0,14);GUI.color=old;}
   Card(new Rect(710,203,220,56),new Color(.02f,.04f,.07f,.84f));
   Text(new Rect(723,204,195,28),T(ArenaNames[selectedArena],ArenaEnglish[selectedArena]).ToUpperInvariant(),15);
   Text(new Rect(723,232,195,20),T("Изменить арену  ›","Change arena  ›"),10,selectionAccent);
   if(GUI.Button(arenaCard,GUIContent.none,GUIStyle.none))OpenArenas();
   Text(new Rect(713,288,220,26),training?T("ТРЕНИРОВКА","PRACTICE"):T("ПРОТИВ БОТА","VS BOT"),13,new Color(.60f,.70f,.82f));
   string level=difficulty==0?T("Легко","Easy"):difficulty==2?T("Сложно","Hard"):T("Обычно","Normal");
   Text(new Rect(713,318,220,28),level+$" · {targetWins} "+T("победы","wins")+$" · {roundSeconds} "+T("сек.","sec"),11);
   if(Pill(new Rect(710,350,220,32),T("Изменить правила  ›","Change rules  ›")))OpenRules();
   if(Pill(new Rect(710,416,220,58),T("В БОЙ  →","FIGHT  →"),false,true)){opponentOpen=true;arenaOpen=rulesOpen=false;}
   Card(new Rect(0,488,960,52),new Color(.015f,.035f,.065f,.97f));
   string[] shortNames={"ЛЕДИ","ВЕДЬМА","МАСТЕР","ОСТРОВНОЙ","ПАНТЕРА","РЕЙНДЖЕР","ГАЛА","ЧЕМПИОН"};
   for(int i=0;i<FighterNames.Length;i++){
    var rect=new Rect(179+i*75,493,67,43);bool active=selectedFighter==i;
    Card(new Rect(rect.x-1,rect.y-1,rect.width+2,rect.height+2),active?selectionAccent:new Color(.20f,.29f,.40f));Card(rect,selectionPanel);
    if(portraits[i]!=null)GUI.DrawTexture(rect,portraits[i],ScaleMode.ScaleAndCrop,true);
    Card(new Rect(rect.x,rect.y+27,rect.width,16),new Color(.02f,.05f,.09f,.88f));Text(new Rect(rect.x,rect.y+27,rect.width,16),russian?shortNames[i]:FighterEnglish[i],8,active?selectionAccent:Color.white,TextAnchor.MiddleCenter);
    if(GUI.Button(rect,GUIContent.none,GUIStyle.none)){selectedFighter=i;previewFighter=new Fighter{Guard=false};previewMove=-1;previewYaw=15;RebuildViews();}
   }
   GUI.enabled=previousEnabled;
   if(arenaOpen||rulesOpen)RulesSheet();
   if(opponentOpen)OpponentSheet();
  }
  private void OpponentSheet(){Card(new Rect(0,48,960,492),new Color(0,0,0,.85f));Text(new Rect(90,75,650,45),T("Выбери соперника","Choose opponent"),28);
   if(Pill(new Rect(790,75,80,34),T("Назад","Back")))opponentOpen=false;
   for(int i=0;i<FighterNames.Length;i++){var rect=new Rect(90+i%4*198,140+i/4*158,180,145);Card(rect,selectionPanel);if(portraits[i])GUI.DrawTexture(new Rect(rect.x+42,rect.y+5,96,96),portraits[i]);Text(new Rect(rect.x+8,rect.y+105,164,34),T(FighterNames[i],FighterEnglish[i]),12,null,TextAnchor.MiddleCenter);if(GUI.Button(rect,GUIContent.none,GUIStyle.none)){selectedOpponent=i;opponentOpen=false;RebuildViews();BeginFight();}}
  }
  private float previewMessageTime;
  private void RulesSheet()
  {
   Card(new Rect(0,48,960,492),new Color(0,0,0,.72f));Card(new Rect(550,48,410,492),selectionPanel);
   if(Pill(new Rect(889,64,48,32),T("Назад","Back"))){arenaOpen=rulesOpen=false;return;}
   Text(new Rect(578,73,300,45),arenaOpen?T("Место встречи","Choose arena"):T("Твои правила","Your rules"),27);
   if(arenaOpen){for(int i=0;i<ArenaNames.Length;i++){
    var rect=new Rect(578,135+i*73,350,62);Card(rect,i==selectedArena?new Color(.08f,.27f,.36f):new Color(.04f,.09f,.16f));
    if(arenaImages[i]!=null)GUI.DrawTexture(new Rect(rect.x+5,rect.y+5,100,52),arenaImages[i],ScaleMode.ScaleAndCrop);
    Text(new Rect(rect.x+119,rect.y,220,62),T(ArenaNames[i],ArenaEnglish[i]),15);
    if(GUI.Button(rect,GUIContent.none,GUIStyle.none)){selectedArena=i;RebuildViews();arenaOpen=false;}
   }return;}
   Text(new Rect(578,151,100,35),T("Режим","Mode"),13);
   if(Pill(new Rect(681,151,104,35),T("Бот","Bot"),!training))training=false;
   if(Pill(new Rect(790,151,138,35),T("Тренировка","Practice"),training))training=true;
   Text(new Rect(578,216,100,35),T("Сложность","Difficulty"),13);
   string[] levels={T("Легко","Easy"),T("Обычно","Normal"),T("Сложно","Hard")};for(int i=0;i<3;i++)if(Pill(new Rect(681+i*84,216,79,35),levels[i],difficulty==i))difficulty=i;
   Text(new Rect(578,281,100,35),T("До побед","Wins"),13);for(int i=1;i<=3;i++)if(Pill(new Rect(681+(i-1)*84,281,79,35),i.ToString(),targetWins==i))targetWins=i;
   Text(new Rect(578,346,100,35),T("Раунд","Round"),13);for(int i=0;i<2;i++){int secs=i==0?60:90;if(Pill(new Rect(681+i*126,346,121,35),secs+T(" сек."," sec"),roundSeconds==secs))roundSeconds=secs;}
   if(Pill(new Rect(578,442,350,57),T("ПРИМЕНИТЬ","APPLY"),false,true))rulesOpen=false;
  }
 }
}
