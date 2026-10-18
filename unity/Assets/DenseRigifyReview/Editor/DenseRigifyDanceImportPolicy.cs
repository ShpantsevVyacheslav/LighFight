using UnityEditor;
// Review-only first-import policy. No other fighter, Avatar, texture or model is touched.
public sealed class DenseRigifyDanceImportPolicy : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (assetPath != "Assets/DenseRigifyReview/SailorOriginalDenseDEF-DanceAdaptiveTspace-v1.fbx") return;
        var m = (ModelImporter)assetImporter;
        m.animationType=ModelImporterAnimationType.Generic;m.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        m.meshCompression=ModelImporterMeshCompression.Off;m.meshOptimizationFlags=0;m.optimizeBones=false;m.optimizeGameObjects=false;
        m.weldVertices=false;m.isReadable=true;m.importNormals=ModelImporterNormals.Import;m.importTangents=ModelImporterTangents.Import;
        m.skinWeights=ModelImporterSkinWeights.Custom;m.maxBonesPerVertex=4;m.minBoneWeight=0;
        m.importAnimation=true;m.animationCompression=ModelImporterAnimationCompression.Off;m.resampleCurves=false;
        m.materialImportMode=ModelImporterMaterialImportMode.None;
    }
}
