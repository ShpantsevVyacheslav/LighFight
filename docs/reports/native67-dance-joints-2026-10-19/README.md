# Dance joint audit

11 dances × 6 samples (start, 10/30/50/70/90%). Actual isolated Unity run exited 0. No production assets changed. Review helper now exposes normalized source/target joint coordinates without modifying pose.

The source/target arm direction gate passes, but target shoulder-to-neck vertical drops remain larger. Directions alone did not certify shoulder placement. Skeleton proportions / canonical-reference choice contribute to this comparison; this does not isolate skin-weight errors or prove anatomical correctness of either skeleton. Source and target use different proportions and each is normalized by its own bind hip-to-ankle height. The diagram is a frontal projection; depth/twist are not visible. Zero percent is first animation frame, NOT bind pose.

Four illustrated midpoint poses: Boom (66), Bass Beats (65), Bubble (67), Shake It Off (82). Cyan=source, amber=target. Same fixed projection scale, no moving alignment fit. Source/target coordinates are expressed in their own posed body basis.

Next controlled experiment: capture true bind and calibrated canonical reference coordinates, then compare clavicle-rest/reference alternatives on the same poses with full surface rendering and wrist displacement checks. Do not globally raise shoulders or promote prior corrections based solely on this audit.
