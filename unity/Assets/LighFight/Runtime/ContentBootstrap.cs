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
        private string status = "Проверка ресурсов…";
        private bool busy, canDownload, playing;
        private long downloadBytes;
        private float progress;
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
            canDownload=false;busy=true;status="Проверка ресурсов…";
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
            var catalog=Addressables.LoadContentCatalogAsync(manifest.catalogUrl,false);
            yield return catalog;bool ok=catalog.Status==AsyncOperationStatus.Succeeded;Addressables.Release(catalog);
            if(!ok){Fail("catalog_failed");yield break;}
            downloader.Estimate(manifest.starterLabel);
        }
        private void Size(long bytes)
        {
            downloadBytes=bytes;busy=false;canDownload=true;
            status=bytes>0?$"Ресурсы: {bytes/1048576f:F1} МБ":"Ресурсы установлены";
        }
        private void Fail(string code)
        {
            Debug.LogWarning("LighFight content: "+code);busy=false;canDownload=false;
            status=code=="delivery_not_configured"?"Доставка ресурсов ещё не настроена":"Не удалось загрузить ресурсы. Проверь соединение и попробуй ещё раз.";
        }
        private IEnumerator Open()
        {
            loadedContent=Addressables.LoadAssetAsync<GameContent>(manifest.assetAddress);yield return loadedContent;
            if(loadedContent.Status!=AsyncOperationStatus.Succeeded || loadedContent.Result==null || loadedContent.Result.fighters==null || loadedContent.Result.fighters.Length!=8 || loadedContent.Result.arenas==null || loadedContent.Result.arenas.Length!=5)
            {if(loadedContent.IsValid())Addressables.Release(loadedContent);Fail("invalid_content");yield break;}
            foreach(var fighter in loadedContent.Result.fighters)if(fighter==null||fighter.GetComponent<FighterView>()==null){Addressables.Release(loadedContent);Fail("invalid_fighter");yield break;}
            foreach(var arena in loadedContent.Result.arenas)if(arena==null||arena.GetComponent<OfficeArena>()==null){Addressables.Release(loadedContent);Fail("invalid_arena");yield break;}
            var game=new GameObject("LighFight").AddComponent<Prototype>();game.content=loadedContent.Result;
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
            GUI.Box(new Rect(220,130,520,280),"LighFight");GUI.Label(new Rect(245,190,470,90),busy&&progress>0?$"Загрузка {progress:P0}":status);
            GUI.enabled=!busy;
            if(canDownload && GUI.Button(new Rect(245,290,220,60),downloadBytes>0?"СКАЧАТЬ И ИГРАТЬ":"ИГРАТЬ"))
            {busy=true;canDownload=false;status="Загрузка ресурсов…";downloader.Download(manifest.starterLabel);}
            else if(!canDownload && GUI.Button(new Rect(245,290,220,60),"ПОВТОРИТЬ"))StartCoroutine(Check());
            GUI.enabled=true;
            if(GUI.Button(new Rect(485,290,220,60),"ВЕРНУТЬСЯ"))FindFirstObjectByType<HostBridge>()?.Exit("");
            GUI.matrix=old;
        }
        private void OnDestroy(){if(loadedContent.IsValid())Addressables.Release(loadedContent);}
    }
}
