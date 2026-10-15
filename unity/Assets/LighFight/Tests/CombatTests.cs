using NUnit.Framework;
using LighFight.Core;
public class CombatTests {
 [Test]public void StartingAnAttackDropsGuardButRejectedInputKeepsIt(){
  var fighter=new Fighter{Guard=true};Assert.False(fighter.Start(-1));Assert.True(fighter.Guard);
  Assert.True(fighter.Start(0));Assert.False(fighter.Guard,"Attack input must leave defence instead of blocking during the strike");
 }
 [Test]public void WhiffDoesNotDamage(){var m=new Match();m.A.Start(0);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(150,m.B.Health);}
 [Test]public void StartupPreventsEarlyDamage(){var m=new Match();m.B.X=0;m.A.Start(0);for(int i=0;i<10;i++)m.Step();Assert.AreEqual(150,m.B.Health);m.Step();Assert.AreEqual(144,m.B.Health);}
 [Test]public void SingleAttackHitsOnce(){var m=new Match();m.B.X=0;m.A.Start(0);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(144,m.B.Health);}
 [Test]public void StandingGuardDoesNotBlockLow(){var m=new Match();m.B.X=0;m.B.Guard=true;m.A.Start(5);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(140,m.B.Health);}
 [Test]public void CrouchingGuardBlocksLow(){var m=new Match();m.B.X=0;m.B.Guard=true;m.B.Crouch=true;m.A.Start(5);for(int i=0;i<40;i++)m.Step();Assert.AreEqual(150,m.B.Health);}
 [Test]public void SimultaneousJabsTrade(){var m=new Match();m.B.X=0;m.A.Start(0);m.B.Start(0);for(int i=0;i<20;i++)m.Step();Assert.AreEqual(144,m.A.Health);Assert.AreEqual(144,m.B.Health);}
 [Test]public void RecoveryPreventsAttackSpam(){var f=new Fighter();Assert.IsTrue(f.Start(0));Assert.IsFalse(f.Start(1));}
 [Test]public void FightersCannotWalkThroughEachOther(){var m=new Match();for(int i=0;i<200;i++)m.Walk(m.A,1);Assert.GreaterOrEqual(m.B.X-m.A.X,Match.MinimumSeparation);}
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
  for(int i=0;i<26;i++)m.Step();Assert.AreEqual(Match.KnockdownTicks-1,m.B.DownTicks);
  float x=m.B.X;m.Walk(m.B,1);Assert.AreEqual(x,m.B.X);Assert.IsFalse(m.B.Start(0));
  for(int i=0;i<Match.KnockdownTicks-1;i++)m.Step();Assert.AreEqual(0,m.B.DownTicks);Assert.IsTrue(m.B.Start(0));
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
  Assert.AreEqual(25,Moves.All.Length);
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
 [Test] public void RejectsFuturePlayer(){var m=Valid();m.minimumPlayerVersion=ContentManifest.CurrentPlayerVersion+1;Assert.AreEqual("incompatible_player",m.Validate("iOS","6000.3.26f1"));}
 [Test] public void RejectsUnsafeCatalog(){foreach(var url in new[]{"file:///tmp/catalog.json","http://cdn.example.org/x","https://user:pass@cdn.example.org/x"}){var m=Valid();m.catalogUrl=url;Assert.AreEqual("invalid_catalog",m.Validate("iOS","6000.3.26f1"));}}
 [Test] public void LocalHttpRequiresDevelopment(){var m=Valid();m.catalogUrl="http://127.0.0.1:8765/catalog.json";Assert.IsNotNull(m.Validate("iOS","6000.3.26f1"));Assert.IsNull(m.Validate("iOS","6000.3.26f1",true));}
 [Test] public void RejectsInvalidVersionAndExcessiveSize(){var m=Valid();m.contentVersion="../x";Assert.AreEqual("invalid_version",m.Validate("iOS","6000.3.26f1"));m=Valid();m.downloadBytes=long.MaxValue;Assert.AreEqual("invalid_size",m.Validate("iOS","6000.3.26f1"));}
}

public class TrainingTests
{
 [Test] public void TrainingDoesNotTimeoutOrLoseHealth() {
  var s=new FightSession(true);s.Match.A.X=-1;s.Match.B.X=0;s.QueueAttack(true,0);
  for(int i=0;i<FightSession.RoundTicks+1;i++)s.Step();
  Assert.AreEqual(150,s.Match.B.Health);Assert.AreEqual(FightPhase.Fighting,s.Phase);
  Assert.AreEqual(FightSession.RoundTicks,s.RemainingTicks);Assert.AreEqual(1,s.Match.B.HitSerial);
 }
}

