using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Runtime;
using LighFight.Core;

namespace LighFight.Editor
{
    public static class Preview
    {
        public static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            OfficeArena.Create(OfficeLocation.Meeting);
            var a=FighterView.Create("Player",new Color(.04f,.3f,.32f),false);
            var b=FighterView.Create("CPU",new Color(.8f,.35f,.45f),true);
            var match=new Match();match.A.X=-.9f;match.B.X=.9f;match.A.Start(4);match.A.Frame=20;
            a.Pose(match.A,1,0);b.Pose(match.B,-1,0);
            var light=new GameObject("Sun").AddComponent<Light>();light.type=LightType.Directional;
            light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-30,0);light.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.45f,.43f,.4f);
            var camera=new GameObject("Preview Camera").AddComponent<Camera>();
            camera.transform.position=new Vector3(0,2.4f,-9);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=42;
            var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();
            RenderTexture.active=rt;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            string output=Path.GetFullPath("../.tools/reports/office-preview.png");Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output,texture.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);
        }
    }
}
