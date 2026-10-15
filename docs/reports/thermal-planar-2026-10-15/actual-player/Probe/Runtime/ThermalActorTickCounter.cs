using UnityEngine;
// Attached to actual artist root. Inactive roots must not receive LateUpdate.
public sealed class ThermalActorTickCounter:MonoBehaviour {public int lateTicks;void LateUpdate(){lateTicks++;}}