public class BotTests
{
 [Test] public void BotApproachesBeforeAttacking(){var s=new FightSession();var b=new BotController();b.Step(s);Assert.Less(s.Match.B.X,1.4f);Assert.IsNull(s.Match.B.Attack);}
 [Test] public void BotHasReactionDelay(){var s=new FightSession();s.Match.A.Start(10);var b=new BotController();for(int i=0;i<13;i++){b.Step(s);s.Step();Assert.IsFalse(s.Match.B.Guard);}}
 [Test] public void TrainingDummyDoesNotAttackOrMove(){var s=new FightSession(true);var b=new BotController();for(int i=0;i<100;i++){b.Step(s);s.Step();}Assert.AreEqual(1.4f,s.Match.B.X);Assert.IsNull(s.Match.B.Attack);}
}

public class ContentBudgetTests
{
 [Test] public void RequiresTemporarySpaceButAllowsCachedContent() { Assert.IsFalse(ContentBudget.CanDownload(1024,1024));Assert.IsTrue(ContentBudget.CanDownload(1024,32L*1024*1024+2048));Assert.IsTrue(ContentBudget.CanDownload(0,0)); }
 [Test] public void RejectsUnboundedSizeEvenWhenFreeSpaceUnknown() { Assert.IsFalse(ContentBudget.CanDownload(long.MaxValue,-1));Assert.IsFalse(ContentBudget.CanDownload(-1,-1));Assert.IsTrue(ContentBudget.CanDownload(1024,-1)); }
}

public class MotionTimingTests
{
        [NUnit.Framework.Test]
        public void AnimatedContactAlignsWithTheFirstDamagingTick()
        {
            foreach(var move in Moves.All)
            {
                var match=new Match();match.A.X=0;match.B.X=.5f;
                match.A.Start(System.Array.IndexOf(Moves.All,move));
                for(int n=0;n<move.Startup;n++)match.Step();
                NUnit.Framework.Assert.That(match.B.Health,NUnit.Framework.Is.EqualTo(150));
                NUnit.Framework.Assert.That(MotionTiming.Phase(move,match.A.Frame,.43f),NUnit.Framework.Is.EqualTo(.43f).Within(.0001));
                match.Step();NUnit.Framework.Assert.That(match.B.Health,NUnit.Framework.Is.LessThan(150));
                float previous=-1;
                for(int frame=0;frame<=move.Duration;frame++)
                { var phase=MotionTiming.Phase(move,frame,.43f);NUnit.Framework.Assert.That(phase,NUnit.Framework.Is.GreaterThanOrEqualTo(previous));previous=phase; }
                NUnit.Framework.Assert.That(previous,NUnit.Framework.Is.EqualTo(1).Within(.0001));
            }
        }
}

public class ChainTests
{
 [Test] public void CleanJabCanChainCrossButWhiffAndBlockCannot(){
  foreach(int situation in new[]{0,1,2}){var m=new Match();m.A.X=0;m.B.X=situation==1?4:1;m.B.Guard=situation==2;m.Attack(m.A,0);
   for(int n=0;n<14;n++)m.Step();
   Assert.AreEqual(situation==0,m.Attack(m.A,1));
   if(situation==0){Assert.AreEqual("cross",m.A.Attack.Value.Id);Assert.AreEqual(2,m.A.ComboCount);}
  }
 }
 [Test] public void ChainCannotCancelStartupOrUseUnlistedMove(){var m=new Match();m.A.X=0;m.B.X=1;m.Attack(m.A,0);Assert.IsFalse(m.Attack(m.A,1));for(int n=0;n<14;n++)m.Step();Assert.IsFalse(m.Attack(m.A,10));}
 [Test] public void ChainWindowExpires(){var m=new Match();m.A.X=0;m.B.X=1;m.Attack(m.A,0);for(int n=0;n<21;n++)m.Step();Assert.IsFalse(m.Attack(m.A,1));}
}

