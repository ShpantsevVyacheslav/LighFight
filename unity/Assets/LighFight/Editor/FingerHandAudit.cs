using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor
{
    public static class FingerHandAudit
    {
        static string Path="Assets/LighFight/Art/Drafts/IslandFingers/island-finger-candidate.fbx";
        public static void VerifyMakeHuman() { Path="Assets/LighFight/Art/Drafts/IslandMakeHumanHands/island-finger-candidate.fbx";Verify(); }
        public static void VerifyAnatomyV2() { Path="Assets/LighFight/Art/Drafts/IslandFingersV2/island-finger-candidate.fbx";Verify(); }
        public static void Verify()
        {
            AssetDatabase.Refresh();
            var importer=(ModelImporter)AssetImporter.GetAtPath(Path);
            importer.animationType=ModelImporterAnimationType.Generic;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.importTangents=ModelImporterTangents.CalculateMikk;
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
            string materialPath=System.IO.Path.GetDirectoryName(Path)+(Path.Contains("MakeHuman")?"/watch-dark.mat":"/hand-skin.mat");
            string oldMaterialPath=System.IO.Path.GetDirectoryName(Path)+"/hand-skin.mat";
            if(Path.Contains("MakeHuman") && AssetDatabase.LoadAssetAtPath<Material>(materialPath)==null && AssetDatabase.LoadAssetAtPath<Material>(oldMaterialPath)!=null)AssetDatabase.MoveAsset(oldMaterialPath,materialPath);
            var existing=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(existing==null)AssetDatabase.CreateAsset(plain,materialPath);else {UnityEngine.Object.DestroyImmediate(plain);plain=existing;}
            if(Path.Contains("MakeHuman")) { plain.color=new Color(.16f,.18f,.21f);EditorUtility.SetDirty(plain); }
            var bodyMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/IslandPremium/review.mat");
            if(Path.Contains("MakeHuman"))
            {
                string folder=System.IO.Path.GetDirectoryName(Path);
                var shader=Shader.Find("LighFight/CharacterSkin");if(shader==null)throw new Exception("Missing character skin shader");
                if(ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new Exception("Character skin shader compilation failed");
                string normalPath=folder+"/normal.png";var normalImporter=(TextureImporter)AssetImporter.GetAtPath(normalPath);
                normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.maxTextureSize=1024;normalImporter.SaveAndReimport();
                string detailNormalPath=folder+"/detail-normal.png";
                var detailNormalImporter=(TextureImporter)AssetImporter.GetAtPath(detailNormalPath);
                if(detailNormalImporter==null)throw new Exception("Missing cropped body skin normal detail");
                detailNormalImporter.textureType=TextureImporterType.NormalMap;detailNormalImporter.maxTextureSize=512;detailNormalImporter.SaveAndReimport();
                string skinPath=folder+"/character-skin.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(skinPath);
                if(material==null) { material=new Material(shader);AssetDatabase.CreateAsset(material,skinPath); }
                material.shader=shader;material.mainTexture=bodyMaterial.mainTexture;material.SetTexture("_DetailSkinTex",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/detail-skin.png"));material.color=Color.white;material.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));material.SetTexture("_DetailNormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(detailNormalPath));material.SetFloat("_NormalStrength",.25f);material.SetFloat("_Glossiness",.18f);EditorUtility.SetDirty(material);bodyMaterial=material;
            }
            skin.sharedMaterials=new[]{bodyMaterial,plain};
            var correction=model.AddComponent<LighFight.Runtime.JointCorrectives>();correction.referenceAngles=new[]{135.99724f,111.25128f,73.56264f};
            review.candidate=animator;review.candidateClip=AssetDatabase.LoadAllAssetsAtPath(Path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
            model.SetActive(false);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),System.IO.Path.GetDirectoryName(Path)+"/FingerArtReview.unity");
            AssetDatabase.SaveAssets();
        }
        static void Check(bool mutation)
        {
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
            var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
            var clip=AssetDatabase.LoadAllAssetsAtPath(Path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
            var a=new Mesh();var b=new Mesh();float difference=0;float minimumWristAngle=180;
            int handBone=Array.FindIndex(skin.bones,bone=>bone.name=="LeftHand");int forearmBone=Array.FindIndex(skin.bones,bone=>bone.name=="LeftForeArm");
            try
            {
                int expectedBones=Path.Contains("MakeHuman")?62:52;
                if(skin.bones.Length!=expectedBones)throw new Exception("Unexpected deform bone count");
                if(Path.Contains("MakeHuman"))
                {
                    if(skin.sharedMesh.uv2.Length!=skin.sharedMesh.vertexCount)throw new Exception("Hand detail UV channel missing");
                    var colors=skin.sharedMesh.colors;
                    if(colors.Length!=skin.sharedMesh.vertexCount || colors.Max(c=>c.r)<.99f || colors.Min(c=>c.r)>.01f)throw new Exception("Hand skin blend mask missing");
                    if(skin.sharedMesh.subMeshCount!=2)throw new Exception("Expected unified skin and rigid watch materials");
                }
                int shape=Enumerable.Range(0,skin.sharedMesh.blendShapeCount).First(i=>skin.sharedMesh.GetBlendShapeName(i).Contains("ClosedFists"));
                var dv=new Vector3[skin.sharedMesh.vertexCount];skin.sharedMesh.GetBlendShapeFrameVertices(shape,0,dv,null,null);
                float wristDelta=0;var restVertices=skin.sharedMesh.vertices;
                for(int i=0;i<dv.Length;i++)
                {
                    var p=restVertices[i];
                    // Source wrist region, before the rebuilt palm, must remain unaffected by fist closure.
                    if(Mathf.Abs(p.x)>.55f && Mathf.Abs(p.x)<.65f)wristDelta=Mathf.Max(wristDelta,dv[i].magnitude);
                }
                if(wristDelta>.00001f)throw new Exception("Fist shape moves source wrist");
                for(int n=0;n<=24;n++)
                {
                    clip.SampleAnimation(root,clip.length*n/24);
                    if(Path.Contains("MakeHuman"))
                    {
                        var deformation=skin.bones[handBone].localToWorldMatrix*skin.sharedMesh.bindposes[handBone];
                        float sign=Mathf.Sign(skin.sharedMesh.bindposes[handBone].inverse.MultiplyPoint3x4(Vector3.zero).x);
                        var direction=skin.bones[handBone].position-skin.bones[forearmBone].position;
                        minimumWristAngle=Mathf.Min(minimumWristAngle,Vector3.Angle(deformation.MultiplyVector(Vector3.right*sign),direction));
                    }
                    skin.SetBlendShapeWeight(shape,0);skin.BakeMesh(a);
                    skin.SetBlendShapeWeight(shape,mutation?0:100);skin.BakeMesh(b);
                    var av=a.vertices;var bv=b.vertices;
                    for(int i=0;i<bv.Length;i++)
                    {
                        var p=bv[i];if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.sqrMagnitude))throw new Exception("Invalid skin geometry");
                        difference=Mathf.Max(difference,Vector3.Distance(av[i],p));
                    }
                }
                if(Path.Contains("MakeHuman") && minimumWristAngle>1)throw new Exception("Neutral impact wrist was not exported");
                if(difference<.02f)throw new Exception("Closed fist shape has no effect");
                Directory.CreateDirectory("../.tools/reports");
                File.WriteAllText("../.tools/reports/finger-hand-audit.txt",$"{expectedBones} bones; {expectedBones-24} added finger/palm bones; 25 jab poses; closure displacement {difference} m; wrist shape displacement {wristDelta} m; finite vertices; minimum sampled wrist angle {minimumWristAngle} deg; clip length {clip.length} s. Visual anatomy/device acceptance pending.");
            }
            finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
        }
    }
}
