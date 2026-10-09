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
