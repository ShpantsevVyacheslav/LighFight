using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;
using LighFight.Runtime;
using LighFight.Core;
namespace LighFight.Editor
{
    public static class ContentBuild
    {
        const string Folder="Assets/LighFight/Content";
        private static GameObject Save(GameObject obj,string name)
        {
            var renderers=obj.GetComponentsInChildren<Renderer>();int index=0;
            foreach(var material in renderers.Select(r=>r.sharedMaterial).Distinct().ToArray())
            {
                string path=Folder+"/"+name+"-material-"+(index++)+".mat";
                var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(saved==null){AssetDatabase.CreateAsset(material,path);saved=material;}
                else {EditorUtility.CopySerialized(material,saved);EditorUtility.SetDirty(saved);}
                foreach(var renderer in renderers)if(renderer.sharedMaterial==material)renderer.sharedMaterial=saved;
            }
            obj.GetComponent<FighterView>()?.UseSharedMaterials();obj.GetComponent<OfficeArena>()?.UseSharedMaterials();
            var prefab=PrefabUtility.SaveAsPrefabAsset(obj,Folder+"/"+name+".prefab");UnityEngine.Object.DestroyImmediate(obj);return prefab;
        }
        public static void Build()
        {
            string url=Environment.GetEnvironmentVariable("LIGHFIGHT_CONTENT_URL");
            if(string.IsNullOrEmpty(url)||!Uri.TryCreate(url,UriKind.Absolute,out _))throw new InvalidOperationException("Set LIGHFIGHT_CONTENT_URL to immutable platform content directory");
            string version=Environment.GetEnvironmentVariable("LIGHFIGHT_CONTENT_VERSION")??"starter-1";
            var check=new ContentManifest{contentVersion=version,unityVersion=Application.unityVersion,platform=EditorUserBuildSettings.activeBuildTarget.ToString(),catalogUrl=url};
            if(check.Validate(check.platform,Application.unityVersion,true)!=null)throw new InvalidOperationException("Invalid content URL/version");
            string output=Path.GetFullPath("../.tools/content/"+EditorUserBuildSettings.activeBuildTarget+"/"+version);
            if(File.Exists(Path.Combine(output,"manifest.json")))throw new InvalidOperationException("Content version already built; choose a new immutable version");
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var content=AssetDatabase.LoadAssetAtPath<GameContent>(Folder+"/GameContent.asset");
            if(content==null){content=ScriptableObject.CreateInstance<GameContent>();AssetDatabase.CreateAsset(content,Folder+"/GameContent.asset");}
            content.fighters=new GameObject[8];content.arenas=new GameObject[5];
            Color[] colors={new Color(.5f,.65f,.25f),new Color(.3f,.18f,.5f),new Color(.05f,.3f,.32f),new Color(.68f,.58f,.36f),new Color(.85f,.42f,.55f),new Color(.26f,.32f,.17f),new Color(.1f,.18f,.32f),new Color(.05f,.25f,.65f)};
            for(int i=0;i<8;i++)
            {
                var custom=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-"+i+".prefab");
                if(custom!=null && (custom.GetComponent<FighterView>()==null || !custom.GetComponent<FighterView>().IsReady))throw new InvalidOperationException("Invalid artist rig "+i);
                content.fighters[i]=custom!=null?custom:Save(FighterView.Create("Fighter "+i,colors[i],i==0||i==1||i==4).gameObject,"fighter-"+i);
            }
            for(int i=0;i<5;i++)
            {
                var custom=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Arenas/arena-"+i+".prefab");
                if(custom!=null && custom.GetComponent<OfficeArena>()==null)throw new InvalidOperationException("Invalid artist arena "+i);
                content.arenas[i]=custom!=null?custom:Save(OfficeArena.Create((OfficeLocation)i).gameObject,"arena-"+i);
            }
            EditorUtility.SetDirty(content);
            var settings=AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group=settings.FindGroup("LighFight Remote Starter")??settings.CreateGroup("LighFight Remote Starter",false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));
            var schema=group.GetSchema<BundledAssetGroupSchema>();schema.BuildPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteBuildPath);schema.LoadPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteLoadPath);
            settings.profileSettings.SetValue(settings.activeProfileId,AddressableAssetSettings.kRemoteBuildPath,"[UnityEngine.Application.dataPath]/../../.tools/content/[BuildTarget]/"+version);
            settings.profileSettings.SetValue(settings.activeProfileId,AddressableAssetSettings.kRemoteLoadPath,url.TrimEnd('/'));
            settings.BuildRemoteCatalog=true;settings.BuildAddressablesWithPlayerBuild=AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;settings.OverridePlayerVersion=version;settings.RemoteCatalogBuildPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteBuildPath);settings.RemoteCatalogLoadPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteLoadPath);
            var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(Folder+"/GameContent.asset"),group);entry.address="lighfight-content";entry.SetLabel("lighfight-starter",true,true);
            var used=AssetDatabase.GetDependencies(Folder+"/GameContent.asset",true);
            foreach(var material in Directory.GetFiles(Folder,"*-material*.mat"))
                if(!used.Contains(material.Replace('\\','/')))AssetDatabase.DeleteAsset(material);
            AssetDatabase.SaveAssets();AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if(!string.IsNullOrEmpty(result.Error))throw new InvalidOperationException(result.Error);
            string catalog=Directory.GetFiles(output,"catalog*.*").Single(p=>p.EndsWith(".json")||p.EndsWith(".bin"));
            var manifest=new ContentManifest {contentVersion=version,unityVersion=Application.unityVersion,platform=EditorUserBuildSettings.activeBuildTarget.ToString(),catalogUrl=url.TrimEnd('/')+"/"+Path.GetFileName(catalog),downloadBytes=Directory.GetFiles(output,"*.bundle",SearchOption.AllDirectories).Sum(p=>new FileInfo(p).Length)};
            File.WriteAllText(Path.Combine(output,"manifest.json"),JsonUtility.ToJson(manifest,true));
            Debug.Log("LighFight content build: "+output);
        }
        public static void Bootstrap()
        {
            string url=Environment.GetEnvironmentVariable("LIGHFIGHT_MANIFEST_URL");
            if(string.IsNullOrWhiteSpace(url))throw new InvalidOperationException("Set LIGHFIGHT_MANIFEST_URL");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Host Bridge").AddComponent<HostBridge>();
            new GameObject("Content Bootstrap").AddComponent<ContentBootstrap>().manifestUrl=url;
            const string path="Assets/LighFight/Scenes/Bootstrap.unity";
            EditorSceneManager.SaveScene(scene,path);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};
        }
    }
}
