using System;using System.Collections.Generic;
namespace UnityEngine {
 public class Object {public static implicit operator bool(Object x)=>x!=null;public static void Destroy(Object x){}public static void DestroyImmediate(Object x){} }
 public class MonoBehaviour:Object {public static Renderer[] TestChildren;public T[] GetComponentsInChildren<T>(bool all){return (T[])(object)TestChildren;}}
 public class Renderer:Object {public Material[] sharedMaterials;public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;public bool receiveShadows;}
 public class Texture:Object{}
 public struct Color {public static Color white=>new Color();}
 public class Shader:Object {public static Shader Find(string s)=>new Shader();}
 public class Material:Object {public Shader shader;public string name;public Texture mainTexture;public object mainTextureScale,mainTextureOffset;public Color color;public Material(Shader s){shader=s;}public bool HasProperty(string s)=>false;public float GetFloat(string s)=>0;public Texture GetTexture(string s)=>null;public void SetFloat(string s,float f){}public void SetTexture(string s,Texture t){} }
 public static class Application {public static bool isPlaying=false;}
}
namespace UnityEngine.Rendering {public enum ShadowCastingMode{Off,On,TwoSided,ShadowsOnly}}
