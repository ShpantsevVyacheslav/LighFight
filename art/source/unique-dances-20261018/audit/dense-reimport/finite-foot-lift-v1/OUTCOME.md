# Finite original-foot lift envelope

EXIT0,64.7007152s scalar offline, ALL143168originalfootIDs ×4759poses (120Hz plus2535 exactkeys). Source footIDs match original positive influence membership in DEF-foot/toe.L/R; all original four weights retained, name-order remapped into NDA1. No Unity/Blender/player, full1M scan not repeated.

Per-pose minimum y ranges from−.017099462015501134m at6s to+.016213403484881778m at12.033333333s (native source metres, fixed C). Independent fullbody result minY beforemargin is also−.017099462015501134m.

For uniform root translation only, monotonic correction c=Clamp(−minY,lower,upper) lies between Clamp(−highestMinimum,lower,upper) and Clamp(−lowestMinimum,lower,upper). Thus:

- Up-only floor: correction[0,min(upcap,.017099462015501134)]. Preserve original finite body minY; expand maxY by at most.017099462015501134m.
- Signed floor: conservative correction[−min(downcap,.016213403484881778),min(upcap,.017099462015501134)]. Expand original finite minY downward and maxY upward independently. This loses correlation, but encloses every sampled translated body.

This is an envelope, not evidence the source physically stays on the floor. No hidden source translation or geometry change. Runtime bindpoint float quantization/scalar evaluation, root nonuniform rotations, actor-scale/ground offset hierarchy and actual grounding caps require separate comparison. Uniform height1.8 scales source distances/envelope1.8 only if caps scale consistently. This does not cover arbitrary entry/return or future B weights. FINITE coverage only, not continuous bound. Grounded production bounds asset not published.
