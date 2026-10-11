# Character material pass

Same-camera, same-light guard captures: art/previews/character-quality/fighter-{3,4}-{before,after}.png.

Rose now uses a dedicated CharacterMatte shader with restrained dielectric highlights and lower direct diffuse energy. OfficeLit and arena materials are unchanged. Island CharacterSkin uses the same restrained highlights and a surface-derived atlas normal basis to avoid relying on tangents from an earlier mesh revision.

This is a material improvement, not a finished professional character rebuild. Island still has visibly uneven face/forearm geometry and baked shading. Next geometry work should target the source mesh and atlas together; increasing polygon count alone will not repair those defects. Closed fists remain static.

Unity EditMode: 107/107 passed. Both current prefabs rendered successfully in graphics batch mode. No iPhone quality validation in this pass; no Meshy credits used.
