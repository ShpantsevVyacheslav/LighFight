using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime {
 public sealed class AttackButtonBindings {
  private static readonly int[] defaults={0,1,4,5};
  private readonly int[] moves=(int[])defaults.Clone();
  public int this[int slot]=>moves[slot];
  public bool Assign(int slot,int move){if(slot<0||slot>=4||move<0||move>=Moves.All.Length)return false;moves[slot]=move;return true;}
  public void Reset(){System.Array.Copy(defaults,moves,4);}
  public int Resolve(int slot,int contextualMove,int modifier){if(slot<0||slot>=4)return -1;int move=moves[slot]!=defaults[slot]?moves[slot]:contextualMove+modifier;return move>=0&&move<Moves.All.Length?move:defaults[slot];}
  public void Load(string prefix){Reset();for(int i=0;i<4;i++)Assign(i,PlayerPrefs.GetInt(prefix+i,defaults[i]));}
  public void Save(string prefix){for(int i=0;i<4;i++)PlayerPrefs.SetInt(prefix+i,moves[i]);}
 }
}
