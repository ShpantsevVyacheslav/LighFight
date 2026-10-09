using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace LighFight.Runtime {
// Caller must first present the size and get the user's download action.
public sealed class ContentDownloader:MonoBehaviour {
 public bool Busy {get;private set;} public event Action<long> SizeReady; public event Action<float> Progress; public event Action<string> Failed; public event Action Ready;
 public void Estimate(string label){if(!Busy)StartCoroutine(Size(label));}
 public void Download(string label){if(!Busy)StartCoroutine(Fetch(label));}
 IEnumerator Size(string label){Busy=true;var h=Addressables.GetDownloadSizeAsync(label);yield return h;bool ok=h.Status==AsyncOperationStatus.Succeeded;long bytes=ok?h.Result:0;Addressables.Release(h);Busy=false;if(ok)SizeReady?.Invoke(bytes);else Failed?.Invoke("content_size_failed");}
 IEnumerator Fetch(string label){Busy=true;var h=Addressables.DownloadDependenciesAsync(label,false);while(!h.IsDone){Progress?.Invoke(h.GetDownloadStatus().Percent);yield return null;}bool ok=h.Status==AsyncOperationStatus.Succeeded;Addressables.Release(h);Busy=false;if(ok)Ready?.Invoke();else Failed?.Invoke("content_download_failed");}
}
}
