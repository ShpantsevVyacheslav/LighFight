using System;using UnityEngine;
// Explicit owner for texture draws only. Shader performs ordinary linear sampling/output, never a guessed gamma transfer.
public sealed class ColorCorrectGuiTexture:IDisposable {
 readonly Material material;bool disposed;
 public ColorCorrectGuiTexture(Shader shader){if(!shader||!shader.isSupported)throw new InvalidOperationException("Color-correct GUI texture shader unavailable");material=new Material(shader){name="ColorCorrectGuiTextureOwned",hideFlags=HideFlags.HideAndDontSave};}
 public void Draw(Rect rect,Texture image,ScaleMode scale=ScaleMode.StretchToFill,bool alphaBlend=true,float imageAspect=0,Color? tint=null,float borderWidth=0,float borderRadius=0){
  if(disposed)throw new ObjectDisposedException(nameof(ColorCorrectGuiTexture));if(!image||Event.current.type!=EventType.Repaint)return;if(borderWidth!=0)throw new NotSupportedException("Border strokes are outside this texture-only adapter");
  if(QualitySettings.activeColorSpace!=ColorSpace.Linear){GUI.DrawTexture(rect,image,scale,alphaBlend,imageAspect,tint??GUI.color,0,borderRadius);return;}
  var matrix=GUI.matrix;if(Mathf.Abs(matrix.m01)+Mathf.Abs(matrix.m10)>1e-5f||matrix.m00<=0||matrix.m11<=0)throw new NotSupportedException("Current production GUI uses positive axis-aligned scale/offset; rotated/sheared GUI is unsupported");
  float aspect=imageAspect>0?imageAspect:(float)image.width/image.height;Rect uv=new Rect(0,0,1,1);float dest=rect.width/rect.height;
  if(scale==ScaleMode.ScaleAndCrop){if(dest>aspect){uv.height=aspect/dest;uv.y=(1-uv.height)*.5f;}else{uv.width=dest/aspect;uv.x=(1-uv.width)*.5f;}}
  else if(scale==ScaleMode.ScaleToFit){if(dest>aspect){float w=rect.height*aspect;rect.x+=(rect.width-w)*.5f;rect.width=w;}else{float h=rect.width/aspect;rect.y+=(rect.height-h)*.5f;rect.height=h;}}
  var screen=new Rect(rect.x*matrix.m00+matrix.m03,rect.y*matrix.m11+matrix.m13,rect.width*matrix.m00,rect.height*matrix.m11);
  Color color=tint??GUI.color;material.SetVector("_Tint",new Vector4(color.r,color.g,color.b,color.a));material.SetVector("_SourceUV",new Vector4(uv.x,uv.y,uv.width,uv.height));material.SetVector("_RectSize",new Vector4(rect.width,rect.height,0,0));material.SetFloat("_Radius",Mathf.Min(borderRadius,Mathf.Min(rect.width,rect.height)*.5f));
  GUI.matrix=Matrix4x4.identity;try{Graphics.DrawTexture(screen,image,uv,0,0,0,0,Color.white,material,alphaBlend?0:1);}finally{GUI.matrix=matrix;}
 }
 public void Dispose(){if(disposed)return;disposed=true;if(material){if(Application.isPlaying)UnityEngine.Object.Destroy(material);else UnityEngine.Object.DestroyImmediate(material);}}
}
