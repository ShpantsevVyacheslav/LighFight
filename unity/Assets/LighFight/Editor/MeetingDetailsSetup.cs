using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using LighFight.Runtime;
namespace LighFight.Editor {
 public static class MeetingDetailsSetup {
  static string Folder => Environment.GetEnvironmentVariable("LF_MEETING_CANDIDATE_FOLDER") ?? "Assets/LighFight/Art/Drafts/MeetingDetailsV1";
  [Serializable] class MaterialRow {public string name,baseTexture,normalTexture,metallicGlossTexture;public float[] color;public float metallic,roughness,normalScale=1;public bool doubleSided;}
  [Serializable] class MaterialRows {public MaterialRow[] materials;}
  static void CreateHoseSocket(GameObject root,Dictionary<string,Material> materials){
   if(!materials.TryGetValue("Hose socket cut cap",out var cap))return;
   foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){
    var renderer=filter.GetComponent<MeshRenderer>();if(!renderer)continue;var mesh=filter.sharedMesh;
    for(int sub=0;sub<Mathf.Min(mesh.subMeshCount,renderer.sharedMaterials.Length);sub++){
     if(renderer.sharedMaterials[sub]!=cap)continue;var ids=new HashSet<int>(mesh.GetIndices(sub));if(ids.Count==0)continue;var vertices=mesh.vertices;var normals=mesh.normals;var center=Vector3.zero;var normal=Vector3.zero;
     foreach(int id in ids){center+=vertices[id];normal+=normals[id];}center/=ids.Count;normal.Normalize();
     var marker=new GameObject("HookahHoseSocket");marker.transform.SetParent(filter.transform,false);marker.transform.localPosition=center;marker.transform.localRotation=Quaternion.LookRotation(normal,Vector3.up);
     var hookah=root.GetComponent<MeetingHookahInteraction>() ?? root.AddComponent<MeetingHookahInteraction>();hookah.outlet=marker.transform;var spectators=root.GetComponent<MeetingSpectators>() ?? root.AddComponent<MeetingSpectators>();spectators.hookah=hookah;
     Debug.Log("MEETING_HOSE_SOCKET_LOCAL "+JsonUtility.ToJson(center)+" parent="+filter.name);return;
    }
   }throw new Exception("Hose cap material present but no mesh socket found");
  }
  static void AddSeatedReview(GameObject root){
   if(Environment.GetEnvironmentVariable("LF_MEETING_SEATED_REVIEW")!="1")return;
   var hookah=root.GetComponent<MeetingHookahInteraction>();
   foreach(var row in new[]{new {index=9,pose="mouthpiece",position=new Vector3(1.2233951f,0,5.75243f),yaw=-90f},new {index=8,pose="idle",position=new Vector3(-1.1843951f,0,6.05603f),yaw=90f}}){
    string path="Assets/LighFight/MeetingSpectatorReview/seated-"+row.index+"-"+row.pose+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new Exception("Missing requested seated review prefab "+path);
    var instance=UnityEngine.Object.Instantiate(prefab,root.transform);instance.name="CAPTURE_ONLY_"+row.index+"_"+row.pose;instance.transform.localPosition=row.position;instance.transform.localRotation=Quaternion.Euler(0,row.yaw,0);
    if(row.pose=="mouthpiece" && hookah)hookah.SetUser(instance.GetComponent<SeatedSpectatorPose>());
   }
  }
  [Serializable] class PromotionGate {public bool sourcePlacement,gameplayLOD,unityActual;}
  public static void PromoteValidated(){
   var report=Path.GetFullPath("../art/drafts/meeting-meshy-v1/unity");var gatePath=report+"/visual-gate.json";if(!File.Exists(gatePath))throw new Exception("Missing actual visual gate receipt");var gate=JsonUtility.FromJson<PromotionGate>(File.ReadAllText(gatePath));if(!gate.sourcePlacement||!gate.gameplayLOD||!gate.unityActual)throw new Exception("Visual gate not accepted");
   string candidatePath=Folder+"/arena.prefab";var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath);if(!candidate)throw new Exception("Missing accepted candidate");var instance=UnityEngine.Object.Instantiate(candidate);int triangles=0;
   try{ArenaLayoutValidator.Validate(instance);triangles=instance.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);if(triangles>250000)throw new Exception("Static arena budget exceeds250k: "+triangles);var npc=instance.GetComponent<MeetingSpectators>();if(!npc||npc.candidates.Length<3||!npc.hookah)throw new Exception("Selected-only spectator install missing");}finally{UnityEngine.Object.DestroyImmediate(instance);}
   const string contentPath="Assets/LighFight/Content/GameContent.asset";var content=AssetDatabase.LoadAssetAtPath<GameContent>(contentPath);if(!content||content.streamed||content.arenas.Length==0)throw new Exception("Expected editable inline content catalog before stream configure");string oldPath=AssetDatabase.GetAssetPath(content.arenas[0]);string backup=report+"/GameContent-before-promotion.asset";if(!File.Exists(backup))File.Copy(contentPath,backup);string beforeGUID=AssetDatabase.AssetPathToGUID(oldPath);
   content.arenas[0]=candidate;EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();File.WriteAllText(report+"/promotion.json","{\"catalogPublished\":true,\"arenaSlot\":0,\"oldArenaPath\":\""+oldPath+"\",\"oldArenaGUID\":\""+beforeGUID+"\",\"candidatePath\":\""+candidatePath+"\",\"environmentTriangles\":"+triangles+",\"validator\":\"PASS\",\"deviceTested\":false,\"phoneBuildUpdated\":false}");Debug.Log("AGENT_RESULT meeting Meshy arena0 promoted "+triangles);
  }
  public static void Run(){
   throw new InvalidOperationException("Review candidate superseded: Meshy prop generation requested; catalog publication disabled.");
  }
  public static void ReviewOnly(){
   var report=Path.GetFullPath("../art/drafts/meeting-meshy-v1/unity");Directory.CreateDirectory(report);
   var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;
   try {
    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    var path=Folder+(File.Exists(Folder+"/meshy-meeting-candidate.fbx")?"/meshy-meeting-candidate.fbx":"/meeting-gameplay.fbx");var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(!importer)throw new Exception("Missing exported meeting FBX");
    importer.meshCompression=ModelImporterMeshCompression.Off;importer.weldVertices=false;importer.isReadable=true;importer.animationType=ModelImporterAnimationType.None;importer.SaveAndReimport();
    var rows=JsonUtility.FromJson<MaterialRows>(File.ReadAllText(Folder+"/materials.json"));var map=new Dictionary<string,Material>();
    foreach(var row in rows.materials){
     var mp=Folder+"/mat-"+map.Count+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);if(!mat){mat=new Material(Shader.Find("LighFight/MeetingEnvironmentPBR"));AssetDatabase.CreateAsset(mat,mp);}mat.shader=Shader.Find("LighFight/MeetingEnvironmentPBR");mat.color=new Color(row.color[0],row.color[1],row.color[2],row.color[3]);mat.SetFloat("_Metallic",row.metallic);mat.SetFloat("_Glossiness",1-row.roughness);mat.SetFloat("_BumpScale",row.normalScale);mat.SetFloat("_Cull",row.doubleSided?0:2);mat.SetFloat("_UseMetallicMap",string.IsNullOrEmpty(row.metallicGlossTexture)?0:1);
     foreach(var field in new[]{"baseTexture","normalTexture","metallicGlossTexture"}){
      string file=field=="baseTexture"?row.baseTexture:field=="normalTexture"?row.normalTexture:row.metallicGlossTexture;if(string.IsNullOrEmpty(file))continue;var tp=Folder+"/"+file;var ti=(TextureImporter)AssetImporter.GetAtPath(tp);ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.sRGBTexture=field=="baseTexture";var platform=ti.GetPlatformTextureSettings("iPhone");platform.name="iPhone";platform.overridden=true;platform.maxTextureSize=2048;platform.format=TextureImporterFormat.ASTC_6x6;platform.compressionQuality=100;ti.SetPlatformTextureSettings(platform);if(field=="normalTexture")ti.textureType=TextureImporterType.NormalMap;ti.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(tp);mat.SetTexture(field=="baseTexture"?"_MainTex":field=="normalTexture"?"_BumpMap":"_MetallicGlossMap",texture);if(field=="normalTexture")mat.EnableKeyword("_NORMALMAP");
     }
     if(row.name=="Glowing coals"){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(.7f,.03f,.002f));}EditorUtility.SetDirty(mat);map[row.name]=mat;
    }
    var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);root=UnityEngine.Object.Instantiate(source);EditorSceneManager.MoveGameObjectToScene(root,scene);root.name="Meeting room photo details v1";
    foreach(var renderer in root.GetComponentsInChildren<Renderer>()){
     renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>map.TryGetValue(m.name.Replace(" (Instance)",""),out var mapped)?mapped:throw new Exception("Unmapped material: "+m.name)).ToArray();renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
    }
    ArenaLayoutValidator.Validate(root);CreateHoseSocket(root,map);
    root.AddComponent<OfficeArena>().UseSharedMaterials();var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/arena.prefab");
    var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");if(!content || content.arenas.Length==0)throw new Exception("Missing arena catalog");// Publication disabled: keep generated prefab as an isolated review candidate.
    int environmentTriangles=root.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);AssetDatabase.SaveAssets();AddSeatedReview(root); // Added strictly after prefab persistence: capture-only, no NPC catalog publication.
    var lightObject=new GameObject("MeetingReviewLight");EditorSceneManager.MoveGameObjectToScene(lightObject,scene);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;lightObject.transform.rotation=Quaternion.Euler(45,-25,0);
    RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.58f,.60f,.63f);
    var co=new GameObject("MeetingReviewCamera");EditorSceneManager.MoveGameObjectToScene(co,scene);var camera=co.AddComponent<Camera>();camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.17f,.2f);camera.fieldOfView=55;camera.nearClipPlane=.05f;camera.farClipPlane=40;co.transform.position=new Vector3(0,2.1f,-6.4f);co.transform.LookAt(new Vector3(0,1.2f,4.1f));var rt=new RenderTexture(1280,760,24);camera.targetTexture=rt;ShaderUtil.allowAsyncCompilation=false;
    root.GetComponent<MeetingHookahInteraction>()?.SampleSmoke(1.2f,camera);for(int i=0;i<3;i++)camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,760,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,760),0,0);image.Apply();File.WriteAllBytes(report+"/gameplay.png",image.EncodeToPNG());co.transform.position=new Vector3(2.1f,1.9f,2.8f);co.transform.LookAt(new Vector3(0,1.05f,5.35f));root.GetComponent<MeetingHookahInteraction>()?.SampleSmoke(1.2f,camera);for(int i=0;i<3;i++)camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,760),0,0);image.Apply();File.WriteAllBytes(report+"/table-closeup.png",image.EncodeToPNG());RenderTexture.active=prior;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
    int tris=root.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);File.WriteAllText(report+"/result.json","{\"ok\":true,\"arenaSlot\":null,\"catalogPublished\":false,\"validator\":\"PASS\",\"environmentTriangles\":"+environmentTriangles+",\"captureTrianglesIncludingNPC\":"+tris+",\"renderers\":"+root.GetComponentsInChildren<Renderer>().Length+",\"deviceTested\":false}");Debug.Log("AGENT_RESULT meeting-details validator PASS review-only no catalog triangles="+tris);
   } finally {if(root)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
  }
 }
}