public class MatchConfigurationTests
{
 [Test] public void ChosenTimerAndWinsAreAppliedAcrossRounds(){var s=new FightSession(false,1,90);Assert.AreEqual(5400,s.RemainingTicks);Assert.AreEqual(1,s.TargetWins);s.Match.B.Health=0;s.Step();Assert.AreEqual(FightPhase.Complete,s.Phase);}
 [Test] public void TimerResetsToChosenLength(){var s=new FightSession(false,3,90);s.Match.B.Health=0;s.Step();for(int i=0;i<FightSession.BreakTicks;i++)s.Step();Assert.AreEqual(5400,s.RemainingTicks);Assert.AreEqual(2,s.Round);}
 [Test] public void InvalidConfigurationIsBounded(){var s=new FightSession(false,100,-1);Assert.AreEqual(3,s.TargetWins);Assert.AreEqual(1800,s.RemainingTicks);}
}

public class SpatialCombatTests
{
 [Test] public void SidestepHasBoundedNormalizedSpeed(){var m=new Match();float x=m.A.X;m.Walk(m.A,1,1);Assert.That(Match.Distance(m.A,new Fighter{X=x}),Is.EqualTo(.045f).Within(.0001));for(int i=0;i<200;i++)m.Walk(m.A,0,1);Assert.AreEqual(1.5f,m.A.Z);}
 [Test] public void DepthSeparationPreventsDistanceHit(){var m=new Match();m.A.X=0;m.B.X=1;m.B.Z=1.5f;m.Attack(m.A,0);for(int i=0;i<20;i++)m.Step();Assert.AreEqual(150,m.B.Health);}
 [Test] public void RequiredPhysicalContactFailsClosed(){var m=new Match{RequirePhysicalContact=true};m.A.X=0;m.B.X=1;m.Attack(m.A,0);for(int i=0;i<20;i++)m.Step();Assert.AreEqual(150,m.B.Health);}
 [Test] public void GeometricMissCannotDealDamage(){var m=new Match{RequirePhysicalContact=true,ContactValidator=(a,b)=>false};m.A.X=0;m.B.X=1;m.Attack(m.A,0);for(int i=0;i<20;i++)m.Step();Assert.AreEqual(150,m.B.Health);}
 [Test] public void GeometricContactOnlyDamagesOnce(){var m=new Match{RequirePhysicalContact=true,ContactValidator=(a,b)=>true};m.A.X=0;m.B.X=1;m.Attack(m.A,0);for(int i=0;i<30;i++)m.Step();Assert.AreEqual(144,m.B.Health);Assert.AreEqual(1,m.B.HitSerial);}
 [Test] public void SweptLimbCrossesBodyEvenWhenEndpointsMiss(){Assert.That(HitGeometry.SegmentDistanceSquared(-1,1,0,1,1,0,0,.5f,0,0,1.5f,0),Is.EqualTo(0).Within(.0001));Assert.Greater(HitGeometry.SegmentDistanceSquared(-1,1,1,1,1,1,0,.5f,0,0,1.5f,0),.9f);}
 [Test] public void FacingLocksDuringAttack(){var m=new Match();m.Attack(m.A,0);m.B.Z=1;Assert.AreEqual(0,m.A.FacingZ);Assert.IsFalse(m.Attack(m.A,1));Assert.AreEqual(0,m.A.FacingZ);}
}

