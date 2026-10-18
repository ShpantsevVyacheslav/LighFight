using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using LighFight.Runtime;

public static partial class DenseRigifyReimportReview
{
    // Qualified source environment only: no actor/material/scene/catalog writes.
    public static void BuildSelectionStudioConfig()
    {
        string previousOutput = Output;
        Output = BaseOutput + "/selection-studio-config-v1";
        Directory.CreateDirectory(Output);
        try
        {
            const string directory = "Assets/LighFight/Resources/SelectionStudio";
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var source = JsonUtility.FromJson<StudioConfig>(File.ReadAllText(StudioPacket + "/neutral-studio-ibl.json"));
            if (source.binarySHA256 != "03ff5432a09eb70af0adb6a6064f8109732a051bb4af310928e44db463660992")
                throw new Exception("Unqualified studio source");
            using (var stream = File.OpenRead(StudioPacket + "/neutral-studio-prefiltered.sib"))
            using (var sha = System.Security.Cryptography.SHA256.Create())
                if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != source.binarySHA256)
                    throw new Exception("Studio binary hash differs from qualified source");
            var cube = StudioCube(false);
            string cubePath = directory + "/NeutralStudio128.asset";
            var existingCube = AssetDatabase.LoadAssetAtPath<Cubemap>(cubePath);
            if (existingCube)
            {
                EditorUtility.CopySerialized(cube, existingCube);
                UnityEngine.Object.DestroyImmediate(cube);
                cube = existingCube;
            }
            else AssetDatabase.CreateAsset(cube, cubePath);
            var sh = StudioNativeSH(source);
            string configPath = directory + "/PreviewLighting.asset";
            var asset = AssetDatabase.LoadAssetAtPath<SelectionStudioLightingConfig>(configPath);
            if (!asset)
            {
                asset = ScriptableObject.CreateInstance<SelectionStudioLightingConfig>();
                AssetDatabase.CreateAsset(asset, configPath);
            }
            asset.prefilteredReflection = cube;
            asset.nativeDiffuseSH = new float[27];
            for (int c = 0; c < 3; c++) for (int i = 0; i < 9; i++) asset.nativeDiffuseSH[c * 9 + i] = sh[c, i];
            asset.sourceSHA256 = source.binarySHA256;
            if (!asset.IsValid) throw new Exception("Invalid serialized studio config");
            EditorUtility.SetDirty(cube);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Output + "/config-provenance.txt", "Qualified neutral studio SIB1 " + source.binarySHA256 +
                "\n128 RGBAHalf cubemap, 8 prefiltered mips; native calibrated RGB27 SH.\n" +
                "No actor materials altered. Source normal1 stays current actor setting.\n" +
                "ConfigGUID=" + AssetDatabase.AssetPathToGUID(configPath) + "\nCubeGUID=" + AssetDatabase.AssetPathToGUID(cubePath));
        }
        finally { Output = previousOutput; }
    }
}
