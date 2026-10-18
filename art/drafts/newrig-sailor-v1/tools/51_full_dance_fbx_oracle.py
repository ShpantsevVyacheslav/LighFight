"""Original-source full-dance independent holdouts vs actual skinned Tspace FBX."""
from pathlib import Path
O=Path(__file__).resolve().parents[1];source=O/'tools/43_tight_tspace_fbx_oracle.py';text=source.read_text()
for old,new in [('adaptive-box_02-FULL_ADAPTIVE_TIGHT008','adaptive-dance_01-FULL_ADAPTIVE_TIGHT008_EXACT_BATCH'),('adaptive-box-tspace-export.json','adaptive-dance-tspace-export.json'),('Fraction(67,24)','Fraction(556,24)'),('actualDuration-67/24','actualDuration-556/24'),('adaptive-box-tspace-fbx-offkey.json','adaptive-dance-tspace-fbx-offkey.json'),('One explicit box review take required','One explicit dance review take required')]:
 assert old in text,old;text=text.replace(old,new)
# Verify ACTUAL imported skin, all original vertices and all named nonzero weights.
# Bind-position identity is checked by the unchanged oracle before using indices.
needle="# Explicit fixed per-bone bind gauge"
gate="""importedInfluences=0
for vertex in body.data.vertices:
 expected={names[int(joints[vertex.index,k])]:np.float32(weights[vertex.index,k]) for k in range(4) if weights[vertex.index,k]>0}
 actual={body.vertex_groups[g.group].name:np.float32(g.weight) for g in vertex.groups if g.weight>0}
 assert set(expected)==set(actual),('Imported named skin influence mismatch',vertex.index,expected,actual)
 for name,value in expected.items():assert value.view(np.uint32)==actual[name].view(np.uint32),('Imported weight bits changed',vertex.index,name,float(value),float(actual[name]))
 importedInfluences+=len(actual)
print('ALL_IMPORTED_NAMED_WEIGHT_BITS_PASS '+str(importedInfluences),flush=True)
"""
assert needle in text;text=text.replace(needle,gate+needle)
text=text.replace("'allOriginalVertices':N,", "'allOriginalVertices':N,'actualImportedAllNamedWeightBitsExact':True,'actualImportedNonzeroInfluences':importedInfluences,")
exec(compile(text,str(source)+'-full-dance-versioned','exec'),globals())
