using System;
using System.Linq;
using UnityEngine;
namespace LighFight.Runtime {
 public static class ArenaLayoutValidator {
 static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
 public static readonly Bounds CombatVolume = new Bounds(new Vector3(0,1.1f,0),new Vector3(11,2.2f,4));
        public static void Validate(GameObject root)
        {
            var renderers=root.GetComponentsInChildren<Renderer>();
            foreach(var renderer in renderers) {
                var b=renderer.bounds;var v=b.center;var size=b.size;
                if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)||!Finite(size.x)||!Finite(size.y)||!Finite(size.z))throw new InvalidOperationException("Non-finite arena bounds: "+renderer.name);
            }
            var floors=renderers.Where(r=>r.name=="FightFloor").ToArray();
            if(floors.Length!=1)throw new InvalidOperationException("Require one separate FightFloor, not a merged scene mesh.");
            var floor=floors[0].bounds;
            if(floor.size.y>.25f || Mathf.Abs(floor.max.y)>.05f || floor.min.x>CombatVolume.min.x || floor.max.x<CombatVolume.max.x || floor.min.z>CombatVolume.min.z || floor.max.z<CombatVolume.max.z)
                throw new InvalidOperationException("FightFloor must be flat at y=0 and cover the 11 x 4 m clear combat area.");
            foreach(var renderer in renderers.Where(r=>r!=floors[0]))
                if(renderer.bounds.Intersects(CombatVolume))throw new InvalidOperationException("Decoration blocks combat area: "+renderer.name);
        }
 }
}
