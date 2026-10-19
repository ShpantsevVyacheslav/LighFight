using UnityEngine;
namespace LighFight.Runtime {
 /// <summary>Authoring evidence marker: static offline baked pose, no per-frame Animator.</summary>
 public sealed class SeatedSpectatorPose:MonoBehaviour {public bool seatedPoseVisuallyAccepted;public string fighterId,poseEvidence;public Transform mouthpieceGrip,mouth;public bool holdingMouthpiece;}
}
