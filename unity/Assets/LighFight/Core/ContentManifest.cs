using System;
using System.Text.RegularExpressions;
namespace LighFight.Core
{
    [Serializable]
    public sealed class ContentManifest
    {
        public int schemaVersion = 1, minimumPlayerVersion = 1;
        public string contentVersion, unityVersion, platform, catalogUrl;
        public string starterLabel = "lighfight-starter", assetAddress = "lighfight-content";
        public long downloadBytes;
        public string Validate(string currentPlatform, string currentUnity, bool development = false)
        {
            if(schemaVersion != 1 || minimumPlayerVersion > 1 || minimumPlayerVersion < 1) return "incompatible_player";
            if(platform != currentPlatform || unityVersion != currentUnity) return "incompatible_platform";
            if(string.IsNullOrEmpty(contentVersion) || !Regex.IsMatch(contentVersion,@"\A[a-zA-Z0-9._-]{1,64}\z")) return "invalid_version";
            if(starterLabel != "lighfight-starter" || assetAddress != "lighfight-content") return "invalid_content";
            if(downloadBytes < 0 || downloadBytes > 2L*1024*1024*1024) return "invalid_size";
            if(!Uri.TryCreate(catalogUrl,UriKind.Absolute,out var uri) || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment)) return "invalid_catalog";
            bool local = development && uri.Scheme == "http" && (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "10.0.2.2");
            return uri.Scheme == "https" || local ? null : "invalid_catalog";
        }
    }
}
