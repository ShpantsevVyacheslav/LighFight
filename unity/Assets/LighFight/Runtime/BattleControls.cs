using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime
{
 public sealed partial class Prototype
 {
  private Texture2D circle;
  public static bool StickContains(Rect area,Vector2 position)=>Vector2.Distance(area.center,position)<=Mathf.Min(area.width,area.height)*.5f;
  public static Vector2 NormalizeStick(Rect area,Vector2 position)
  {
   float radius=Mathf.Min(area.width,area.height)*.375f;
   if(radius<=0)return Vector2.zero;
   var value=(position-area.center)/radius;value.y=-value.y;
   value=Vector2.ClampMagnitude(value,1);return value.magnitude<.14f?Vector2.zero:value;
  }
  private readonly StrikeGesture strikeGesture=new StrikeGesture();
  private bool gesturesEnabled=true;
  private Rect GestureArea=>selecting?new Rect(300,442,265,36):leftHanded?new Rect(25,175,340,340):new Rect(595,175,340,340);
  private static Rect GestureToggle=>new Rect(490,80,190,40);
  private void UpdateStrikeGestures()
  {

   if(!gesturesEnabled || advancedMoves || (!selecting && session.Phase!=FightPhase.Fighting)){strikeGesture.Cancel();return;}
   if(Input.touchCount==0)
   {
    if(Input.GetMouseButtonDown(0) && GestureArea.Contains(Position(Input.mousePosition))){var p=Position(Input.mousePosition);strikeGesture.Begin(0,p.x,-p.y,Time.unscaledTime);}
    if(Input.GetMouseButtonUp(0)){var p=Position(Input.mousePosition);int move=strikeGesture.End(0,p.x,-p.y,Time.unscaledTime);if(move>=0)SubmitGesture(move);}
    if(!Input.GetMouseButton(0))strikeGesture.Cancel();
    return;
   }
   bool tracked=false;
   for(int i=0;i<Input.touchCount;i++)
   {
    var touch=Input.GetTouch(i);var p=Position(touch.position);
    if(touch.phase==TouchPhase.Began && GestureArea.Contains(p))strikeGesture.Begin(touch.fingerId,p.x,-p.y,Time.unscaledTime);
    if(touch.fingerId!=strikeGesture.ActiveFinger)continue;
    tracked=true;
    if(touch.phase==TouchPhase.Ended || touch.phase==TouchPhase.Canceled){int move=strikeGesture.End(touch.fingerId,p.x,-p.y,Time.unscaledTime,touch.phase==TouchPhase.Canceled);if(move>=0)SubmitGesture(move);}
   }
   if(!tracked)strikeGesture.Cancel();
  }
  private void SubmitGesture(int move){if(selecting){previewMove=move;previewFrame=0;previewFighter.Attack=null;}else pendingAttack=move;}
  private void Disc(Rect rect,Color color){if(circle==null){circle=new Texture2D(64,64,TextureFormat.RGBA32,false);circle.hideFlags=HideFlags.HideAndDontSave;for(int y=0;y<64;y++)for(int x=0;x<64;x++)circle.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(31.5f-new Vector2(x-31.5f,y-31.5f).magnitude)));circle.Apply();}var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,circle);GUI.color=old;}
  private int MainMove(int button)
  {
   if(match.A.Attack.HasValue){string id=match.A.Attack.Value.Id;if(id=="cross" && button<2)return button==0?2:3;if(id=="body")return button==2?8:button==1?7:button==0?3:5;if(id=="hook" && button==3)return 5;if(id=="knee" && button==0)return 9;if(id=="lowKick" && button==1)return 3;}
   return button<2?button:button==2?4:5;
  }
  private Vector2 StickVector(Vector2 position)
  {
   return NormalizeStick(StickArea,position);
  }
  private void UpdateJoystick()
  {
   if(Input.touchCount==0)
   {
    stickFinger=-1;stickInput=Input.GetMouseButton(0)&&StickContains(StickArea,Position(Input.mousePosition))?StickVector(Position(Input.mousePosition)):Vector2.zero;return;
   }
   bool found=false;
   for(int i=0;i<Input.touchCount;i++)
   {
    var t=Input.GetTouch(i);
    if(stickFinger<0 && t.phase==TouchPhase.Began && StickContains(StickArea,Position(t.position)))stickFinger=t.fingerId;
    if(t.fingerId!=stickFinger)continue;found=true;
    if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled){stickFinger=-1;stickInput=Vector2.zero;}else stickInput=StickVector(Position(t.position));
   }
   if(!found){stickFinger=-1;stickInput=Vector2.zero;}
  }
  private void DrawTrainingAdvice()
  {
   if(!training)return;
   var fighter=match.A;string title=T("ПРОБУЙ УДАРЫ И СЕРИИ","TRY STRIKES AND CHAINS"),hint=T("Начни с джеба → кросса","Start with jab → cross");
   if(fighter.LastStrikeIndex>=0)
   {
    title=T(MoveNames[fighter.LastStrikeIndex],Moves.All[fighter.LastStrikeIndex].Id);
    switch(fighter.Outcome)
    {
     case StrikeOutcome.Hit:hint=T("Попадание · ","Hit · ");if(fighter.Attack.HasValue && fighter.Frame<=fighter.Attack.Value.Startup+fighter.Attack.Value.Active+6 && fighter.ComboCount<Combos.MaxHits){string next="";for(int i=0;i<Moves.All.Length;i++)if(Combos.CanFollow(fighter.Attack.Value.Id,i))next+=(next.Length>0?" / ":"")+T(MoveNames[i],Moves.All[i].Id);hint+=next.Length>0?T("Продолжай: ","Continue: ")+next:T("Заверши движение","Finish recovery");}else hint+=T("Дождись стойки","Wait for guard stance");break;
     case StrikeOutcome.Blocked:hint=T("Блок · серия прервана, смени уровень удара","Blocked · chain stopped, change strike level");break;
     case StrikeOutcome.Miss:hint=T("Промах · проверь дистанцию и направление","Miss · check distance and direction");break;
     case StrikeOutcome.Interrupted:hint=T("Удар прерван · сначала защита","Interrupted · defend first");break;
     default:hint=T("Подготовка · продолжение вводи ближе к попаданию","Startup · input follow-up near contact");break;
    }
   }
   string[] labels={T("Без защиты","Open"),T("Блок стоя","Stand block"),T("Низкий блок","Low block")};
   for(int i=0;i<3;i++)if(Pill(new Rect(215+i*124,303,118,34),labels[i],(int)session.Defence==i))session.SetDummyDefence(i);
   if(Pill(new Rect(215,344,165,34),T("Сброс дистанции","Reset spacing")))Reset();
   Card(new Rect(215,220,370,75),new Color(.03f,.08f,.14f,.8f));Text(new Rect(228,223,345,28),title,15,selectionAccent);Text(new Rect(228,253,345,35),hint,12);
  }
  private void DrawBattleControls()
  {
   SelectionResources();DrawTrainingAdvice();Disc(StickArea,new Color(.03f,.08f,.14f,controlsOpacity));
   Vector2 knob=StickArea.center+new Vector2(stickInput.x,-stickInput.y)*(Mathf.Min(StickArea.width,StickArea.height)*.3125f);
   Disc(new Rect(knob.x-24*controlsScale,knob.y-24*controlsScale,48*controlsScale,48*controlsScale),selectionAccent);

   Disc(CrouchArea,new Color(.08f,.2f,.29f,controlsOpacity));Text(CrouchArea,"↓",28,selectionAccent,TextAnchor.MiddleCenter);
   if(gesturesEnabled){Disc(ControlRect(new Rect(660,335,220,170)),new Color(.04f,.12f,.19f,controlsOpacity*.45f));Text(ControlRect(new Rect(720,383,110,60)),"↑  →\n←  ↓",22,selectionAccent,TextAnchor.MiddleCenter);}
   if(match.A.ComboCount>1 && match.A.Attack.HasValue)Text(new Rect(430,440,100,40),"×"+match.A.ComboCount,26,selectionAccent,TextAnchor.MiddleCenter);
  }
 }
}
