using UnityEngine;
namespace LighFight.Runtime {
[ExecuteAlways,RequireComponent(typeof(Camera))]
public sealed class FittedFilmicCamera:MonoBehaviour {
 public Shader filmicShader;
 [Min(0)] public float exposure=1;
 Material material;
 bool reported;
 public void SetEnabled(string value){enabled=value!="0";}
 void OnEnable(){GetComponent<Camera>().allowHDR=true;}
 void OnRenderImage(RenderTexture source,RenderTexture destination){
  if(QualitySettings.activeColorSpace!=ColorSpace.Linear || filmicShader==null || !filmicShader.isSupported){Graphics.Blit(source,destination);return;}
  if(!reported&&Application.isPlaying&&Debug.isDebugBuild){Debug.Log("FILMIC_RUNTIME source="+source.format+" linear="+QualitySettings.activeColorSpace+" shaderSupported="+filmicShader.isSupported);reported=true;}
  if(material==null){material=new Material(filmicShader){hideFlags=HideFlags.HideAndDontSave};}
  material.SetFloat("_Exposure",exposure);Graphics.Blit(source,destination,material);
 }
 void OnDisable(){if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);material=null;}}
}

}
