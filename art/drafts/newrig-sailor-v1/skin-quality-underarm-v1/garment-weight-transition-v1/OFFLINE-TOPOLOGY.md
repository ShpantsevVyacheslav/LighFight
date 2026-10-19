# Actual offline topology proposal — awaiting boundary review

NumPy/PIL, OPENBLAS/OMP/MKL threads1. Actual run3.08seconds; no Blender/Unity.
No geometry or weights modified. Source-indexed data SHA matches authoritative
sidecar. Protected221624original IDs excluded exactly, including distal anchors.

Two proposed intrinsic25mm neighborhoods contain6380vertices,343fixed outer
boundary vertices. Components4249+2131. They are not disconnected original cloth:
the shortest permitted original-edge route from control0to1 is77.320mm through
74vertices. Hence two25mm balls do not meet. Do not silently connect these by
spatial proximity across a folded armhole. A future connected corridor needs
explicit review, or keep the two components separate with their own conditions.

Images use the actual frozen dance50 original-LBS surface and original−X camera.
connected-patch-side-proposal.png shows four geodesic control domains and cyan
outer boundary. control-route-anchor-proposal.png shows numbered4mm control
cores (50/69/59/120vertices) and the real graph path. Vertex depth-buffer visibility
is an approximation, not a full triangle raster. The source background is an
existing actual source render; no pixel content used as geometry truth.

|Control domain|Vertices|Largest actual mean named weights|
|---|---:|---|
|0(red)|2131|breast.R .586; upper_arm.R.001 .240; spine.002 .106|
|1(yellow)|1922|upper_arm.R.001 .608; forearm.R .318; breast.R .065|
|2(green)|1272|forearm.R .513; upper_arm.R.001 .463; breast.R .013|
|3(purple)|1055|forearm.R .543; upper_arm.R.001 .454; forearm.R.001 .0026|

These distributions are actual source weights, not proof of a misplaced joint
or erroneous anatomical assignment. Domain0 appears torso cloth in the view;
others appear sleeve cloth. Root must review anchor semantics and corridor
before these become boundary conditions. Existing cloth-mask approval did not
approve these new weight boundaries. No solver or small weight candidate yet.

Reproduce with tools/prepare_connected_patch.py. Four original control-face
corner/two-ring witnesses are separately in control-topology-original.json.
