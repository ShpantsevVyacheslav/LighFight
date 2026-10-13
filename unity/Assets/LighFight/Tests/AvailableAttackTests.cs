using NUnit.Framework;
using LighFight.Core;
namespace LighFight.Tests {
 public sealed class AvailableAttackTests {
  [Test]public void UnavailableCandidateMoveCannotEnterEitherInputBuffer(){
   var session=new FightSession();session.AttackAvailable=(player,index)=>index!=6;
   Assert.False(session.QueueAttack(true,6));Assert.False(session.QueueAttack(false,6));
   session.Step();Assert.False(session.Match.A.Attack.HasValue);Assert.False(session.Match.B.Attack.HasValue);
   Assert.True(session.QueueAttack(true,0));session.Step();Assert.AreEqual(Moves.All[0].Id,session.Match.A.Attack.Value.Id);
  }
  [Test]public void AvailabilityCanDifferBetweenFighters(){
   var session=new FightSession();session.AttackAvailable=(player,index)=>!player||index!=6;
   Assert.False(session.QueueAttack(true,6));Assert.True(session.QueueAttack(false,6));
   session.Step();Assert.AreEqual(Moves.All[6].Id,session.Match.B.Attack.Value.Id);
  }
 }
}
