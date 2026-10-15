using System;using System.Collections.Generic;using UnityEngine;
// Experimental isolated adapter. GPU-derived cache only; no source texture mutation.
public sealed class LinearGuiTextureCache:IDisposable {
 readonly Dictionary<Texture2D,RenderTexture> entries=new Dictionary<Texture2D,RenderTexture>();readonly RenderTextureFormat format;readonly Material transfer;
 public LinearGuiTextureCache(RenderTextureFormat format,Shader extraDecode=null){this.format=format;if(extraDecode)transfer=new Material(extraDecode){hideFlags=HideFlags.HideAndDontSave};}
 public Texture Resolve(Texture source){if(!source||source is RenderTexture||QualitySettings.activeColorSpace!=ColorSpace.Linear)return source;if(!(source is Texture2D t))return source;if(entries.TryGetValue(t,out var existing)&&existing)return existing;var rt=new RenderTexture(t.width,t.height,0,format,RenderTextureReadWrite.Linear){name=t.name+" derived GUI linear cache",hideFlags=HideFlags.HideAndDontSave,filterMode=t.filterMode,wrapMode=t.wrapMode};rt.Create();var old=RenderTexture.active;bool write=GL.sRGBWrite;try{GL.sRGBWrite=false;if(transfer)Graphics.Blit(t,rt,transfer);else Graphics.Blit(t,rt);}finally{RenderTexture.active=old;GL.sRGBWrite=write;}entries[t]=rt;return rt;}
 public void Dispose(){foreach(var rt in entries.Values)if(rt){rt.Release();UnityEngine.Object.Destroy(rt);}entries.Clear();if(transfer)UnityEngine.Object.Destroy(transfer);}
}
