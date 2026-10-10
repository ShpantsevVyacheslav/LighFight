using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LighFight.Editor
{
    public static class IslandMotionAudit
    {
        public static void VerifyReview()
        {
            var scene=EditorSceneManager.OpenScene("Assets/LighFight/Art/Drafts/Island/IslandMotionReview.unity");
            var review=UnityEngine.Object.FindFirstObjectByType<LighFight.Runtime.IslandMotionSandbox>();
            if(review==null || review.clips.Length!=12 || !review.fighter.avatar.isValid)
                throw new Exception("Incomplete review scene");
            review.SendMessage("Start");
            var method=review.GetType().GetMethod("Switch",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            foreach(var name in new[]{"Walk","Crouch","jab","cross","hook","uppercut","frontKick","Hit","Fall","GetUp","Guard"})
                method.Invoke(review,new object[]{name});
            review.SendMessage("Update");
            UnityEngine.Object.DestroyImmediate(review.gameObject);
            Debug.Log("Island review: all 11 selectable motions switched through a live PlayableGraph");
        }
        public static void Run()
        {
            const string path="Assets/LighFight/Art/Drafts/Island/island-motion-draft.fbx";
            const string source="Assets/LighFight/Art/Source/Island";
            string output=Path.GetFullPath("../art/previews/island-motion"); Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"sampling.txt"),"");
            AssetDatabase.Refresh();
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Human; importer.isReadable=true; importer.importAnimation=true;
            var settings=importer.defaultClipAnimations;
            foreach(var setting in settings)
            {
                setting.lockRootRotation=true;setting.keepOriginalOrientation=true;
                setting.lockRootPositionXZ=true;setting.keepOriginalPositionXZ=true;
                setting.lockRootHeightY=true;setting.keepOriginalPositionY=true;
            }
            importer.clipAnimations=settings;
            importer.SaveAndReimport();
            var transforms=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Transform>();
            var names=transforms.Select(x=>x.name).ToArray();
            var description=importer.humanDescription;
            description.skeleton=transforms.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
            description.human=HumanTrait.BoneName.Where(n=>names.Contains(n.Replace(" ",""))).Select(n=>new HumanBone
                {humanName=n,boneName=n.Replace(" ",""),limit=new HumanLimit{useDefaultValues=true}}).ToArray();
            importer.humanDescription=description;importer.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(x=>!x.name.StartsWith("__preview__")).ToArray();
            File.WriteAllText(Path.Combine(output,"clips.txt"),string.Join("\n",clips.Select(x=>x.name+" "+x.length+"s "+x.frameRate+"fps")));
            var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().Single();
            if(!avatar.isHuman || !avatar.isValid)throw new Exception("Invalid Humanoid avatar");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var animator=model.GetComponent<Animator>(); animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var material=new Material(Shader.Find("Standard"));
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Directory.GetFiles(source,"*_texture.png").Single());
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Directory.GetFiles(source,"*_normal.png").Single()));
            material.EnableKeyword("_NORMALMAP"); material.SetFloat("_Glossiness",.25f);
            var fistMaterial=new Material(Shader.Find("Standard"));fistMaterial.color=new Color(.68f,.51f,.43f);fistMaterial.SetFloat("_Glossiness",.25f);
            var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;
            light.transform.rotation=Quaternion.Euler(35,-30,0);RenderSettings.ambientLight=new Color(.45f,.45f,.45f);
            var camera=new GameObject("Camera").AddComponent<Camera>();camera.fieldOfView=33;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.065f,.085f);
            camera.transform.position=new Vector3(2.4f,1.25f,3.2f);camera.transform.LookAt(new Vector3(0,.9f,0));
            Vector3 fallEnd=Vector3.zero;
            foreach(string name in new[]{"Guard","jab","frontKick","cross","hook","uppercut","Walk","Crouch","Hit","Fall","GetUp"})
            {
                var clip=clips.Single(x=>x.name==name || x.name.EndsWith("|"+name));
                var graph=PlayableGraph.Create("Motion audit");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable=AnimationClipPlayable.Create(graph,clip);
                playable.SetApplyFootIK(true);
                AnimationPlayableOutput.Create(graph,"Model",animator).SetSourcePlayable(playable);graph.Play();
                bool transition=name=="Walk" || name=="Crouch" || name=="Hit" || name=="Fall" || name=="GetUp";
                int frames=name=="Guard"?1:12;
                Vector3 start=Vector3.zero,end=Vector3.zero,supportStart=Vector3.zero;float displacement=0,torsoAngle=0,supportDrift=0,minHeight=float.PositiveInfinity;
                Quaternion torsoStart=Quaternion.identity;
                var chest=animator.GetBoneTransform(HumanBodyBones.Chest);
                var tracked=animator.GetBoneTransform(name=="frontKick"?HumanBodyBones.RightFoot:
                    name=="cross" || name=="uppercut"?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                var support=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                if(chest==null || tracked==null)throw new Exception("Required motion bones were not mapped for "+name);
                for(int frame=0;frame<frames;frame++)
                {
                    playable.SetTime(clip.length*(frames==1?0:(float)frame/(frames-1)));graph.Evaluate(.00001f);
                    if(frame==0){start=tracked.position;torsoStart=chest.rotation;supportStart=support.position;
                        if(name=="GetUp" && Vector3.Distance(fallEnd,animator.GetBoneTransform(HumanBodyBones.Hips).position)>.02f)
                            throw new Exception("Fall/GetUp pelvis discontinuity");
                    }
                    if(name=="Fall" && frame==frames-1)fallEnd=animator.GetBoneTransform(HumanBodyBones.Hips).position;
                    end=tracked.position;displacement=Mathf.Max(displacement,Vector3.Distance(start,end));
                    torsoAngle=Mathf.Max(torsoAngle,Quaternion.Angle(torsoStart,chest.rotation));
                    supportDrift=Mathf.Max(supportDrift,Vector3.Distance(supportStart,support.position));
                    var previews=new System.Collections.Generic.List<GameObject>();
                    foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        skin.enabled=false;
                        var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);var vertices=mesh.vertices;var weights=mesh.boneWeights;
                        var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
                        for(int i=0;i<vertices.Length;i++)
                        {
                            var w=weights[i];var p=vertices[i];
                            vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+
                                matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
                        }
                        mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();
                        minHeight=Mathf.Min(minHeight,mesh.bounds.min.y);
                        if((!skin.name.StartsWith("CombatFist") && mesh.bounds.size.y<(name=="Fall" || name=="GetUp"?.25f:1f)) || mesh.bounds.size.y>2.5f)throw new Exception("Invalid sampled height: "+mesh.bounds);
                        var obj=new GameObject("Sample");obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                        obj.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials.Select(m=>m!=null && m.name.StartsWith("FistSkin")?fistMaterial:material).ToArray();previews.Add(obj);
                    }
                    var rt=new RenderTexture(640,720,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                    var image=new Texture2D(640,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,720),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(output,name+"-"+frame.ToString("D2")+".png"),image.EncodeToPNG());
                    RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
                    if(name=="Guard" || name=="jab")
                    {
                        var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;float oldFov=camera.fieldOfView;
                        var wrist=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
                        var center=(wrist+tracked.position)*.5f;
                        camera.transform.position=center+new Vector3(.32f,.12f,.65f);camera.transform.LookAt(center);camera.fieldOfView=38;
                        rt=new RenderTexture(480,480,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                        image=new Texture2D(480,480,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,480,480),0,0);image.Apply();
                        File.WriteAllBytes(Path.Combine(output,name=="Guard"?"fist-closeup.png":"jab-wrist-"+frame.ToString("D2")+".png"),image.EncodeToPNG());
                        RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
                        camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.fieldOfView=oldFov;
                    }
                    foreach(var obj in previews){UnityEngine.Object.DestroyImmediate(obj.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(obj);}
                }
                if(minHeight<-.02f)throw new Exception(name+" penetrates floor: "+minHeight);
                if(name=="Walk" && supportDrift<.08f)throw new Exception("Walk feet did not step");
                if((name=="Fall" || name=="GetUp" || name=="Hit") && displacement<.08f)throw new Exception(name+" did not animate");
                if(!transition && frames>1 && displacement<(name=="frontKick"?.3f:.08f))throw new Exception(name+" motion did not move: "+displacement);
                if(!transition && frames>1 && Vector3.Distance(start,end)>.03f)throw new Exception(name+" did not return to guard");
                if(!transition && supportDrift>.005f)throw new Exception(name+" support foot drifted: "+supportDrift);
                if((name=="jab" || name=="cross" || name=="hook" || name=="uppercut") && torsoAngle<15)
                    throw new Exception(name+" torso rotation missing: "+torsoAngle);
                File.AppendAllText(Path.Combine(output,"sampling.txt"),name+" max displacement="+displacement+"m; return error="+Vector3.Distance(start,end)+"m; torso angle="+torsoAngle+"deg; support drift="+supportDrift+"m; minimum height="+minHeight+"m\n");
                graph.Destroy();
            }
            // Save a separate review scene, never promote an incomplete fighter to the roster.
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.enabled=true;
                skin.sharedMaterials=skin.sharedMaterials.Select(m=>m!=null && m.name.StartsWith("FistSkin")?fistMaterial:material).ToArray();
            }
            const string folder="Assets/LighFight/Art/Drafts/Island";
            AssetDatabase.DeleteAsset(folder+"/review-body.mat");AssetDatabase.DeleteAsset(folder+"/review-fist.mat");
            AssetDatabase.CreateAsset(material,folder+"/review-body.mat");
            AssetDatabase.CreateAsset(fistMaterial,folder+"/review-fist.mat");
            var review=model.AddComponent<LighFight.Runtime.IslandMotionSandbox>();review.fighter=animator;review.clips=clips;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.localScale=Vector3.one*.65f;
            camera.tag="MainCamera";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),folder+"/IslandMotionReview.unity");

            Debug.Log("Island motion audit: valid Humanoid, eleven motions sampled");
        }
    }
}
