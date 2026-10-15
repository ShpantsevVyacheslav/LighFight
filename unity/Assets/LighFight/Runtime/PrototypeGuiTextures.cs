using UnityEngine;
namespace LighFight.Runtime {
 public sealed partial class Prototype {
  private ColorCorrectGuiTexture guiTextureDraw;
  private void DrawTexture(Rect rect,Texture image,ScaleMode scale=ScaleMode.StretchToFill,bool alphaBlend=true,float imageAspect=0,Color? tint=null,float borderWidth=0,float borderRadius=0){
   if(!image||Event.current.type!=EventType.Repaint)return;
   if(guiTextureDraw==null)guiTextureDraw=new ColorCorrectGuiTexture(Resources.Load<Shader>("Shaders/GuiTextureLinear"));
   guiTextureDraw.Draw(rect,image,scale,alphaBlend,imageAspect,tint,borderWidth,borderRadius);
  }
  private void ReleaseGuiTextureDraw(){guiTextureDraw?.Dispose();guiTextureDraw=null;}
 }
}
