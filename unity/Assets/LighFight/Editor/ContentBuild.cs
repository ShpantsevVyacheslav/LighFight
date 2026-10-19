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
  static AddressableAssetGroup Group(AddressableAssetSettings settings,string name,bool separate,bool local=false){
   var group=settings.FindGroup(name)??settings.CreateGroup(name,false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));
   var schema=group.GetSchema<BundledAssetGroupSchema>();schema.BuildPath.SetVariableByName(settings,local?AddressableAssetSettings.kLocalBuildPath:AddressableAssetSettings.kRemoteBuildPath);schema.LoadPath.SetVariableByName(settings,local?AddressableAssetSettings.kLocalLoadPath:AddressableAssetSettings.kRemoteLoadPath);schema.BundleMode=separate?BundledAssetGroupSchema.BundlePackingMode.PackSeparately:BundledAssetGroupSchema.BundlePackingMode.PackTogether;
   return group;
  }
  // The approved inline roster is read only. This method never regenerates or replaces production fighters.
  public const string EmbeddedStreamPath="Assets/LighFight/Content/EmbeddedGameContent.asset";
  public const string EmbeddedDescriptorPath="Assets/LighFight/Content/EmbeddedContentDescriptor.json";
  public static GameContent ConfigureSelectedContent(GameContent source,AddressableAssetSettings settings,string targetPath=StreamPath)=>Configure(source,settings,targetPath,false);
  public static GameContent ConfigureEmbeddedContent(GameContent source,AddressableAssetSettings settings,string targetPath=EmbeddedStreamPath){var result=Configure(source,settings,targetPath,true);settings.BuildRemoteCatalog=false;EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();return result;}
  static GameContent Configure(GameContent source,AddressableAssetSettings settings,string targetPath,bool local){
   if(source==null||source.streamed||!source.ValidateCatalog(out var error))throw new InvalidOperationException("Approved inline catalog missing or invalid");
   if(AssetDatabase.GetAssetPath(source)==targetPath)throw new InvalidOperationException("Streaming output must differ from approved source");
   var catalog=AssetDatabase.LoadAssetAtPath<GameContent>(targetPath);if(!catalog){catalog=ScriptableObject.CreateInstance<GameContent>();AssetDatabase.CreateAsset(catalog,targetPath);}
   catalog.streamed=true;catalog.fighters=new GameObject[0];catalog.arenas=new GameObject[0];catalog.fighterMetadata=source.fighterMetadata==null?new GameContent.FighterMetadata[0]:(GameContent.FighterMetadata[])source.fighterMetadata.Clone();
   catalog.fighterAddresses=new string[source.FighterCount];catalog.arenaAddresses=new string[source.ArenaCount];
   var fighterGroup=Group(settings,local?"LighFight Embedded Fighters":"LighFight Fighters",true,local);var arenaGroup=Group(settings,local?"LighFight Embedded Arenas":"LighFight Arenas",true,local);var sharedGroup=Group(settings,local?"LighFight Embedded Shared Motions":"LighFight Shared Motions",false,local);var metadataGroup=Group(settings,local?"LighFight Embedded Starter":"LighFight Remote Starter",false,local);
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
   var spectatorGroup=Group(settings,local?"LighFight Embedded Spectators":"LighFight Spectators",true,local);
   foreach(var entry in spectatorGroup.entries.ToArray())if(entry.address.StartsWith("lighfight/spectator/",StringComparison.Ordinal))settings.RemoveAssetEntry(entry.guid);
   foreach(var arena in source.arenas)foreach(var crowd in arena.GetComponentsInChildren<MeetingSpectators>(true))foreach(var candidate in crowd.candidates)foreach(var address in new[]{candidate.address,candidate.smokingAddress}){
    if(string.IsNullOrEmpty(address))continue;
    const string prefix="lighfight/spectator/";if(!address.StartsWith(prefix,StringComparison.Ordinal))throw new InvalidOperationException("Unexpected seated spectator address");
    string guid=address.Substring(prefix.Length);string path=AssetDatabase.GUIDToAssetPath(guid);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var pose=prefab?prefab.GetComponent<SeatedSpectatorPose>():null;
    if(!pose||!pose.seatedPoseVisuallyAccepted||prefab.GetComponentInChildren<Animator>(true)||prefab.GetComponentInChildren<SkinnedMeshRenderer>(true))throw new InvalidOperationException("Unaccepted/non-static seated spectator "+path);
    var entry=settings.CreateOrMoveEntry(guid,spectatorGroup);entry.address=address;entry.SetLabel("lighfight-spectator",true,true);
   }
   // Explicit entries stop common standalone clips/controllers from duplicating into every character bundle.
   foreach(var pair in dependencyUse)if(pair.Value>1&&(pair.Key.EndsWith(".anim")||pair.Key.EndsWith(".controller"))){var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(pair.Key),sharedGroup);entry.address="lighfight/shared/"+entry.guid;entry.SetLabel("lighfight-starter",false);}
   // Remove only the obsolete direct-root entry, never its approved asset or unrelated addressable content.
   string oldGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));if(!string.IsNullOrEmpty(oldGuid))settings.RemoveAssetEntry(oldGuid);
   string targetGuid=AssetDatabase.AssetPathToGUID(targetPath);
   foreach(var group in settings.groups.Where(g=>g!=null))foreach(var obsolete in group.entries.Where(e=>e.address=="lighfight-content"&&e.guid!=targetGuid).ToArray())settings.RemoveAssetEntry(obsolete.guid);
   var root=settings.CreateOrMoveEntry(targetGuid,metadataGroup);root.address="lighfight-content";root.SetLabel("lighfight-starter",true,true);
   if(!catalog.ValidateCatalog(out error))throw new InvalidOperationException(error);
   EditorUtility.SetDirty(catalog);EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();return catalog;
  }
  public static void BuildEmbedded(){
   string version=Environment.GetEnvironmentVariable("LIGHFIGHT_EMBEDDED_VERSION");if(string.IsNullOrWhiteSpace(version))throw new InvalidOperationException("Set LIGHFIGHT_EMBEDDED_VERSION");
   var descriptor=new EmbeddedContentDescriptor{contentVersion=version,unityVersion=Application.unityVersion,platform=EditorUserBuildSettings.activeBuildTarget.ToString()};
   var error=descriptor.Validate(descriptor.platform,Application.unityVersion);if(error!=null)throw new InvalidOperationException(error);
   var settings=AddressableAssetSettingsDefaultObject.GetSettings(true);var catalog=ConfigureEmbeddedContent(AssetDatabase.LoadAssetAtPath<GameContent>(SourcePath),settings);
   foreach(var group in settings.groups.Where(g=>g!=null&&g.Name.StartsWith("LighFight Embedded ",StringComparison.Ordinal))){var schema=group.GetSchema<BundledAssetGroupSchema>();if(schema!=null&&(schema.LoadPath.GetValue(settings).StartsWith("http",StringComparison.OrdinalIgnoreCase)||schema.BuildPath.GetValue(settings).StartsWith("http",StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Local profile paths must be local");}
   if(AssetDatabase.GetDependencies(EmbeddedStreamPath,true).Any(p=>p.EndsWith(".prefab")))throw new InvalidOperationException("Embedded metadata has inline body dependencies");
   settings.BuildRemoteCatalog=false;settings.BuildAddressablesWithPlayerBuild=AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;settings.OverridePlayerVersion=version;ProjectConfigData.GenerateBuildLayout=true;AssetDatabase.SaveAssets();
   AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);if(!string.IsNullOrEmpty(result.Error))throw new InvalidOperationException(result.Error);
   File.WriteAllText(EmbeddedDescriptorPath,JsonUtility.ToJson(descriptor,true));AssetDatabase.Refresh();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Host Bridge").AddComponent<HostBridge>();var bootstrap=new GameObject("Embedded content bootstrap").AddComponent<ContentBootstrap>();bootstrap.useEmbeddedCatalog=true;bootstrap.embeddedDescriptor=AssetDatabase.LoadAssetAtPath<TextAsset>(EmbeddedDescriptorPath);
   const string path="Assets/LighFight/Scenes/EmbeddedBootstrap.unity";Directory.CreateDirectory(Path.GetDirectoryName(path));EditorSceneManager.SaveScene(scene,path);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};AssetDatabase.SaveAssets();
   Debug.Log("AGENT_RESULT embedded Addressables configured/built with local paths; starter contains metadata/portraits only; runtime/player delivery proof required");
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
