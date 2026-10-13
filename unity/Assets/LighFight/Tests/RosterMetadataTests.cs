using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class RosterMetadataTests {
  [Test] public void ElevenFightersUseMetadataAndKeepLegacyPortraitFallback(){
   var root=new GameObject("Roster metadata");var catalog=ScriptableObject.CreateInstance<GameContent>();var portrait=new Texture2D(1,1);
   try{
    catalog.fighters=new GameObject[11];catalog.fighterMetadata=new GameContent.FighterMetadata[11];
    catalog.fighterMetadata[8]=new GameContent.FighterMetadata{id="new-fighter",nameRu="Новый боец",nameEn="New Fighter",portrait=portrait};
    var game=root.AddComponent<Prototype>();game.content=catalog;
    Assert.AreEqual(11,game.RosterCount);Assert.AreEqual("new-fighter",game.FighterId(8));Assert.AreEqual("Новый боец",game.FighterLabel(8));Assert.AreSame(portrait,game.FighterPortrait(8));
    Assert.AreEqual("Костяная леди",game.FighterLabel(0));Assert.AreEqual("fighter-0",game.FighterId(0));
    Assert.AreSame(Resources.Load<Texture2D>("Selection/fighter-0"),game.FighterPortrait(0));
    Assert.AreSame(Resources.Load<Texture2D>("Selection/fighter-10"),game.FighterPortrait(10),"New portrait fallback must use its own resource slot");
    Assert.AreEqual(2,game.RosterPageCount);Assert.AreEqual(8,game.RosterPageStart(1));Assert.AreEqual(11,game.RosterPageEnd(1));
    typeof(Prototype).GetField("russian",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,false);
    Assert.AreEqual("New Fighter",game.FighterLabel(8));Assert.AreEqual("Fighter 11",game.FighterLabel(10));
   }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(catalog);Object.DestroyImmediate(portrait);}
  }
  [UnityTest] public IEnumerator ExpandedRosterArrowsWrapTheActualCatalog(){
   yield return new EnterPlayMode();
   var source=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");var catalog=Object.Instantiate(source);
   catalog.fighters=new GameObject[11];for(int i=0;i<11;i++)catalog.fighters[i]=source.fighters[i%source.fighters.Length];
   var root=new GameObject("Expanded roster navigation");var game=root.AddComponent<Prototype>();game.content=catalog;yield return null;yield return null;
   var field=typeof(Prototype).GetField("selectedFighter",BindingFlags.Instance|BindingFlags.NonPublic);
   game.SelectRosterFighter(-1);int last=(int)field.GetValue(game);
   game.SelectRosterFighter(11);int first=(int)field.GetValue(game);
   game.SelectRosterFighter(-23);int repeated=(int)field.GetValue(game);
   Object.Destroy(root);Object.Destroy(catalog);yield return null;yield return new ExitPlayMode();
   Assert.AreEqual(10,last);Assert.AreEqual(0,first);Assert.AreEqual(10,repeated);
  }
 }
}
