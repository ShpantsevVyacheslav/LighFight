using UnityEngine;
// Explicit view selection; neither mesh is simplified or regenerated at runtime.
public sealed class OriginalDensityViewMode:MonoBehaviour {
 public SkinnedMeshRenderer target;public Mesh originalDense,acceptedNative;public Material originalAtlas,acceptedAtlas;
 void OnEnable(){if(Application.isPlaying&&originalDense)originalDense.UploadMeshData(true);}
 public void SelectOriginalDensity(){target.sharedMesh=originalDense;target.sharedMaterial=originalAtlas;}
 public void SelectAcceptedNative(){if(!acceptedNative||!acceptedAtlas)throw new System.InvalidOperationException("This original-density option has no low-detail asset dependency; select the separate accepted prefab instead.");target.sharedMesh=acceptedNative;target.sharedMaterial=acceptedAtlas;}
 public RenderTexture CaptureOriginalPortrait(Camera camera,int size=1024){var oldMesh=target.sharedMesh;var oldMaterials=target.sharedMaterials;var oldTarget=camera.targetTexture;SelectOriginalDensity();var image=new RenderTexture(size,size,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);image.Create();camera.targetTexture=image;camera.Render();camera.targetTexture=oldTarget;target.sharedMesh=oldMesh;target.sharedMaterials=oldMaterials;return image;}
}
