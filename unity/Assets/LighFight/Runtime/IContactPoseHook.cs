namespace LighFight.Runtime {
 public interface IContactPoseHook {
  void ApplyContactPose();
  void InvalidateContactPose();
  bool TryGetContactPhase(int stateHash,out float phase);
 }
}
