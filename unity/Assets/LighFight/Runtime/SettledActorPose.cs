using System;
using System.Collections.Generic;
using UnityEngine;
namespace LighFight.Runtime {
 // An authored source may override the carrier clip's duration/loop policy.
 public interface IResultMotionSource { bool TryGetResultDuration(string state,out float duration); }
 // After fingers (200), original curves (250), and final grounding (300).
 [DefaultExecutionOrder(400)]
 public sealed class SettledActorPose : MonoBehaviour {
  private bool requested;
  public bool IsHeld { get; private set; }
  private Behaviour[] writers; private bool[] wasEnabled;
  private Transform[] transforms; private Vector3[] positions,scales; private Quaternion[] rotations;
  public void Request(){if(!IsHeld)requested=true;}
  private void LateUpdate(){if(!requested||IsHeld)return;requested=false;
   transforms=GetComponentsInChildren<Transform>(true);
   positions=new Vector3[transforms.Length];rotations=new Quaternion[transforms.Length];scales=new Vector3[transforms.Length];
   for(int i=0;i<transforms.Length;i++){positions[i]=transforms[i].localPosition;rotations[i]=transforms[i].localRotation;scales[i]=transforms[i].localScale;}
   var list=new List<Behaviour>();
   foreach(var component in GetComponentsInChildren<Behaviour>(true))
    if(component is Animator || component is FighterView || component is NativeFingerPoseDriver || component is JointCorrectives || component is CandidateFootGrounding || component is IContactPoseHook)list.Add(component);
   writers=list.ToArray();wasEnabled=new bool[writers.Length];
   for(int i=0;i<writers.Length;i++){wasEnabled[i]=writers[i].enabled;if(wasEnabled[i])writers[i].enabled=false;}
   RestoreTransforms();IsHeld=true;
  }
  private void RestoreTransforms(){if(transforms==null)return;for(int i=0;i<transforms.Length;i++)if(transforms[i]){if(!transforms[i].localPosition.Equals(positions[i]))transforms[i].localPosition=positions[i];if(!transforms[i].localRotation.Equals(rotations[i]))transforms[i].localRotation=rotations[i];if(!transforms[i].localScale.Equals(scales[i]))transforms[i].localScale=scales[i];}}
  public void Resume(){requested=false;if(!IsHeld)return;
   for(int i=0;i<writers.Length;i++)if(writers[i])writers[i].enabled=wasEnabled[i];
   RestoreTransforms();IsHeld=false;writers=null;wasEnabled=null;transforms=null;positions=null;rotations=null;scales=null;
  }
 }
}
