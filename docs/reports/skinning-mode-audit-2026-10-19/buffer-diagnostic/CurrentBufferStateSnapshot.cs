using System;using System.IO;using System.Text.RegularExpressions;using System.Security.Cryptography;using System.Text;using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;
public static class CurrentBufferStateSnapshot {
 [Serializable]sealed class State{public string ambientMode,lightProbeUsage,postShader;public float postExposure;public float[] ambientProbe;public int frame,aa;public bool hdr,allowMSAA;public Matrix4x4 view,projection,nonJitteredProjection;public Vector4 shaderTime;public Color ambient;public MaterialState[] materials;public LightState[] lights;}
 [Serializable]sealed class MaterialState{public string name,shader;public string[] keywords;public bool propertyBlockEmpty;public Property[] properties;}
 [Serializable]sealed class Property{public string name,type,texture;public Vector4 value,blockValue;public bool blockOverrides;}
 [Serializable]sealed class LightState{public string name,type,shadows;public float intensity;public Color color;public Matrix4x4 localToWorld;}
 public static void Write(SkinnedMeshRenderer skin,string path){
  var camera=Camera.main;if(!camera){var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);if(cameras.Length!=1)throw new Exception("Unambiguous automatic camera required");camera=cameras[0];}
  var ms=new List<MaterialState>();var block=new MaterialPropertyBlock();skin.GetPropertyBlock(block);
  foreach(var mat in skin.sharedMaterials){var ps=new List<Property>();for(int i=0;i<mat.shader.GetPropertyCount();i++){var type=mat.shader.GetPropertyType(i);string name=mat.shader.GetPropertyName(i);int id=Shader.PropertyToID(name);var p=new Property{name=name,type=type.ToString(),blockOverrides=block.HasProperty(id)};
   if(type==ShaderPropertyType.Float||type==ShaderPropertyType.Range||type==ShaderPropertyType.Int){p.value=new Vector4(mat.GetFloat(id),0,0,0);p.blockValue=new Vector4(block.GetFloat(id),0,0,0);}
   else if(type==ShaderPropertyType.Vector||type==ShaderPropertyType.Color){p.value=mat.GetVector(id);p.blockValue=block.GetVector(id);}
   else if(type==ShaderPropertyType.Texture){var texture=mat.GetTexture(id);p.texture=texture?texture.name:"none";}
   ps.Add(p);
  }ms.Add(new MaterialState{name=mat.name,shader=mat.shader.name,keywords=mat.shaderKeywords,propertyBlockEmpty=block.isEmpty,properties=ps.ToArray()});}
  var ls=new List<LightState>();foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))ls.Add(new LightState{name=l.name,type=l.type.ToString(),shadows=l.shadows.ToString(),intensity=l.intensity,color=l.color,localToWorld=l.transform.localToWorldMatrix});
  var coefficients=new float[27];var sh=RenderSettings.ambientProbe;for(int rgb=0;rgb<3;rgb++)for(int coefficient=0;coefficient<9;coefficient++)coefficients[rgb*9+coefficient]=sh[rgb,coefficient];var post=camera.GetComponent<LighFight.Runtime.FittedFilmicCamera>();
  File.WriteAllText(path,JsonUtility.ToJson(new State{ambientMode=RenderSettings.ambientMode.ToString(),lightProbeUsage=skin.lightProbeUsage.ToString(),ambientProbe=coefficients,postExposure=post?post.exposure:0,postShader=post&&post.filmicShader?post.filmicShader.name:"none",frame=Time.frameCount,aa=QualitySettings.antiAliasing,hdr=camera.allowHDR,allowMSAA=camera.allowMSAA,view=camera.worldToCameraMatrix,projection=camera.projectionMatrix,nonJitteredProjection=camera.nonJitteredProjectionMatrix,shaderTime=Shader.GetGlobalVector("_Time"),ambient=RenderSettings.ambientLight,materials=ms.ToArray(),lights=ls.ToArray()},true));
 }
 public static string StableHash(string path){
  string text=File.ReadAllText(path);
  // Frame number and shader global clock are reported but expected to advance.
  // Every pose/camera/light/material/probe/post field remains in the stable hash.
  text=Regex.Replace(text,"\"frame\":\\s*[-0-9]+,", "\"frame\":0,");
  text=Regex.Replace(text,"\"shaderTime\":\\s*\\{[^}]*\\},", "\"shaderTime\":{},");
  using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");
 }

}
