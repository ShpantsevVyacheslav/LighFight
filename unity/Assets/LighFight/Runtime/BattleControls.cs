using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime
{
 public sealed partial class Prototype
 {
  private Texture2D circle;
  private readonly StrikeGesture strikeGesture=new StrikeGesture();
  private bool gesturesEnabled=true;
  private static Rect GestureArea=>new Rect(610,145,325,145);
  private static Rect GestureToggle=>new Rect(490,80,190,40);
  private void UpdateStrikeGestures()
  {
   if(Pressed(GestureToggle)){gesturesEnabled=!gesturesEnabled;strikeGesture.Cancel();return;}
   if(!gesturesEnabled || advancedMoves || session.Phase!=FightPhase.Fighting){strikeGesture.Cancel();return;}
   if(Input.touchCount==0)
   {
    if(Input.GetMouseButtonDown(0) && GestureArea.Contains(Position(Input.mousePosition))){var p=Position(Input.mousePosition);strikeGesture.Begin(0,p.x,-p.y,Time.unscaledTime);}
    if(Input.GetMouseButtonUp(0)){var p=Position(Input.mousePosition);int move=strikeGesture.End(0,p.x,-p.y,Time.unscaledTime);if(move>=0)pendingAttack=move;}
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
    if(touch.phase==TouchPhase.Ended || touch.phase==TouchPhase.Canceled){int move=strikeGesture.End(touch.fingerId,p.x,-p.y,Time.unscaledTime,touch.phase==TouchPhase.Canceled);if(move>=0)pendingAttack=move;}
   }
   if(!tracked)strikeGesture.Cancel();
  }
  private void Disc(Rect rect,Color color){if(circle==null){circle=new Texture2D(64,64,TextureFormat.RGBA32,false);circle.hideFlags=HideFlags.HideAndDontSave;for(int y=0;y<64;y++)for(int x=0;x<64;x++)circle.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(31.5f-new Vector2(x-31.5f,y-31.5f).magnitude)));circle.Apply();}var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,circle);GUI.color=old;}
  private int MainMove(int button)
  {
   if(match.A.Attack.HasValue){string id=match.A.Attack.Value.Id;if(id=="cross" && button<2)return button==0?2:3;if(id=="body")return button==2?8:button==1?7:button==0?3:5;if(id=="hook" && button==3)return 5;if(id=="knee" && button==0)return 9;if(id=="lowKick" && button==1)return 3;}
   return button<2?button:button==2?4:5;
  }
  private static Vector2 StickVector(Vector2 position)
  {
   var value=(position-StickArea.center)/60f;value.y=-value.y;
   value=Vector2.ClampMagnitude(value,1);return value.magnitude<.14f?Vector2.zero:value;
  }
  private void UpdateJoystick()
  {
   if(Input.touchCount==0)
   {
    stickFinger=-1;stickInput=Input.GetMouseButton(0)&&StickArea.Contains(Position(Input.mousePosition))?StickVector(Position(Input.mousePosition)):Vector2.zero;return;
   }
   bool found=false;
   for(int i=0;i<Input.touchCount;i++)
   {
    var t=Input.GetTouch(i);
    if(stickFinger<0 && t.phase==TouchPhase.Began && StickArea.Contains(Position(t.position)))stickFinger=t.fingerId;
    if(t.fingerId!=stickFinger)continue;found=true;
    if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled){stickFinger=-1;stickInput=Vector2.zero;}else stickInput=StickVector(Position(t.position));
   }
   if(!found){stickFinger=-1;stickInput=Vector2.zero;}
  }
  private void DrawBattleControls()
  {
   SelectionResources();Disc(StickArea,new Color(.03f,.08f,.14f,.75f));
   Vector2 knob=StickArea.center+new Vector2(stickInput.x,-stickInput.y)*50;
   Disc(new Rect(knob.x-24,knob.y-24,48,48),selectionAccent);
   Text(new Rect(30,515,175,23),T("Движение + шаг в сторону","Move + sidestep"),11,Color.white,TextAnchor.MiddleCenter);
   Pill(new Rect(660,423,78,78),T("БЛОК","GUARD"));Pill(new Rect(270,455,78,52),T("ПРИСЕД","CROUCH"));
   Pill(GestureToggle,T("Жесты: ","Gestures: ")+(gesturesEnabled?T("ВКЛ","ON"):T("ВЫКЛ","OFF")));
   if(gesturesEnabled && !advancedMoves){Card(GestureArea,new Color(.03f,.08f,.14f,.5f));Text(new Rect(620,155,305,28),T("СВАЙП ДЛЯ УДАРА","SWIPE TO STRIKE"),13,selectionAccent,TextAnchor.MiddleCenter);Text(new Rect(620,187,305,75),T("↑ Апперкот   ↓ Корпус\n← Хук   → Кросс\nДиагонали — ноги · Касание — джеб","↑ Uppercut   ↓ Body\n← Hook   → Cross\nDiagonals: kicks · Tap: jab"),12,Color.white,TextAnchor.MiddleCenter);}
   Text(new Rect(590,308,350,30),match.A.Attack.HasValue && match.A.ComboCount>1?T("Серия: ","Chain: ")+match.A.ComboCount+" / 4":T("Р1 → Р2 → Р1 → Н2","H1 → H2 → H1 → F2"),12,Color.white,TextAnchor.MiddleCenter);
  }
 }
}
