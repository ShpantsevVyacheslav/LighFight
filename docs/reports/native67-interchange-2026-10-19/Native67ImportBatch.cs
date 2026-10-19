using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class Native67ImportBatch {
 [Serializable] public class Result { public string status,unity,asset; public SailorNative67SpatialImportReview.ImportedStats stats; public string[] clips; public int coveredOriginalIds; public string error; }
 public static void Run(){
 var r=new Result{status="RUNNING_NO_ACCEPTANCE",unity=Application.unityVersion,asset="Assets/Native67SpatialReview/SailorReview.fbx"};int exit=1;
 try {AssetDatabase.ImportAsset(r.asset,ImportAssetOptions.ForceSynchronousImport);SailorNative67SpatialImportReview.ConfigureReviewImport(r.asset);
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(r.asset); var skins=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);if(skins.Length!=1)throw new Exception("One authority mesh required");var skin=skins[0];SailorNative67SpatialImportReview.ConfigureReviewRenderer(skin);r.stats=SailorNative67SpatialImportReview.ReadImportedStats(r.asset,skin);
 r.clips=AssetDatabase.LoadAllAssetsAtPath(r.asset).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).Select(c=>c.name+" | "+c.length).ToArray();
 var ids=SailorNative67SpatialImportReview.ReadSourceVertexIds(skin.sharedMesh,new SailorNative67SpatialImportReview.SourceIndexLayerContract{layerName="__LF_SOURCE_VERTEX_ID_V1",originalVertexCount=994452,denominator=1048576,originalUVLayerNames=new[]{"UVMap"},exportUVLayerOrder=new[]{"UVMap","__LF_SOURCE_VERTEX_ID_V1"},manifestCandidateChannel=1,verifiedImportedV=0});r.coveredOriginalIds=ids.Distinct().Count();
 r.status="IMPORTED_STATS_ONLY_PENDING_FULL_CORNER_AND_WEIGHT_PROOF";exit=0;
 }catch(Exception e){r.status="IMPORT_GATE_FAILED_NO_PROMOTION";r.error=e.ToString();Debug.LogException(e);}
 finally {File.WriteAllText(Environment.GetEnvironmentVariable("LF_NATIVE67_REPORT"),JsonUtility.ToJson(r,true));Debug.Log("AGENT_RESULT "+r.status);EditorApplication.Exit(exit);}
 }
}
