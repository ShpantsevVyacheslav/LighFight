using UnityEngine;
// Existing game filmic curve, one explicit output transfer for linear PNG targets;
// one hardware output transfer for sRGB targets/backbuffer. Review-only camera.
[ExecuteAlways,RequireComponent(typeof(Camera))]public sealed class DenseRigifyReviewDisplay:MonoBehaviour{
 public Shader filmicShader,encodeShader;Material filmic,encode;public string LastOutputPath{get;private set;}
 void Ensure(){if(filmic&&encode)return;if(!filmicShader||!encodeShader||!filmicShader.isSupported||!encodeShader.isSupported)throw new System.InvalidOperationException("Qualified review display shaders missing");filmic=new Material(filmicShader){hideFlags=HideFlags.HideAndDontSave};encode=new Material(encodeShader){hideFlags=HideFlags.HideAndDontSave};filmic.SetFloat("_Exposure",1);}
 public void RenderForReview(RenderTexture source,RenderTexture destination){Ensure();var linear=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);bool previous=GL.sRGBWrite;try{GL.sRGBWrite=false;Graphics.Blit(source,linear,filmic);bool hardware=QualitySettings.activeColorSpace==ColorSpace.Linear&&(destination==null||destination.sRGB);GL.sRGBWrite=hardware;if(hardware){LastOutputPath="FilmicLinear -> one hardware sRGB";Graphics.Blit(linear,destination);}else{LastOutputPath="FilmicLinear -> explicitIEC-sRGB into linear target";Graphics.Blit(linear,destination,encode);}}finally{GL.sRGBWrite=previous;RenderTexture.ReleaseTemporary(linear);}}
 void OnRenderImage(RenderTexture source,RenderTexture destination){RenderForReview(source,destination);}
 void OnDisable(){if(filmic){if(Application.isPlaying)Destroy(filmic);else DestroyImmediate(filmic);}if(encode){if(Application.isPlaying)Destroy(encode);else DestroyImmediate(encode);}filmic=null;encode=null;}
}