public class GestureAndComboTests
{
 [TestCase(80,0,1)] [TestCase(-80,0,2)] [TestCase(0,80,7)] [TestCase(0,-80,3)]
 [TestCase(60,60,15)] [TestCase(-60,60,14)] [TestCase(60,-60,5)] [TestCase(-60,-60,6)]
 public void SwipeDirectionChoosesStrike(float x,float y,int move){Assert.AreEqual(move,StrikeGesture.Classify(x,y,.2f));}
 [Test] public void TapJitterHoldAndCancelDoNotBecomeSwipes(){Assert.AreEqual(0,StrikeGesture.Classify(2,3,.1f));Assert.AreEqual(-1,StrikeGesture.Classify(20,0,.2f));Assert.AreEqual(-1,StrikeGesture.Classify(1,1,.4f));Assert.AreEqual(-1,StrikeGesture.Classify(80,0,.9f));Assert.AreEqual(-1,StrikeGesture.Classify(float.NaN,0,.1f));var g=new StrikeGesture();g.Begin(7,0,0,0);Assert.AreEqual(-1,g.End(7,80,0,.2f,true));}
 [Test] public void GestureOwnsOneFingerAndFiresOnce(){var g=new StrikeGesture();g.Begin(7,0,0,0);g.Begin(8,0,0,0);Assert.AreEqual(-1,g.End(8,80,0,.2f));Assert.AreEqual(1,g.End(7,80,0,.2f));Assert.AreEqual(-1,g.End(7,80,0,.2f));g.Begin(9,0,0,0);g.Cancel();Assert.AreEqual(-1,g.End(9,80,0,.2f));}
 [Test] public void FourStrikeChainUsesConfirmedHitsAndScaledDamage(){var m=new Match();m.A.X=0;m.B.X=1;int[] sequence={0,1,2,5};for(int n=0;n<sequence.Length;n++){Assert.IsTrue(m.Attack(m.A,sequence[n]));while(m.A.Frame<m.A.Attack.Value.Startup+m.A.Attack.Value.Active)m.Step();}Assert.AreEqual(4,m.A.ComboCount);Assert.AreEqual(4,m.B.HitSerial);Assert.AreEqual(118,m.B.Health);Assert.IsFalse(m.Attack(m.A,3));}
 [Test] public void BodyChainBranchesIntoKneeAndElbow(){var m=new Match();m.A.X=0;m.B.X=.6f;foreach(int index in new[]{1,3,8,9}){Assert.IsTrue(m.Attack(m.A,index));while(m.A.Frame<m.A.Attack.Value.Startup+m.A.Attack.Value.Active)m.Step();}Assert.AreEqual(4,m.B.HitSerial);Assert.IsFalse(m.A.Chain(-1));}
 [Test] public void EarlyBufferedCrossWaitsForConfirmedRecovery(){var s=new FightSession();s.Match.A.X=0;s.Match.B.X=1;s.QueueAttack(true,0);for(int i=0;i<8;i++)s.Step();s.QueueAttack(true,1);for(int i=0;i<7;i++)s.Step();Assert.AreEqual("cross",s.Match.A.Attack.Value.Id);Assert.AreEqual(2,s.Match.A.ComboCount);}
 [Test] public void LateralImpactPushesAwayInDepth(){var m=new Match();m.A.X=m.B.X=0;m.A.Z=-.5f;m.B.Z=.5f;m.Attack(m.A,0);for(int i=0;i<11;i++)m.Step();Assert.Greater(m.B.Z,.5f);Assert.AreEqual(0,m.B.X);}
 [Test] public void BotRetreatsOnPlaneAgainstCommittedAttack(){var s=new FightSession();s.Match.A.X=0;s.Match.B.X=1.7f;s.Match.A.Start(10);var bot=new BotController(1);float retreat=0;for(int i=0;i<50;i++){float before=s.Match.B.X;bot.Step(s);retreat+=System.Math.Max(0,s.Match.B.X-before);s.Step();Assert.Zero(s.Match.B.Z);}Assert.Greater(retreat,.05f);}
 [Test] public void ChainStepIsGradualBoundedAndDoesNotHome(){var m=new Match();m.A.X=0;m.B.X=1;m.Attack(m.A,0);for(int i=0;i<13;i++)m.Step();Assert.IsTrue(m.Attack(m.A,1));Assert.AreEqual(0,m.A.X);m.B.Z=1;for(int i=0;i<12;i++)m.Step();Assert.That(m.A.X,Is.EqualTo(.24f).Within(.0001));Assert.AreEqual(0,m.A.Z);for(int i=0;i<6;i++)m.Step();Assert.That(m.A.X,Is.EqualTo(.24f).Within(.0001));}
 [Test] public void ChainStepCannotCrossDefender(){var m=new Match();m.A.X=0;m.B.X=.64f;m.Attack(m.A,0);for(int i=0;i<13;i++)m.Step();Assert.IsTrue(m.Attack(m.A,1));for(int i=0;i<8;i++)m.Step();Assert.GreaterOrEqual(Match.Distance(m.A,m.B),Match.MinimumSeparation);}
 [Test] public void HitReactionTracksLevelAndSimulationTicks(){foreach(int index in new[]{0,3,5}){var m=new Match();m.A.X=0;m.B.X=1;m.Attack(m.A,index);for(int i=0;i<=Moves.All[index].Startup;i++)m.Step();Assert.AreEqual(Moves.All[index].Level,m.B.LastHitLevel);Assert.AreEqual(1,m.B.ReactionTicks);Assert.Greater(m.B.ReactionDuration,m.B.Stun);}}
}

