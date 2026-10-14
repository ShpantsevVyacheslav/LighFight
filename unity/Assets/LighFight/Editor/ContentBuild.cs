using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;
using LighFight.Runtime;
using LighFight.Core;
namespace LighFight.Editor {
 public static class ContentBuild {
  const string SourcePath="Assets/LighFight/Content/GameContent.asset";
  public const string StreamPath="Assets/LighFight/Content/StreamingGameContent.asset";
  static AddressableAssetGroup Group(AddressableAssetSettings settings,string name,bool separate){
   var group=settings.FindGroup(name)??settings.CreateGroup(name,false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));
   var schema=group.GetSchema<BundledAssetGroupSchema>();schema.BuildPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteBuildPath);schema.LoadPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteLoadPath);schema.BundleMode=separate?BundledAssetGroupSchema.BundlePackingMode.PackSeparately:BundledAssetGroupSchema.BundlePackingMode.PackTogether;
   return group;
  }
  // The approved inline roster is read only. This method never regenerates or replaces production fighters.
  public static GameContent ConfigureSelectedContent(GameContent source,AddressableAssetSettings settings,string targetPath=StreamPath){
   if(source==null||source.streamed||!source.ValidateCatalog(out var error))throw new InvalidOperationException("Approved inline catalog missing or invalid");
   if(AssetDatabase.GetAssetPath(source)==targetPath)throw new InvalidOperationException("Streaming output must differ from approved source");
   var catalog=AssetDatabase.LoadAssetAtPath<GameContent>(targetPath);if(!catalog){catalog=ScriptableObject.CreateInstance<GameContent>();AssetDatabase.CreateAsset(catalog,targetPath);}
   catalog.streamed=true;catalog.fighters=new GameObject[0];catalog.arenas=new GameObject[0];catalog.fighterMetadata=source.fighterMetadata==null?new GameContent.FighterMetadata[0]:(GameContent.FighterMetadata[])source.fighterMetadata.Clone();
   catalog.fighterAddresses=new string[source.FighterCount];catalog.arenaAddresses=new string[source.ArenaCount];
   var fighterGroup=Group(settings,"LighFight Fighters",true);var arenaGroup=Group(settings,"LighFight Arenas",true);var sharedGroup=Group(settings,"LighFight Shared Motions",false);var metadataGroup=Group(settings,"LighFight Remote Starter",false);
   // Remove stale entries owned by this builder; preserve unrelated user addresses in these groups.
   foreach(var pair in new[]{new KeyValuePair<AddressableAssetGroup,string>(fighterGroup,"lighfight/fighter/"),new KeyValuePair<AddressableAssetGroup,string>(arenaGroup,"lighfight/arena/"),new KeyValuePair<AddressableAssetGroup,string>(sharedGroup,"lighfight/shared/")})foreach(var entry in pair.Key.entries.ToArray())if(entry.address.StartsWith(pair.Value,StringComparison.Ordinal))settings.RemoveAssetEntry(entry.guid);
   var dependencyUse=new Dictionary<string,int>();
   for(int i=0;i<source.FighterCount;i++){
    string path=AssetDatabase.GetAssetPath(source.fighters[i]);if(string.IsNullOrEmpty(path))throw new InvalidOperationException("Fighter must be a saved prefab");
    string key="lighfight/fighter/"+AssetDatabase.AssetPathToGUID(path);catalog.fighterAddresses[i]=key;var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),fighterGroup);entry.address=key;entry.SetLabel("lighfight-starter",false);entry.SetLabel("lighfight-fighter",true,true);
    foreach(var dependency in AssetDatabase.GetDependencies(path,true).Distinct()){if(dependencyUse.ContainsKey(dependency))dependencyUse[dependency]++;else dependencyUse[dependency]=1;}
   }
   for(int i=0;i<source.ArenaCount;i++){
    string path=AssetDatabase.GetAssetPath(source.arenas[i]);if(string.IsNullOrEmpty(path))throw new InvalidOperationException("Arena must be a saved prefab");
    string key="lighfight/arena/"+AssetDatabase.AssetPathToGUID(path);catalog.arenaAddresses[i]=key;var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),arenaGroup);entry.address=key;entry.SetLabel("lighfight-starter",false);entry.SetLabel("lighfight-arena",true,true);
   }
   // Explicit entries stop common standalone clips/controllers from duplicating into every character bundle.
   foreach(var pair in dependencyUse)if(pair.Value>1&&(pair.Key.EndsWith(".anim")||pair.Key.EndsWith(".controller"))){var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(pair.Key),sharedGroup);entry.address="lighfight/shared/"+entry.guid;entry.SetLabel("lighfight-starter",false);}
   // Remove only the obsolete direct-root entry, never its approved asset or unrelated addressable content.
   string oldGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));if(!string.IsNullOrEmpty(oldGuid))settings.RemoveAssetEntry(oldGuid);
   var root=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(targetPath),metadataGroup);root.address="lighfight-content";root.SetLabel("lighfight-starter",true,true);
   if(!catalog.ValidateCatalog(out error))throw new InvalidOperationException(error);
   EditorUtility.SetDirty(catalog);EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();return catalog;
  }
  public static void Build(){
   string url=Environment.GetEnvironmentVariable("LIGHFIGHT_CONTENT_URL");if(string.IsNullOrEmpty(url)||!Uri.TryCreate(url,UriKind.Absolute,out _))throw new InvalidOperationException("Set LIGHFIGHT_CONTENT_URL to immutable platform content directory");
   string version=Environment.GetEnvironmentVariable("LIGHFIGHT_CONTENT_VERSION")??"selected-1";
   var check=new ContentManifest{minimumPlayerVersion=ContentManifest.CurrentPlayerVersion,contentVersion=version,unityVersion=Application.unityVersion,platform=EditorUserBuildSettings.activeBuildTarget.ToString(),catalogUrl=url};if(check.Validate(check.platform,Application.unityVersion,true)!=null)throw new InvalidOperationException("Invalid content URL/version");
   string output=Path.GetFullPath("../.tools/content/"+EditorUserBuildSettings.activeBuildTarget+"/"+version);if(Directory.Exists(output))throw new InvalidOperationException("Content version already exists; choose a new immutable version");
   var settings=AddressableAssetSettingsDefaultObject.GetSettings(true);ConfigureSelectedContent(AssetDatabase.LoadAssetAtPath<GameContent>(SourcePath),settings);
   settings.profileSettings.SetValue(settings.activeProfileId,AddressableAssetSettings.kRemoteBuildPath,"[UnityEngine.Application.dataPath]/../../.tools/content/[BuildTarget]/"+version);settings.profileSettings.SetValue(settings.activeProfileId,AddressableAssetSettings.kRemoteLoadPath,url.TrimEnd('/'));
   settings.BuildRemoteCatalog=true;settings.BuildAddressablesWithPlayerBuild=AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;settings.OverridePlayerVersion=version;settings.RemoteCatalogBuildPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteBuildPath);settings.RemoteCatalogLoadPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteLoadPath);AssetDatabase.SaveAssets();
   ProjectConfigData.GenerateBuildLayout=true;
   AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);if(!string.IsNullOrEmpty(result.Error))throw new InvalidOperationException(result.Error);
   string catalog=Directory.GetFiles(output,"catalog*.*").Single(p=>p.EndsWith(".json")||p.EndsWith(".bin"));var manifest=new ContentManifest{minimumPlayerVersion=ContentManifest.CurrentPlayerVersion,contentVersion=version,unityVersion=Application.unityVersion,platform=EditorUserBuildSettings.activeBuildTarget.ToString(),catalogUrl=url.TrimEnd('/')+"/"+Path.GetFileName(catalog),downloadBytes=Directory.GetFiles(output,"lighfightremotestarter*.bundle",SearchOption.AllDirectories).Sum(p=>new FileInfo(p).Length)};File.WriteAllText(Path.Combine(output,"manifest.json"),JsonUtility.ToJson(manifest,true));Debug.Log("LighFight selected content build: "+output);
  }
  public static void Bootstrap(){
   string url=Environment.GetEnvironmentVariable("LIGHFIGHT_MANIFEST_URL");if(string.IsNullOrWhiteSpace(url))throw new InvalidOperationException("Set LIGHFIGHT_MANIFEST_URL");
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Host Bridge").AddComponent<HostBridge>();new GameObject("Content Bootstrap").AddComponent<ContentBootstrap>().manifestUrl=url;
   const string path="Assets/LighFight/Scenes/Bootstrap.unity";EditorSceneManager.SaveScene(scene,path);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};
  }
 }
}
