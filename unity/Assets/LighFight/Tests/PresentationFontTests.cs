using NUnit.Framework;
using UnityEngine;
namespace LighFight.Tests {
 public sealed class PresentationFontTests {
  [Test]public void BundledFontContainsRussianFightLabels(){
   var font=Resources.Load<Font>("Fonts/NotoSans-Regular");Assert.NotNull(font,"Web cannot rely on macOS fallback fonts");
   foreach(char c in "БойцыРозоваяпантераВыберисоперникаИзменитьправилаНастройкиВБОЙ150")Assert.True(font.HasCharacter(c),"Missing glyph: "+c);
  }
 }
}
