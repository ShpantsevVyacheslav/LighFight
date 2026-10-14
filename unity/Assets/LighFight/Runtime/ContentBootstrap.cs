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
            canDownload=false;busy=true;status=T("Проверка ресурсов…","Checking game content…");
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
        private void Size(long bytes)
        {
            if(!ContentBudget.CanDownload(bytes,ContentStorage.AvailableBytes())){Fail("storage_full");return;}
            downloadBytes=bytes;busy=false;canDownload=true;
            status=bytes>0?$"{T("Ресурсы","Content")}: {bytes/1048576f:F1} MB":T("Ресурсы установлены","Content installed");
        }
        private void Fail(string code)
        {
            Debug.LogWarning("LighFight content: "+code);busy=false;canDownload=false;
            status=code=="storage_full"?T("Недостаточно свободного места для ресурсов игры","Not enough storage for game content"):code=="delivery_not_configured"?T("Доставка ресурсов ещё не настроена","Content delivery is not configured"):T("Не удалось загрузить ресурсы. Проверь соединение и попробуй ещё раз.","Could not load content. Check your connection and retry.");
        }
        private IEnumerator Open()
        {
            loadedContent=Addressables.LoadAssetAsync<GameContent>(manifest.assetAddress);yield return loadedContent;
            if(loadedContent.Status!=AsyncOperationStatus.Succeeded || loadedContent.Result==null)
            {if(loadedContent.IsValid())Addressables.Release(loadedContent);Fail("invalid_content");yield break;}
            if(!loadedContent.Result.ValidateCatalog(out var validationError)){Addressables.Release(loadedContent);Fail(validationError);yield break;}
            var game=new GameObject("LighFight").AddComponent<Prototype>();loadedGame=game;game.content=loadedContent.Result;
            playing=true;busy=false;
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
