using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace LighFight.Editor {
public static class StaticFistAudit {
static string Path="Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/island-finger-candidate.fbx";
public static void Verify() {
 AssetDatabase.Refresh();var importer=(ModelImporter)AssetImporter.GetAtPath(Path);
 importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
 Check(false);Check(false,"Right");CreateScene();
 try { Check(true);throw new Exception("Wrong-bone mutation escaped detection"); }
 catch(Exception e) when(e.Message=="Rigid fist deforms") { File.WriteAllText("../.tools/reports/static-fist-mutation.txt","PASS: rigid fist vertex bound to forearm detected"); }
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
            string materialPath=System.IO.Path.GetDirectoryName(Path)+"/watch-dark.mat";
            string oldMaterialPath=System.IO.Path.GetDirectoryName(Path)+"/hand-skin.mat";
            if( AssetDatabase.LoadAssetAtPath<Material>(materialPath)==null && AssetDatabase.LoadAssetAtPath<Material>(oldMaterialPath)!=null)AssetDatabase.MoveAsset(oldMaterialPath,materialPath);
            var existing=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(existing==null)AssetDatabase.CreateAsset(plain,materialPath);else {UnityEngine.Object.DestroyImmediate(plain);plain=existing;}
            { plain.color=new Color(.16f,.18f,.21f);EditorUtility.SetDirty(plain); }
            var bodyMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/IslandPremium/review.mat");
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
public static void RenderPreview() {
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(System.IO.Path.GetDirectoryName(Path)+"/FingerArtReview.unity");
 if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)throw new Exception("Preview requires graphics device");
 var review=UnityEngine.Object.FindFirstObjectByType<LighFight.Runtime.MeshyArtReview>();
 review.fighter.gameObject.SetActive(false);review.candidate.gameObject.SetActive(true);
 review.candidateClip.SampleAnimation(review.candidate.gameObject,9f/24f);
 review.candidate.GetComponent<LighFight.Runtime.JointCorrectives>().Apply();
 var camera=review.reviewCamera;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.15f,.18f);
 var skin=review.candidate.GetComponentInChildren<SkinnedMeshRenderer>();
 var wrist=skin.bones.First(b=>b.name=="LeftHand");
 int wristIndex=Array.IndexOf(skin.bones,wrist);var deform=wrist.localToWorldMatrix*skin.sharedMesh.bindposes[wristIndex];
 var isolatedMesh=new Mesh();skin.BakeMesh(isolatedMesh);var rest=skin.sharedMesh.vertices;
 for(int sub=0;sub<isolatedMesh.subMeshCount;sub++) {var triangles=isolatedMesh.GetTriangles(sub);var kept=new System.Collections.Generic.List<int>();for(int i=0;i<triangles.Length;i+=3)if(rest[triangles[i]].x<-.52f && rest[triangles[i+1]].x<-.52f && rest[triangles[i+2]].x<-.52f){kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);}isolatedMesh.SetTriangles(kept,sub);}
 var isolated=new GameObject("Isolated wrist preview");isolated.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);isolated.transform.localScale=skin.transform.lossyScale;isolated.AddComponent<MeshFilter>().sharedMesh=isolatedMesh;isolated.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
 var studio=new GameObject("Review fill light");var fill=studio.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.65f;fill.color=new Color(.95f,.97f,1);studio.transform.rotation=Quaternion.Euler(35,150,0);
 foreach(string view in new[]{"unity-whole","unity-fist","unity-front"}) {
 Vector3 focus=view=="unity-whole"?new Vector3(0,.9f,0):wrist.position+deform.MultiplyVector(Vector3.left)*.065f;
 camera.transform.position=focus+(view=="unity-whole"?new Vector3(1.4f,.3f,2.8f):deform.MultiplyVector(view=="unity-front"?new Vector3(-.4f,.05f,.1f):new Vector3(0,.1f,.4f)));camera.transform.LookAt(focus);
 skin.enabled=view=="unity-whole";isolated.SetActive(view!="unity-whole");
 var rt=new RenderTexture(900,700,24);var old=RenderTexture.active;var previous=camera.targetTexture;var tex=new Texture2D(900,700,TextureFormat.RGB24,false);
 try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,900,700),0,0);tex.Apply();
 Directory.CreateDirectory("../art/previews/user-fist-meshy-25/integrated");File.WriteAllBytes("../art/previews/user-fist-meshy-25/integrated/"+view+".png",tex.EncodeToPNG());}
 finally {camera.targetTexture=previous;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
 UnityEngine.Object.DestroyImmediate(isolated);UnityEngine.Object.DestroyImmediate(isolatedMesh);UnityEngine.Object.DestroyImmediate(studio);
}
static void Check(bool mutation,string side="Left") {
 var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Path));var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);skin.sharedMesh=mesh;var baked=new Mesh();
 try {
 if(skin.bones.Length!=24)throw new Exception("Expected original 24-bone rig");
 if(mesh.uv2.Length!=mesh.vertexCount || mesh.colors.Length!=mesh.vertexCount || mesh.subMeshCount!=2)throw new Exception("Skin detail channels/materials missing");
 int hand=Array.FindIndex(skin.bones,b=>b.name==side+"Hand"),forearm=Array.FindIndex(skin.bones,b=>b.name==side+"ForeArm");
 var restVertices=mesh.vertices;var ids=Enumerable.Range(0,mesh.vertexCount).Where(i=>(side=="Left"?restVertices[i].x<-.71f:restVertices[i].x>.71f)).ToArray();if(ids.Length<100)throw new Exception("Missing fist surface");
 var weights=mesh.boneWeights;foreach(int i in ids)if(weights[i].boneIndex0!=hand || weights[i].weight0<.999f)throw new Exception($"Fist is not rigidly attached to wrist: vertex {i}, x {restVertices[i].x}, bone {skin.bones[weights[i].boneIndex0].name}, weight {weights[i].weight0}");
 if(mutation){var w=weights[ids[0]];w.boneIndex0=forearm;weights[ids[0]]=w;mesh.boneWeights=weights;}
 var clip=AssetDatabase.LoadAllAssetsAtPath(Path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));float[] reference=null;float error=0,minAngle=180;
 for(int n=0;n<=24;n++) {
 clip.SampleAnimation(root,clip.length*n/24);skin.BakeMesh(baked);var points=baked.vertices;
 foreach(var p in points)if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.sqrMagnitude))throw new Exception("Invalid geometry");
 var distances=ids.Where((_,i)=>i%Math.Max(1,ids.Length/100)==0).Select(i=>Vector3.Distance(points[i],points[ids[0]])).ToArray();if(reference==null)reference=distances;error=Mathf.Max(error,distances.Select((d,i)=>Mathf.Abs(d-reference[i])).Max());
 var transform=skin.bones[hand].localToWorldMatrix*mesh.bindposes[hand];minAngle=Mathf.Min(minAngle,Vector3.Angle(transform.MultiplyVector(side=="Left"?Vector3.left:Vector3.right),skin.bones[hand].position-skin.bones[forearm].position));
 }
 if(error>.00001f)throw new Exception("Rigid fist deforms");if(side=="Left" && minAngle>1)throw new Exception("Impact wrist is not neutral");
 Directory.CreateDirectory("../.tools/reports");File.WriteAllText("../.tools/reports/static-fist-"+side.ToLowerInvariant()+"-audit.txt",$"24 source bones; 25 jab samples; rigid fist distance error {error} m; minimum wrist angle {minAngle} deg; {mesh.vertexCount} vertices; skin detail channels present. Device and visual acceptance pending.");
 } finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(baked);}
}
}}
