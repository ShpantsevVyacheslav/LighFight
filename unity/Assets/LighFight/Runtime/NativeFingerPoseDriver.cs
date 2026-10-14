using System;
using UnityEngine;
namespace LighFight.Runtime {
 // Keep the original hand mesh and weights; use a pose captured from this same rig.
 [DefaultExecutionOrder(200)]
 public sealed class NativeFingerPoseDriver : MonoBehaviour {
  [SerializeField] private Animator animator;
  [SerializeField] private NativeFingerPose pose;
  [SerializeField] private string[] closedStates=new string[0];
  private int contactFrame=-1;
  public void ApplyContactPose(){if(!enabled||!animator||!pose)return;if(joints==null)Initialize();bool closed=Array.IndexOf(states,animator.GetCurrentAnimatorStateInfo(0).shortNameHash)>=0;ApplyForReview(closed,Application.isPlaying&&contactFrame!=Time.frameCount?Time.deltaTime:0);contactFrame=Time.frameCount;}
  private Transform[] joints;private int[] states;private float weight;
  public void Configure(Animator value,NativeFingerPose source,string[] motions){animator=value;pose=source;closedStates=motions;Initialize();}
  private void Initialize(){
   if(!animator||!pose||pose.sourceAvatar!=animator.avatar)throw new InvalidOperationException("Finger pose belongs to another native rig");
   if(pose.joints.Length!=30)throw new InvalidOperationException("Require all thirty finger joints");
   joints=new Transform[30];for(int i=0;i<30;i++){joints[i]=animator.transform.Find(pose.joints[i].path);if(!joints[i])throw new InvalidOperationException("Finger joint missing: "+pose.joints[i].path);}
   states=Array.ConvertAll(closedStates,Animator.StringToHash);
  }
  private void LateUpdate(){
   if(!Application.isPlaying||!animator||!pose)return;
   if(joints==null)Initialize();
   ApplyForReview(Array.IndexOf(states,animator.GetCurrentAnimatorStateInfo(0).shortNameHash)>=0,contactFrame==Time.frameCount?0:Time.deltaTime);
  }
  public void ApplyForReview(bool closed,float seconds){
   if(joints==null)Initialize();weight=Mathf.MoveTowards(weight,closed?1:0,Mathf.Max(0,seconds)/.12f);
   if(weight<=0)return;for(int i=0;i<joints.Length;i++)joints[i].localRotation=Quaternion.Slerp(joints[i].localRotation,pose.joints[i].rotation,weight);
  }
 }
}
