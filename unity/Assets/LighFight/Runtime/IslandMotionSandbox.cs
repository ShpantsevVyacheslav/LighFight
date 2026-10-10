using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LighFight.Runtime
{
    // Isolated art review scene; gameplay damage remains in the combat simulation.
    public sealed class IslandMotionSandbox : MonoBehaviour
    {
        public Animator fighter;
        public AnimationClip[] clips;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable current, previous;
        string motion="";
        float elapsed, blend;
        bool grounded;
        void Start()
        {
            fighter.applyRootMotion=false;
            fighter.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            graph=PlayableGraph.Create("Island motion sandbox");
            mixer=AnimationMixerPlayable.Create(graph,2);
            AnimationPlayableOutput.Create(graph,"Fighter",fighter).SetSourcePlayable(mixer);
            graph.Play(); Switch("Guard");
        }
        void Switch(string name)
        {
            var clip=Array.Find(clips,c=>c.name==name || c.name.EndsWith("|"+name));
            if(clip==null || motion==name)return;
            if(previous.IsValid()){graph.Disconnect(mixer,0);graph.DestroyPlayable(previous);}
            if(current.IsValid())
            {
                graph.Disconnect(mixer,1);previous=current;graph.Connect(previous,0,mixer,0);
            }
            current=AnimationClipPlayable.Create(graph,clip);current.SetApplyFootIK(true);
            graph.Connect(current,0,mixer,1);motion=name;elapsed=0;blend=0;
        }
        void Update()
        {
            elapsed+=Time.deltaTime;blend=Mathf.Min(1,blend+Time.deltaTime/.12f);
            mixer.SetInputWeight(0,previous.IsValid()?1-blend:0);mixer.SetInputWeight(1,previous.IsValid()?blend:1);
            float length=current.GetAnimationClip().length;
            if(motion=="Fall")
            {
                if(elapsed>=length){current.SetTime(length);current.SetSpeed(0);grounded=true;}
                if(grounded && Input.GetKeyDown(KeyCode.R)){grounded=false;Switch("GetUp");}
                return;
            }
            if(motion=="GetUp" && elapsed<length)return;
            if(Input.GetKeyDown(KeyCode.F)){Switch("Fall");return;}
            if(Input.GetKeyDown(KeyCode.H)){Switch("Hit");return;}
            string[] attacks={"jab","cross","hook","uppercut","frontKick"};
            KeyCode[] keys={KeyCode.J,KeyCode.K,KeyCode.L,KeyCode.U,KeyCode.I};
            for(int i=0;i<keys.Length;i++)if(Input.GetKeyDown(keys[i])){Switch(attacks[i]);return;}
            bool oneShot=Array.IndexOf(attacks,motion)>=0 || motion=="Hit";
            if(oneShot && elapsed<length)return;
            float direction=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);
            transform.position+=Vector3.right*(direction*Time.deltaTime*.5f);
            var position=transform.position;position.x=Mathf.Clamp(position.x,-2,2);transform.position=position;
            Switch(Input.GetKey(KeyCode.DownArrow)?"Crouch":direction!=0?"Walk":"Guard");
            if(current.GetTime()>=current.GetAnimationClip().length)current.SetTime(0);
        }
        void OnGUI()
        {
            GUI.Box(new Rect(16,16,620,64),"LighFight · Island · animation review\n← → move   ↓ crouch   J jab   K cross   L hook   U uppercut   I kick\nH hit reaction   F fall   R get up   |   "+motion);
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
