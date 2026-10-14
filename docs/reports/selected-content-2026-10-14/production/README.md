# Production selected-content BuildLayout audit

Read-only audit of the real main build `selected-quality-20261014-2`, StandaloneOSX, Unity6000.3.26f1 / Addressables2.7.3. This is production content, not the earlier11-fighter procedural fixture. Current roster contains8 fighters (five Tripo models including Master, three procedural entries) and5 arenas; new three fighters are not included or claimed accepted.

Passed asset-graph gates:

- 8 PackSeparately fighter bundles;5 PackSeparately arena bundles.
- Lightweight StreamingGameContent has empty direct prefab/arena arrays and8/5 addresses. Its bundle has no direct or transitive fighter, arena or motion dependency. It depends only on the MonoScripts bundle.
- No fighter bundle depends on another fighter bundle; no arena bundle depends on fighter bundles.
- One shared-motion bundle contains30 standalone animation clips,4,082,535bytes (3.893MiB). It has no implicit other assets, character meshes or maps. Shared motion assets are not duplicated.
- Current MasterHumanoidCandidate.prefab is present under GUID74c6545d8c9d9453b9fd1b455128e1f0. Its own bundle is44,651,883bytes (42.583MiB).
- All production bundle file sizes match BuildLayout. Total17 bundles (including builtin/MonoScripts bundles):495,313,822bytes /472.368MiB.

Metadata bundle is2,290bytes, matching manifest.downloadBytes. Metadata plus its MonoScripts dependency is4,355bytes; runtime GetDownloadSizeAsync determines the real cached initial download. Catalog/manifest transfers are separate. Manifest minimumPlayerVersion2 and platformStandaloneOSX are correct for this artifact.

One duplication remains: OfficeLit.shader, GUID42f48bd19fbf64f05b26c76960c1be16, occurs in four procedural arena bundles and three procedural fighter bundles. Each copy has9,580 serialized bytes; six extra copies total57,480bytes (~56.13KiB). BuildLayout reports no other duplicate assets—no geometry, texture or motion duplication. This does not create cross-fighter loading. Optional small follow-up: make OfficeLit.shader an explicit shared runtime asset entry; no shader/pixel changes are needed. No change was made in this audit.

Compressed package closure examples, including selected pair, courtyard, shared dependencies and metadata: Master+Rose190.525MiB; Island+Ranger347.488MiB. These are disk/download bytes, not resident CPU/GPU memory. The complete roster472.368MiB is not an initial forced download. This Standalone report does not establish iOS ASTC sizes, actual device memory/performance, remote publishing or a final phone build.

Files: audit.json includes exact bundle paths/dependency closures/duplicate details; bundles.csv is compact; buildlayout.json is the captured production report; manifest.json is the production manifest; audit.py reproduces structural assertions. No Unity process, main mutation or network request was made.
