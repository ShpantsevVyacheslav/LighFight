using NUnit.Framework;
using LighFight.Runtime;
namespace LighFight.Tests {
 public class MeetingSpectatorSelectionTests {
  [Test] public void ExcludesBothActiveFightersAndHonorsSmallPool(){
   var c=new MeetingSpectators.Candidate[6];for(int i=0;i<c.Length;i++)c[i]=new MeetingSpectators.Candidate{fighterIndex=i,fighterId="fighter-"+i,address="seated-"+i};
   Assert.That(MeetingSpectators.Select(c,0,2,2),Is.EqualTo(new[]{1,3}));
  }
  [Test] public void RejectsSameRosterIndexEvenWhenIdentityStringsDiffer(){
   var c=new[]{new MeetingSpectators.Candidate{fighterIndex=2,fighterId="a",address="a"},new MeetingSpectators.Candidate{fighterIndex=2,fighterId="alias",address="b"},new MeetingSpectators.Candidate{fighterIndex=3,fighterId="c",address="c"}};
   Assert.That(MeetingSpectators.Select(c,0,1,4),Is.EqualTo(new[]{0,2}));
  }
  [Test] public void ApprovedPoolLeavesANonFightingSpectatorForEveryRosterMatch(){
   var indices=new[]{8,9,3};var pool=new MeetingSpectators.Candidate[3];for(int i=0;i<3;i++)pool[i]=new MeetingSpectators.Candidate{fighterIndex=indices[i],fighterId="fighter-"+indices[i],address="static-"+indices[i]};
   for(int a=0;a<11;a++)for(int b=a+1;b<11;b++){
    var picked=MeetingSpectators.Select(pool,a,b,2);Assert.That(picked.Length,Is.InRange(1,2),"match "+a+"/"+b);
    foreach(int k in picked){Assert.That(pool[k].fighterIndex,Is.Not.EqualTo(a));Assert.That(pool[k].fighterIndex,Is.Not.EqualTo(b));}
   }
  }
  [Test] public void QualityBudgetsBoundBackgroundCost(){Assert.That(MeetingSpectators.BudgetForQuality(0),Is.Zero);Assert.That(MeetingSpectators.BudgetForQuality(1),Is.EqualTo(1));Assert.That(MeetingSpectators.BudgetForQuality(2),Is.EqualTo(2));}
  [Test] public void RejectsDuplicateIdentityAndMissingAddresses(){
   var c=new[]{new MeetingSpectators.Candidate{fighterIndex=2,fighterId="same",address="a"},new MeetingSpectators.Candidate{fighterIndex=3,fighterId="same",address="b"},new MeetingSpectators.Candidate{fighterIndex=4,fighterId="four",address=""},new MeetingSpectators.Candidate{fighterIndex=5,fighterId="five",address="a"}};
   Assert.That(MeetingSpectators.Select(c,0,1,4),Is.EqualTo(new[]{0}));
  }
 }
}
