using NUnit.Framework;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
public class AttackButtonBindingsTests {
 [Test] public void CustomHighestMoveSurvivesComboContextAndModifier(){var bindings=new AttackButtonBindings();int chosen=Moves.All.Length-1;Assert.IsTrue(bindings.Assign(1,chosen));int move=bindings.Resolve(1,7,12);var fighter=new Fighter();Assert.IsTrue(fighter.Start(move));Assert.AreEqual(Moves.All[chosen].Id,fighter.Attack.Value.Id);}
 [Test] public void DefaultButtonsKeepContextualChains(){var bindings=new AttackButtonBindings();Assert.AreEqual(2,bindings.Resolve(0,2,0));Assert.AreEqual(12,bindings.Resolve(2,4,8));bindings.Assign(0,9);bindings.Reset();Assert.AreEqual(2,bindings.Resolve(0,2,0));}
 [Test] public void AssignmentsPersistAndInvalidStoredMovesRecover(){string prefix="lf.test.bindings."+System.Guid.NewGuid()+".";try{var first=new AttackButtonBindings();first.Assign(0,9);first.Assign(3,23);first.Save(prefix);PlayerPrefs.SetInt(prefix+2,999);var second=new AttackButtonBindings();second.Load(prefix);Assert.AreEqual(9,second.Resolve(0,2,12));Assert.AreEqual(23,second.Resolve(3,5,8));Assert.AreEqual(4,second[2]);Assert.IsFalse(second.Assign(4,0));Assert.IsFalse(second.Assign(0,-1));Assert.AreEqual(9,second[0]);}finally{for(int i=0;i<4;i++)PlayerPrefs.DeleteKey(prefix+i);}}
}
