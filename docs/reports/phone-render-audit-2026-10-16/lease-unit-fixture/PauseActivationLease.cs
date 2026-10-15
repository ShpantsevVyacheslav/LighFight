using System;
using UnityEngine;
namespace LighFight.Runtime {
// Captures exact previous flags; never activates an object that was hidden by roster/loading.
public sealed class PauseActivationLease : IDisposable {
 readonly GameObject[] actors; readonly bool[] flags; readonly Camera camera; readonly bool cameraEnabled;bool disposed;
 public PauseActivationLease(Camera camera,GameObject[] actors){
  this.camera=camera;cameraEnabled=camera&&camera.enabled;this.actors=(GameObject[])actors.Clone();flags=new bool[actors.Length];
  for(int i=0;i<actors.Length;i++){flags[i]=actors[i]&&actors[i].activeSelf;if(actors[i])actors[i].SetActive(false);}
  if(camera)camera.enabled=false;
 }
 public void Dispose(){if(disposed)return;disposed=true;for(int i=0;i<actors.Length;i++)if(actors[i])actors[i].SetActive(flags[i]);if(camera)camera.enabled=cameraEnabled;}
}
}
