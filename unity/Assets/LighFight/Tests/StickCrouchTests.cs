using NUnit.Framework;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Tests {
 public sealed class StickCrouchTests {
  static bool Crouch(float y,bool held){var method=typeof(Prototype).GetMethod("StickCrouchIntent");Assert.NotNull(method,"Crouch must come from downward joystick intent");return (bool)method.Invoke(null,new object[]{y,held});}
  [Test]public void DownCrouchesAndNeutralReleases(){Assert.True(Crouch(-.8f,false));Assert.False(Crouch(0,true));Assert.False(Crouch(.9f,false));}
  [Test]public void ThresholdHasHysteresis(){Assert.False(Crouch(-.4f,false));Assert.True(Crouch(-.4f,true));Assert.False(Crouch(-.2f,true));}
  [Test]public void DiagonalKeepsVerticalIntent(){var normalized=Prototype.NormalizeStick(new Rect(0,0,160,160),new Vector2(115,130));Assert.Less(normalized.y,-.55f);Assert.Greater(normalized.x,0);Assert.True(Crouch(normalized.y,false));}
 }
}
