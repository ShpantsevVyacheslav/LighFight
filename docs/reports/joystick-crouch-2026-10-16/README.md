# Joystick down crouch input

User re-test: smoothness is acceptable; heat after several minutes persists. Block pose and crouch pose rejected; low strikes not apparent. These visual/thermal issues remain open.

Removed separate crouch touch button. Joystick retains vertical intent; DOWN enters crouch at -0.55 and releases above -0.30 to prevent boundary flicker. Horizontal axis still drives only planar forward/back movement. Neutral/release clears crouch; pause/background/reset discard held intent. Keyboard Down remains supported. No move is silently remapped to uppercut.

Unity6000.3.26f1 StandaloneOSX EditMode: 261/261 passed (XML attached), including downward/neutral/up, hysteresis, diagonal intent and mirrored block-control spacing. The legacy layout assertion against a removed crouch button was removed. This proves input logic, not iPhone ergonomics or pose acceptance.

Original meshes/textures/Avatars, frame-rate and graphics quality unchanged. Follow-up source-pose work remains QA-only under .tools/guard-crouch-v3; not part of this input change.

Final combined source check: 272/272 EditMode passed, including pause clearing held crouch; XML in sibling phone-release-policy report.
