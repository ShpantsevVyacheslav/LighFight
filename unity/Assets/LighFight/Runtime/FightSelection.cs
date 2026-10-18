using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime
{
 public sealed partial class Prototype
 {
  private int targetWins=2,roundSeconds=60,difficulty=1,previewMove=-1;
  private int selectedOpponent=-1;
  private bool opponentOpen;
  private bool rosterOpen=true;
  private int rosterPage,opponentPage;
  public int RosterCount=>content!=null&&content.FighterCount>0?content.FighterCount:FighterNames.Length;
  public int RosterPageCount=>(RosterCount+7)/8;
  public int RosterPageStart(int page)=>WrapRosterIndex(page,RosterPageCount)*8;
  public int RosterPageEnd(int page)=>Mathf.Min(RosterCount,RosterPageStart(page)+8);
  private static int WrapRosterIndex(int index,int count)=>((index%count)+count)%count;
  private GameContent.FighterMetadata Metadata(int index)=>content!=null&&content.fighterMetadata!=null&&index>=0&&index<content.fighterMetadata.Length?content.fighterMetadata[index]:default;
  public string FighterId(int index){var id=Metadata(index).id;return string.IsNullOrEmpty(id)?"fighter-"+index:id;}
  public string FighterLabel(int index){
   var data=Metadata(index);string name=russian?data.nameRu:data.nameEn;
   if(string.IsNullOrEmpty(name))name=russian?data.nameEn:data.nameRu;
   if(!string.IsNullOrEmpty(name))return name;
   return index>=0&&index<FighterNames.Length?T(FighterNames[index],FighterEnglish[index]):T("Боец ","Fighter ")+(index+1);
  }
  public Texture2D FighterPortrait(int index){
   var image=Metadata(index).portrait;if(image)return image;
   if(index<0||index>=RosterCount)return null;
   if(portraits==null||portraits.Length!=RosterCount){portraits=new Texture2D[RosterCount];for(int i=0;i<portraits.Length;i++)portraits[i]=Resources.Load<Texture2D>("Selection/fighter-"+i);}
   return portraits[index];
  }
  private Camera selectionDisplayCamera;
  public void SelectRosterFighter(int index){selectedFighter=WrapRosterIndex(index,RosterCount);rosterPage=selectedFighter/8;rosterOpen=false;advancedMoves=false;previewFighter=new Fighter{Guard=false};previewMove=-1;previewFrame=0;previewClock=0;previewYaw=15;RebuildViews();}
  private bool rulesOpen,arenaOpen;
  public void OpenRules(){arenaOpen=false;rulesOpen=true;dragging=false;}
  public void OpenArenas(){rulesOpen=false;arenaOpen=true;dragging=false;}
  private float previewYaw=15,previewDistance=2.65f,previewClock,previewFrame;
  private Vector2 dragPrevious;
  private bool dragging;
  private Fighter previewFighter=new Fighter{Guard=false};
  private GUIStyle selectionText;
  private readonly Color selectionAccent=new Color(.68f,.70f,1),selectionPanel=new Color(.09f,.10f,.15f,.96f);
  private Rect PreviewArea=>new Rect(245,54,430,378);
  private RenderTexture selectionRender;
  private bool selectionRenderFallback;
  public static Vector2Int SelectionPreviewSize(Vector2 safePixels,int quality)
  {
   float scale=FightGraphicsQuality.RenderScale(quality);
   var size=new Vector2(430*Mathf.Max(1,safePixels.x)/960,378*Mathf.Max(1,safePixels.y)/540)*scale;
   size*=Mathf.Min(1,2048/Mathf.Max(size.x,size.y));
   return new Vector2Int(Mathf.Max(1,Mathf.RoundToInt(size.x)),Mathf.Max(1,Mathf.RoundToInt(size.y)));
  }
  private Texture2D selectionBackdrop,selectionHalo;
  private Texture2D[] arenaImages,portraits;
  private void ReleaseSelectionRender(){if(selectionLighting)selectionLighting.SetPreviewEnabled(false);if(selectionDisplayCamera!=null)selectionDisplayCamera.enabled=false;if(fightCamera!=null){fightCamera.targetTexture=null;fightCamera.clearFlags=CameraClearFlags.Skybox;}if(selectionRender!=null){selectionRender.Release();Destroy(selectionRender);selectionRender=null;}}

  private void UpdateSelection()
  {
   if(ActorsLoading)return;
   opponent.gameObject.SetActive(false);arena.gameObject.SetActive(false);
   if(selectionDisplayCamera==null){selectionDisplayCamera=new GameObject("Selection display").AddComponent<Camera>();selectionDisplayCamera.transform.SetParent(transform,false);selectionDisplayCamera.cullingMask=0;selectionDisplayCamera.clearFlags=CameraClearFlags.SolidColor;selectionDisplayCamera.backgroundColor=new Color(.03f,.035f,.055f);selectionDisplayCamera.depth=-10;}
   // The opaque portrait grid does not display the 3D preview. Stop its entire
   // animation/late-update hierarchy and release its HDR/MSAA target until shown.
   if(rosterOpen){
    if(selectionLighting)selectionLighting.SetPreviewEnabled(false);
    if(selectionRender!=null)ReleaseSelectionRender();
    selectionDisplayCamera.enabled=true;fightCamera.enabled=false;
    player.gameObject.SetActive(false);dragging=false;return;
   }
   player.gameObject.SetActive(true);fightCamera.enabled=true;
   var previewSize=SelectionPreviewSize(UiArea.size,graphicsQuality);
   var renderFormat=!selectionRenderFallback&&QualitySettings.activeColorSpace==ColorSpace.Linear&&SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)?RenderTextureFormat.ARGBHalf:RenderTextureFormat.ARGB32;
   var descriptor=new RenderTextureDescriptor(previewSize.x,previewSize.y,renderFormat,24){msaaSamples=Mathf.Max(1,FightGraphicsQuality.Antialiasing(graphicsQuality))};
   int renderMsaa=selectionRenderFallback?1:Mathf.Max(1,SystemInfo.GetRenderTextureSupportedMSAASampleCount(descriptor));
   if(selectionRender!=null && (selectionRender.width!=previewSize.x || selectionRender.height!=previewSize.y || selectionRender.format!=renderFormat || selectionRender.antiAliasing!=renderMsaa))ReleaseSelectionRender();
   if(selectionRender==null && SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null){
    selectionRender=new RenderTexture(previewSize.x,previewSize.y,24,renderFormat){antiAliasing=renderMsaa};
    if(!selectionRender.Create()){
     Destroy(selectionRender);selectionRenderFallback=true;
     selectionRender=new RenderTexture(previewSize.x,previewSize.y,24,RenderTextureFormat.ARGB32){antiAliasing=1};
     if(!selectionRender.Create()){Destroy(selectionRender);selectionRender=null;}
    }
   }
   selectionDisplayCamera.enabled=selecting;
   fightCamera.targetTexture=selectionRender;fightCamera.rect=new Rect(0,0,1,1);
   fightCamera.clearFlags=CameraClearFlags.SolidColor;fightCamera.backgroundColor=Color.clear;
   if(selectionLighting)selectionLighting.SetPreviewEnabled(true);
   var center=new Vector3(match.A.X,1,0);
   fightCamera.transform.position=center+new Vector3(0,.18f,-previewDistance);fightCamera.transform.LookAt(center);
   previewClock+=Time.deltaTime;
   previewFighter.X=match.A.X;previewFighter.Guard=false;
   if(previewMove>=0){if(!previewFighter.Attack.HasValue)previewFighter.Start(previewMove);float clipSeconds=player.PreviewDuration(previewFighter.Attack.Value.Id);previewFrame+=Time.deltaTime*(clipSeconds>0?previewFighter.Attack.Value.Duration/clipSeconds:60);previewFighter.Frame=Mathf.Min((int)previewFrame,previewFighter.Attack.Value.Duration);if(previewFrame>previewFighter.Attack.Value.Duration+1){previewFrame=0;previewFighter.Attack=null;previewMove=-1;}}
   else {previewFighter.Attack=null;previewFrame=0;}
   player.FullPreviewMotion=true;player.Pose(previewFighter,1,Time.deltaTime);if(previewMove==-2)player.PreviewNativeMotion("Dance",previewClock);player.transform.rotation=Quaternion.Euler(0,(player.UsesNativeMotion?180:90)+previewYaw,0);
   if(previewMove==-1)player.transform.position+=Vector3.up*Mathf.Sin(previewClock*2)*.004f;
   var renderers=player.GetComponentsInChildren<Renderer>();
   if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
    float vertical=Mathf.Tan(fightCamera.fieldOfView*Mathf.Deg2Rad*.5f);
    float fit=Mathf.Max(bounds.extents.y/vertical,bounds.extents.x/(vertical*previewSize.x/previewSize.y))+bounds.extents.z;
    float distance=Mathf.Max(.3f,fit*1.15f*(previewDistance/2.65f));
    fightCamera.transform.position=bounds.center+Vector3.back*distance;fightCamera.transform.LookAt(bounds.center);
   }

   if(rosterOpen||rulesOpen||arenaOpen||opponentOpen||optionsOpen){dragging=false;return;}
   UpdateStrikeGestures();
   if(Input.touchCount>=2)
   {
    var a=Input.GetTouch(0);var b=Input.GetTouch(1);
    if(PreviewArea.Contains(Position(a.position))&&PreviewArea.Contains(Position(b.position)))
    {previewYaw+=(a.deltaPosition.x+b.deltaPosition.x)*.2f;float old=Vector2.Distance(a.position-a.deltaPosition,b.position-b.deltaPosition);float current=Vector2.Distance(a.position,b.position);previewDistance=Mathf.Clamp(previewDistance+(old-current)*.006f,1.7f,4.5f);}dragging=false;
   }
   else if(Input.touchCount==1)
   {
    var t=Input.GetTouch(0);if(t.phase==TouchPhase.Began){dragging=PreviewArea.Contains(Position(t.position));dragPrevious=t.position;}

    if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)dragging=false;
   }
   else
   {
    if(Input.GetMouseButtonDown(1)){dragging=PreviewArea.Contains(Position(Input.mousePosition));dragPrevious=Input.mousePosition;}
    if(dragging&&Input.GetMouseButton(1)){Vector2 current=Input.mousePosition;previewYaw+=(current.x-dragPrevious.x)*.4f;dragPrevious=current;}
    if(Input.GetMouseButtonUp(1))dragging=false;
    if(PreviewArea.Contains(Position(Input.mousePosition)))previewDistance=Mathf.Clamp(previewDistance-Input.mouseScrollDelta.y*.15f,1.7f,4.5f);
   }
  }
  private void SelectionResources()
  {
   if(selectionText==null)selectionText=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NotoSans-Regular"),fontSize=16,alignment=TextAnchor.MiddleLeft,wordWrap=true};
   if(selectionBackdrop!=null)return;
   arenaImages=new Texture2D[5];
   for(int i=0;i<5;i++)arenaImages[i]=Resources.Load<Texture2D>("Selection/arena-"+i);
   selectionBackdrop=new Texture2D(256,144,TextureFormat.RGBA32,false);
   for(int y=0;y<144;y++)for(int x=0;x<256;x++){float d=Vector2.Distance(new Vector2(x/256f,y/144f),new Vector2(.48f,.45f));selectionBackdrop.SetPixel(x,y,Color.Lerp(new Color(.17f,.16f,.22f),new Color(.025f,.028f,.045f),Mathf.Clamp01(d*2)));}selectionBackdrop.Apply();
   selectionHalo=new Texture2D(512,512,TextureFormat.RGBA32,false);
   for(int y=0;y<512;y++)for(int x=0;x<512;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(256,256));float ring=Mathf.Clamp01(1-Mathf.Abs(r-232)/2);float inner=Mathf.Clamp01(1-Mathf.Abs(r-217)/1.2f)*.3f;float glow=Mathf.Exp(-Mathf.Abs(r-232)/14)*.14f;selectionHalo.SetPixel(x,y,new Color(1,.48f,.16f,Mathf.Max(ring*.45f,inner)+glow));}selectionHalo.Apply();
  }
  private static Color InterfaceColor(Color authored)=>QualitySettings.activeColorSpace==ColorSpace.Linear?authored.linear:authored;
  private void Card(Rect rect,Color color){DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,InterfaceColor(color),0,Mathf.Min(14,rect.height*.5f));}
  private void Text(Rect rect,string text,int size=16,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft)
  {selectionText.fontStyle=size>=25?FontStyle.BoldAndItalic:FontStyle.Normal;selectionText.fontSize=size;selectionText.alignment=align;selectionText.normal.textColor=InterfaceColor(color??Color.white);GUI.Label(rect,text,selectionText);}
  private static int FitSelectionTitleFontSize(GUIStyle style,string text,Rect bounds,int maximumSize)
  {
   int previous=style.fontSize;var content=new GUIContent(text);var words=text.Split(' ');
   try{
    for(int size=maximumSize;size>1;size--){
     style.fontSize=size;if(style.CalcHeight(content,bounds.width)>bounds.height)continue;
     bool fits=true;foreach(string word in words)if(style.CalcSize(new GUIContent(word)).x>bounds.width){fits=false;break;}
     if(fits)return size;
    }
    return 1;
   }finally{style.fontSize=previous;}
  }
  private void FighterTitle(Rect rect,string text)
  {
   selectionText.fontStyle=FontStyle.BoldAndItalic;
   selectionText.fontSize=FitSelectionTitleFontSize(selectionText,text,rect,30);
   selectionText.alignment=TextAnchor.MiddleLeft;selectionText.normal.textColor=InterfaceColor(Color.white);
   GUI.Label(rect,text,selectionText);
  }
  private bool Pill(Rect rect,string label,bool active=false,bool primary=false)
  {
   Card(rect,primary?new Color(1,.62f,.16f):active?new Color(.25f,.25f,.42f):selectionPanel);
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
   var tint=GUI.color;GUI.color=Color.white;DrawTexture(new Rect(rect.center.x-10,rect.center.y-10,20,20),icon);GUI.color=tint;
  }
  private void SelectionGUI()
  {
   SelectionResources();
   bool previousEnabled=GUI.enabled;GUI.enabled=previousEnabled && !rulesOpen && !arenaOpen && !opponentOpen && !optionsOpen;
   DrawTexture(new Rect(0,0,960,540),selectionBackdrop);
   if(rosterOpen){RosterGUI();GUI.enabled=previousEnabled;return;}
   DrawTexture(new Rect(267,69,365,365),selectionHalo);
   var platformColor=GUI.color;GUI.color=InterfaceColor(new Color(.28f,.78f,1,.65f));DrawTexture(new Rect(275,388,350,30),selectionHalo);GUI.color=platformColor;
   if(selectionRender!=null)DrawTexture(PreviewArea,selectionRender,ScaleMode.StretchToFill,true);
   Card(new Rect(0,0,960,48),new Color(.01f,.025f,.05f,.95f));
   if(Pill(new Rect(16,7,73,34),T("‹ Назад","‹ Back")))rosterOpen=true;
   SelectionBrand();
   Text(new Rect(642,4,220,38),T("ПОДГОТОВКА БОЯ","FIGHT SETUP"),11,new Color(.60f,.70f,.82f),TextAnchor.MiddleRight);
   if(Pill(new Rect(880,7,62,34),"⚙"))OpenOptions();
   Text(new Rect(30,65,212,24),T("ВЫБЕРИ БОЙЦА","CHOOSE FIGHTER"),11,selectionAccent);
   FighterTitle(new Rect(30,96,212,92),FighterLabel(selectedFighter).ToUpperInvariant());
   int available=0;foreach(var move in Moves.All)if(!player.UsesNativeMotion||player.HasNativeMotion(move.Id))available++;
   Text(new Rect(30,190,210,32),available+T(" приёмов · серии до 4 ударов"," moves · up to 4-hit chains"),11,new Color(.60f,.70f,.82f));
   Text(new Rect(30,233,210,22),T("ЗДОРОВЬЕ","HEALTH"),11,new Color(.60f,.70f,.82f));
   Text(new Rect(30,255,210,49),"150",38,new Color(1,.89f,.54f));
   string[] labels={T("Урон ударов","Damage"),T("Дальность","Reach"),T("Серия","Chain")};string[] values={"6–25","0.6–1.2 м","4"};float[] fills={.76f,.65f,1};
   for(int i=0;i<3;i++){float y=320+i*41;Text(new Rect(30,y,130,24),labels[i],12);Text(new Rect(158,y,78,24),values[i],12,new Color(.76f,.91f,.98f),TextAnchor.MiddleRight);Card(new Rect(30,y+28,205,4),new Color(.12f,.19f,.29f));Card(new Rect(30,y+28,205*fills[i],4),selectionAccent);}
   if(Pill(new Rect(265,235,34,46),"‹"))SelectRosterFighter(selectedFighter-1);
   if(Pill(new Rect(634,235,34,46),"›"))SelectRosterFighter(selectedFighter+1);

   if(Pill(new Rect(578,442,36,36),"♫",previewMove==-2)){if(player.HasNativeMotion("Dance")){previewMove=previewMove==-2?-1:-2;previewClock=0;}else previewMessageTime=3;}
   if(previewMessageTime>0)Text(new Rect(265,382,400,30),T("Танец пока не добавлен","Dance not available yet"),12,selectionAccent,TextAnchor.MiddleCenter);
   Text(new Rect(713,100,214,24),T("АРЕНА","ARENA"),11,new Color(.60f,.70f,.82f));
   Rect arenaCard=new Rect(710,133,220,126);Card(arenaCard,selectionPanel);
   if(arenaImages[selectedArena]!=null){var old=GUI.color;GUI.color=InterfaceColor(new Color(.62f,.68f,.76f));DrawTexture(arenaCard,arenaImages[selectedArena],ScaleMode.ScaleAndCrop,true,0,Color.white,0,14);GUI.color=old;}
   Card(new Rect(710,203,220,56),new Color(.02f,.04f,.07f,.84f));
   Text(new Rect(723,204,195,28),T(ArenaNames[selectedArena],ArenaEnglish[selectedArena]).ToUpperInvariant(),15);
   Text(new Rect(723,232,195,20),T("Изменить арену  ›","Change arena  ›"),10,selectionAccent);
   if(GUI.Button(arenaCard,GUIContent.none,GUIStyle.none))OpenArenas();
   Text(new Rect(713,288,220,26),training?T("ТРЕНИРОВКА","PRACTICE"):T("ПРОТИВ БОТА","VS BOT"),13,new Color(.60f,.70f,.82f));
   string level=difficulty==0?T("Легко","Easy"):difficulty==2?T("Сложно","Hard"):T("Обычно","Normal");
   Text(new Rect(713,318,220,28),level+$" · {targetWins} "+T("победы","wins")+$" · {roundSeconds} "+T("сек.","sec"),11);
   if(Pill(new Rect(710,350,220,32),T("Изменить правила  ›","Change rules  ›")))OpenRules();
   if(Pill(new Rect(710,416,220,58),T("В БОЙ  →","FIGHT  →"),false,true)){opponentOpen=true;opponentPage=OpponentIndex/8;arenaOpen=rulesOpen=false;}
   GUI.enabled=previousEnabled;
   if(arenaOpen||rulesOpen)RulesSheet();
   if(opponentOpen)OpponentSheet();
  }
  private Texture2D brandMark;
  private void SelectionBrand(){
   if(brandMark==null){brandMark=new Texture2D(96,64,TextureFormat.RGBA32,false);brandMark.hideFlags=HideFlags.HideAndDontSave;
    for(int y=0;y<64;y++)for(int x=0;x<96;x++){
     float u=x-y*.38f;bool left=u>8&&u<22&&y>8&&y<55;bool foot=u>=22&&u<42&&y>8&&y<20;
     bool stem=u>48&&u<61&&y>8&&y<55;bool top=u>=61&&u<84&&y>43&&y<55;bool mid=u>=61&&u<77&&y>28&&y<39;
     brandMark.SetPixel(x,y,left||foot?Color.white:stem||top||mid?new Color(1,.61f,.22f):Color.clear);
    }brandMark.Apply();}
   DrawTexture(new Rect(104,5,53,37),brandMark);
   Text(new Rect(169,3,230,40),"LIGHFIGHT",23,Color.white);
  }
  private void RosterGUI(){
   Card(new Rect(0,0,960,48),new Color(.015f,.018f,.03f,.95f));SelectionBrand();
   if(Pill(new Rect(16,7,73,34),T("‹ Назад","‹ Back")))bridge?.Exit("");
   Text(new Rect(380,4,220,38),T("БОЙЦЫ","FIGHTERS"),14,selectionAccent,TextAnchor.MiddleCenter);
   if(Pill(new Rect(880,7,62,34),"⚙"))OpenOptions();
   rosterPage=WrapRosterIndex(rosterPage,RosterPageCount);
   for(int i=RosterPageStart(rosterPage);i<RosterPageEnd(rosterPage);i++){
    int cell=i%8;Rect r=new Rect(76+cell%4*208,77+cell/4*219,184,201);
    Card(r,new Color(.11f,.115f,.16f));
    var portrait=FighterPortrait(i);if(portrait)DrawTexture(new Rect(r.x+12,r.y+10,160,143),portrait,ScaleMode.ScaleAndCrop,true,0,Color.white,0,12);
    Text(new Rect(r.x+12,r.y+151,160,37),FighterLabel(i),14,null,TextAnchor.MiddleCenter);
    if(GUI.Button(r,GUIContent.none,GUIStyle.none))SelectRosterFighter(i);
   }
   RosterPager(ref rosterPage,513);
  }
  private void RosterPager(ref int page,float y){
   if(RosterPageCount<=1)return;
   if(Pill(new Rect(375,y,45,25),"‹"))page=WrapRosterIndex(page-1,RosterPageCount);
   Text(new Rect(425,y,110,25),(page+1)+" / "+RosterPageCount,12,selectionAccent,TextAnchor.MiddleCenter);
   if(Pill(new Rect(540,y,45,25),"›"))page=WrapRosterIndex(page+1,RosterPageCount);
  }
  private void OpponentSheet(){Card(new Rect(0,48,960,492),new Color(0,0,0,.85f));Text(new Rect(90,75,650,45),T("Выбери соперника","Choose opponent"),28);
   if(Pill(new Rect(790,75,80,34),T("Назад","Back")))opponentOpen=false;
   opponentPage=WrapRosterIndex(opponentPage,RosterPageCount);
   for(int i=RosterPageStart(opponentPage);i<RosterPageEnd(opponentPage);i++){int cell=i%8;var rect=new Rect(90+cell%4*198,140+cell/4*158,180,145);Card(rect,selectionPanel);var portrait=FighterPortrait(i);if(portrait)DrawTexture(new Rect(rect.x+42,rect.y+5,96,96),portrait,ScaleMode.ScaleAndCrop);Text(new Rect(rect.x+8,rect.y+105,164,34),FighterLabel(i),12,null,TextAnchor.MiddleCenter);if(GUI.Button(rect,GUIContent.none,GUIStyle.none)){selectedOpponent=i;opponentOpen=false;RebuildViews();BeginFight();}}
   RosterPager(ref opponentPage,480);
  }
  private float previewMessageTime;
  private void RulesSheet()
  {
   Card(new Rect(0,48,960,492),new Color(0,0,0,.72f));Card(new Rect(550,48,410,492),selectionPanel);
   if(Pill(new Rect(889,64,48,32),T("Назад","Back"))){arenaOpen=rulesOpen=false;return;}
   Text(new Rect(578,73,300,45),arenaOpen?T("Место встречи","Choose arena"):T("Твои правила","Your rules"),27);
   if(arenaOpen){for(int i=0;i<ArenaNames.Length;i++){
    var rect=new Rect(578,135+i*73,350,62);Card(rect,i==selectedArena?new Color(.08f,.27f,.36f):new Color(.04f,.09f,.16f));
    if(arenaImages[i]!=null)DrawTexture(new Rect(rect.x+5,rect.y+5,100,52),arenaImages[i],ScaleMode.ScaleAndCrop);
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
