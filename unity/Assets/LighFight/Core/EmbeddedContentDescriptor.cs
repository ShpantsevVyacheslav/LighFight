using System;using System.Text.RegularExpressions;
namespace LighFight.Core {
[Serializable]public sealed class EmbeddedContentDescriptor {
 public int schemaVersion=1,minimumPlayerVersion=ContentManifest.CurrentPlayerVersion;
 public string contentVersion,unityVersion,platform;
 public string starterLabel="lighfight-starter",assetAddress="lighfight-content";
 public long downloadBytes;
 public string Validate(string currentPlatform,string currentUnity){
  if(schemaVersion!=1||minimumPlayerVersion<1||minimumPlayerVersion>ContentManifest.CurrentPlayerVersion)return "incompatible_player";
  if(platform!=currentPlatform||unityVersion!=currentUnity)return "incompatible_platform";
  if(string.IsNullOrEmpty(contentVersion)||!Regex.IsMatch(contentVersion,@"\A[a-zA-Z0-9._-]{1,64}\z"))return "invalid_version";
  if(starterLabel!="lighfight-starter"||assetAddress!="lighfight-content")return "invalid_content";
  return downloadBytes==0?null:"invalid_size";
 }
}}
