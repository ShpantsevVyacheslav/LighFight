using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Editor {
 public static class FighterPortraitCapture {
  public static void Run() {
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.65f,.65f);
   var light=new GameObject("Portrait key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.7f;light.transform.rotation=Quaternion.Euler(20,-25,0);
   var camera=new GameObject("Portrait camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.08f,.14f);camera.orthographic=true;camera.orthographicSize=.24f;
   var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   for(int id=0;id<content.fighters.Length;id++) {
    // Island portrait comes from the exact neutral web source, never the combat rig.
    if(id==3)continue;
    var obj=Object.Instantiate(id==3?AssetDatabase.LoadAssetAtPath<GameObject>(NativeIslandSetup.Folder+"/web-motions.fbx"):content.fighters[id]);var view=obj.GetComponent<FighterView>()??obj.AddComponent<FighterView>();if(id!=3)view.Pose(new LighFight.Core.Fighter(),1,0);
    // Face the camera without raising fists into the portrait.
    obj.transform.rotation=Quaternion.Euler(0,id==3?180:90,0);
    var head=view.BonePoint("Head",Vector3.up*1.55f);head+=Vector3.up*.035f;
    camera.transform.position=head+new Vector3(0,.015f,-1.2f);camera.transform.LookAt(head);
    foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>()) {
     var mesh=new Mesh();skin.BakeMesh(mesh,false);var frozen=new GameObject("Portrait surface");frozen.transform.SetParent(skin.transform,false);
     frozen.transform.localScale=new Vector3(1/skin.transform.lossyScale.x,1/skin.transform.lossyScale.y,1/skin.transform.lossyScale.z);
     frozen.AddComponent<MeshFilter>().sharedMesh=mesh;frozen.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
    }
    var rt=new RenderTexture(256,256,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
    var png=new Texture2D(256,256,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,256,256),0,0);png.Apply();
    File.WriteAllBytes($"Assets/LighFight/Resources/Selection/fighter-{id}.png",png.EncodeToPNG());
    RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(png);Object.DestroyImmediate(rt);Object.DestroyImmediate(obj);
   }
   SourceQualityComparison.Run();
   AssetDatabase.Refresh();
  }
 }
}
