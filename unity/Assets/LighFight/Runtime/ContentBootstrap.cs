using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using LighFight.Core;
namespace LighFight.Runtime
{
    public sealed class ContentBootstrap : MonoBehaviour
    {
        public string manifestUrl;
        public bool useEmbeddedCatalog;
        public TextAsset embeddedDescriptor;
        private EmbeddedContentDescriptor embedded;
        public bool IsPlaying => playing;
        public string ErrorCode { get; private set; }
        public GameContent OpenedContent => playing && loadedContent.IsValid() ? loadedContent.Result : null;
        private ContentManifest manifest;
        private ContentDownloader downloader;
        private AsyncOperationHandle<GameContent> loadedContent;
        private AsyncOperationHandle pendingCatalog;
        private Prototype loadedGame;
        sealed class CatalogRelease:IDisposable{AsyncOperationHandle<GameContent> handle;public CatalogRelease(AsyncOperationHandle<GameContent> value){handle=value;}public void Dispose(){if(handle.IsValid())Addressables.Release(handle);handle=default;}}
        private string status = "Проверка ресурсов…";
        private bool busy, canDownload, playing;
        private long downloadBytes;
        private float progress;
        private string T(string ru,string en) { var host=FindFirstObjectByType<HostBridge>();return host?.CurrentLaunch?.locale?.StartsWith("ru")==false?en:ru; }
        private string CachePath => Path.Combine(Application.persistentDataPath,"lighfight-manifest.json");
        public static string PlatformName => Application.platform == RuntimePlatform.IPhonePlayer ? "iOS" : Application.platform == RuntimePlatform.Android ? "Android" : "StandaloneOSX";
        private void Start()
        {
            downloader=gameObject.AddComponent<ContentDownloader>();
            downloader.SizeReady+=Size;downloader.Progress+=value=>progress=value;
            downloader.Failed+=Fail;downloader.Ready+=()=>StartCoroutine(Open());
            StartCoroutine(Check());
        }
        private IEnumerator Check()
        {
            ErrorCode=null;canDownload=false;busy=true;status=T("Проверка ресурсов…","Checking game content…");
            if(useEmbeddedCatalog){yield return CheckEmbedded();yield break;}
            if(string.IsNullOrEmpty(manifestUrl)){Fail("delivery_not_configured");yield break;}
            // Same transport policy as catalogs: HTTPS, with a localhost exception for development.
            var probe=new ContentManifest{contentVersion="check",platform=PlatformName,unityVersion=Application.unityVersion,catalogUrl=manifestUrl};
            if(probe.Validate(PlatformName,Application.unityVersion,Debug.isDebugBuild)!=null){Fail("invalid_catalog");yield break;}
            using(var request=UnityWebRequest.Get(manifestUrl))
            {
                request.timeout=20;yield return request.SendWebRequest();
                string json=null;
                if(request.result==UnityWebRequest.Result.Success)
                {
                    if(request.downloadHandler.data.Length>1024*1024){Fail("invalid_manifest");yield break;}
                    json=request.downloadHandler.text;
                }
                else
                {
                    try { if(File.Exists(CachePath) && new FileInfo(CachePath).Length<=1024*1024)json=File.ReadAllText(CachePath); }catch(IOException){}
                    if(json==null){Fail("network_failed");yield break;}
                }
                try{manifest=JsonUtility.FromJson<ContentManifest>(json);}
                catch(Exception){manifest=null;}
            }
            string error=manifest==null?"invalid_manifest":manifest.Validate(PlatformName,Application.unityVersion,Debug.isDebugBuild);
            if(error!=null){Fail(error);yield break;}
            var catalog=Addressables.LoadContentCatalogAsync(manifest.catalogUrl,false);pendingCatalog=catalog;
            yield return catalog;bool ok=catalog.Status==AsyncOperationStatus.Succeeded;
            if(ok)ContentCatalogSelection.Activate(catalog.Result);
            Addressables.Release(catalog);pendingCatalog=default;
            if(!ok){Fail("catalog_failed");yield break;}
            downloader.Estimate(manifest.starterLabel);
        }
        public static string EmbeddedPlatformName { get { switch(Application.platform){case RuntimePlatform.IPhonePlayer:return "iOS";case RuntimePlatform.Android:return "Android";case RuntimePlatform.WebGLPlayer:return "WebGL";case RuntimePlatform.WindowsPlayer:case RuntimePlatform.WindowsEditor:return "StandaloneWindows64";case RuntimePlatform.LinuxPlayer:case RuntimePlatform.LinuxEditor:return "StandaloneLinux64";default:return "StandaloneOSX";} } }
        private IEnumerator CheckEmbedded()
        {
            if(!embeddedDescriptor||embeddedDescriptor.bytes.Length>1024*1024){Fail("embedded_not_configured");yield break;}
            try{embedded=JsonUtility.FromJson<EmbeddedContentDescriptor>(embeddedDescriptor.text);}catch(Exception){embedded=null;}
            var error=embedded==null?"invalid_descriptor":embedded.Validate(EmbeddedPlatformName,Application.unityVersion);
            if(error!=null){Fail(error);yield break;}
            var init=Addressables.InitializeAsync(false);pendingCatalog=init;yield return init;
            bool ok=init.Status==AsyncOperationStatus.Succeeded;if(ok)ContentCatalogSelection.Activate(init.Result);
            Addressables.Release(init);pendingCatalog=default;if(!ok){Fail("embedded_catalog_failed");yield break;}
            if(HasRemoteLocation(embedded.assetAddress,typeof(GameContent))){Fail("embedded_remote_location");yield break;}
            var size=Addressables.GetDownloadSizeAsync(embedded.starterLabel);pendingCatalog=size;yield return size;
            bool local=size.Status==AsyncOperationStatus.Succeeded&&size.Result==0;Addressables.Release(size);pendingCatalog=default;
            if(!local){Fail("embedded_download_required");yield break;}
            yield return Open();
        }
        private static bool HasRemoteLocation(object key,Type type)
        {
            var seen=new System.Collections.Generic.HashSet<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>();
            foreach(var locator in Addressables.ResourceLocators)if(locator.Locate(key,type,out var locations))foreach(var location in locations)if(Remote(location,seen))return true;
            return false;
        }
        private static bool Remote(UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation location,System.Collections.Generic.HashSet<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation> seen)
        {
            if(!seen.Add(location))return false;
            if(Uri.TryCreate(location.InternalId,UriKind.Absolute,out var uri)&&(uri.Scheme=="http"||uri.Scheme=="https"))return true;
            if(location.HasDependencies)foreach(var dependency in location.Dependencies)if(Remote(dependency,seen))return true;
            return false;
        }
        private void Size(long bytes)
        {
            if(!ContentBudget.CanDownload(bytes,ContentStorage.AvailableBytes())){Fail("storage_full");return;}
            downloadBytes=bytes;busy=false;canDownload=true;
            status=bytes>0?$"{T("Ресурсы","Content")}: {bytes/1048576f:F1} MB":T("Ресурсы установлены","Content installed");
        }
        private void Fail(string code)
        {
            ErrorCode=code;Debug.LogWarning("LighFight content: "+code);busy=false;canDownload=false;
            status=useEmbeddedCatalog?T("Встроенные ресурсы игры недоступны. Попробуй ещё раз.","Packaged game content is unavailable. Please retry."):code=="storage_full"?T("Недостаточно свободного места для ресурсов игры","Not enough storage for game content"):code=="delivery_not_configured"?T("Доставка ресурсов ещё не настроена","Content delivery is not configured"):T("Не удалось загрузить ресурсы. Проверь соединение и попробуй ещё раз.","Could not load content. Check your connection and retry.");
        }
        private string ValidateOpenedContent(GameContent content){if(content==null)return "invalid_content";if(useEmbeddedCatalog&&!content.streamed)return "embedded_inline_content";return content.ValidateCatalog(out var error)?null:error;}
        private IEnumerator Open()
        {
            loadedContent=Addressables.LoadAssetAsync<GameContent>(useEmbeddedCatalog?embedded.assetAddress:manifest.assetAddress);yield return loadedContent;
            if(loadedContent.Status!=AsyncOperationStatus.Succeeded || loadedContent.Result==null)
            {if(loadedContent.IsValid())Addressables.Release(loadedContent);Fail("invalid_content");yield break;}
            var validationError=ValidateOpenedContent(loadedContent.Result);
            if(validationError!=null){Addressables.Release(loadedContent);Fail(validationError);yield break;}
            if(useEmbeddedCatalog){
                var keys=new System.Collections.Generic.List<string>();keys.AddRange(loadedContent.Result.fighterAddresses);keys.AddRange(loadedContent.Result.arenaAddresses);
                foreach(var key in keys)if(HasRemoteLocation(key,typeof(GameObject))){Addressables.Release(loadedContent);loadedContent=default;Fail("embedded_remote_location");yield break;}
                var size=Addressables.GetDownloadSizeAsync(keys);pendingCatalog=size;yield return size;
                bool local=size.Status==AsyncOperationStatus.Succeeded&&size.Result==0;Addressables.Release(size);pendingCatalog=default;
                if(!local){Addressables.Release(loadedContent);loadedContent=default;Fail("embedded_download_required");yield break;}
            }
            var game=new GameObject("LighFight").AddComponent<Prototype>();loadedGame=game;game.content=loadedContent.Result;
            playing=true;busy=false;
            if(useEmbeddedCatalog)yield break;
            try
            {
                string temporary=CachePath+".tmp";File.WriteAllText(temporary,JsonUtility.ToJson(manifest));
                if(File.Exists(CachePath))File.Replace(temporary,CachePath,null);else File.Move(temporary,CachePath);
            }
            catch(Exception e){Debug.LogWarning("LighFight manifest cache: "+e.GetType().Name);}
        }
        private void OnGUI()
        {
            if(playing)return;
            float scale=Mathf.Min(Screen.width/960f,Screen.height/540f);
            var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
            GUI.Box(new Rect(220,130,520,280),"LighFight");GUI.Label(new Rect(245,190,470,90),busy&&progress>0?$"{T("Загрузка","Downloading")} {progress:P0}":status);
            GUI.enabled=!busy;
            if(canDownload && GUI.Button(new Rect(245,290,220,60),downloadBytes>0?T("СКАЧАТЬ И ИГРАТЬ","DOWNLOAD AND PLAY"):T("ИГРАТЬ","PLAY")))
            {busy=true;canDownload=false;status=T("Загрузка ресурсов…","Downloading content…");downloader.Download(manifest.starterLabel);}
            else if(!canDownload && GUI.Button(new Rect(245,290,220,60),T("ПОВТОРИТЬ","RETRY")))StartCoroutine(Check());
            GUI.enabled=true;
            if(GUI.Button(new Rect(485,290,220,60),T("ВЕРНУТЬСЯ","BACK")))FindFirstObjectByType<HostBridge>()?.Exit("");
            GUI.matrix=old;
        }
        private void OnDestroy(){if(loadedGame)Destroy(loadedGame.gameObject);if(pendingCatalog.IsValid())Addressables.Release(pendingCatalog);pendingCatalog=default;if(loadedContent.IsValid())SelectedContentReleaseQueue.Defer(new CatalogRelease(loadedContent));loadedContent=default;}
    }
}
