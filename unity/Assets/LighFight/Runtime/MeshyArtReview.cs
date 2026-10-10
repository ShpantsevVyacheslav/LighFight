using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LighFight.Runtime
{
    // Isolated provider animation review. Does not change combat or global timeScale.
    public sealed class MeshyArtReview : MonoBehaviour
    {
        public Animator fighter;
        public AnimationClip clip;
        public Camera reviewCamera;
        public Animator candidate;
        public AnimationClip candidateClip;
        public bool startWithCandidate;
        public string candidateLabel = "правка суставов";
        public string reviewDescription = "Локти · левая икра · открытые ладони";
        PlayableGraph graph;
        AnimationClipPlayable motion;
        AnimationClipPlayable candidateMotion;
        bool showCandidate;
        float time, speed = 1, yaw = 215, distance = 3, focusHeight = .9f;
        bool paused;
        Vector2 drag;
        bool dragging;
        void Start()
        {
            fighter.applyRootMotion = false;
            fighter.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create("Meshy art review");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            motion = AnimationClipPlayable.Create(graph, clip);
            motion.SetApplyFootIK(false); // Inspect provider weights without corrective IK.
            motion.SetSpeed(0);
            AnimationPlayableOutput.Create(graph, "Island", fighter).SetSourcePlayable(motion);
            if(candidate!=null && candidateClip!=null)
            {
                candidate.applyRootMotion=false;candidate.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                candidateMotion=AnimationClipPlayable.Create(graph,candidateClip);
                candidateMotion.SetApplyFootIK(false);candidateMotion.SetSpeed(0);
                AnimationPlayableOutput.Create(graph,"Joint candidate",candidate).SetSourcePlayable(candidateMotion);
                showCandidate=startWithCandidate;candidate.gameObject.SetActive(showCandidate);fighter.gameObject.SetActive(!showCandidate);
            }
            graph.Play(); Evaluate();
        }
        void Evaluate()
        {
            motion.SetTime(time);
            if(candidateMotion.IsValid())candidateMotion.SetTime(time);
            graph.Evaluate(0);
            if(candidate!=null){var correction=candidate.GetComponent<JointCorrectives>();if(correction!=null)correction.Apply();}
            var focus = new Vector3(0, focusHeight, 0);
            reviewCamera.transform.position = focus + Quaternion.Euler(8, yaw, 0) * new Vector3(0, 0, -distance);
            reviewCamera.transform.LookAt(focus);
        }
        void Update()
        {
            if (!paused) time = Mathf.Repeat(time + Time.unscaledDeltaTime * speed, clip.length);
            // Mouse also receives the primary touch on the legacy mobile input backend.
            if (Input.GetMouseButtonDown(0) && Input.mousePosition.y > Screen.height * .36f)
            { dragging = true; drag = Input.mousePosition; }
            if (Input.GetMouseButtonUp(0)) dragging = false;
            if (dragging)
            { Vector2 p = Input.mousePosition; yaw += (p.x - drag.x) * .25f; drag = p; }
            Evaluate();
        }
        void OnGUI()
        {
            var safe = Screen.safeArea;
            var old = GUI.matrix;
            float scale = Mathf.Min(safe.width / 900f, safe.height / 500f);
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height-safe.yMax, 0), Quaternion.identity, Vector3.one * scale);
            GUI.Box(new Rect(10, 10, 880, 58), "LighFight · Island / Meshy · ПРОВЕРКА ГРАФИКИ\n"+reviewDescription+". Перетащи персонажа для поворота камеры.");
            float y = safe.height / scale - 110;
            if (GUI.Button(new Rect(10,y-50,180,42), "Весь персонаж")) { focusHeight=.9f; distance=3; }
            if (GUI.Button(new Rect(200,y-50,180,42), "Локти / кисти")) { focusHeight=1.25f; distance=1.6f; }
            if (GUI.Button(new Rect(390,y-50,180,42), "Икры / колени")) { focusHeight=.45f; distance=1.6f; }
            if(candidate!=null && GUI.Button(new Rect(580,y-50,300,42),showCandidate ? "Модель: "+candidateLabel : "Модель: исходник Meshy"))
            {
                showCandidate=!showCandidate;fighter.gameObject.SetActive(!showCandidate);candidate.gameObject.SetActive(showCandidate);
            }
            if (GUI.Button(new Rect(10,y,140,46), paused ? "Продолжить" : "Пауза")) paused = !paused;
            if (GUI.Button(new Rect(160,y,140,46), "Сначала")) time = 0;
            if (GUI.Button(new Rect(310,y,140,46), "Кадр +")) { paused=true; time=Mathf.Min(clip.length, time+1f/60); }
            if (GUI.Button(new Rect(460,y,140,46), speed==1 ? "Скорость ¼" : "Скорость 1×")) speed = speed==1 ? .25f : 1;
            if (GUI.Button(new Rect(610,y,130,46), "Ближе")) distance=Mathf.Max(1.5f,distance-.3f);
            if (GUI.Button(new Rect(750,y,130,46), "Дальше")) distance=Mathf.Min(5,distance+.3f);
            float scrub=GUI.HorizontalSlider(new Rect(20,y+66,850,30),time,0,clip.length);
            if(scrub!=time) { time=scrub; paused=true; }
            GUI.matrix=old;
        }
        void OnDestroy() { if(graph.IsValid()) graph.Destroy(); }
    }
}
