using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using LighFight.Runtime;
namespace LighFight.Editor
{
    public static class MeshyMotionSetup
    {
        const string Folder="Assets/LighFight/Art/Motions/Meshy";
        static readonly string[] Map={"Left_Jab_from_Guard:jab:Джеб","Right_Jab_from_Guard:cross:Кросс","Left_Hook_from_Guard:hook:Хук","Right_Uppercut_from_Guard:uppercut:Апперкот","Spartan_Kick:frontKick:Фронт-кик","Roundhouse_Kick:roundhouseKick:Круговой удар","Boxing_Guard_Step_Knee_Strike:knee:Колено","Elbow_Strike:elbow:Локоть","Leg_Sweep:sweep:Подсечка","High_Kick:highKick:Высокий удар","Lunge_Spin_Kick:spinKick:Удар с разворотом","Hit_Reaction_to_Waist:Hit:Реакция на удар","Stand_Dodge:Dodge:Уклон","Left_Short_Hook_from_Guard:shortHook:Короткий хук","Punch_Forward_with_Both_Fists:doublePunch:Два кулака","Step_in_High_Kick:stepKick:Удар с шагом","Counterstrike:counterstrike:Контрудар","Knock_Down:Fall:Падение","Stand_Up1:GetUp:Подъём","Cautious_Crouch_Walk_Forward_inplace:Crouch:Присед","Walk_Fight_Forward_inplace:Walk:Шаг вперёд","Walk_Fight_Back_inplace:WalkBack:Шаг назад","body:body:В корпус","lowKick:lowKick:Лоу-кик","jumpKick:jumpKick:Прыжковый удар","backfist:backfist:Бэкфист","axeKick:axeKick:Акс-кик","sideKick:sideKick:Боковой удар","hammer:hammer:Молот","palm:palm:Толчок кулаками","heelKick:heelKick:Удар пяткой","switchKick:switchKick:Смена стойки"};
        public static void Run()
        {
            AssetDatabase.Refresh();
            string bakedPath=RoseReviewSetup.Folder+"/rose-motions.fbx";
            var bakedImporter=(ModelImporter)AssetImporter.GetAtPath(bakedPath);bakedImporter.animationType=ModelImporterAnimationType.Generic;bakedImporter.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;bakedImporter.importAnimation=true;bakedImporter.materialImportMode=ModelImporterMaterialImportMode.None;bakedImporter.isReadable=true;bakedImporter.SaveAndReimport();
            EditorSceneManager.OpenScene(RoseReviewSetup.Folder+"/RoseReview.unity");
            UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindFirstObjectByType<Animator>().gameObject);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(bakedPath));var animator=model.GetComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;var skin=animator.GetComponentInChildren<SkinnedMeshRenderer>();skin.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(RoseReviewSetup.Folder+"/rose.mat");
            var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();camera.orthographic=true;camera.orthographicSize=1.7f;
            Func<string,Transform> bone=name=>animator.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
            var clips=new List<AnimationClip>();var labels=new List<string>();var poses=new List<FighterView.ContactPose>();var report=new List<string>();
            string controllerPath=RoseReviewSetup.Folder+"/rose.controller";
            AssetDatabase.DeleteAsset(controllerPath);var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            foreach(var line in Map)
            {
                var parts=line.Split(':');string path=Folder+"/"+parts[0]+".fbx";
                var clip=AssetDatabase.LoadAllAssetsAtPath(bakedPath).OfType<AnimationClip>().SingleOrDefault(c=>!c.name.StartsWith("__preview") && c.name.EndsWith("|"+parts[0]));if(clip==null)continue;
                if(clip.length<=0 || AnimationUtility.GetCurveBindings(clip).Length<24)throw new Exception("Invalid humanoid clip: "+path);
                clips.Add(clip);labels.Add(parts[2]);controller.AddMotion(clip).name=parts[1];
                var graph=PlayableGraph.Create("Pose audit");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);AnimationPlayableOutput.Create(graph,"Rose",animator).SetSourcePlayable(playable);graph.Play();
                bool kick=parts[1].Contains("Kick") || parts[1]=="knee" || parts[1]=="sweep";
                var candidates=new[]{bone(kick?"LeftFoot":"LeftHand"),bone(kick?"RightFoot":"RightHand")};
                var starts=new Vector3[2];var ranges=new float[2];
                for(int n=0;n<=60;n++){playable.SetTime(clip.length*n/60);graph.Evaluate(.00001f);for(int k=0;k<2;k++){if(n==0)starts[k]=candidates[k].position;ranges[k]=Mathf.Max(ranges[k],Vector3.Distance(starts[k],candidates[k].position));}}
                var tracked=candidates[ranges[0]>ranges[1]?0:1];
                var hips=bone("Hips");var mesh=new Mesh();Vector3 start=Vector3.zero;float displacement=0,maxReach=float.NegativeInfinity,contact=.4f,previousY=0;
                for(int frame=0;frame<=60;frame++)
                {
                    float phase=frame/60f;playable.SetTime(clip.length*phase);graph.Evaluate(.00001f);skin.BakeMesh(mesh);
                    if((parts[1]=="axeKick" || parts[1]=="heelKick" || parts[1]=="sideKick") && frame%10==0)Capture(camera,skin,parts[1]+"-phase-"+frame);
                    var vertices=mesh.vertices;if(vertices.Any(v=>float.IsNaN(v.sqrMagnitude)||float.IsInfinity(v.sqrMagnitude)))throw new Exception("Nonfinite skin: "+parts[1]);
                    if(mesh.bounds.size.magnitude>4)throw new Exception("Exploding skin: "+parts[1]);
                    if(frame==0)start=tracked.position;displacement=Mathf.Max(displacement,Vector3.Distance(start,tracked.position));
                    float reach=Vector3.Dot(tracked.position-hips.position,animator.transform.forward);
                    if(parts[1]=="axeKick" || parts[1]=="hammer")reach=frame==0?0:(previousY-tracked.position.y)*60;
                    previousY=tracked.position.y;
                    if(phase>.05f && phase<.95f && reach>maxReach){maxReach=reach;contact=phase;}
                }
                if(displacement<.025f)throw new Exception("Static motion: "+parts[1]);
                poses.Add(new FighterView.ContactPose{moveId=parts[1],phase=contact,start=0,end=1});report.Add(parts[1]+" contact="+contact+" displacement="+displacement+"s length="+clip.length);
                playable.SetTime(clip.length*contact);graph.Evaluate(0);Capture(camera,skin,parts[1]);graph.Destroy();UnityEngine.Object.DestroyImmediate(mesh);
            }
            if(clips.Count!=Map.Length)throw new Exception("Missing motion clips: "+clips.Count+"/"+Map.Length);
            var guardSource=clips[0];var guardGraph=PlayableGraph.Create("Guard pose selection");guardGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var guardPlayable=AnimationClipPlayable.Create(guardGraph,guardSource);AnimationPlayableOutput.Create(guardGraph,"Rose",animator).SetSourcePlayable(guardPlayable);guardGraph.Play();float bestScore=float.PositiveInfinity,guardPhase=0;
            for(int frame=0;frame<=60;frame++){float phase=frame/60f;guardPlayable.SetTime(guardSource.length*phase);guardGraph.Evaluate(0);float score=Vector3.Distance(bone("LeftHand").position,bone("Head").position)+Vector3.Distance(bone("RightHand").position,bone("Head").position);if(score<bestScore){bestScore=score;guardPhase=phase;}}
            guardGraph.Destroy();var guard=CreatePoseClip(guardSource,guardPhase,RoseReviewSetup.Folder+"/guard.anim");poses.Add(new FighterView.ContactPose{moveId="_guard",phase=guardPhase});controller.AddMotion(guard).name="Guard";controller.AddMotion(guard).name="Idle";report.Add("guard phase="+guardPhase+" hand-to-head total distance="+bestScore);
            animator.runtimeAnimatorController=controller;
            var view=animator.gameObject.GetComponent<FighterView>()??animator.gameObject.AddComponent<FighterView>();view.ConfigureAnimation(animator,poses.ToArray());
            Directory.CreateDirectory("Assets/LighFight/Art/Fighters");PrefabUtility.SaveAsPrefabAsset(animator.gameObject,"Assets/LighFight/Art/Fighters/fighter-4.prefab");
            animator.runtimeAnimatorController=null;UnityEngine.Object.DestroyImmediate(view);
            var review=new GameObject("Motion Library").AddComponent<MotionLibraryReview>();review.fighter=animator;review.clips=clips.ToArray();review.labels=labels.ToArray();review.reviewCamera=camera;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),RoseReviewSetup.Folder+"/RoseMotionReview.unity");
            Directory.CreateDirectory("../art/source/motion-expansion");File.WriteAllLines("../art/source/motion-expansion/unity-audit.txt",report);AssetDatabase.SaveAssets();Debug.Log("Rose retarget audit passed: "+clips.Count+" motions");
        }
        public static AnimationClip CreatePoseClip(AnimationClip source,float phase,string path)
        {
            var clip=new AnimationClip{name="GuardPose",frameRate=60};
            foreach(var binding in AnimationUtility.GetCurveBindings(source))
            {
                float value=AnimationUtility.GetEditorCurve(source,binding).Evaluate(source.length*phase);
                AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,1,value));
            }
            AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(clip,path);return clip;
        }
        static void Capture(Camera camera,SkinnedMeshRenderer skin,string name)
        {
            var baked=new Mesh();skin.BakeMesh(baked);var display=new GameObject("Baked audit pose");display.transform.SetParent(skin.transform,false);display.AddComponent<MeshFilter>().sharedMesh=baked;display.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
            Directory.CreateDirectory("../art/previews/rose-motion");camera.clearFlags=CameraClearFlags.SolidColor;var rt=new RenderTexture(600,720,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(600,720);image.ReadPixels(new Rect(0,0,600,720),0,0);image.Apply();File.WriteAllBytes("../art/previews/rose-motion/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);skin.enabled=true;UnityEngine.Object.DestroyImmediate(display);UnityEngine.Object.DestroyImmediate(baked);
        }
    }
}
