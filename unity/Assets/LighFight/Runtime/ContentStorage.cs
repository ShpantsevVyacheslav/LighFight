using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
namespace LighFight.Runtime
{
    public static class ContentStorage
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern long LighFightAvailableBytes();
#endif
        public static long AvailableBytes()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using(var fs=new AndroidJavaObject("android.os.StatFs",Application.persistentDataPath))return fs.Call<long>("getAvailableBytes");
#elif UNITY_IOS && !UNITY_EDITOR
                return LighFightAvailableBytes();
#else
                return new DriveInfo(Path.GetPathRoot(Application.persistentDataPath)).AvailableFreeSpace;
#endif
            }
            catch(Exception e){Debug.LogWarning("LighFight storage check: "+e.GetType().Name);return -1;}
        }
    }
}
