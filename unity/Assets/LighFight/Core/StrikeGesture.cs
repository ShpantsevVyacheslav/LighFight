namespace LighFight.Core
{
    // Coordinates use x right, y up; distances are in the 960x540 logical UI.
    public sealed class StrikeGesture
    {
        private int finger=-1;
        public int ActiveFinger=>finger;
        private float x,y,time;
        public void Begin(int id,float px,float py,float now){if(finger>=0)return;finger=id;x=px;y=py;time=now;}
        public void Cancel()=>finger=-1;
        public int End(int id,float px,float py,float now,bool canceled=false)
        {
            if(id!=finger || finger<0)return -1;
            finger=-1;return canceled?-1:Classify(px-x,py-y,now-time);
        }
        public static int Classify(float dx,float dy,float seconds)
        {
            if(float.IsNaN(dx)||float.IsInfinity(dx)||float.IsNaN(dy)||float.IsInfinity(dy)||float.IsNaN(seconds)||seconds<0||seconds>.65f)return -1;
            float distance=dx*dx+dy*dy;
            if(distance<14*14)return seconds<=.22f?0:-1;
            if(distance<34*34 || seconds<.04f)return -1;
            float ax=System.Math.Abs(dx),ay=System.Math.Abs(dy);
            if(ax>ay*1.6f)return dx>0?1:2;
            if(ay>ax*1.6f)return dy>0?7:3;
            return dy>0?(dx>0?15:14):(dx>0?5:6);
        }
    }
}
