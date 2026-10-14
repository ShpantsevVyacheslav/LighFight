using System;using System.Collections.Generic;using UnityEngine;
namespace LighFight.Runtime {
 // Ordinary Instantiate is paired with deferred asset release after Unity destroys the instances.
 public sealed class SelectedContentReleaseQueue:MonoBehaviour {
  struct Pending{public IDisposable lease;public int frame;}
  static SelectedContentReleaseQueue instance;readonly List<Pending> pending=new List<Pending>();
  public static void Defer(IDisposable lease){if(lease==null)return;if(!Application.isPlaying){lease.Dispose();return;}if(!instance){var obj=new GameObject("Selected content release");DontDestroyOnLoad(obj);instance=obj.AddComponent<SelectedContentReleaseQueue>();}instance.pending.Add(new Pending{lease=lease,frame=Time.frameCount+1});}
  void LateUpdate(){for(int i=pending.Count-1;i>=0;i--)if(Time.frameCount>=pending[i].frame){pending[i].lease.Dispose();pending.RemoveAt(i);}}
  void OnDestroy(){foreach(var p in pending)p.lease.Dispose();pending.Clear();if(instance==this)instance=null;}
 }
}
