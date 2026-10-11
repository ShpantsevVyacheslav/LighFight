using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace LighFight.Runtime
{
    public sealed class MotionLibraryReview : MonoBehaviour
    {
        public Animator fighter;
        public AnimationClip[] clips;
        public string[] labels;
        public Camera reviewCamera;
        PlayableGraph graph;AnimationClipPlayable motion;int selected;float time;bool paused;
        public void Select(int index)
        {
            if(index<0 || index>=clips.Length)return;
            if(graph.IsValid())graph.Destroy();
            selected=index;time=0;fighter.applyRootMotion=false;fighter.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            graph=PlayableGraph.Create("LighFight motion library");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            motion=AnimationClipPlayable.Create(graph,clips[index]);
            AnimationPlayableOutput.Create(graph,"Rose",fighter).SetSourcePlayable(motion);graph.Play();graph.Evaluate(0);
        }
        void Start(){if(reviewCamera!=null){reviewCamera.rect=new Rect(.25f,0,.75f,1);reviewCamera.orthographicSize=1.3f;}Select(0);}
        void Update()
        {
            if(!graph.IsValid())return;
            if(!paused)time=Mathf.Repeat(time+Time.unscaledDeltaTime,clips[selected].length);
            motion.SetTime(time);graph.Evaluate(0);
            if(Input.GetKeyDown(KeyCode.RightArrow))Select((selected+1)%clips.Length);
            if(Input.GetKeyDown(KeyCode.LeftArrow))Select((selected+clips.Length-1)%clips.Length);
        }
        void OnGUI()
        {
            var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1100f,Screen.height/700f,1));
            GUI.Box(new Rect(10,10,1080,52),"LighFight · Розовая пантера · библиотека движений\n← → выбрать движение");
            for(int i=0;i<clips.Length;i++)if(GUI.Button(new Rect(10+(i%2)*125,120+(i/2)*32,120,28),labels[i]))Select(i);
            if(GUI.Button(new Rect(10,70,120,40),paused?"Продолжить":"Пауза"))paused=!paused;
            time=GUI.HorizontalSlider(new Rect(280,85,760,25),time,0,clips[selected].length);GUI.matrix=old;
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
