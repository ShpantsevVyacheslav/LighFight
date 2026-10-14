Shared cached contact pose fix

The old LateUpdate callback restored a sampled contact pose and then unconditionally moved hipXZ back to the imported rest anchor. Source anchoring now applies only to native source motion or its return blend; shared Human contact poses retain their sampled hipXZ. Source meshes, weights, curves and hit volumes are unchanged.

Main focused warmed PlayMode regression passes1/1. Full main GPU EditMode suite passes200/200 after Sailor and Retro import. Isolated old-callback mutation fails with a deliberate150mm anchor offset; restored version passes. Automatic Late/EOF full-surface coverage is retained in the Sailor packet (1890 cases). Close-range punch timing and shoulder coverage are separate unresolved work; this fix does not claim to resolve them.
