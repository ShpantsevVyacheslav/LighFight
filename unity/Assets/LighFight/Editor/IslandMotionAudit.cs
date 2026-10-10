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
            foreach(string name in new[]{"Guard","jab","frontKick","cross","hook","uppercut"})
            {
                var clip=clips.Single(x=>x.name==name || x.name.EndsWith("|"+name));
                var graph=PlayableGraph.Create("Motion audit");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable=AnimationClipPlayable.Create(graph,clip);
                playable.SetApplyFootIK(true);
                AnimationPlayableOutput.Create(graph,"Model",animator).SetSourcePlayable(playable);graph.Play();
                int frames=name=="Guard"?1:12;
                Vector3 start=Vector3.zero,end=Vector3.zero,supportStart=Vector3.zero;float displacement=0,torsoAngle=0,supportDrift=0;
                Quaternion torsoStart=Quaternion.identity;
                var chest=animator.GetBoneTransform(HumanBodyBones.Chest);
                var tracked=animator.GetBoneTransform(name=="frontKick"?HumanBodyBones.RightFoot:
                    name=="cross" || name=="uppercut"?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                var support=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                if(chest==null || tracked==null)throw new Exception("Required motion bones were not mapped for "+name);
                for(int frame=0;frame<frames;frame++)
                {
                    playable.SetTime(clip.length*(frames==1?0:(float)frame/(frames-1)));graph.Evaluate(.00001f);
                    if(frame==0){start=tracked.position;torsoStart=chest.rotation;supportStart=support.position;}
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
                        if((!skin.name.StartsWith("CombatFist") && mesh.bounds.size.y<1) || mesh.bounds.size.y>2.5f)throw new Exception("Invalid sampled height: "+mesh.bounds);
                        var obj=new GameObject("Sample");obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                        obj.AddComponent<MeshRenderer>().sharedMaterials=Enumerable.Repeat(skin.name.StartsWith("CombatFist")?fistMaterial:material,skin.sharedMaterials.Length).ToArray();previews.Add(obj);
                    }
                    var rt=new RenderTexture(640,720,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                    var image=new Texture2D(640,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,720),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(output,name+"-"+frame.ToString("D2")+".png"),image.EncodeToPNG());
                    RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
                    if(name=="Guard")
                    {
                        var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;float oldFov=camera.fieldOfView;
                        camera.transform.position=tracked.position+new Vector3(.32f,.12f,.65f);camera.transform.LookAt(tracked.position);camera.fieldOfView=22;
                        rt=new RenderTexture(480,480,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                        image=new Texture2D(480,480,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,480,480),0,0);image.Apply();
                        File.WriteAllBytes(Path.Combine(output,"fist-closeup.png"),image.EncodeToPNG());
                        RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
                        camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.fieldOfView=oldFov;
                    }
                    foreach(var obj in previews){UnityEngine.Object.DestroyImmediate(obj.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(obj);}
                }
                if(frames>1 && displacement<(name=="frontKick"?.3f:.08f))throw new Exception(name+" motion did not move: "+displacement);
                if(frames>1 && Vector3.Distance(start,end)>.03f)throw new Exception(name+" did not return to guard");
                if(supportDrift>.005f)throw new Exception(name+" support foot drifted: "+supportDrift);
                if((name=="jab" || name=="cross" || name=="hook" || name=="uppercut") && torsoAngle<15)
                    throw new Exception(name+" torso rotation missing: "+torsoAngle);
                File.AppendAllText(Path.Combine(output,"sampling.txt"),name+" max displacement="+displacement+"m; return error="+Vector3.Distance(start,end)+"m; torso angle="+torsoAngle+"deg; support drift="+supportDrift+"m\n");
                graph.Destroy();
            }
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(fistMaterial);
            Debug.Log("Island motion audit: valid Humanoid, six motions sampled");
        }
    }
}
