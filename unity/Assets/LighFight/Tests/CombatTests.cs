using NUnit.Framework;
using LighFight.Core;
public class CombatTests {
 [Test]public void WhiffDoesNotDamage(){var m=new Match();m.A.Start(0);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(150,m.B.Health);}
 [Test]public void StartupPreventsEarlyDamage(){var m=new Match();m.B.X=0;m.A.Start(0);for(int i=0;i<10;i++)m.Step();Assert.AreEqual(150,m.B.Health);m.Step();Assert.AreEqual(144,m.B.Health);}
 [Test]public void SingleAttackHitsOnce(){var m=new Match();m.B.X=0;m.A.Start(0);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(144,m.B.Health);}
 [Test]public void StandingGuardDoesNotBlockLow(){var m=new Match();m.B.X=0;m.B.Guard=true;m.A.Start(5);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(140,m.B.Health);}
 [Test]public void CrouchingGuardBlocksLow(){var m=new Match();m.B.X=0;m.B.Guard=true;m.B.Crouch=true;m.A.Start(5);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(150,m.B.Health);}
 [Test]public void SimultaneousJabsTrade(){var m=new Match();m.B.X=0;m.A.Start(0);m.B.Start(0);for(int i=0;i<20;i++)m.Step();Assert.AreEqual(144,m.A.Health);Assert.AreEqual(144,m.B.Health);}
 [Test]public void RecoveryPreventsAttackSpam(){var f=new Fighter();Assert.IsTrue(f.Start(0));Assert.IsFalse(f.Start(1));}
 [Test]public void FightersCannotWalkThroughEachOther(){var m=new Match();for(int i=0;i<200;i++)m.Walk(m.A,1);Assert.GreaterOrEqual(m.B.X-m.A.X,.75f);}
 [Test]public void HitInterruptsSlowerAttack(){var m=new Match();m.B.X=0;m.A.Start(0);m.B.Start(10);for(int i=0;i<50;i++)m.Step();Assert.AreEqual(150,m.A.Health);Assert.IsNull(m.B.Attack);}
 [Test]public void StandingGuardBlocksMid(){var m=new Match();m.A.X=-1;m.B.X=0;m.B.Guard=true;m.A.Start(3);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(150,m.B.Health);}
 [Test]public void CrouchingGuardDoesNotBlockMid(){var m=new Match();m.A.X=-1;m.B.X=0;m.B.Guard=true;m.B.Crouch=true;m.A.Start(3);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(140,m.B.Health);}
 [Test]public void CrouchEvadesHigh(){var m=new Match();m.B.X=0;m.B.Crouch=true;m.A.Start(0);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(150,m.B.Health);}
 [Test]public void FinishedMatchRejectsInputs(){var m=new Match();m.B.Health=0;Assert.IsFalse(m.Attack(m.A,0));m.Walk(m.A,1);m.Step();Assert.AreEqual(-1.4f,m.A.X);Assert.AreEqual(0,m.Tick);}
 [Test]public void StunPreventsCounterAttack(){var m=new Match();m.B.X=0;m.A.Start(0);for(int i=0;i<11;i++)m.Step();Assert.IsFalse(m.B.Start(0));}
 [Test]public void InvalidInputDoesNotCorruptPosition(){var m=new Match();m.Walk(m.A,float.NaN);Assert.AreEqual(-1.4f,m.A.X);Assert.IsFalse(m.A.Start(-1));Assert.IsFalse(m.A.Start(99));}
 [Test]public void WallStopsWalk(){var m=new Match();for(int i=0;i<500;i++)m.Walk(m.A,-1);Assert.AreEqual(-5f,m.A.X);}
}

public class FightSessionTests
{
 [Test] public void BufferedAttackStartsAfterRecovery() {
  var s=new FightSession(); s.Match.A.Start(0);
  for(int i=0;i<Moves.All[0].Duration-3;i++)s.Step();
  s.QueueAttack(true,1); for(int i=0;i<4;i++)s.Step();
  Assert.AreEqual("cross",s.Match.A.Attack.Value.Id);
 }
 [Test] public void ExpiredBufferDoesNotFireLater() {
  var s=new FightSession();s.Match.A.Start(10);s.QueueAttack(true,1);
  for(int i=0;i<100;i++)s.Step();Assert.IsNull(s.Match.A.Attack);
 }
 [Test] public void TimeoutAwardsHealthLeaderAtExactTick() {
  var s=new FightSession();s.Match.B.Health=140;
  for(int i=0;i<FightSession.RoundTicks-1;i++)s.Step();
  Assert.AreEqual(FightPhase.Fighting,s.Phase);s.Step();
  Assert.AreEqual(1,s.WinsA);Assert.AreEqual(FightPhase.RoundOver,s.Phase);
 }
 [Test] public void NextRoundResetsHealthAndPendingAttack() {
  var s=new FightSession();s.Match.B.Health=0;s.QueueAttack(true,1);s.Step();
  Assert.IsFalse(s.QueueAttack(true,2));
  for(int i=0;i<FightSession.BreakTicks-1;i++)s.Step();
  Assert.AreEqual(FightPhase.RoundOver,s.Phase);s.Step();
  Assert.AreEqual(2,s.Round);Assert.AreEqual(150,s.Match.B.Health);
  s.Step();Assert.IsNull(s.Match.A.Attack);
 }
 [Test] public void TwoWinsCompleteAndFreezeSession() {
  var s=new FightSession();s.Match.A.Health=0;s.Step();
  for(int i=0;i<FightSession.BreakTicks;i++)s.Step();
  s.Match.A.Health=0;s.Step();Assert.AreEqual(FightPhase.Complete,s.Phase);
  Assert.AreEqual(1,s.Winner);int clock=s.RemainingTicks;s.Step();
  Assert.AreEqual(clock,s.RemainingTicks);Assert.IsFalse(s.QueueAttack(false,0));
 }
 [Test] public void DrawAwardsNeitherFighter() {
  var s=new FightSession();s.Match.A.Health=s.Match.B.Health=0;s.Step();
  Assert.AreEqual(-1,s.RoundWinner);Assert.AreEqual(0,s.WinsA+s.WinsB);
  Assert.AreEqual(FightPhase.RoundOver,s.Phase);
 }
 [Test] public void InvalidQueueDoesNotReplaceValidInput() {
  var s=new FightSession();s.QueueAttack(true,0);Assert.IsFalse(s.QueueAttack(true,99));
  s.Step();Assert.AreEqual("jab",s.Match.A.Attack.Value.Id);
 }
}