public class StrikeOutcomeTests
{
 [Test] public void ContactBlockAndMissProduceDifferentTrainingResults(){foreach(int scenario in new[]{0,1,2}){var m=new Match();m.A.X=0;m.B.X=scenario==2?4:.8f;m.B.Guard=scenario==1;m.Attack(m.A,0);for(int i=0;i<14;i++)m.Step();Assert.AreEqual(scenario==0?StrikeOutcome.Hit:scenario==1?StrikeOutcome.Blocked:StrikeOutcome.Miss,m.A.Outcome);Assert.AreEqual(0,m.A.LastStrikeIndex);}}
 [Test] public void InterruptedStartupDoesNotBecomeAMiss(){var m=new Match();m.A.X=0;m.B.X=.8f;m.Attack(m.A,10);m.Attack(m.B,0);for(int i=0;i<35;i++)m.Step();Assert.AreEqual(StrikeOutcome.Interrupted,m.A.Outcome);}
 [Test] public void NewAttackClearsOldTrainingResult(){var m=new Match();m.Attack(m.A,0);for(int i=0;i<30;i++)m.Step();Assert.AreEqual(StrikeOutcome.Miss,m.A.Outcome);m.Attack(m.A,1);Assert.AreEqual(StrikeOutcome.None,m.A.Outcome);Assert.AreEqual(1,m.A.LastStrikeIndex);}
 [Test] public void BotRespectsCalibratedReach(){var s=new FightSession();s.Match.A.X=0;s.Match.B.X=1.3f;s.Match.EffectiveReach=(f,i)=>.6f;var bot=new BotController();for(int i=0;i<14;i++){bot.Step(s);s.Step();}Assert.IsNull(s.Match.B.Attack);}
}

public class TrainingDummyDefenceTests
{
 [TestCase(0,0,StrikeOutcome.Hit)] [TestCase(1,0,StrikeOutcome.Blocked)]
 [TestCase(1,5,StrikeOutcome.Hit)] [TestCase(2,5,StrikeOutcome.Blocked)]
 [TestCase(2,3,StrikeOutcome.Hit)] [TestCase(2,0,StrikeOutcome.Miss)]
 public void ChosenDefenceChangesActualStrikeResult(int defence,int move,StrikeOutcome expected){var s=new FightSession(true);s.Match.A.X=0;s.Match.B.X=.8f;Assert.IsTrue(s.SetDummyDefence(defence));s.QueueAttack(true,move);for(int i=0;i<Moves.All[move].Startup+Moves.All[move].Active+1;i++)s.Step();Assert.AreEqual(expected,s.Match.A.Outcome);Assert.AreEqual(150,s.Match.B.Health);Assert.AreEqual(FightPhase.Fighting,s.Phase);}
 [Test] public void SwitchingBackToOpenClearsBothGuardAndCrouch(){var s=new FightSession(true);s.SetDummyDefence(2);s.Step();Assert.IsTrue(s.Match.B.Guard);Assert.IsTrue(s.Match.B.Crouch);s.SetDummyDefence(0);s.Step();Assert.IsFalse(s.Match.B.Guard);Assert.IsFalse(s.Match.B.Crouch);Assert.IsFalse(s.QueueAttack(false,0));}
 [Test] public void NormalFightRejectsDummySettingsAndKeepsDefence(){var s=new FightSession();s.Match.B.Guard=true;s.Match.B.Crouch=true;Assert.IsFalse(s.SetDummyDefence(2));s.Step();Assert.IsTrue(s.Match.B.Guard);Assert.IsTrue(s.Match.B.Crouch);}
 [Test] public void InvalidDefenceCannotOverwriteChosenMode(){var s=new FightSession(true);s.SetDummyDefence(1);Assert.IsFalse(s.SetDummyDefence(-1));Assert.IsFalse(s.SetDummyDefence(3));Assert.AreEqual(DummyDefence.StandingGuard,s.Defence);}
 [Test] public void BlockedDummyContactCannotContinueChain(){var s=new FightSession(true);s.Match.A.X=0;s.Match.B.X=.8f;s.SetDummyDefence(1);s.QueueAttack(true,0);for(int i=0;i<14;i++)s.Step();Assert.AreEqual(StrikeOutcome.Blocked,s.Match.A.Outcome);Assert.IsFalse(s.Match.Attack(s.Match.A,1));}
}
