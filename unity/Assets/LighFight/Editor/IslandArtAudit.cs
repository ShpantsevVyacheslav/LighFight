using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LighFight.Editor
{
    // Import audit only: never promotes an unrigged mesh to the playable roster.
    public static class IslandArtAudit
    {
        const string Folder = "Assets/LighFight/Art/Source/Island";
        public static void Run()
            => Audit(Directory.GetFiles(Folder, "*.fbx").Single(), false);
        public static void RunRig()
            => Audit("Assets/LighFight/Art/Drafts/Island/island-rig-draft.fbx", true);
        static void Audit(string modelPath, bool rig)
        {
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.isReadable = true;
            if (rig) importer.animationType = ModelImporterAnimationType.Human;
            importer.SaveAndReimport();
            foreach (string path in Directory.GetFiles(Folder, "*.png"))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                bool normal = path.EndsWith("_normal.png");
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = path.EndsWith("_texture.png");
                ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new Exception("FBX contains no rendered geometry");
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            var originalSize = bounds.size;
            model.transform.localScale *= 1.7f / bounds.size.y;
            bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            string colorPath = Directory.GetFiles(Folder, "*_texture.png").Single();
            var material = new Material(Shader.Find("Standard"));
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Directory.GetFiles(Folder, "*_normal.png").Single()));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Glossiness", .25f);
            foreach (var r in renderers) r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
            var light = new GameObject("Key").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(35, -35, 0);
            RenderSettings.ambientLight = new Color(.42f, .42f, .42f);
            var camera = new GameObject("Audit Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f, .065f, .085f);
            camera.fieldOfView = 32;
            string output = Path.GetFullPath("../art/previews"); Directory.CreateDirectory(output);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 90 * Mathf.Deg2Rad;
                camera.transform.position = new Vector3(Mathf.Sin(angle)*3.7f, 1.05f, Mathf.Cos(angle)*3.7f);
                camera.transform.LookAt(new Vector3(0, .88f, 0));
                var rt = new RenderTexture(900, 1100, 24); camera.targetTexture = rt; camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(900, 1100, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0,0,900,1100),0,0); image.Apply();
                File.WriteAllBytes(Path.Combine(output,(rig?"island-rig-rest-":"island-unity-")+i+".png"),image.EncodeToPNG());
                RenderTexture.active = null; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
            }
            var meshes = model.GetComponentsInChildren<MeshFilter>().Select(x=>x.sharedMesh)
                .Concat(model.GetComponentsInChildren<SkinnedMeshRenderer>().Select(x=>x.sharedMesh)).Where(x=>x!=null).Distinct().ToArray();
            int bones = model.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(x=>x.bones.Length);
            string report = "Original bounds: "+originalSize+"\nNormalized height: 1.7m\nVertices: "+meshes.Sum(x=>x.vertexCount)+
                "\nTriangles: "+meshes.Sum(x=>x.triangles.Length/3)+"\nSkin bones: "+bones+
                "\nAnimation clips: "+AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Count()+
                "\nPlayable prefab: NOT created; rig, weighting and animations require validation.\n";
            File.WriteAllText(Path.Combine(output,rig?"island-rig-import-audit.txt":"island-import-audit.txt"),report); Debug.Log(report);
            if (rig)
            {
                if (bones == 0) throw new Exception("Rig draft has no skin bones");
                var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
                File.WriteAllText(Path.Combine(output,"island-rig-audit.txt"), report+"Humanoid avatar valid: "+(avatar!=null && avatar.isValid && avatar.isHuman)+"\n"+
                    string.Join("\n",model.GetComponentsInChildren<Transform>().Select(x=>x.name)));
                foreach(var animator in model.GetComponentsInChildren<Animator>()) animator.enabled = false;
                int moved = 0;
                foreach (var t in model.GetComponentsInChildren<Transform>())
                    if (t.name.EndsWith("LowerArm")) { t.localRotation *= Quaternion.Euler(75,0,0); moved++; }
                if (moved != 2) throw new Exception("Expected both lower arm bones, found "+moved);
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    // CPU skinning avoids editor-frame/GPU update timing in batch renders.
                    var baked = UnityEngine.Object.Instantiate(skin.sharedMesh);
                    var vertices = baked.vertices;
                    var weights = baked.boneWeights;
                    var matrices = skin.bones.Select((b,i)=>b.localToWorldMatrix*baked.bindposes[i]).ToArray();
                    for(int v=0;v<vertices.Length;v++)
                    {
                        var w=weights[v]; var p=vertices[v];
                        vertices[v]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+
                            matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+
                            matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+
                            matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
                    }
                    baked.vertices=vertices; baked.RecalculateBounds(); baked.RecalculateNormals();
                    Debug.Log("Deformed world bounds: "+baked.bounds);
                    var preview = new GameObject("Deformation preview");
                    preview.AddComponent<MeshFilter>().sharedMesh = baked;
                    preview.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                    skin.enabled = false;
                }
                camera.transform.position = new Vector3(0,1.05f,3.7f);
                camera.transform.LookAt(new Vector3(0,.88f,0));
                var rt = new RenderTexture(900,1100,24); camera.targetTexture = rt; camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(900,1100,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,900,1100),0,0); image.Apply();
                File.WriteAllBytes(Path.Combine(output,"island-rig-elbows.png"),image.EncodeToPNG());
                RenderTexture.active = null; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
            }
            UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
