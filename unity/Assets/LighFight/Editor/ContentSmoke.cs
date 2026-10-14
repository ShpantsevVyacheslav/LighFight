using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using LighFight.Runtime;
namespace LighFight.Editor
{
    public static class ContentSmoke
    {
        private static double started;
        private static SelectedContentLease selected;private static EditorApplication.CallbackFunction pollSelected;private static AsyncOperationHandle<GameContent> contentHandle;
        public static void Run()
        {
            started=EditorApplication.timeSinceStartup;
            EditorApplication.update+=Timeout;
            // Explicit packed runtime data, so this check cannot silently use the Asset Database.
            PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath,Path.GetFullPath("Library/com.unity.addressables/aa/OSX/settings.json"));
            Addressables.InitializeAsync().Completed+=init=>
            {
                if(init.Status!=AsyncOperationStatus.Succeeded){Done(false,"initialize");return;}
                var catalog=Addressables.LoadContentCatalogAsync(Environment.GetEnvironmentVariable("LIGHFIGHT_CATALOG_URL"),false);
                catalog.Completed+=loaded=>
                {
                    bool ok=loaded.Status==AsyncOperationStatus.Succeeded;Addressables.Release(catalog);
                    if(!ok){Done(false,"catalog");return;}
                    var size=Addressables.GetDownloadSizeAsync("lighfight-starter");
                    size.Completed+=s=>
                    {
                        if(s.Status!=AsyncOperationStatus.Succeeded){Addressables.Release(size);Done(false,"size");return;}
                        Debug.Log("LighFight HTTP download bytes: "+s.Result);Addressables.Release(size);
                        var download=Addressables.DownloadDependenciesAsync("lighfight-starter",false);
                        download.Completed+=d=>
                        {
                            bool success=d.Status==AsyncOperationStatus.Succeeded;Addressables.Release(download);
                            if(!success){Done(false,"download");return;}
                            var asset=Addressables.LoadAssetAsync<GameContent>("lighfight-content");contentHandle=asset;
                            asset.Completed+=a=>
                            {
                                bool valid=a.Status==AsyncOperationStatus.Succeeded && a.Result!=null && a.Result.ValidateCatalog(out _);
                                if(!valid){Done(false,"invalid catalog");return;}
                                if(!a.Result.streamed){var obj=UnityEngine.Object.Instantiate(a.Result.fighters[0]);obj.GetComponent<FighterView>().Pose(new LighFight.Core.Fighter(),1,0);UnityEngine.Object.DestroyImmediate(obj);Done(true,"inline catalog / instantiated rig");return;}
                                selected=new SelectedContentLease(a.Result,0,Math.Min(1,a.Result.FighterCount-1),0,new AddressableSelectedAssetProvider());
                                pollSelected=()=>{if(!selected.IsDone&&selected.Error==null)return;EditorApplication.update-=pollSelected;pollSelected=null;bool ready=selected.ValidateSelected(out var error);if(ready){var obj=UnityEngine.Object.Instantiate(selected.Player);obj.GetComponent<FighterView>().Pose(new LighFight.Core.Fighter(),1,0);UnityEngine.Object.DestroyImmediate(obj);}Done(ready,ready?"stream catalog / selected pair / instantiated rig":error);};
                                EditorApplication.update+=pollSelected;
                            };
                        };
                    };
                };
            };
        }
        private static void Timeout(){if(EditorApplication.timeSinceStartup-started>120)Done(false,"timeout");}
        private static void Done(bool success,string detail)
        {
            EditorApplication.update-=Timeout;if(pollSelected!=null){EditorApplication.update-=pollSelected;pollSelected=null;}selected?.Dispose();selected=null;if(contentHandle.IsValid())Addressables.Release(contentHandle);contentHandle=default;PlayerPrefs.DeleteKey(Addressables.kAddressablesRuntimeDataPath);
            Debug.Log("LighFight content smoke "+(success?"PASS: ":"FAIL: ")+detail);
            EditorApplication.Exit(success?0:1);
        }
    }
}
