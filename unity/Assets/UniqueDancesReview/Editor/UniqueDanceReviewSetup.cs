using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using LighFight.Runtime;

public static class UniqueDanceReviewSetup
{
    // Bone/Boom, Witch/Genie, Master/Arm Circle, Island/Bubble, Rose/Shake,
    // Ranger/Cardio, Gala/Swing, Cobalt/Bass, Retro/Denim, Sailor/Gangnam, Summer/All Night.
    static readonly int[] Actions={66,77,63,67,82,70,75,65,71,74,64};
    const string Folder="Assets/UniqueDancesReview";
    public static void Run()
    {
        var roster=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
        if(!roster||roster.fighters.Length!=11)throw new Exception("Expected current approved 11-fighter catalog.");
        Directory.CreateDirectory(Folder+"/Sources");
        foreach(int action in Actions)
        {
            string path=Folder+"/Sources/action-"+action+".fbx";
            if(!File.Exists(path))File.Copy("../art/source/unique-dances-20261018/action-"+action+".fbx",path);
        }
        AssetDatabase.Refresh();
        foreach(int action in Actions)
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/Sources/action-"+action+".fbx");
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression=ModelImporterAnimationCompression.Off;
            importer.meshCompression=ModelImporterMeshCompression.Off;
            importer.optimizeGameObjects=false;
            importer.SaveAndReimport();
            // Meshy names its lower spine Spine02 and upper chest Spine: automatic
            // name matching alone reverses the chain. Reuse the established mapping.
            LighFight.Editor.RoseReviewSetup.ConfigureHumanoid(Folder+"/Sources/action-"+action+".fbx");
        }
        var report=new List<string>();
        for(int i=0;i<Actions.Length;i++)
        {
            int action=Actions[i];string output=Folder+"/Fighter-"+i;
            Directory.CreateDirectory(output);
            var clips=AssetDatabase.LoadAllAssetsAtPath(Folder+"/Sources/action-"+action+".fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
            if(clips.Length!=1||!clips[0].humanMotion)throw new Exception("Invalid donor Human clip "+action);
            var target=roster.fighters[i];
            var baked=UniqueDanceRetargetBake.Bake(target,clips[0],output+"/Dance.transform.anim",false);
            var pose=Export(baked,output+"/Dance.pose.asset");
            UnityEngine.Object.DestroyImmediate(baked);
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(target);
            try
            {
                var animator=actor.GetComponentInChildren<Animator>();
                var old=animator.runtimeAnimatorController as AnimatorController;
                if(!old)throw new Exception("Expected standalone approved target controller.");
                string cp=output+"/Review.controller";
                if(!File.Exists(cp))AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(old),cp);
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(cp);
                var state=controller.layers[0].stateMachine.states.First(x=>x.state.name=="Dance").state;
                string carrierPath=output+"/Dance.carrier.anim";
                var carrier=AssetDatabase.LoadAssetAtPath<AnimationClip>(carrierPath);
                if(!carrier){carrier=UnityEngine.Object.Instantiate(clips[0]);AssetDatabase.CreateAsset(carrier,carrierPath);}
                else EditorUtility.CopySerialized(clips[0],carrier);
                var settings=AnimationUtility.GetAnimationClipSettings(carrier);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(carrier,settings);
                state.motion=carrier;state.iKOnFeet=false;animator.runtimeAnimatorController=controller;
                var overlay=actor.GetComponent<NativeSupportOverlay>();
                if(!overlay)throw new Exception("Expected native support on approved actor "+target.name);
                overlay.entries=overlay.entries.Where(e=>e.state!="Dance").Concat(new[]{new NativeSupportOverlay.Entry{state="Dance",sourceName="MeshyAction"+action,pose=pose,loop=false}}).ToArray();
                var view=actor.GetComponent<FighterView>();
                var field=typeof(FighterView).GetField("nativeClips",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                var native=((FighterView.NativeClip[])field.GetValue(view)).Where(c=>c.state!="Dance").Concat(new[]{new FighterView.NativeClip{state="Dance",clip=carrier}}).ToArray();
                view.UseNativeMotion(native);
                PrefabUtility.SaveAsPrefabAsset(actor,output+"/Review.prefab");
                report.Add(i+" "+target.name+" action="+action+" duration="+clips[0].length+" Human="+clips[0].humanMotion);
            }
            finally{UnityEngine.Object.DestroyImmediate(actor);}
        }
        AssetDatabase.SaveAssets();File.WriteAllLines("../art/source/unique-dances-20261018/review-bake.txt",report);
        Debug.Log("AGENT_RESULT unique dance review 11 baked; production roster unchanged");
    }
    static NativeSupportPoseData Export(AnimationClip clip,string path)
    {
        var data=AssetDatabase.LoadAssetAtPath<NativeSupportPoseData>(path);
        if(!data){data=ScriptableObject.CreateInstance<NativeSupportPoseData>();AssetDatabase.CreateAsset(data,path);}
        data.sourceName=clip.name;data.duration=clip.length;
        var bindings=AnimationUtility.GetCurveBindings(clip);
        data.joints=bindings.Select(b=>b.path).Distinct().OrderBy(p=>p).Select(pathName=>
        {
            var joint=new NativeSupportPoseData.Joint{path=pathName};
            foreach(var b in bindings.Where(b=>b.path==pathName))
            {
                string field=b.propertyName.StartsWith("m_LocalPosition.")?"p"+b.propertyName.Last():b.propertyName.StartsWith("m_LocalRotation.")?"q"+b.propertyName.Last():b.propertyName.StartsWith("m_LocalScale.")?"s"+b.propertyName.Last():null;
                if(field==null)throw new Exception("Unexpected transform field");
                typeof(NativeSupportPoseData.Joint).GetField(field).SetValue(joint,AnimationUtility.GetEditorCurve(clip,b));
            }
            return joint;
        }).ToArray();
        EditorUtility.SetDirty(data);return data;
    }
}
