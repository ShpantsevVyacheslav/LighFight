using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LighFight.Runtime;

public static class AdditionalTripoRosterSetup
{
    public static void BuildGalaReview()
    {
        LighFight.Editor.MobileSourceTextureSetup.Configure();
        NativeSupportMainProbeBuilder.Build();
    }

    public static void ReplaceGala()
    {
        TripoGalaPlayablePipeline.Build();
        const string path = "Assets/LighFight/Art/Drafts/TripoGalaPlayable/GalaHumanoidCandidate.prefab";
        var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/LighFight/Art/Drafts/TripoGalaPlayable/Source/GalaFull/source-calibrated-native-pbr-review.mat");
        var portrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LighFight/Resources/Selection/fighter-6.png");
        var candidate = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var overlay = candidate ? candidate.GetComponent<NativeSupportOverlay>() : null;
        var animator = candidate ? candidate.GetComponentInChildren<Animator>() : null;
        if (!content || content.streamed || content.FighterCount != 11 || !material || !portrait || portrait.width != 1024 || !overlay || overlay.entries.Length != 8 || !animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
            throw new InvalidOperationException("Reviewed Gala source, native rig/eight takes, calibrated material and source portrait required");
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                skin.sharedMaterials = skin.sharedMaterials.Select(_ => material).ToArray();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var previous = content.fighters[6];
        content.fighters[6] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!content.ValidateCatalog(out var error))
        {
            content.fighters[6] = previous;
            throw new InvalidOperationException(error);
        }
        EditorUtility.SetDirty(content);
        AssetDatabase.SaveAssets();
        Debug.Log("AGENT_RESULT Gala slot6 uses preserved100k native rig/eight support takes and source-calibrated scalar material; roster IDs preserved; device validation pending");
    }

    public static void ReplaceWitch()
    {
        var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/TripoWitchPlayable/WitchHumanoidCandidate.prefab");
        var view = prefab ? prefab.GetComponent<FighterView>() : null;
        var overlay = prefab ? prefab.GetComponent<NativeSupportOverlay>() : null;
        var animator = prefab ? prefab.GetComponentInChildren<Animator>() : null;
        var portrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LighFight/Resources/Selection/fighter-1.png");
        if (!content || content.streamed || content.FighterCount != 11 || !view || !view.IsReady || !overlay || !overlay.enabled || overlay.entries.Length != 8 || !animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman || !portrait || portrait.width < 1024)
            throw new InvalidOperationException("Reviewed Witch rig, source portrait and eleven-fighter catalog required");
        var before = content.fighters[1];
        content.fighters[1] = prefab;
        if (!content.ValidateCatalog(out var error))
        {
            content.fighters[1] = before;
            throw new InvalidOperationException(error);
        }
        EditorUtility.SetDirty(content);
        AssetDatabase.SaveAssets();
        Debug.Log("AGENT_RESULT Night Witch slot1 replaced with reviewed Tripo source rig/eight native support motions; original roster index and ID preserved");
    }

    public static void Run()
    {
        var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
        if (!content || content.streamed || (content.FighterCount != 8 && content.FighterCount != 11))
            throw new InvalidOperationException("Expected the preserved eight-fighter inline catalog or its eleven-fighter extension");
        string[] roles = { "Retro", "Sailor", "Summer" };
        string[] ids = { "retro-denim", "striped-sailor", "summer-beard" };
        string[] ru = { "Ретро-деним", "Моряк", "Летний боец" };
        string[] en = { "Retro Denim", "Striped Sailor", "Summer Fighter" };
        string[] states = { "Victory", "Greeting", "Dance", "Defeat", "Walk", "NativeBox02", "NativeFrontKick", "NativeHeadFull" };
        var fighters = new GameObject[11];
        Array.Copy(content.fighters, fighters, Math.Min(content.fighters.Length, fighters.Length));
        var metadata = new GameContent.FighterMetadata[11];
        if (content.fighterMetadata != null)
            Array.Copy(content.fighterMetadata, metadata, Math.Min(content.fighterMetadata.Length, metadata.Length));
        for (int i = 0; i < roles.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/Tripo" + roles[i] + "Playable/" + roles[i] + "HumanoidCandidate.prefab");
            var view = prefab ? prefab.GetComponent<FighterView>() : null;
            var overlay = prefab ? prefab.GetComponent<NativeSupportOverlay>() : null;
            var animator = prefab ? prefab.GetComponentInChildren<Animator>() : null;
            var portrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LighFight/Resources/Selection/fighter-" + (8 + i) + ".png");
            if (!view || !view.IsReady || !overlay || !overlay.enabled || !animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("Reviewed playable rig missing: " + roles[i]);
            if (overlay.entries == null || overlay.entries.Length != 8 || states.Any(s => !overlay.entries.Any(e => e.state == s && e.pose != null)))
                throw new InvalidOperationException("Original eight-motion support catalog incomplete: " + roles[i]);
            if (!portrait || portrait.width < 1024 || portrait.height < 1024)
                throw new InvalidOperationException("Reviewed source-rendered portrait missing: " + roles[i]);
            fighters[8 + i] = prefab;
            metadata[8 + i] = new GameContent.FighterMetadata { id = ids[i], nameRu = ru[i], nameEn = en[i], portrait = portrait };
        }
        var oldFighters = content.fighters;
        var oldMetadata = content.fighterMetadata;
        content.fighters = fighters;
        content.fighterMetadata = metadata;
        if (!content.ValidateCatalog(out var error))
        {
            content.fighters = oldFighters;
            content.fighterMetadata = oldMetadata;
            throw new InvalidOperationException(error);
        }
        EditorUtility.SetDirty(content);
        AssetDatabase.SaveAssets();
        Debug.Log("AGENT_RESULT eleven-fighter catalog configured; original eight slots preserved, Retro/Sailor/Summer appended with source portraits and eight native support motions each; shared combat bindings remain distinct");
    }
}
