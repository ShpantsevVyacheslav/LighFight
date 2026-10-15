using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Diagnostic only: no source correction, gamma tint, or production UI changes.
public sealed class GuiColorTransportProbe : MonoBehaviour {
 public Texture2D portrait, importedSrgb, importedLinear;
 public Camera viewCamera;
 Texture2D dynamicSrgb,dynamicLinear; string mode=""; readonly List<string> rows=new List<string>();
 const string Out="GuiColorProbeEvidence";
 readonly string[] names={"actualPortrait0","importedFlat_sRGB","importedFlat_linear","dynamicFlat_sRGB","dynamicFlat_linear"};
 Texture2D[] Textures=>new[]{portrait,importedSrgb,importedLinear,dynamicSrgb,dynamicLinear};
 static Texture2D Flat(bool linear){var t=new Texture2D(16,16,TextureFormat.RGBA32,false,linear);t.SetPixels32(Enumerable.Repeat(new Color32(56,60,66,255),256).ToArray());t.Apply();return t;}
 IEnumerator Start(){
  Directory.CreateDirectory(Out);Screen.SetResolution(1200,700,false);dynamicSrgb=Flat(false);dynamicLinear=Flat(true);
  rows.Add("record,mode,texture,readback,x,y,r,g,b,a,detail");
  rows.Add("environment,,,,,,,,,,"+Application.unityVersion+" "+SystemInfo.graphicsDeviceType+" color="+QualitySettings.activeColorSpace+" screen="+Screen.width+"x"+Screen.height);
  for(int i=0;i<Textures.Length;i++){var t=Textures[i];var c=t.GetPixels32()[0];Log("source",names[i],"GetPixels32",0,0,c,"graphicsFormat="+t.graphicsFormat+" isDataSRGB="+t.isDataSRGB);}
  BlitControls();
  foreach(bool hdr in new[]{false,true}){
   viewCamera.allowHDR=hdr;var filmic=viewCamera.GetComponent<LighFight.Runtime.FittedFilmicCamera>();if(filmic)filmic.enabled=hdr;mode=hdr?"Linear_HDR_filmic":"Linear_LDR_noPost";
   for(int f=0;f<5;f++)yield return null;
   yield return new WaitForEndOfFrame();
   foreach(bool linear in new[]{false,true}){
    var t=new Texture2D(Screen.width,Screen.height,TextureFormat.RGBA32,false,linear);t.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);t.Apply();File.WriteAllBytes(Out+"/"+mode+"-ReadPixels-linear"+linear+".png",t.EncodeToPNG());
    for(int i=0;i<5;i++){// actual top-left corner maps to texture corner after centered ScaleAndCrop: test background well inside rounded mask
     Sample(t,i,12,12,"roundedInterior",linear);Sample(t,i,80,71,"center",linear);
    }Destroy(t);
   }
   var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+"/"+mode+"-ScreenCapture.png",shot.EncodeToPNG());Destroy(shot);
  }
  File.WriteAllLines(Out+"/samples.csv",rows);File.WriteAllText(Out+"/done.txt","Actual standalone EOF captures completed. Read observed RGB before inferring cause; no global gamma/image correction applied.\n");Application.Quit(0);
 }
 Rect Box(int i)=>new Rect(30+i*230,100,160,143);
 void OnGUI(){GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;GUI.backgroundColor=Color.white;for(int i=0;i<5;i++){var rect=Box(i);GUI.Label(new Rect(rect.x,60,220,35),names[i]);GUI.DrawTexture(rect,Textures[i],ScaleMode.ScaleAndCrop,true,0,Color.white,0,12);GUI.DrawTexture(new Rect(rect.x,300,160,143),Textures[i],ScaleMode.ScaleAndCrop);}}
 void Sample(Texture2D image,int i,int dx,int dy,string point,bool linear){var b=Box(i);int x=(int)b.x+dx,y=Screen.height-1-((int)b.y+dy);var tex=Textures[i];float scale=Mathf.Max(160f/tex.width,143f/tex.height);float sourceX=(dx-80)/scale+tex.width*.5f,sourceY=tex.height*.5f-(dy-71.5f)/scale;var source=tex.GetPixelBilinear(sourceX/tex.width,sourceY/tex.height);Log("CPU-corresponding-reference",names[i],"GetPixelBilinear",dx,dy,(Color32)source,"encodedCPUTextureLookup; not GPU sample");Log("EOF-rounded",names[i],"Texture2Dlinear="+linear,x,y,image.GetPixels32()[y*image.width+x],point+" format="+image.graphicsFormat);y=Screen.height-1-(300+dy);Log("EOF-basic",names[i],"Texture2Dlinear="+linear,x,y,image.GetPixels32()[y*image.width+x],point);}
 void Log(string record,string texture,string readback,int x,int y,Color32 c,string detail){rows.Add(string.Join(",",record,mode,texture,readback,x,y,c.r,c.g,c.b,c.a,detail.Replace(',',';')));}
 void BlitControls(){foreach(var format in new[]{RenderTextureFormat.ARGB32,RenderTextureFormat.ARGBHalf})foreach(var rw in new[]{RenderTextureReadWrite.Linear,RenderTextureReadWrite.sRGB})foreach(bool write in new[]{false,true}){
  var rt=new RenderTexture(16,16,0,format,rw);if(!rt.Create())continue;var old=RenderTexture.active;bool oldWrite=GL.sRGBWrite;
  try{GL.sRGBWrite=write;Graphics.Blit(importedSrgb,rt);RenderTexture.active=rt;foreach(bool linear in new[]{false,true}){var t=new Texture2D(16,16,TextureFormat.RGBA32,false,linear);t.ReadPixels(new Rect(0,0,16,16),0,0);t.Apply();Log("blit-control","importedFlat_sRGB","linear="+linear,8,8,t.GetPixels32()[136],"RT="+format+" requested="+rw+" actualSRGB="+rt.sRGB+" GLwrite="+write+" format="+rt.graphicsFormat);Destroy(t);}}
  finally{GL.sRGBWrite=oldWrite;RenderTexture.active=old;rt.Release();Destroy(rt);}
 }}
 void OnDestroy(){if(dynamicSrgb)Destroy(dynamicSrgb);if(dynamicLinear)Destroy(dynamicLinear);}
}
