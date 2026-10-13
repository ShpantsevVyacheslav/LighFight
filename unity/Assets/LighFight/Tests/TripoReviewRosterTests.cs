using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class TripoReviewRosterTests {
  [TestCase(0,"Bone")][TestCase(3,"Island")][TestCase(5,"Ranger")]
  public void ReviewUsesCalibratedTripoCharacter(int index,string name){
   var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Art/Drafts/TripoPlayable/ReviewContent.asset");
   var shipping=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");Assert.AreSame(content.fighters[index],shipping.fighters[index],"Next app build must use the tested replacement");
   var prefab=content.fighters[index];Assert.That(AssetDatabase.GetAssetPath(prefab),Does.Contain("Tripo"+name+"Playable/"));
   var view=prefab.GetComponent<FighterView>();Assert.True(view.IsReady);
   var animator=prefab.GetComponentInChildren<Animator>();Assert.True(animator.avatar.isHuman&&animator.avatar.isValid);
   var skin=prefab.GetComponentInChildren<SkinnedMeshRenderer>();Assert.AreEqual(100000,skin.sharedMesh.triangles.Length/3);
   Assert.AreEqual(67,skin.bones.Length);Assert.True(view.HasNativeMotion("jab"));Assert.False(view.HasNativeMotion("sweep"));
  }
  [Test]public void IslandReviewKeepsOriginalEightKAlbedo(){
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LighFight/Art/Drafts/TripoIslandPlayable/Source/Island/island-h31-unity-native-pack_basecolor.JPEG");
   Assert.NotNull(texture);Assert.AreEqual(8192,texture.width);Assert.AreEqual(8192,texture.height);
  }
 }
}
