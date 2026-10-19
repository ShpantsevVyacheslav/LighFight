# Swing/twist diagnostic — 19 October 2026

User hypothesis: shoulder/clavicle over-twist, not merely elevation. 66 actual Unity sampled poses, 11 dances,4segments (two clavicles and upper arms). Twist is extracted by quaternion vector projection onto canonical bone axis from delta = inverse(canonicalLocalRotation)*currentLocalRotation. Shortest quaternion branch, undefined decomposition flagged if projected quaternion norm <1e-10. Source virtualT and native-target canonical references differ: signed twist numbers are reference-relative, NOT anatomical joint angles, nor direct proof of an incorrect rig.

Bubble action67 at50%: source/target clavicle twist L -4.342/-17.050deg; R3.155/11.860deg. Upper-arm relative twists agree source/target: L13.889deg,R-131.694deg. Strong upper-arm relative rotation already exists in source. No singularities in these66sampled poses; continuous branch behavior NOT yet checked. Previous endpoint-direction tests cannot detect axial roll errors.

Controlled next candidate matches source-relative clavicle twist while preserving target swing and restoring upper-arm world orientation. It does not blindly clamp upper-arm rotation or alter hand pose. Gates: source/target decomposition finite, correction <=30deg diagnostic bound, hand-origin movement <=1um. Reference mismatch means this is a visual experiment, not a theoretically guaranteed retarget correction. No production promotion from isolated samples.

## Twist-only six-pose result

Actual numeric surface tests completed, framebuffer-repeat remains RED. Source-matched clavicle twist preserves hand positions within1um gate. Surface changes at the collar, but no sufficient overall shoulder silhouette improvement. Thus axial twist is a relevant missing measurement, not a confirmed sole root cause or accepted fix. Strong upper-arm rotation already present in source remains to be isolated from the clavicle experiment; do not suppress it globally without preserving elbow/hand pose and verifying continuous trajectories. Production unchanged.
