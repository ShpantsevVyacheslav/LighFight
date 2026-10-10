# Черновик обращения в Meshy — не отправлено

Two image-to-3D tasks returned unrelated subjects despite correct local image inputs.

1. Task 01a127da-33df-7046-a42e-dbc9f3b65243: input is a woman in a pink sweatshirt, white sunglasses, white trousers, T pose. Downloaded GLB depicts a man in a blue hoodie and dark trousers.
2. Task 01a127dd-2668-70f7-ae09-aeeef8ecdf9f: input is an office courtyard with cars, gray walls, burgundy railings and trees. Downloaded GLB depicts a humanoid figurine with a long rod.

Both used image-to-3D, meshy-7.1, geometry_resolution=2k, should_texture=true, texture_resolution=4k, enable_pbr=true, image_enhancement=false, should_remesh=false, target_formats=[fbx,glb]. Only the first task had pose_mode=t-pose; the courtyard had no pose parameter and no rigging request.

The MCP resolves local files to data URIs. An offline replay of its current request builder confirms that decoded payload image hashes equal the original files. No historical network payload capture is available.
The API lists both tasks alongside an earlier successful task 01a124d3-8a5f-76b8-9f78-0969d1bf2cfe. Website and API balance agree at 3992. Please verify the input images received by your backend and whether output model URLs are associated with the correct tasks.

Image tasks consumed 35 credits each. Rigging task 01a127e1-b2a9-73ee-ae0c-d2e92ca1ffc8 was started prematurely for the first result, consumed another 5; cancellation reported previous_status=SUCCEEDED. Please advise whether any refund is possible for the unrelated image-to-3D outputs. We acknowledge the premature rig was our error.

No API keys or signed URLs should be included in the support message. Attach reference images/results only after owner authorizes sending these materials to support.
