"""Versioned full-dance transport using the proven authoritative-source Tspace exporter.
No source changes. Requires full dance fit PASS before this wrapper can execute.
"""
from pathlib import Path
O=Path(__file__).resolve().parents[1];source=O/'tools/42_export_tight_source_tspace.py';text=source.read_text()
for old,new in [('adaptive-box_02-FULL_ADAPTIVE_TIGHT008','adaptive-dance_01-FULL_ADAPTIVE_TIGHT008_EXACT_BATCH'),('Fraction(67,24)','Fraction(556,24)'),('Fraction(67)','Fraction(556)'),('frameCount<=20000','frameCount<=50000'),('Dense_box_02_adaptive_tspace_v2','Dense_dance_01_adaptive_tspace_v1'),('SailorOriginalDenseDEF-BoxAdaptiveTspace-v2.fbx','SailorOriginalDenseDEF-DanceAdaptiveTspace-v1.fbx'),("'durationSeconds':67/24","'durationSeconds':556/24"),('adaptive-box-tspace-export.json','adaptive-dance-tspace-export.json')]:
 assert old in text,old;text=text.replace(old,new)
# Skin discovery in installed FBX exporter uses modifier type/object, not viewport
# visibility. Raw geometry export already has use_mesh_modifiers=False. Avoid
# evaluating the full dense LBS on every bone-only bake frame in this owned copy.
line=next(line for line in text.splitlines() if line.startswith('bpy.ops.export_scene.fbx('))
replacement="ownedModifierVisibility=[(m,m.show_viewport) for m in body.modifiers]\nfor m,visible in ownedModifierVisibility:m.show_viewport=False\ntry:\n "+line+"\nfinally:\n for m,visible in ownedModifierVisibility:m.show_viewport=visible"
text=text.replace(line,replacement)
text=text.replace("'use_tspace':True,", "'use_tspace':True,'ownedViewportSkinDisabledDuringBoneBake':True,")
exec(compile(text,str(source)+'-full-dance-versioned','exec'),globals())
