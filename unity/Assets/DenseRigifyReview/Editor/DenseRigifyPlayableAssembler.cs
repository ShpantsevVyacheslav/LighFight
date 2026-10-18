using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using LighFight.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static partial class DenseRigifyReimportReview
{
    const string PlayableRoot = Root + "/PlayableReview";
    // Review only. Exact source-key Dance authority; SourceBox remains diagnostic, no catalog writes.
    public static void BuildDensePlayableReview()
    {
        string previousOutput = Output;
        Output = BaseOutput + "/playable-assembler-v1";
        Directory.CreateDirectory(Output); Directory.CreateDirectory(PlayableRoot);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        GameObject root = null; Mesh transientMesh = null;
        try
        {
            QualitySettings.skinWeights = SkinWeights.Unlimited;
            var config = LoadConfig();
            config.model = Root + "/SailorOriginalDenseDEF-BoxAdaptiveTspace-v2.fbx";
            var source = Source.Read(config.oracle);
            var input = AssetDatabase.LoadAssetAtPath<GameObject>(config.model);
            if (!input) throw new Exception("Qualified Tspace input missing");
            var clips = AssetDatabase.LoadAllAssetsAtPath(config.model).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            if (clips.Length != 1 || Math.Abs(clips[0].length - 2.7916667f) > .001f)
                throw new Exception("Review accepts sole qualified BOX take only");
            root = new GameObject("Sailor Dense SOURCE KEY DANCE + Generic BOX REVIEW ONLY"); root.SetActive(false);
            var offset = new GameObject("Review Ground Offset").transform; offset.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(input);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.transform.SetParent(offset, false);
            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            var animator = model.GetComponentInChildren<Animator>();
            if (!skin || !animator || !animator.avatar || !animator.avatar.isValid || animator.avatar.isHuman)
                throw new Exception("Expected unchanged valid Generic hierarchy");
            var report = new List<string>();
            transientMesh = RestoreWeights(source, skin, report, out _); skin.sharedMesh = transientMesh;
            int[] mapping = Geometry(source, skin, report); // all faces/UV/weights/binds BEFORE serializing derivative
            var mesh = PersistReviewAsset(transientMesh, PlayableRoot + "/OriginalFullSkinTypedWeights.asset");
            transientMesh = null; skin.sharedMesh = mesh;
            skin.updateWhenOffscreen = true; skin.quality = SkinQuality.Bone4;
            var avatar = PersistReviewAsset(UnityEngine.Object.Instantiate(animator.avatar), PlayableRoot + "/OriginalGenericAvatar.asset");
            animator.avatar = avatar; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var box = PersistReviewAsset(UnityEngine.Object.Instantiate(clips[0]), PlayableRoot + "/SourceGenericBOX.anim");
            var boxSettings = AnimationUtility.GetAnimationClipSettings(box); boxSettings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(box, boxSettings); EditorUtility.SetDirty(box);
            // Neutral means imported-default review hold, never labelled a calibrated fight guard.
            var neutral = new AnimationClip { name = "Review neutral imported default hold", frameRate = 60 };
            foreach (var node in model.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(node, animator.transform);
                if (node != animator.transform && !node.IsChildOf(animator.transform)) continue;
                var p = node.localPosition; var q = node.localRotation; var scale = node.localScale;
                string[] props = {"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w","m_LocalScale.x","m_LocalScale.y","m_LocalScale.z"};
                float[] values = { p.x,p.y,p.z,q.x,q.y,q.z,q.w,scale.x,scale.y,scale.z };
                for (int i = 0; i < props.Length; i++) AnimationUtility.SetEditorCurve(neutral, EditorCurveBinding.FloatCurve(path, typeof(Transform), props[i]), AnimationCurve.Constant(0, .5f, values[i]));
            }
            neutral = PersistReviewAsset(neutral, PlayableRoot + "/ReviewNeutralDefaultHold.anim");
            var clockNode=new GameObject("Review Clock Only");clockNode.transform.SetParent(animator.transform,false);
            var danceClock=new AnimationClip{name="Exact source Dance authored clock only",frameRate=24};
            AnimationUtility.SetEditorCurve(danceClock,EditorCurveBinding.FloatCurve("Review Clock Only",typeof(Transform),"m_LocalPosition.x"),AnimationCurve.Constant(0,(float)(556.0/24),0));
            var clockSettings=AnimationUtility.GetAnimationClipSettings(danceClock);clockSettings.loopTime=false;AnimationUtility.SetAnimationClipSettings(danceClock,clockSettings);
            danceClock=PersistReviewAsset(danceClock,PlayableRoot+"/DanceFullAuthorClock.anim");
            if(Math.Abs(danceClock.length-556.0/24)>.00001)throw new Exception("Tiny clock full duration lost");
            string controllerPath = PlayableRoot + "/ReviewOnly.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var item in machine.states) if (item.state.name != "Idle" && item.state.name != "SourceBox" && item.state.name != "Dance") machine.RemoveState(item.state);
            AnimatorState idle = ReviewState(machine, "Idle", neutral), strike = ReviewState(machine, "SourceBox", box);
            var danceState=ReviewState(machine,"Dance",danceClock);danceState.writeDefaultValues=false;
            machine.defaultState = idle; animator.runtimeAnimatorController = controller;
            var baseMaterial = CreateOriginalMaterial(config);
            var material = PersistReviewAsset(new Material(baseMaterial) { name = "Original4K Artist Normal0.5 REVIEW" }, PlayableRoot + "/ArtistNormal0p5.mat");
            material.SetFloat("_BumpScale", .5f); EditorUtility.SetDirty(material); skin.sharedMaterial = material;
            var view = root.AddComponent<FighterView>(); view.ConfigureAnimation(animator, new FighterView.ContactPose[0]);
            view.UseSharedMaterials(); view.UseNativeMotion(new[] { new FighterView.NativeClip {state="Idle",clip=neutral}, new FighterView.NativeClip {state="SourceBox",clip=box}, new FighterView.NativeClip {state="Dance",clip=danceClock} });
            var grounding = root.AddComponent<CandidateFootGrounding>();
            var owner = root.AddComponent<DenseRigifySupportOwner>();
            owner.grounding = grounding; owner.groundOffset = offset; owner.bones = skin.bones;
            owner.sourceVertices = source.positions.Length / 3;
            owner.expectedOriginalFootPoints = WriteOriginalFootSupport(source, skin, mapping, PlayableRoot + "/AllOriginalWeightedFeet.bytes");
            owner.supportBytes = AssetDatabase.LoadAssetAtPath<TextAsset>(PlayableRoot + "/AllOriginalWeightedFeet.bytes");
            if (owner.expectedOriginalFootPoints != 143168 || !owner.supportBytes) throw new Exception("Full original foot support provenance mismatch");
            const string keyPath=Root+"/AdaptiveDance/FullAuthorSourceKeys.bytes";
            if(StreamSHA(keyPath)!="296ea14ea4c70fc41d20f1e013d43cebd635daff06b61d3f881be2cf31ac67e4")throw new Exception("Exact source key data required");
            var bridge=root.AddComponent<DenseAdaptivePoseBridge>();bridge.sourceKeys=AssetDatabase.LoadAssetAtPath<TextAsset>(keyPath);bridge.target=skin;bridge.actorAnchor=model.transform;
            var playback=root.AddComponent<DenseAdaptiveReviewOwner>();playback.bridge=bridge;playback.view=view;playback.grounding=grounding;
            root.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, PlayableRoot + "/SailorDenseBOXReview.prefab");
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
            var dependencies=AssetDatabase.GetDependencies(PlayableRoot+"/SailorDenseBOXReview.prefab",true);
            File.WriteAllLines(Output+"/standalone-dependencies.txt",dependencies);
            if(dependencies.Any(path=>path.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase)||path==config.roughness||path==config.metallic))throw new Exception("Standalone actor leaked FBX or unbound scalar-map dependency");
            long boxKeys=0;foreach(var binding in AnimationUtility.GetCurveBindings(box))boxKeys+=AnimationUtility.GetEditorCurve(box,binding).length;
            File.WriteAllLines(Output+"/standalone-memory-budget.txt",new[]{"Explicit SourceBox diagnostic only runtime/editor bytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(box)+" keys="+boxKeys,"Tiny fullDanceClock bytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(danceClock)+" bindings="+AnimationUtility.GetCurveBindings(danceClock).Length,"Exact2535Float64DanceData bytes="+new FileInfo(keyPath).Length,"Standalone restoredMesh bytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(mesh),"Bound material maps only: "+AssetDatabase.GetAssetPath(material.GetTexture("_MainTex"))+"; "+AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap"))+"; "+AssetDatabase.GetAssetPath(material.GetTexture("_MetallicGlossMap")),"Editor memory is not phone residency; original raw maps/FBXs preserved separately."});

            File.WriteAllLines(Output + "/ASSEMBLY-SCOPE.txt", report.Concat(new[] {
                "REVIEW ONLY: exact2535Float64source-key Dance23.1667s with tiny full-author clock; Generic BOX2.7916667s is SourceBox diagnostic, never Idle/Guard. Neutral is explicitly imported-default review hold; no production catalog promotion.",
                "Standalone full original mesh/GenericAvatar/BOXclip and tinyDanceclock; no FBX dependency; typed restored influences; raw FBX/maps unchanged.",
                "Original foot set143168: any positive DEF-foot/toe influence, ALL influences retained, no stride. Loading-only decode; naive fullset runtime cost NOT ACCEPTED.",
                "Artist material normal0.5 separately labelled; original material normal1 retained.",
                "Actual FighterView/floor/entry-return/lease/GPU/player gates pending." }));
        }
        finally { if (root) UnityEngine.Object.DestroyImmediate(root); if (transientMesh) UnityEngine.Object.DestroyImmediate(transientMesh); Output = previousOutput; }
    }
    static AnimatorState ReviewState(AnimatorStateMachine machine, string name, Motion motion)
    {
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
        state.motion = motion; state.writeDefaultValues = true; return state;
    }
    static T PersistReviewAsset<T>(T value, string path) where T : UnityEngine.Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (!existing) { AssetDatabase.CreateAsset(value, path); return value; }
        EditorUtility.CopySerialized(value, existing); UnityEngine.Object.DestroyImmediate(value); EditorUtility.SetDirty(existing); return existing;
    }
    static int WriteOriginalFootSupport(Source source, SkinnedMeshRenderer skin, int[] mapping, string path)
    {
        var foot = new HashSet<int>(source.names.Select((name, index) => new { name, index }).Where(x => x.name == "DEF-foot.L" || x.name == "DEF-toe.L" || x.name == "DEF-foot.R" || x.name == "DEF-toe.R").Select(x => x.index));
        if (foot.Count != 4) throw new Exception("Exact source foot names missing");
        var selected = new List<int>();
        for (int i = 0; i < mapping.Length; i++) for (int k = 0; k < 4; k++)
            if (source.weights[i*4+k] > 0 && foot.Contains(source.joints[i*4+k])) { selected.Add(i); break; }
        var vertices = skin.sharedMesh.vertices; var binds = skin.sharedMesh.bindposes; var weights = ReadTypedWeights(skin.sharedMesh);
        using (var file = File.Create(path)) using (var compressed = new DeflateStream(file, System.IO.Compression.CompressionLevel.Optimal)) using (var writer = new BinaryWriter(compressed))
        {
            writer.Write(0x31465344u); writer.Write(mapping.Length); writer.Write(vertices.Length); writer.Write(skin.bones.Length); writer.Write(selected.Count);
            foreach (int original in selected)
            {
                int imported = mapping[original]; var w = weights[imported];
                int[] bones = { w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3 }; float[] values = { w.weight0,w.weight1,w.weight2,w.weight3 };
                writer.Write(original); writer.Write(imported); writer.Write((byte)values.Count(x => x > 0));
                for (int k = 0; k < 4; k++) if (values[k] > 0)
                { var p = binds[bones[k]].MultiplyPoint3x4(vertices[imported]); writer.Write((ushort)bones[k]); writer.Write(values[k]); writer.Write(p.x); writer.Write(p.y); writer.Write(p.z); }
            }
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); return selected.Count;
    }
}
