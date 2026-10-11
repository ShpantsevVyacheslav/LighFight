using UnityEngine;
namespace LighFight.Runtime
{
 public sealed partial class Prototype
 {
  private Texture2D circle;
  private void Disc(Rect rect,Color color){if(circle==null){circle=new Texture2D(64,64,TextureFormat.RGBA32,false);circle.hideFlags=HideFlags.HideAndDontSave;for(int y=0;y<64;y++)for(int x=0;x<64;x++)circle.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(31.5f-new Vector2(x-31.5f,y-31.5f).magnitude)));circle.Apply();}var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,circle);GUI.color=old;}
  private int MainMove(int button)
  {
   if(match.A.Attack.HasValue){string id=match.A.Attack.Value.Id;if(id=="cross" && button<2)return button==0?2:3;if(id=="body" && button==2)return 8;}
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
   Text(new Rect(610,308,330,30),T("Р1 → Р2 → Р1: серия рук","H1 → H2 → H1: punch chain"),12,Color.white,TextAnchor.MiddleCenter);
  }
 }
}
