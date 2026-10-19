using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// Import into an Editor folder only after the acquired source clips have been audited.
// Produces review assets; deliberately never rewrites a roster prefab/controller/catalog.
public static class UniqueDanceRetargetBake
{
    public static AnimationClip Bake(GameObject target, AnimationClip humanSource, string outputPath)
    {
        if (!target || !humanSource || !humanSource.humanMotion)
            throw new ArgumentException("A target prefab and genuine Humanoid source are required.");
        if (humanSource.length <= 0 || !outputPath.StartsWith("Assets/UniqueDancesReview/"))
            throw new ArgumentException("Invalid duration or review output path.");
        var actor = UnityEngine.Object.Instantiate(target);
        AnimatorController controller = null;
        AnimationClip sourceCopy = null;
        try
        {
            // Prevent existing native Dance overlays or grounding from contaminating retarget capture.
            foreach (var behaviour in actor.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            var animator = actor.GetComponentInChildren<Animator>(true);
            if (!animator || !animator.avatar || !animator.avatar.isHuman || !animator.avatar.isValid)
                throw new InvalidOperationException("Target must retain its approved valid Human avatar.");
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            controller = new AnimatorController();
            controller.AddLayer("DanceBake");
            var state = controller.layers[0].stateMachine.AddState("Source");
            sourceCopy = UnityEngine.Object.Instantiate(humanSource);
            var sourceSettings=AnimationUtility.GetAnimationClipSettings(sourceCopy);
            sourceSettings.loopTime=false;
            AnimationUtility.SetAnimationClipSettings(sourceCopy,sourceSettings);
            state.motion = sourceCopy;
            state.iKOnFeet = false;
            animator.runtimeAnimatorController = controller;
            animator.Rebind();
            var bones = animator.GetComponentsInChildren<Transform>(true);
            var curves = new Dictionary<string, AnimationCurve>();
            int intervals = Mathf.CeilToInt(humanSource.length * 60f);
            // Include both exact endpoints; no curve/key reduction or source asset mutation.
            for (int frame = 0; frame <= intervals; frame++)
            {
                float time = humanSource.length * frame / intervals;
                animator.Play("DanceBake.Source", 0, time / humanSource.length);
                animator.Update(0f);
                foreach (var bone in bones)
                {
                    string path = AnimationUtility.CalculateTransformPath(bone, animator.transform);
                    var p = bone.localPosition; var q = bone.localRotation; var s = bone.localScale;
                    Add(curves,path,"m_LocalPosition.x",time,p.x); Add(curves,path,"m_LocalPosition.y",time,p.y); Add(curves,path,"m_LocalPosition.z",time,p.z);
                    Add(curves,path,"m_LocalRotation.x",time,q.x); Add(curves,path,"m_LocalRotation.y",time,q.y); Add(curves,path,"m_LocalRotation.z",time,q.z); Add(curves,path,"m_LocalRotation.w",time,q.w);
                    Add(curves,path,"m_LocalScale.x",time,s.x); Add(curves,path,"m_LocalScale.y",time,s.y); Add(curves,path,"m_LocalScale.z",time,s.z);
                }
            }
            var result = new AnimationClip {name = target.name + "_" + humanSource.name, frameRate = 60};
            foreach (var pair in curves)
            {
                int split = pair.Key.LastIndexOf('|');
                AnimationUtility.SetEditorCurve(result, EditorCurveBinding.FloatCurve(pair.Key.Substring(0,split),typeof(Transform),pair.Key.Substring(split+1)),pair.Value);
            }
            result.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(result); settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(result,settings);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
            if (existing) { EditorUtility.CopySerialized(result,existing); UnityEngine.Object.DestroyImmediate(result); result=existing; }
            else AssetDatabase.CreateAsset(result,outputPath);
            EditorUtility.SetDirty(result); AssetDatabase.SaveAssets();
            return result;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(actor);
            if (controller) UnityEngine.Object.DestroyImmediate(controller);
            if (sourceCopy) UnityEngine.Object.DestroyImmediate(sourceCopy);
        }
    }
    static void Add(Dictionary<string,AnimationCurve> curves,string path,string property,float time,float value)
    {
        string key=path+"|"+property;
        if(!curves.TryGetValue(key,out var curve)) curves[key]=curve=new AnimationCurve();
        int index=curve.AddKey(time,value);
        AnimationUtility.SetKeyLeftTangentMode(curve,index,AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyRightTangentMode(curve,index,AnimationUtility.TangentMode.Linear);
    }
}
