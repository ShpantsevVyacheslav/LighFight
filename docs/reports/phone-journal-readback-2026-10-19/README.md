# Read-only iPhone journal retrieval, 19 October

The paired iPhone was reachable. Downloaded the existing staging app Documents/Diagnostics through devicectl; no app launch, game test, settings or device state was changed. Installed staging app reports build115. The newest stored journal is dated18October and contains80samples /80.90seconds, content marker `tripo11-result-pose-cpu-ios-20261016-r4`. It does not contain a bundle build number; do not label it a verified build115 run.

Recorded settings remain Maximum/60FPS/4xAA/shadowsAll/2868×1320. There are59fight samples; all samples report thermal nominal. This short historical run does not close the previously confirmed multi-minute heating defect. GPU timing is unavailable. Exact sample summaries and source SHA are in summary.json. No long cooled-device or new-code acceptance is claimed.

This confirms that a saved Maximum/60 preference persists; the new-install High/30 default intentionally does not overwrite an explicit saved preference. The new shadow-material fix is not proven installed by this journal.
