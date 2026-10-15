namespace LighFight.Core {
 // Coordinates are relative to joystick radius, independent of handedness and UI scale.
 public sealed class JoystickGuard {
  private int finger=-1;private float startX,startY,started;private bool armed,consumed,wasGuarding;
  public void Begin(int id,float x,float y,float time,bool guarding){Cancel();if(x*x+y*y>.32f*.32f)return;finger=id;startX=x;startY=y;started=time;wasGuarding=guarding;armed=true;}
  private bool Track(int id,float x,float y){if(id!=finger||!armed)return false;if(x*x+y*y>.32f*.32f||(x-startX)*(x-startX)+(y-startY)*(y-startY)>.18f*.18f){armed=false;return false;}return true;}
  public bool Advance(int id,float x,float y,float time){if(!Track(id,x,y)||consumed||wasGuarding||time-started<.25f)return false;consumed=true;return true;}
  public bool End(int id,float x,float y,float time){if(id!=finger)return false;bool release=Track(id,x,y)&&!consumed&&wasGuarding&&time-started<.25f;Cancel();return release;}
  public void Cancel(){finger=-1;armed=consumed=wasGuarding=false;}
 }
}
