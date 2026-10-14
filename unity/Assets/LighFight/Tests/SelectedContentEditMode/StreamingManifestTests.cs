using NUnit.Framework;using LighFight.Core;
public class StreamingManifestTests {
 ContentManifest Valid()=>new ContentManifest{minimumPlayerVersion=2,contentVersion="selected-1",unityVersion="6000.3.26f1",platform="iOS",catalogUrl="https://cdn.example.org/ios/catalog.bin"};
 [Test]public void StreamingPlayerTwoAccepted(){Assert.IsNull(Valid().Validate("iOS","6000.3.26f1"));}
 [Test]public void LegacyPlayerOneStillAccepted(){var m=Valid();m.minimumPlayerVersion=1;Assert.IsNull(m.Validate("iOS","6000.3.26f1"));}
 [Test]public void FuturePlayerRejected(){var m=Valid();m.minimumPlayerVersion=3;Assert.AreEqual("incompatible_player",m.Validate("iOS","6000.3.26f1"));}
}
