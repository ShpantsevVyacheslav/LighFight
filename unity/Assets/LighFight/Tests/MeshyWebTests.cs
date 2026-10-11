using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LighFight.Runtime;
using System;
using System.Linq;
public class MeshyWebTests
{
 [Test] public void RulesAndArenaSheetsAreMutuallyExclusive()
 {
  var obj=new GameObject("Sheet transition test");try {
   var prototype=obj.AddComponent<Prototype>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   var rules=typeof(Prototype).GetField("rulesOpen",flags);var arenas=typeof(Prototype).GetField("arenaOpen",flags);
   prototype.OpenArenas();Assert.IsTrue((bool)arenas.GetValue(prototype));
   prototype.OpenRules();Assert.IsTrue((bool)rules.GetValue(prototype));Assert.IsFalse((bool)arenas.GetValue(prototype));
   prototype.OpenArenas();Assert.IsFalse((bool)rules.GetValue(prototype));Assert.IsTrue((bool)arenas.GetValue(prototype));
  }finally{UnityEngine.Object.DestroyImmediate(obj);}
 }
 [Test] public void WebExportRetainsActualSourceAndFingerAnimation()
 {
  const string path="Assets/LighFight/Art/Drafts/MeshyWebIsland/web-motions.fbx";
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.IsNotNull(model);
  var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();Assert.AreEqual(66364,skin.sharedMesh.triangles.Length/3,"Source surface or Blender helper geometry changed");
  Assert.AreEqual(66,skin.bones.Length);var influenceCounts=skin.sharedMesh.GetBonesPerVertex();try{Assert.IsTrue(influenceCounts.Any(c=>c>4),"Secondary glTF skin influences were truncated");}finally{influenceCounts.Dispose();}Assert.IsTrue(skin.bones.Any(b=>b.name.Contains("HandIndex1")));
  var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();Assert.GreaterOrEqual(clips.Length,12);
  foreach(var clip in clips){Assert.Greater(clip.length,0);Assert.IsTrue(AnimationUtility.GetCurveBindings(clip).Any(b=>b.path.Contains("HandIndex")),"Finger channels lost: "+clip.name);}
  var importer=(ModelImporter)AssetImporter.GetAtPath(path);Assert.AreEqual(ModelImporterAnimationType.Generic,importer.animationType);Assert.AreEqual(ModelImporterAnimationCompression.Off,importer.animationCompression);Assert.AreEqual(8,importer.maxBonesPerVertex);
 }
 [Test] public void EveryNativeMotionContainsChangingRotations()
 {
  const string path="Assets/LighFight/Art/Drafts/MeshyWebIsland/web-motions.fbx";
  foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview"))) {
   float variation=AnimationUtility.GetCurveBindings(clip).Where(b=>b.propertyName.StartsWith("m_LocalRotation")).Select(b=>{
    var curve=AnimationUtility.GetEditorCurve(clip,b);float[] values=Enumerable.Range(0,61).Select(i=>curve.Evaluate(clip.length*i/60f)).ToArray();return values.Max()-values.Min();
   }).DefaultIfEmpty(0).Max();
   Assert.Greater(variation,.02f,"Named clip contains a frozen pose: "+clip.name);
  }
 }
 [Test] public void NativePlaybackUsesProviderDurationAndCanonicalContactBones()
 {
  Assert.That(FighterView.NativeLoopPhase(.5f,2),Is.EqualTo(.25f).Within(.00001f));
  Assert.That(FighterView.NativeLoopPhase(2.5f,2),Is.EqualTo(.25f).Within(.00001f));
  var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/MeshyWebIsland/web-motions.fbx");var obj=UnityEngine.Object.Instantiate(model);
  try{var view=obj.AddComponent<FighterView>();var hand=obj.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith(":LeftHand"));Assert.AreEqual(hand.position,view.BonePoint("LeftHand",Vector3.zero));}
  finally{UnityEngine.Object.DestroyImmediate(obj);}
 }
 static void ValidateNativeClip(GameObject model,AnimationClip clip) {
  var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LighFight.Editor.NativeFighterImport")).First(t=>t!=null);
  try{type.GetMethod("ValidateClip").Invoke(null,new object[]{model,clip});}catch(System.Reflection.TargetInvocationException e){throw e.InnerException;}
 }
 [Test] public void SourceRoughnessAndMetallicChannelsArePreserved()
 {
  var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LighFight.Editor.NativeMaterialImport")).First(t=>t!=null);
  var converted=(Color)type.GetMethod("ConvertChannels").Invoke(null,new object[]{new Color(.7f,.8f,.2f,1)});
  Assert.That(converted.r,Is.EqualTo(.2f).Within(.00001f));Assert.That(converted.a,Is.EqualTo(.2f).Within(.00001f));
 }
 [Test] public void NativeImporterRejectsFrozenOrForeignRigClips()
 {
  var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/MeshyWebIsland/web-motions.fbx");
  var clip=new AnimationClip();try{
   string path=AnimationUtility.CalculateTransformPath(model.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith(":LeftHand")),model.transform);
   clip.SetCurve(path,typeof(Transform),"m_LocalRotation.x",AnimationCurve.Constant(0,1,0));
   Assert.Throws<InvalidOperationException>(()=>ValidateNativeClip(model,clip));
   clip.SetCurve(path,typeof(Transform),"m_LocalRotation.x",AnimationCurve.Linear(0,0,1,.2f));
   Assert.DoesNotThrow(()=>ValidateNativeClip(model,clip));
   clip.SetCurve("DifferentRig/Hand",typeof(Transform),"m_LocalRotation.x",AnimationCurve.Linear(0,0,1,.2f));
   Assert.Throws<InvalidOperationException>(()=>ValidateNativeClip(model,clip));
  }finally{UnityEngine.Object.DestroyImmediate(clip);}
 }
 [Test] public void ComposedSceneRejectsObstaclesAndUndersizedFloors()
 {
  var root=new GameObject("Arena test");
  try {
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="FightFloor";floor.transform.SetParent(root.transform);floor.transform.position=new Vector3(0,-.1f,0);floor.transform.localScale=new Vector3(12,.2f,5);
   Assert.DoesNotThrow(()=>ArenaLayoutValidator.Validate(root));
   var prop=GameObject.CreatePrimitive(PrimitiveType.Cube);prop.name="Parked car";prop.transform.SetParent(root.transform);prop.transform.position=new Vector3(7,1,0);
   Assert.DoesNotThrow(()=>ArenaLayoutValidator.Validate(root));
   prop.transform.position=new Vector3(0,1,0);Assert.Throws<InvalidOperationException>(()=>ArenaLayoutValidator.Validate(root),"Blocked combat floor was accepted");
   UnityEngine.Object.DestroyImmediate(prop);floor.transform.localScale=new Vector3(9,.2f,5);Assert.Throws<InvalidOperationException>(()=>ArenaLayoutValidator.Validate(root),"Movement extends beyond floor");
  } finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public void ComposedArenaImporterPreservesPlacementAndRemovesDecorativePhysics()
 {
  const string folder="Assets/NativeArenaTestFixture";
  AssetDatabase.CreateFolder("Assets","NativeArenaTestFixture");var root=new GameObject("Composed scene");
  try {
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="FightFloor";floor.transform.SetParent(root.transform);floor.transform.localPosition=new Vector3(0,-.1f,0);floor.transform.localScale=new Vector3(12,.2f,5);
   var prop=GameObject.CreatePrimitive(PrimitiveType.Cube);prop.name="Courtyard wall";prop.transform.SetParent(root.transform);prop.transform.localPosition=new Vector3(7,1,0);
   var source=PrefabUtility.SaveAsPrefabAsset(root,folder+"/source.prefab");
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LighFight.Editor.MeshyArenaImport")).First(t=>t!=null);
   var result=(GameObject)type.GetMethod("Import").Invoke(null,new object[]{source,folder+"/arena.prefab",-1});
   Assert.IsNotNull(result.GetComponent<OfficeArena>());Assert.IsEmpty(result.GetComponentsInChildren<Collider>());
   Assert.AreEqual(new Vector3(7,1,0),result.GetComponentsInChildren<Transform>().Single(t=>t.name=="Courtyard wall").position);
   Assert.DoesNotThrow(()=>ArenaLayoutValidator.Validate(result));
  }finally{UnityEngine.Object.DestroyImmediate(root);AssetDatabase.DeleteAsset(folder);}
 }

 [Test] public void InspectorSurfaceAndSkeletonStayAtSameWorldScale()
 {
  var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LighFight.Editor.CharacterRigInspector")).First(t=>t!=null);
  var inspector=ScriptableObject.CreateInstance(type);const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  try {
   var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/MeshyWebIsland/web-motions.fbx");
   type.GetField("source",flags).SetValue(inspector,model);type.GetMethod("Load",flags).Invoke(inspector,null);
   var instance=(GameObject)type.GetField("instance",flags).GetValue(inspector);
   var surface=instance.GetComponentInChildren<MeshRenderer>();Assert.That(surface.bounds.size.y,Is.InRange(1.4f,2.1f),"FBX scale applied twice to sampled surface");
   var positions=(System.Collections.Generic.Dictionary<Transform,Vector3>)type.GetField("sampledBones",flags).GetValue(inspector);
   Assert.That(positions.Values.Max(v=>v.y),Is.InRange(1.4f,2.1f));
   Assert.IsTrue(positions.Values.All(v=>surface.bounds.Contains(v)||Vector3.Distance(surface.bounds.ClosestPoint(v),v)<.15f));
  }finally{UnityEngine.Object.DestroyImmediate(inspector);}
 }

 [Test] public void NativeRosterUsesDistinctBackwardMotionAndDancePreview()
 {
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-3.prefab");var obj=UnityEngine.Object.Instantiate(prefab);
  try {
   var view=obj.GetComponent<FighterView>();var animator=obj.GetComponent<Animator>();Assert.IsTrue(view.UsesNativeMotion);Assert.IsTrue(view.HasNativeMotion("Dance"));
   view.Pose(new LighFight.Core.Fighter(),1,0);Assert.IsTrue(view.PreviewNativeMotion("Dance",.2f));Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Dance"));
   Assert.IsFalse(view.PreviewNativeMotion("Unknown motion",.2f));
   var controller=animator.runtimeAnimatorController;Assert.IsTrue(controller.animationClips.Any(c=>c.name.Contains("Walk_Fight_Back")));Assert.IsTrue(controller.animationClips.Any(c=>c.name.Contains("Walk_Fight_Forward")));
  }finally{UnityEngine.Object.DestroyImmediate(obj);}
 }

}
