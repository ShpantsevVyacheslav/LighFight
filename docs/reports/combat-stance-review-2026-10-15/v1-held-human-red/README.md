# Rejected held-Human-clip candidate

Derived held Crouch clip plus arm mask failed preservation of original lower-body pose. First run changed Hips local position by0.0127428m and rotation2.569627degrees; copying original root/feet clip settings removed rotation error but position differed0.01744664m. Thresholds unchanged; no catalog/runtime source promotion. Original meshes, weights and source motion assets unchanged. This failure concerns test clip reconstruction, not loss in original provider model.

Next candidate must sample the original Crouch clip directly at phase.3 and independently phase original arm clips, then pass the same lower-body invariants and actual runtime/EOF/contact gates. Logs preserve the failed mechanism for comparison.
