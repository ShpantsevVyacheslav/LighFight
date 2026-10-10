using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LighFight.Editor
{
    public static class RoseReviewSetup
    {
        public const string Folder="Assets/LighFight/Art/Drafts/Rose";
        public static Avatar ConfigureHumanoid(string path)
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=true;importer.isReadable=true;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var transforms=root.GetComponentsInChildren<Transform>();
            var mapping=new[]{"Hips:Hips","Spine:Spine02","Chest:Spine01","UpperChest:Spine","Neck:neck","Head:Head","LeftShoulder:LeftShoulder","RightShoulder:RightShoulder","LeftUpperArm:LeftArm","RightUpperArm:RightArm","LeftLowerArm:LeftForeArm","RightLowerArm:RightForeArm","LeftHand:LeftHand","RightHand:RightHand","LeftUpperLeg:LeftUpLeg","RightUpperLeg:RightUpLeg","LeftLowerLeg:LeftLeg","RightLowerLeg:RightLeg","LeftFoot:LeftFoot","RightFoot:RightFoot","LeftToes:LeftToeBase","RightToes:RightToeBase"};
            var description=importer.humanDescription;
            description.human=mapping.Select(pair=>pair.Split(':')).Where(p=>transforms.Any(t=>t.name==p[1])).Select(p=>new HumanBone{humanName=p[0],boneName=p[1],limit=new HumanLimit{useDefaultValues=true}}).ToArray();
            description.skeleton=transforms.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
            importer.humanDescription=description;
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips){clip.lockRootRotation=true;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.keepOriginalPositionY=true;}
            importer.clipAnimations=clips;importer.SaveAndReimport();
            var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().Single();
            if(!avatar.isValid || !avatar.isHuman)throw new Exception("Invalid humanoid: "+path);
            return avatar;
        }
        public static void Run()
        {
            AssetDatabase.Refresh();ConfigureHumanoid(Folder+"/rose-rigged.fbx");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/rose-rigged.fbx"));
            var animator=root.GetComponent<Animator>();
            var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
            var material=new Material(Shader.Find("LighFight/OfficeLit"));material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/skin-albedo.png");material.SetFloat("_Glossiness",.2f);
            string matPath=Folder+"/rose.mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(existing==null)AssetDatabase.CreateAsset(material,matPath);else{EditorUtility.CopySerialized(material,existing);UnityEngine.Object.DestroyImmediate(material);material=existing;}
            skin.sharedMaterials=Enumerable.Repeat(material,skin.sharedMaterials.Length).ToArray();
            if(skin.sharedMesh.vertexCount<40000 || skin.sharedMesh.boneWeights.Any(w=>w.weight0+w.weight1+w.weight2+w.weight3<.99f))throw new Exception("Incomplete Rose skin weights");
            var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(35,-30,0);RenderSettings.ambientLight=new Color(.45f,.45f,.45f);
            var camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(2,1.2f,3);camera.transform.LookAt(new Vector3(0,.9f,0));camera.fieldOfView=34;camera.backgroundColor=new Color(.12f,.14f,.16f);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Folder+"/RoseReview.unity");
            Directory.CreateDirectory("../art/previews/rose-unity");var rt=new RenderTexture(900,1000,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(900,1000);image.ReadPixels(new Rect(0,0,900,1000),0,0);image.Apply();File.WriteAllBytes("../art/previews/rose-unity/rest.png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
            AssetDatabase.SaveAssets();File.WriteAllText("../.tools/rose-unity-audit.txt","Valid Humanoid avatar; normalized weights; material and rest render inspected separately.");
        }
    }
}
