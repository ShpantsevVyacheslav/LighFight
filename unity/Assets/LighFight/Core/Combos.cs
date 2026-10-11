namespace LighFight.Core
{
    public static class Combos
    {
        public const int MaxHits=4;
        public static bool CanFollow(string from,int next)
        {
            switch(from)
            {
                case "jab":return next==1;
                case "cross":return next==2 || next==3;
                case "hook":return next==5;
                case "body":return next==8 || next==7;
                case "knee":return next==9;
                case "lowKick":return next==3;
                default:return false;
            }
        }
        public static int Damage(int damage,int hit)=>System.Math.Max(1,damage*(100-10*System.Math.Max(0,System.Math.Min(MaxHits-1,hit-1)))/100);
    }
}
