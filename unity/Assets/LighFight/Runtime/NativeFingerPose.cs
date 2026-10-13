using System;
using UnityEngine;
namespace LighFight.Runtime {
 [CreateAssetMenu(menuName="LighFight/Native finger pose")]
 public sealed class NativeFingerPose : ScriptableObject {
  [Serializable] public struct Joint { public string path;public Quaternion rotation; }
  public Avatar sourceAvatar;
  public Joint[] joints=new Joint[0];
  public string sourceClip;
  public float sourceSeconds;
 }
}
