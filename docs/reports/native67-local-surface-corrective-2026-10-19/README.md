# Local surface corrective — 19 October 2026

Actual six-pose baseline world positions exported from Unity, original-ID seam agreement <=1um and complete coverage. Triangle strain is computed on original topology without moving fits. Empirical collar region, area >1e-9 m² and first-edge length >0.2mm conditioning filters. These ratios are not anatomical acceptance: cloth can legitimately fold.

Derived correction: 5-step local uniform Laplacian delta mush with proper SVD rotation to transport original detail, smooth collar mask, blend .75 and tanh8mm displacement cap. No reduced topology. Rest identity max3.47e-18m; positions outside mask unchanged. Actual maximum pose displacements .50–1.35mm.

Source pose identity checked again in renderer <=1um. Normals/tangents are transported by geometric area-normal rotation while retaining existing seam shading offsets. This is an approximation, not exact surface Jacobian or a GPU parity proof. Numeric PNT gates describe the pre-correction skinner only. Snapshot CPU work is NOT realtime/mobile implementation. Framebuffer repeat remains RED.

REJECTED: no sufficient shoulder contour improvement. Area-below-half-rest fraction improves in2/6 poses and worsens in4/6; no promotion. Top comparison row=baseline, bottom=derived surface. Production untouched. Next hypothesis: excessive clavicle motion in source dance, as distinct from rest position or skin weights.
