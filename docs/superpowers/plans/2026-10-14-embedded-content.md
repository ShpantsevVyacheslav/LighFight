# Embedded Selected Content Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Launch the packaged game from LighChat without a development server while retaining selected-only loading.

**Architecture:** A generated compatibility descriptor and explicit bootstrap mode initialize the standard local Addressables catalog. The existing SelectedContentLoader and release queue own body/arena residency; remote delivery remains separate.

**Tech Stack:** Unity6000.3.26f1, C#, NUnit/Unity Test Framework, Addressables.

**Spec:** `docs/superpowers/specs/2026-10-14-embedded-content-design.md`

## Global Constraints

- Preserve content playerVersion2 and host protocol1.
- No networking requirement, localhost fallback, or remote-cache overwrite in embedded mode.
- No source geometry/map/curve reduction or inline fighter dependency in startup metadata.
- No new packages, publication, push, host framework replacement, or device success claim.
- Main Unity is root-owned; implementation and proof are prepared in an isolated project and supplied as a narrow packet.

## Review Focus

- A wrong platform/Unity descriptor must fail before opening content.
- A future unsupported playerVersion must fail, with the current version accepted.
- A local build containing remote bundle references must fail the zero-download gate instead of silently fetching.
- Embedded opening/destruction must release catalog/game handles through the existing ownership model.
- Remote mode must retain its transport validation and download UI/cache behavior.

### Task1: Embedded compatibility and startup

**Files:** Create `unity/Assets/LighFight/Core/EmbeddedContentDescriptor.cs`; modify `unity/Assets/LighFight/Runtime/ContentBootstrap.cs`; create `unity/Assets/LighFight/Tests/EmbeddedContentTests.cs`.

**Interfaces:** `EmbeddedContentDescriptor.Validate(string platform, string unityVersion)` returns null or an error code. ContentBootstrap consumes a serialized `useEmbeddedCatalog` flag and a small descriptor TextAsset. It reuses the existing downloader/catalog handle/open/release path without changing remote ContentManifest policy.

- [x] Write failing tests for accepted schema1/player2/platform/Unity; future playerVersion; wrong platform/Unity; invalid label/address/version/size. Record actual RED.
- [x] Implement minimal descriptor validation and GREEN tests.
- [x] Write/run a failing bootstrap behavior test: explicit embedded mode opens local content without a manifestUrl and does not write the remote cache; remote configuration still fails unchanged if absent.
- [x] Implement local initialization and zero-download gate, automatic opening, visible errors and cleanup; run focused tests and the isolated full suite.
- [x] Save a narrow source/test packet and RED/GREEN logs. Root integrates and reviews it before main execution.

### Task2: Local packaging and real player proof

**Files:** Modify `unity/Assets/LighFight/Editor/ContentBuild.cs`; create `unity/Assets/LighFight/Editor/EmbeddedContentReview.cs` and isolated test/capture fixtures as needed.

**Interfaces:** `ContentBuild.BuildEmbedded()` configures only owned content groups to standard local build/load paths, builds the packaged catalog and descriptor, and creates a Bootstrap scene using Task1's explicit embedded mode. Existing `ContentBuild.Build()` remote behavior remains unchanged. Review output paths use task-specific environment variables and refuse overwriting completed evidence.

- [x] Write a failing owned-group packaging test: all fighter/arena/shared/metadata owned groups local, unrelated entries preserved, remote-catalog building off, starter roots have zero body dependencies. Record RED.
- [x] Implement minimal local packaging, run GREEN plus the isolated suite, and inspect generated build layout and local load paths.
- [x] Build/run a real standalone player using actual local Addressables bundles. Exercise loading, roster opening, selection/swaps and release without network delivery. Record catalog/body/arena handles, per-selection loaded assets and completion/error output.
- [x] Produce a small proof packet with source/meta preservation, test results and actual player evidence. Root applies it and reproduces the actual main build/player proof before authorizing the iOS artifact stage.

This is a continuation of the approved autonomous delivery plan; no new user approval gate is requested. A source-only packet or editor test pass is intermediate evidence, not completed phone delivery.

2026-10-15 evidence: isolated121/121 and rootmain222/222 passed; RED retained under docs/reports/embedded-content-2026-10-15. Real main offline packaged player remains pending; corrected QA now cycles every roster slot and arena index moduloArenaCount, refuses runtime errors/network attempts/resident bundles after release. This is QA-only and must be removed by clean BuildEmbedded before iOS delivery. Gala integration committed70085f6;10/11Tripo. Cobalt nativefullpack acquired/under independent review, not mainpromoted yet.

2026-10-15 checkpoint update: Cobalt integrated in089879b; all11roster bodies now Tripo. Main227/227 and combined Gala/Cobalt490actual samples pass. New explicit ExportEmbeddedIos entry has focused wrong-platform RED→GREEN (1/1), full aggregate rerun pending recovery fix. Main offline all11/all5 Addressables player and actual iOS/framework/device delivery remain unchecked.

Main aggregate now228/228Metal after recovery fixe9ecef8. Actual source all11/all5 standalone offline-packaging build started (versiontripo11-recovery-20261015-r1); no real-player acceptance yet. Device inventory sees paired iPhone17ProMax available, not a device test.

Actualmainall11/all5offlineplayerPASS exit0,0HTTP/0runtimeerrors/33loads33releases/final0handles0bundles andcacheunchanged. main-player-r1 evidenceverified. This authorizes moving to iOSartifactstage underexistinguserauthority; no newpermission requested. Device/integration stillpending.
