using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class HostFightCatalogTests {
  const string TemporaryAsset="Assets/HostFightCatalogRegression.asset";
  private static GameContent SnapshotCatalog(GameContent source,string assetPath){
   var setup=System.Type.GetType("LighFight.Editor.HostFightReviewSetup, Assembly-CSharp-Editor");
   Assert.NotNull(setup,"Host review editor setup must be available");
   var snapshot=setup.GetMethod("SnapshotCatalog",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
   Assert.NotNull(snapshot,"Host review setup must expose catalog snapshots");
   return (GameContent)snapshot.Invoke(null,new object[]{source,assetPath});
  }
  [TearDown] public void Cleanup(){AssetDatabase.DeleteAsset(TemporaryAsset);}
  [TestCase(2)][TestCase(8)][TestCase(11)] public void ReviewSnapshotKeepsEveryApprovedFighterAndArena(int count){
   var source=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
   var expanded=Object.Instantiate(source);expanded.fighters=new GameObject[count];
   for(int i=0;i<count;i++)expanded.fighters[i]=source.fighters[i%source.fighters.Length];
   source=expanded;
   try{
    var review=SnapshotCatalog(source,TemporaryAsset);
    Assert.AreEqual(count,review.fighters.Length);Assert.AreEqual(5,review.arenas.Length);
    for(int i=0;i<count;i++)Assert.AreSame(source.fighters[i],review.fighters[i],"Fighter slot "+i);
    for(int i=0;i<5;i++)Assert.AreSame(source.arenas[i],review.arenas[i],"Arena slot "+i);
    Assert.AreNotSame(source.fighters,review.fighters);Assert.AreNotSame(source.arenas,review.arenas);
   }finally{Object.DestroyImmediate(expanded);}
  }
  [Test] public void RepeatedSnapshotUpdatesChangedCatalogWithoutReplacingAssetGuid(){
   var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset"));
   try{
    SnapshotCatalog(source,TemporaryAsset);string guid=AssetDatabase.AssetPathToGUID(TemporaryAsset);
    source.fighterMetadata=new[]{new GameContent.FighterMetadata{id="catalog-custom",nameRu="Обновлённый",nameEn="Updated"}};
    var fighter=source.fighters[0];source.fighters[0]=source.fighters[4];source.fighters[4]=fighter;
    var arena=source.arenas[0];source.arenas[0]=source.arenas[3];source.arenas[3]=arena;
    SnapshotCatalog(source,TemporaryAsset);AssetDatabase.SaveAssets();
    var review=AssetDatabase.LoadAssetAtPath<GameContent>(TemporaryAsset);
    Assert.AreEqual(guid,AssetDatabase.AssetPathToGUID(TemporaryAsset));
    Assert.AreNotSame(source.fighterMetadata,review.fighterMetadata);
    Assert.AreEqual("catalog-custom",review.fighterMetadata[0].id);Assert.AreEqual("Updated",review.fighterMetadata[0].nameEn);
    for(int i=0;i<source.fighters.Length;i++)Assert.AreSame(source.fighters[i],review.fighters[i],"Updated fighter slot "+i);
    for(int i=0;i<5;i++)Assert.AreSame(source.arenas[i],review.arenas[i],"Updated arena slot "+i);
   }finally{Object.DestroyImmediate(source);}
  }
 }
}
