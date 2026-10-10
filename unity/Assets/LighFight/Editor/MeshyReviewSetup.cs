using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LighFight.Editor
{
    public static class MeshyReviewSetup
    {
        public const string ScenePath="Assets/LighFight/Art/Drafts/IslandPremium/MeshyArtReview.unity";
        public static void Verify() => VerifyCore(false);
        public static void VerifyWeightsReversionMutation()
        {
            try { VerifyCore(true); }
            catch(Exception error) when(error.Message=="Joint repair candidate has no visible deformation change")
            {
                File.WriteAllText("../.tools/reports/meshy-joint-mutation.txt","PASS: audit rejects candidate with original skin weights");
                Debug.Log("PASS: weight reversion detected");return;
            }
            throw new Exception("Audit did not detect reverted skin weights");
        }
        static void VerifyCore(bool revertWeights)
        {
            EditorSceneManager.OpenScene(ScenePath);
            var review=UnityEngine.Object.FindFirstObjectByType<LighFight.Runtime.MeshyArtReview>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var type=review.GetType();
            type.GetMethod("Start",flags).Invoke(review,null);
            var skin=review.fighter.GetComponentInChildren<SkinnedMeshRenderer>();
            var candidateSkin=review.candidate!=null?review.candidate.GetComponentInChildren<SkinnedMeshRenderer>():null;
            if(revertWeights)
            {
                if(candidateSkin==null)throw new Exception("Missing candidate for mutation");
                candidateSkin.sharedMesh=skin.sharedMesh;
            }
            if(review.candidate!=null)review.candidate.gameObject.SetActive(true);
            var mesh=new Mesh();skin.BakeMesh(mesh);var initial=mesh.vertices;
            var candidateMesh=new Mesh();float maxVariantDifference=0;
            float maxDisplacement=0;
            for(int frame=0;frame<=24;frame++)
            {
                type.GetField("time",flags).SetValue(review,review.clip.length*frame/24);
                type.GetMethod("Evaluate",flags).Invoke(review,null);
                skin.BakeMesh(mesh);var vertices=mesh.vertices;
                if(candidateSkin!=null)
                {
                    candidateSkin.BakeMesh(candidateMesh);var revised=candidateMesh.vertices;
                    if(revised.Length!=vertices.Length)throw new Exception("Comparison topology differs");
                    for(int i=0;i<vertices.Length;i++)maxVariantDifference=Mathf.Max(maxVariantDifference,Vector3.Distance(vertices[i],revised[i]));
                }
                for(int i=0;i<vertices.Length;i++)
                {
                    if(float.IsNaN(vertices[i].x)||float.IsInfinity(vertices[i].x))throw new Exception("Invalid deformed vertex");
                    maxDisplacement=Mathf.Max(maxDisplacement,Vector3.Distance(initial[i],vertices[i]));
                }
            }
            if(maxDisplacement<.05f)throw new Exception("Provider clip does not deform the model");
            if(candidateSkin!=null && maxVariantDifference<.001f)throw new Exception("Joint repair candidate has no visible deformation change");
            if(review.candidate!=null)review.candidate.gameObject.SetActive(false);
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                type.GetField("time",flags).SetValue(review,review.clip.length*.4f);
                type.GetMethod("Evaluate",flags).Invoke(review,null);
                var rt=new RenderTexture(600,720,24);review.reviewCamera.targetTexture=rt;
                review.reviewCamera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
                var image=new Texture2D(600,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,600,720),0,0);image.Apply();
                File.WriteAllBytes("../art/previews/island-premium/unity-jab.png",image.EncodeToPNG());
                RenderTexture.active=old;review.reviewCamera.targetTexture=null;
                UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            }
            string result="Meshy review: "+skin.bones.Length+" bones; "+review.clip.length+" sec; max vertex displacement "+maxDisplacement+" m; max candidate difference "+maxVariantDifference+" m; 25 samples";
            Directory.CreateDirectory("../.tools/reports");File.WriteAllText("../.tools/reports/meshy-unity-audit.txt",result);
            Debug.Log(result);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(candidateMesh);UnityEngine.Object.DestroyImmediate(review.gameObject);
        }
        // Build owner runs this explicitly; never overwrites LighChat exports.
        public static void ExportIosReview()
        {
            if(!File.Exists(ScenePath)) throw new Exception("Create the Meshy review scene first");
            var target=UnityEditor.Build.NamedBuildTarget.iOS;
            string oldId=PlayerSettings.GetApplicationIdentifier(target);
            string oldName=PlayerSettings.productName;
            var oldOrientation=PlayerSettings.defaultInterfaceOrientation;
            try
            {
                PlayerSettings.SetApplicationIdentifier(target,"com.lighchat.lighfight.artreview");
                PlayerSettings.productName="LighFight Art Review";
                PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{ScenePath}, target=BuildTarget.iOS,
                    locationPathName="Builds/ArtReviewiOS", options=BuildOptions.Development });
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    throw new Exception("Art review export failed: "+report.summary.result);
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(target,oldId);
                PlayerSettings.productName=oldName;
                PlayerSettings.defaultInterfaceOrientation=oldOrientation;
            }
        }
        [MenuItem("LighFight/Create Meshy art review")]
        public static void Create()
        {
            const string folder="Assets/LighFight/Art/Drafts/IslandPremium";
            const string path=folder+"/island-meshy-jab.fbx";
            AssetDatabase.Refresh();
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Generic;
            importer.importAnimation=true;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
            if(clip.length<1) throw new Exception("Incomplete provider jab");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var animator=model.GetComponent<Animator>();
            if(animator==null)animator=model.AddComponent<Animator>();
            var material=new Material(Shader.Find("LighFight/OfficeLit"));
            var texture=folder+"/texture_0.png";
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
            material.SetFloat("_Glossiness",.18f);
            AssetDatabase.DeleteAsset(folder+"/review.mat");AssetDatabase.CreateAsset(material,folder+"/review.mat");
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.sharedMaterial=material;
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.backgroundColor=new Color(.04f,.05f,.07f);camera.clearFlags=CameraClearFlags.SolidColor;camera.nearClipPlane=.05f;
            var light=new GameObject("Review light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.55f;light.transform.rotation=Quaternion.Euler(35,-35,0);
            var fill=new GameObject("Front fill light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.7f;fill.transform.rotation=Quaternion.Euler(30,145,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.3f,.3f,.3f);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Floor";
            var floorMat=new Material(Shader.Find("LighFight/OfficeLit"));floorMat.color=new Color(.065f,.075f,.09f);
            AssetDatabase.DeleteAsset(folder+"/floor.mat");AssetDatabase.CreateAsset(floorMat,folder+"/floor.mat");floor.GetComponent<Renderer>().sharedMaterial=floorMat;
            var review=new GameObject("Art review controls").AddComponent<LighFight.Runtime.MeshyArtReview>();review.fighter=animator;review.clip=clip;review.reviewCamera=camera;
            const string candidatePath=folder+"/island-joints-candidate.fbx";
            if(File.Exists(candidatePath))
            {
                var candidateImporter=(ModelImporter)AssetImporter.GetAtPath(candidatePath);
                candidateImporter.animationType=ModelImporterAnimationType.Generic;candidateImporter.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                candidateImporter.materialImportMode=ModelImporterMaterialImportMode.None;candidateImporter.importAnimation=true;candidateImporter.SaveAndReimport();
                var candidateModel=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath));
                candidateModel.name="Island joint repair candidate";
                var candidateAnimator=candidateModel.GetComponent<Animator>();if(candidateAnimator==null)candidateAnimator=candidateModel.AddComponent<Animator>();
                foreach(var skin in candidateModel.GetComponentsInChildren<SkinnedMeshRenderer>())skin.sharedMaterial=material;
                review.candidate=candidateAnimator;review.candidateClip=AssetDatabase.LoadAllAssetsAtPath(candidatePath).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
                candidateModel.SetActive(false);
            }
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            Debug.Log("Meshy review scene created: "+clip.length+" seconds; isolated scene, build settings unchanged");
        }
    }
}