public class CombatReactionTests
{
 [Test] public void CounterHitDealsExtraDamageAndLongerStun() {
  var m=new Match();m.B.X=0;m.A.Start(0);m.B.Start(10);
  for(int i=0;i<11;i++)m.Step();
  Assert.AreEqual(143,m.B.Health);Assert.IsTrue(m.B.LastCounter);Assert.AreEqual(25,m.B.Stun);
 }
 [Test] public void SweepKnocksDownAndRecoveryRestoresControl() {
  var m=new Match();m.A.X=-1;m.B.X=0;m.A.Start(6);
  for(int i=0;i<26;i++)m.Step();Assert.AreEqual(63,m.B.DownTicks);
  float x=m.B.X;m.Walk(m.B,1);Assert.AreEqual(x,m.B.X);Assert.IsFalse(m.B.Start(0));
  for(int i=0;i<63;i++)m.Step();Assert.AreEqual(0,m.B.DownTicks);Assert.IsTrue(m.B.Start(0));
 }
 [Test] public void BlockedSweepDoesNotKnockDown() {
  var m=new Match();m.A.X=-1;m.B.X=0;m.B.Guard=m.B.Crouch=true;m.A.Start(6);
  for(int i=0;i<26;i++)m.Step();Assert.AreEqual(0,m.B.DownTicks);Assert.IsTrue(m.B.LastBlocked);Assert.AreEqual(150,m.B.Health);
 }
 [Test] public void GroundedFighterCannotBeHitRepeatedly() {
  var m=new Match();m.A.X=-1;m.B.X=0;m.B.DownTicks=30;m.A.Start(0);
  for(int i=0;i<15;i++)m.Step();Assert.AreEqual(150,m.B.Health);
 }
 [Test] public void EveryMoveHasReachableDamageWindow() {
  Assert.AreEqual(20,Moves.All.Length);
  foreach(var move in Moves.All) { var m=new Match();m.A.X=-.8f;m.B.X=0;
   m.A.Start(System.Array.IndexOf(Moves.All,move));for(int i=0;i<move.Duration;i++)m.Step();
   Assert.AreEqual(150-move.Damage,m.B.Health,move.Id);
  }
 }
}

public class ContentManifestTests
{
 private ContentManifest Valid() => new ContentManifest { contentVersion="starter-1", unityVersion="6000.3.26f1", platform="iOS", catalogUrl="https://cdn.example.org/ios/catalog.json", downloadBytes=1024 };
 [Test] public void AcceptsCompatibleHttpsManifest(){Assert.IsNull(Valid().Validate("iOS","6000.3.26f1"));}
 [Test] public void RejectsWrongPlatformAndEngine(){Assert.AreEqual("incompatible_platform",Valid().Validate("Android","6000.3.26f1"));Assert.AreEqual("incompatible_platform",Valid().Validate("iOS","6000.3.27f1"));}
 [Test] public void RejectsFuturePlayer(){var m=Valid();m.minimumPlayerVersion=2;Assert.AreEqual("incompatible_player",m.Validate("iOS","6000.3.26f1"));}
 [Test] public void RejectsUnsafeCatalog(){foreach(var url in new[]{"file:///tmp/catalog.json","http://cdn.example.org/x","https://user:pass@cdn.example.org/x"}){var m=Valid();m.catalogUrl=url;Assert.AreEqual("invalid_catalog",m.Validate("iOS","6000.3.26f1"));}}
 [Test] public void LocalHttpRequiresDevelopment(){var m=Valid();m.catalogUrl="http://127.0.0.1:8765/catalog.json";Assert.IsNotNull(m.Validate("iOS","6000.3.26f1"));Assert.IsNull(m.Validate("iOS","6000.3.26f1",true));}
 [Test] public void RejectsInvalidVersionAndExcessiveSize(){var m=Valid();m.contentVersion="../x";Assert.AreEqual("invalid_version",m.Validate("iOS","6000.3.26f1"));m=Valid();m.downloadBytes=long.MaxValue;Assert.AreEqual("invalid_size",m.Validate("iOS","6000.3.26f1"));}
}
