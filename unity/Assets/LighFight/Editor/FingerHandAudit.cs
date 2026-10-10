using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor
{
    public static class FingerHandAudit
    {
        const string Path="Assets/LighFight/Art/Drafts/IslandFingers/island-finger-candidate.fbx";
        public static void Verify()
        {
            AssetDatabase.Refresh();
            var importer=(ModelImporter)AssetImporter.GetAtPath(Path);
            importer.animationType=ModelImporterAnimationType.Generic;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            Check(false);
            CreateScene();
            try { Check(true);throw new Exception("Mutation escaped detection"); }
            catch(Exception e) when(e.Message=="Closed fist shape has no effect")
            { File.WriteAllText("../.tools/reports/finger-hand-mutation.txt","PASS: zeroed fist morph rejected"); }
        }
        static void CreateScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(MeshyReviewSetup.ScenePath);
            var review=UnityEngine.Object.FindFirstObjectByType<LighFight.Runtime.MeshyArtReview>();
            if(review.candidate!=null)UnityEngine.Object.DestroyImmediate(review.candidate.gameObject);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
            var animator=model.GetComponent<Animator>()??model.AddComponent<Animator>();
            var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
            var plain=new Material(Shader.Find("LighFight/OfficeLit"));plain.color=new Color(.78f,.63f,.57f);
            const string materialPath="Assets/LighFight/Art/Drafts/IslandFingers/hand-skin.mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(existing==null)AssetDatabase.CreateAsset(plain,materialPath);else {UnityEngine.Object.DestroyImmediate(plain);plain=existing;}
            var bodyMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/IslandPremium/review.mat");
            skin.sharedMaterials=new[]{bodyMaterial,plain};
            var correction=model.AddComponent<LighFight.Runtime.JointCorrectives>();correction.referenceAngles=new[]{135.99724f,111.25128f,73.56264f};
            review.candidate=animator;review.candidateClip=AssetDatabase.LoadAllAssetsAtPath(Path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
            model.SetActive(false);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/LighFight/Art/Drafts/IslandFingers/FingerArtReview.unity");
            AssetDatabase.SaveAssets();
        }
        static void Check(bool mutation)
        {
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
            var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
            var clip=AssetDatabase.LoadAllAssetsAtPath(Path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
            var a=new Mesh();var b=new Mesh();float difference=0;
            try
            {
                if(skin.bones.Length!=52)throw new Exception("Expected 52 deform bones");
                int shape=Enumerable.Range(0,skin.sharedMesh.blendShapeCount).First(i=>skin.sharedMesh.GetBlendShapeName(i).Contains("ClosedFists"));
                var dv=new Vector3[skin.sharedMesh.vertexCount];skin.sharedMesh.GetBlendShapeFrameVertices(shape,0,dv,null,null);
                float wristDelta=0;var restVertices=skin.sharedMesh.vertices;
                for(int i=0;i<dv.Length;i++)
                {
                    var p=restVertices[i];
                    // Source wrist region, before the rebuilt palm, must remain unaffected by fist closure.
                    if(Mathf.Abs(p.x)>.64f && Mathf.Abs(p.x)<.69f)wristDelta=Mathf.Max(wristDelta,dv[i].magnitude);
                }
                if(wristDelta>.00001f)throw new Exception("Fist shape moves source wrist");
                for(int n=0;n<=24;n++)
                {
                    clip.SampleAnimation(root,clip.length*n/24);
                    skin.SetBlendShapeWeight(shape,0);skin.BakeMesh(a);
                    skin.SetBlendShapeWeight(shape,mutation?0:100);skin.BakeMesh(b);
                    var av=a.vertices;var bv=b.vertices;
                    for(int i=0;i<bv.Length;i++)
                    {
                        var p=bv[i];if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.sqrMagnitude))throw new Exception("Invalid skin geometry");
                        difference=Mathf.Max(difference,Vector3.Distance(av[i],p));
                    }
                }
                if(difference<.02f)throw new Exception("Closed fist shape has no effect");
                Directory.CreateDirectory("../.tools/reports");
                File.WriteAllText("../.tools/reports/finger-hand-audit.txt",$"52 bones; 28 added finger bones; 25 jab poses; closure displacement {difference} m; wrist shape displacement {wristDelta} m; finite vertices. Visual anatomy/device acceptance pending.");
            }
            finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
        }
    }
}
