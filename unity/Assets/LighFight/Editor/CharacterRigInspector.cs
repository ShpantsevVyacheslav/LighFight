using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LighFight.Editor
{
    // Samples the actual imported rig, including its renderer bone hierarchy.
    public sealed class CharacterRigInspector : EditorWindow
    {
        GameObject source, instance;
        AnimationClip clip;
        float time, speed = 1;
        bool playing, skeleton = true;
        double last;
        readonly HashSet<Transform> bones = new HashSet<Transform>();
        readonly Dictionary<SkinnedMeshRenderer,Mesh> sampledMeshes = new Dictionary<SkinnedMeshRenderer,Mesh>();
        readonly Dictionary<Transform,Vector3> sampledBones = new Dictionary<Transform,Vector3>();

        [MenuItem("LighFight/Character Rig Inspector")]
        public static void Open() => GetWindow<CharacterRigInspector>("Rig Inspector");
        void OnEnable() { last = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; SceneView.duringSceneGui += DrawBones; }
        void OnDisable() { EditorApplication.update -= Tick; SceneView.duringSceneGui -= DrawBones; Release(); }
        void Release() { foreach(var mesh in sampledMeshes.Values) DestroyImmediate(mesh); sampledMeshes.Clear(); sampledBones.Clear(); if(instance) DestroyImmediate(instance); instance=null; bones.Clear(); }
        void Load()
        {
            Release(); if(!source) return;
            instance=Instantiate(source); instance.name="Rig inspection (temporary)"; instance.hideFlags=HideFlags.HideAndDontSave;
            foreach(var behaviour in instance.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled=false;
            foreach(var animator in instance.GetComponentsInChildren<Animator>()){animator.Rebind();animator.Update(0);animator.enabled=false;}
            foreach(var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>()) foreach(var bone in renderer.bones) if(bone) bones.Add(bone);
            if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.Frame(new Bounds(instance.transform.position+Vector3.up*.85f,new Vector3(2,2,2)),false);
            if(!clip){var path=AssetDatabase.GetAssetPath(source);var clips=System.Linq.Enumerable.OfType<AnimationClip>(AssetDatabase.LoadAllAssetsAtPath(path));var animator=source.GetComponent<Animator>();if(animator&&animator.runtimeAnimatorController)clips=System.Linq.Enumerable.Concat(clips,animator.runtimeAnimatorController.animationClips);clip=System.Linq.Enumerable.FirstOrDefault(clips,c=>c.name.Contains("Boxing_Guard_")&&!c.name.Contains("Prep"))??System.Linq.Enumerable.FirstOrDefault(clips,c=>!c.name.StartsWith("__preview"));}if(clip)Sample();else FreezePose();
        }
        void FreezePose()
        {
            sampledBones.Clear(); foreach(var bone in bones) sampledBones[bone]=bone.position;
            foreach(var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(!sampledMeshes.TryGetValue(skin,out var mesh))
                {
                    mesh=new Mesh(); sampledMeshes.Add(skin,mesh);
                    var frozen=new GameObject("Sampled surface"); frozen.transform.SetParent(skin.transform,false);var scale=skin.transform.localScale;frozen.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z); frozen.hideFlags=HideFlags.HideAndDontSave;
                    frozen.AddComponent<MeshFilter>().sharedMesh=mesh; frozen.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                }
                skin.BakeMesh(mesh,false);mesh.RecalculateBounds();foreach(var surface in skin.GetComponentsInChildren<MeshRenderer>())surface.ResetBounds();skin.enabled=false;
            }
        }
        void Sample()
        {
            if(!instance || !clip) return;
            clip.SampleAnimation(instance,time); FreezePose(); SceneView.RepaintAll();
        }
        void Tick()
        {
            double now=EditorApplication.timeSinceStartup; float delta=(float)(now-last); last=now;
            if(!playing || !clip || !instance) return;
            time=Mathf.Repeat(time+delta*speed,Mathf.Max(.001f,clip.length)); Sample(); Repaint();
        }
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Select a fighter prefab or imported FBX, then an animation clip. The Scene view shows the actual mesh and bones. Drag to orbit; scroll to zoom. Temporary inspection objects are removed when this window closes.",MessageType.Info);
            EditorGUI.BeginChangeCheck(); source=(GameObject)EditorGUILayout.ObjectField("Character",source,typeof(GameObject),false); if(EditorGUI.EndChangeCheck()) Load();
            EditorGUI.BeginChangeCheck(); clip=(AnimationClip)EditorGUILayout.ObjectField("Animation",clip,typeof(AnimationClip),false); if(EditorGUI.EndChangeCheck()){time=0;Sample();}
            skeleton=EditorGUILayout.Toggle("Skeleton overlay",skeleton);
            speed=EditorGUILayout.Slider("Playback speed",speed,.05f,2);
            EditorGUI.BeginChangeCheck(); time=EditorGUILayout.Slider("Time (seconds)",time,0,clip?clip.length:0); if(EditorGUI.EndChangeCheck()) Sample();
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button(playing?"Pause":"Play")) playing=!playing;
                if(GUILayout.Button("Previous frame")) Step(-1);
                if(GUILayout.Button("Next frame")) Step(1);
                if(GUILayout.Button("Rest pose")){playing=false;if(instance){foreach(var sourceBone in source.GetComponentsInChildren<Transform>()){string path=AnimationUtility.CalculateTransformPath(sourceBone,source.transform);var target=instance.transform.Find(path);if(target&&path!=""){target.localPosition=sourceBone.localPosition;target.localRotation=sourceBone.localRotation;target.localScale=sourceBone.localScale;}}FreezePose();}SceneView.RepaintAll();}
            }
            EditorGUILayout.LabelField("Bones",bones.Count.ToString());
            if(clip) EditorGUILayout.LabelField("Frame",Mathf.RoundToInt(time*clip.frameRate)+" / "+Mathf.RoundToInt(clip.length*clip.frameRate));
            SceneView.RepaintAll();
        }
        void Step(int direction){playing=false;if(!clip)return;time=Mathf.Clamp(time+direction/Mathf.Max(1,clip.frameRate),0,clip.length);Sample();}
        void DrawBones(SceneView view)
        {
            if(!skeleton || !instance)return;
            var previous=Handles.zTest; Handles.zTest=UnityEngine.Rendering.CompareFunction.Always;
            foreach(var bone in bones)
            {
                Handles.color=new Color(.7f,1,.15f);
                if(bone.parent && bones.Contains(bone.parent)) Handles.DrawAAPolyLine(3,sampledBones[bone.parent],sampledBones[bone]);
                Handles.color=new Color(1,.65f,.15f); Handles.SphereHandleCap(0,sampledBones[bone],Quaternion.identity,.018f,EventType.Repaint);
            }
            Handles.zTest=previous;
        }
    }
}
