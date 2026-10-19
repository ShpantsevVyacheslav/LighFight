using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Editor {
 public static class NativeContactReview {
  public static void Audit(){var lines=new System.Collections.Generic.List<string>();foreach(int id in new[]{3,4}){var obj=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LighFight/Art/Fighters/fighter-{id}.prefab"));try{var view=obj.GetComponent<FighterView>();var fighter=new Fighter();var other=new Fighter{X=.85f};view.PoseTowards(fighter,other,0);lines.Add(id+" idle "+Dump(view));foreach(var move in Moves.All){fighter=new Fighter();fighter.Start(System.Array.IndexOf(Moves.All,move));fighter.Frame=move.Startup;view.PoseTowards(fighter,other,0);lines.Add(id+" "+move.Id+" "+Dump(view));}}finally{Object.DestroyImmediate(obj);}}File.WriteAllLines("../art/source/original-island/native-contact-audit.txt",lines);}
  public static void TraceHammer(){var a=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-3.prefab"));var b=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-4.prefab"));try{var view=a.GetComponent<FighterView>();var target=b.GetComponent<FighterView>();var m=new Match();m.A.X=0;m.B.X=.6f;new PhysicalContacts(m,view,target);var sample=m.BeforeContact;var rows=new System.Collections.Generic.List<string>();m.BeforeContact=value=>{sample(value);if(m.A.Attack.HasValue&&m.A.Frame>=m.A.Attack.Value.Startup&&m.A.Frame<m.A.Attack.Value.Startup+m.A.Attack.Value.Active)rows.Add(m.A.Frame+" strike "+Dump(view)+" target "+Dump(target));};m.Attack(m.A,System.Array.FindIndex(Moves.All,x=>x.Id=="hammer"));for(int i=0;i<80;i++)m.Step();rows.Add("health "+m.B.Health);File.WriteAllLines("../art/source/original-island/native-hammer-trace.txt",rows);}finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}}
  static string Dump(FighterView view)=>string.Join(";",new[]{"Head","Spine","Spine2","Hips","LeftHand","RightHand","LeftToeBase","RightToeBase"}.Select(n=>n+" "+view.BonePoint(n,Vector3.zero).ToString("F3")));
 }
}
