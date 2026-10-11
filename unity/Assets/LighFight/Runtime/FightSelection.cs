using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime
{
 public sealed partial class Prototype
 {
  private int targetWins=2,roundSeconds=60,difficulty=1,previewMove=-1;
  private bool rulesOpen,arenaOpen;
  private float previewYaw=15,previewDistance=3.2f,previewClock,previewFrame;
  private Vector2 dragPrevious;
  private bool dragging;
  private Fighter previewFighter=new Fighter{Guard=true};
  private Texture2D rounded;
  private GUIStyle selectionText;
  private readonly Color selectionAccent=new Color(.28f,.82f,1),selectionPanel=new Color(.045f,.085f,.15f,.96f);
  private Rect PreviewArea=>new Rect(250,65,430,365);
  private void UpdateSelection()
  {
   opponent.gameObject.SetActive(false);arena.gameObject.SetActive(false);
   fightCamera.rect=new Rect(.25f,.15f,.47f,.75f);
   fightCamera.backgroundColor=new Color(.035f,.055f,.10f);
   var center=new Vector3(match.A.X,1,0);
   fightCamera.transform.position=center+new Vector3(0,.18f,-previewDistance);fightCamera.transform.LookAt(center);
   previewClock+=Time.deltaTime;
   previewFighter.X=match.A.X;previewFighter.Guard=previewMove<0;
   if(previewMove>=0){if(!previewFighter.Attack.HasValue)previewFighter.Start(previewMove);previewFrame+=Time.deltaTime*60;previewFighter.Frame=(int)previewFrame;if(previewFighter.Frame>=previewFighter.Attack.Value.Duration){previewFrame=0;previewFighter.Attack=null;}}
   else {previewFighter.Attack=null;previewFrame=0;}
   player.Pose(previewFighter,1,Time.deltaTime);player.transform.rotation=Quaternion.Euler(0,90+previewYaw,0);
   if(previewMove<0)player.transform.position+=Vector3.up*Mathf.Sin(previewClock*2)*.004f;
   if(rulesOpen||arenaOpen){dragging=false;return;}
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
   if(selectionText==null)selectionText=new GUIStyle(GUI.skin.label){fontSize=16,alignment=TextAnchor.MiddleLeft,wordWrap=true};
   if(rounded!=null)return;rounded=new Texture2D(64,64,TextureFormat.RGBA32,false);rounded.hideFlags=HideFlags.HideAndDontSave;
   for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(12-x,x-51),dy=Mathf.Max(12-y,y-51);float d=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;rounded.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(12.5f-d)));}rounded.Apply();
  }
  private void Card(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,rounded,ScaleMode.StretchToFill);GUI.color=old;}
  private void Text(Rect rect,string text,int size=16,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft)
  {selectionText.fontSize=size;selectionText.alignment=align;selectionText.normal.textColor=color??Color.white;GUI.Label(rect,text,selectionText);}
  private bool Pill(Rect rect,string label,bool active=false,bool primary=false)
  {
   Card(rect,primary?new Color(1,.62f,.16f):active?new Color(.07f,.26f,.36f):selectionPanel);
   Text(rect,label,15,primary?new Color(.10f,.06f,.02f):active?selectionAccent:Color.white,TextAnchor.MiddleCenter);
   return GUI.Button(rect,GUIContent.none,GUIStyle.none);
  }
  private void SelectionGUI()
  {
   SelectionResources();
   Card(new Rect(0,0,960,52),new Color(.015f,.03f,.055f,.98f));
   if(Pill(new Rect(16,8,80,36),"‹ Назад"))bridge?.Exit("");
   Text(new Rect(115,4,200,45),"LighFight",27,selectionAccent);
   Text(new Rect(605,5,230,40),T("ПОДГОТОВКА БОЯ","FIGHT SETUP"),13,new Color(.65f,.76f,.87f));
   if(Pill(new Rect(860,8,84,36),"⚙"))rulesOpen=true;
   Text(new Rect(28,66,225,26),T("ВЫБЕРИ БОЙЦА","CHOOSE FIGHTER"),12,selectionAccent);
   Text(new Rect(28,100,225,80),T(FighterNames[selectedFighter],FighterEnglish[selectedFighter]),29);
   Text(new Rect(28,198,225,45),T("Боевые параметры","Combat stats"),14,new Color(.65f,.76f,.87f));
   // These values describe shared combat mechanics, not invented progression ratings.
   string[] labels={T("Здоровье","Health"),T("Урон ударов","Damage"),T("Дальность","Reach")};string[] values={"150","6–25","1.1–2.35 м"};
   for(int i=0;i<3;i++){Text(new Rect(28,250+i*44,140,30),labels[i],14);Text(new Rect(165,250+i*44,90,30),values[i],17,selectionAccent);}
   Text(new Rect(290,393,360,25),T("Вращай пальцем · Приближай двумя пальцами","Drag to rotate · Pinch to zoom"),11,new Color(.65f,.76f,.87f),TextAnchor.MiddleCenter);
   if(Pill(new Rect(293,428,98,35),T("Стойка","Guard"),previewMove<0))previewMove=-1;
   if(Pill(new Rect(400,428,98,35),T("Удары","Attacks"),previewMove>=0)){previewMove=previewMove==0?1:previewMove==1?4:0;previewFrame=0;previewFighter.Attack=null;}
   if(Pill(new Rect(508,428,98,35),T("Танец","Dance")))previewMessageTime=3;
   if(previewMessageTime>0)Text(new Rect(265,355,400,35),T("Танец пока не добавлен","Dance not available yet"),13,selectionAccent,TextAnchor.MiddleCenter);
   Text(new Rect(713,70,200,30),T("АРЕНА","ARENA"),12,selectionAccent);
   if(Pill(new Rect(710,108,220,75),T(ArenaNames[selectedArena],ArenaEnglish[selectedArena])+"  ›"))arenaOpen=true;
   Text(new Rect(713,220,220,26),training?T("ТРЕНИРОВКА","PRACTICE"):T("ПРОТИВ БОТА","VS BOT"),14);
   Text(new Rect(713,256,220,45),$"{targetWins} "+T("победы","wins")+$" · {roundSeconds} "+T("сек.","sec"),13,new Color(.65f,.76f,.87f));
   if(Pill(new Rect(710,309,220,36),T("Правила боя  ›","Fight rules  ›")))rulesOpen=true;
   if(Pill(new Rect(710,390,220,65),T("В БОЙ  →","FIGHT  →"),false,true))BeginFight();
   Card(new Rect(0,480,960,60),selectionPanel);
   for(int i=0;i<FighterNames.Length;i++)if(Pill(new Rect(12+i*117,491,110,38),T(FighterNames[i],FighterEnglish[i]),selectedFighter==i)){selectedFighter=i;previewFighter=new Fighter{Guard=true};previewMove=-1;previewYaw=15;RebuildViews();}
   if(arenaOpen||rulesOpen)RulesSheet();
  }
  private float previewMessageTime;
  private void RulesSheet()
  {
   Card(new Rect(0,52,960,488),new Color(.015f,.025f,.05f,.94f));Card(new Rect(250,75,460,385),selectionPanel);
   if(Pill(new Rect(646,87,45,35),"✕")){arenaOpen=rulesOpen=false;return;}
   Text(new Rect(276,88,340,45),arenaOpen?T("Выбери арену","Choose arena"):T("Настройки боя","Fight settings"),25);
   if(arenaOpen){for(int i=0;i<ArenaNames.Length;i++)if(Pill(new Rect(276,143+i*54,408,44),T(ArenaNames[i],ArenaEnglish[i]),i==selectedArena)){selectedArena=i;RebuildViews();arenaOpen=false;}return;}
   Text(new Rect(276,143,90,35),T("Режим","Mode"),14);
   if(Pill(new Rect(378,143,143,35),T("Бот","Bot"),!training))training=false;
   if(Pill(new Rect(531,143,153,35),T("Тренировка","Practice"),training))training=true;
   Text(new Rect(276,196,100,35),T("Сложность","Difficulty"),14);
   string[] levels={T("Легко","Easy"),T("Обычно","Normal"),T("Сложно","Hard")};for(int i=0;i<3;i++)if(Pill(new Rect(378+i*103,196,96,35),levels[i],difficulty==i))difficulty=i;
   Text(new Rect(276,249,100,35),T("До побед","Wins"),14);for(int i=1;i<=3;i++)if(Pill(new Rect(378+(i-1)*103,249,96,35),i.ToString(),targetWins==i))targetWins=i;
   Text(new Rect(276,302,100,35),T("Раунд","Round"),14);for(int i=0;i<2;i++){int secs=i==0?60:90;if(Pill(new Rect(378+i*155,302,145,35),secs+T(" сек."," sec"),roundSeconds==secs))roundSeconds=secs;}
   if(Pill(new Rect(276,377,408,50),T("ПРИМЕНИТЬ  ✓","APPLY  ✓"),false,true))rulesOpen=false;
  }
 }
}
