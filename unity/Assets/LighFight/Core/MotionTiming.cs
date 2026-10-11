using System;
namespace LighFight.Core
{
    public static class MotionTiming
    {
        // Align the observed contact pose with the deterministic first active combat frame.
        public static float Phase(Move move,int frame,float contact)
        {
            contact=Math.Max(.05f,Math.Min(.95f,contact));
            frame=Math.Max(0,Math.Min(move.Duration,frame));
            if(frame<=move.Startup)return contact*frame/Math.Max(1,move.Startup);
            float activeEnd=Math.Min(.98f,contact+.025f);
            if(frame<move.Startup+move.Active)return contact+(activeEnd-contact)*(frame-move.Startup)/Math.Max(1,move.Active);
            return activeEnd+(1-activeEnd)*(frame-move.Startup-move.Active)/Math.Max(1,move.Recovery);
        }
    }
}
