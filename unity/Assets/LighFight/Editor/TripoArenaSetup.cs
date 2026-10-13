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
 public static class TripoArenaSetup {
  public const string Folder="Assets/LighFight/Art/Drafts/TripoCourtyard";
  static readonly Vector3 Center=new Vector3(0,1.45f,0),Half=new Vector3(5.5f,1.4f,2);
  public static bool IntersectsTriangle(Vector3 a,Vector3 b,Vector3 c){
   a-=Center;b-=Center;c-=Center;
   var edges=new[]{b-a,c-b,a-c};var axes=new List<Vector3>{Vector3.right,Vector3.up,Vector3.forward,Vector3.Cross(edges[0],edges[1])};
   foreach(var edge in edges){axes.Add(Vector3.Cross(edge,Vector3.right));axes.Add(Vector3.Cross(edge,Vector3.up));axes.Add(Vector3.Cross(edge,Vector3.forward));}
   foreach(var axis in axes){if(axis.sqrMagnitude<1e-14f)continue;float pa=Vector3.Dot(a,axis),pb=Vector3.Dot(b,axis),pc=Vector3.Dot(c,axis);float radius=Half.x*Mathf.Abs(axis.x)+Half.y*Mathf.Abs(axis.y)+Half.z*Mathf.Abs(axis.z);if(Mathf.Min(pa,Mathf.Min(pb,pc))>=radius-1e-6f||Mathf.Max(pa,Mathf.Max(pb,pc))<=-radius+1e-6f)return false;}
   return true;
  }
  public static int Validate(GameObject root){
   if(!IntersectsTriangle(new Vector3(0,1,0),new Vector3(.2f,1,0),new Vector3(0,1,.2f)))throw new InvalidOperationException("Obstacle control was missed");
   int total=0;
   foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){
    var vertices=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;var matrix=filter.transform.localToWorldMatrix;
    for(int i=0;i<indices.Length;i+=3){
     total++;var a=matrix.MultiplyPoint3x4(vertices[indices[i]]);var b=matrix.MultiplyPoint3x4(vertices[indices[i+1]]);var c=matrix.MultiplyPoint3x4(vertices[indices[i+2]]);
     var min=Vector3.Min(a,Vector3.Min(b,c));var max=Vector3.Max(a,Vector3.Max(b,c));
     if(max.x<=-5.5f||min.x>=5.5f||max.y<=.05f||min.y>=2.85f||max.z<=-2||min.z>=2)continue;
     if(IntersectsTriangle(a,b,c))throw new InvalidOperationException("Decoration intersects combat volume: "+filter.name+" triangle "+i/3);
    }
   }
   if(total!=1922912)throw new InvalidOperationException("Source topology changed: "+total);
   return total;
  }
  [MenuItem("LighFight/Review Tripo Courtyard")]
  public static void Run(){
   string path=Folder+"/courtyard.fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.meshCompression=ModelImporterMeshCompression.Off;importer.weldVertices=false;importer.isReadable=true;importer.animationType=ModelImporterAnimationType.None;importer.SaveAndReimport();
   string texturePath=Folder+"/courtyard-h31-original_basecolor.jpg";var textureImporter=(TextureImporter)AssetImporter.GetAtPath(texturePath);textureImporter.maxTextureSize=4096;textureImporter.textureCompression=TextureImporterCompression.Uncompressed;textureImporter.SaveAndReimport();
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);if(texture.width!=4096)throw new InvalidOperationException("Source 4K texture was reduced");
   string materialPath=Folder+"/courtyard.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!material){material=new Material(Shader.Find("LighFight/ArenaSource"));AssetDatabase.CreateAsset(material,materialPath);}material.shader=Shader.Find("LighFight/ArenaSource");material.mainTexture=texture;material.SetFloat("_HideForeground",1);EditorUtility.SetDirty(material);
   var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Tripo Courtyard");EditorSceneManager.MoveGameObjectToScene(root,scene);
   try{
    var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));EditorSceneManager.MoveGameObjectToScene(model,scene);model.transform.SetParent(root.transform,false);root.transform.rotation=Quaternion.Euler(0,180,0);
    foreach(var renderer in root.GetComponentsInChildren<Renderer>()){renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>material).ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
    int triangles=Validate(root);root.AddComponent<OfficeArena>().UseSharedMaterials();
    PrefabUtility.SaveAsPrefabAsset(root,Folder+"/arena.prefab");AssetDatabase.SaveAssets();
    string output=Path.GetFullPath("../art/previews/courtyard-tripo-v1/unity");var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-tripoOutput");if(index>=0)output=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(output);
    ShaderUtil.allowAsyncCompilation=false;
    material.SetFloat("_HideForeground",0);
    Capture(scene,root,new Vector3(12,11,-17),new Vector3(0,2,0),true,output+"/overview.png");
    material.SetFloat("_HideForeground",1);
    Capture(scene,root,new Vector3(0,1.6f,-3.6f),new Vector3(0,1,0),false,output+"/game-view.png");
    EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
    File.WriteAllText(output+"/audit.json","{\"triangles\":"+triangles+",\"textureWidth\":4096,\"clearanceIntersections\":0,\"combatArea\":[11,4],\"deviceVerified\":false}");
    Debug.Log("TRIPO_ARENA_RESULT "+triangles+" triangles, 4096 texture, clear 11x4m volume");
   }finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
  }
  static void Capture(UnityEngine.SceneManagement.Scene scene,GameObject root,Vector3 position,Vector3 target,bool overview,string path){
   var go=new GameObject("Arena review camera");EditorSceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.transform.position=position;camera.transform.LookAt(target);camera.backgroundColor=new Color(.09f,.11f,.14f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=overview?60:42;camera.orthographic=overview;camera.orthographicSize=13;
   var render=new RenderTexture(1600,900,24);render.Create();camera.targetTexture=render;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var old=RenderTexture.active;
   try{camera.Render();RenderTexture.active=render;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
   finally{RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(render);UnityEngine.Object.DestroyImmediate(go);}
  }
  public static void Promote(){
   var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/arena.prefab");if(!content||!prefab||content.arenas.Length!=5)throw new InvalidOperationException("Missing catalog or verified arena");
   var probe=UnityEngine.Object.Instantiate(prefab);try{Validate(probe);}finally{UnityEngine.Object.DestroyImmediate(probe);}
   var serialized=new SerializedObject(content);serialized.FindProperty("arenas").GetArrayElementAtIndex(4).objectReferenceValue=prefab;serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
  }
 }
}
