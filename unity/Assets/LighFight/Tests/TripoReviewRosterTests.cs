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
  [TestCase(0)][TestCase(3)][TestCase(4)][TestCase(5)]
  public void DirectPunchUsesTheSourceClipsStrikingHand(int index){
   var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var view=content.fighters[index].GetComponent<FighterView>();
   Assert.AreEqual(1,view.NativeStrikeSide("jab"),"Left_Jab_from_Guard must sample the left hand");
   Assert.AreEqual(2,view.NativeStrikeSide("cross"),"Right_Jab_from_Guard must sample the right hand, not the stationary guard hand");
  }
  [TestCase(0)][TestCase(3)][TestCase(4)][TestCase(5)]
  public void CharacterPreviewPlaysTheWholeStrikeIncludingRecovery(int index){
   var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var actor=Object.Instantiate(content.fighters[index]);
   try{
    var view=actor.GetComponent<FighterView>();view.FullPreviewMotion=true;
    var fighter=new LighFight.Core.Fighter();fighter.Start(0);
    fighter.Frame=Mathf.RoundToInt(fighter.Attack.Value.Duration*.9f);
    view.Pose(fighter,1,0);
    var state=actor.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0);
    Assert.True(state.IsName("jab"));
    Assert.That(state.normalizedTime,Is.EqualTo((float)fighter.Frame/fighter.Attack.Value.Duration).Within(.001f),"Preview must sample the original clip recovery rather than the compressed combat contact window");
    Assert.That(view.PreviewDuration("jab"),Is.GreaterThan(0));
   }finally{Object.DestroyImmediate(actor);}
  }
 }
}
