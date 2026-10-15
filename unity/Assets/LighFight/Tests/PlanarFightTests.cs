using NUnit.Framework;
using LighFight.Core;
public sealed class PlanarFightTests {
 [Test]public void SessionIgnoresSideInputWithoutSlowingForwardMovement(){
  var session=new FightSession();var match=session.Match;float start=match.A.X;
  match.Walk(match.A,1,1);Assert.That(match.A.X-start,Is.EqualTo(.045f).Within(.00001f));Assert.Zero(match.A.Z);
  start=match.A.X;for(int i=0;i<100;i++)match.Walk(match.A,0,-1);
  Assert.AreEqual(start,match.A.X);Assert.Zero(match.A.Z);
 }
 [Test]public void ContactAndKnockbackRemainOnPlaneAfterRestoringExternalDepth(){
  var session=new FightSession();var match=session.Match;match.A.X=0;match.B.X=1;match.A.Z=.2f;match.B.Z=-.4f;
  Assert.True(match.Attack(match.A,0));Assert.Zero(match.A.Z);Assert.Zero(match.B.Z);Assert.Zero(match.A.FacingZ);
  for(int i=0;i<20;i++)session.Step();Assert.Less(match.B.Health,150);Assert.Zero(match.A.Z);Assert.Zero(match.B.Z);
 }
 [Test]public void BotEvadesAndApproachesOnSamePlane(){
  var session=new FightSession();session.Match.A.X=0;session.Match.B.X=1.7f;session.Match.A.Start(10);var bot=new BotController(1);
  float start=session.Match.B.X;bool moved=false;
  for(int i=0;i<300;i++){bot.Step(session);session.Step();moved|=System.Math.Abs(session.Match.B.X-start)>.01f;Assert.Zero(session.Match.A.Z);Assert.Zero(session.Match.B.Z);Assert.Zero(session.Match.B.FacingZ);}
  Assert.True(moved,"Removing sidestep must not leave the bot unable to move");
 }
 [Test]public void NextRoundKeepsThePlaneAndNoWalkCanCrossOpponent(){
  var session=new FightSession(false,3,30);session.Match.B.Health=0;session.Step();for(int i=0;i<FightSession.BreakTicks;i++)session.Step();Assert.AreEqual(2,session.Round);
  for(int i=0;i<400;i++){session.Match.Walk(session.Match.A,1,1);session.Match.Walk(session.Match.B,-1,-1);Assert.Zero(session.Match.A.Z);Assert.Zero(session.Match.B.Z);Assert.LessOrEqual(session.Match.A.X+Match.MinimumSeparation-.00001f,session.Match.B.X);}
 }
}
