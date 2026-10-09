namespace LighFight.Core
{
    public static class ContentBudget
    {
        public const long MaxDownload=2L*1024*1024*1024;
        public static bool CanDownload(long bytes,long available)
        {
            if(bytes<0 || bytes>MaxDownload)return false;
            if(bytes==0 || available<0)return true;
            return available>=bytes*2+32L*1024*1024;
        }
    }
}
