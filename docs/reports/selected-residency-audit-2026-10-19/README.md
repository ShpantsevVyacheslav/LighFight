# Read-only selected-content residency audit,19October

No engine/device launch, no production lifetime changes. Read existing selected-content2026-10-14 production/player reports and latestphonejournal first; their scopes are retained below. A1.55–1.64GiB processfootprint is a P0measurement concern, not evidence by itself of leakedall11actors.

## Existing measurements, not rerun

phone-journal-readback-2026-10-19:80.90s historicaljournal,59fight rows max1552.018MiB,16victory rows max1642.768MiB. Markertripo11-result-pose-cpu-ios-20261016-r4, fighter0/opponent3/arena0 atstart, Maximum60/4xAA/2868×1320. Journal has no buildnumber; installedstaging115 does not identifycreationbuild. No allocationcategory/GPUmemory/Addressableshandle/curveusers counters. Victoryincrease≈90.75MiB cannot be attributed to leak, clipdecode or GPUcache without category/counter snapshots. Not a cooledlongthermaltest.

Older actualMac streamedplayer20selections proved3live selectedhandles/settledpair, all60handles released ondestruction, nativeusers/decoded0 and unusedassetcount0 afterUnloadUnusedAssets. Allocated123–223MiB, processpeak715MiB there, not currentiPhone/current11roster. Older productionBuildLayout was8fighters/5arenas; no crossfighterbundledeps and sharedmotionsnotduplicated. Do not reuse its sizes/dependencycounts as currentiOS11proof.

## Intended current ownership

| Item | Owner and lifetime | Evidence/path |
|---|---|---|
| selected fighter/opponent/arena prefabs/dependencies | SelectedContentLease acquires unique selectedaddresses only (2fighters+1arena, samefighterdedup), idempotentDispose releases | Runtime/SelectedContentLoader.cs:22–47 |
| ordinary actorinstances | Prototype explicitly disposes typedwarmups beforeDestroy; oldselectedhandles deferred oneframe afterdestroy | Prototype.cs:80–86,114–118; PrototypeStreaming.cs:9–13,20–47; SelectedContentReleaseQueue.cs:7–9 |
| currentembeddedmetadata | ContentBootstrap retains one GameContent handle untilgame/bootstrapdestroy; validatesstreamedonly; GetDownloadSizeAsync(allkeys) validateslocalavailability, does NOT load allprefabs | ContentBootstrap.cs:115–130,154 |
| ALL11lightweightmetadata/portraits | EmbeddedGameContent hasemptyfighters/arenas,streamed1,11fighteraddresses;11metadata portraitassetreferences intentional. SelectionResources alsoResources.Loads all11portraits and5arenaimages; no all11actorInstantiate | Content/EmbeddedGameContent.asset:60–79; FightSelection.cs:25–28,140–146 |
| selected nativecurves | Warmup dedups ALL poseentries for each selectedactor, PinAndPrewarmresidentleases until explicitDispose/actordestroy. Includesinactiveopponent duringselection intentionally | ActorCurveWarmup.cs:26–64; NativeSupportOverlay.cs:11–37 |
| activepose extrapin | Overlay Evaluate acquirescurrentpose, OnDisable releasesactivepin; residentwarmleases deliberatelysurviveDisable; Dispose releasesboth | NativeSupportOverlay.cs:10,37,51 |
| decodedcurve/evaluationplans | asset-scoped nonstatic refcount; lastRelease clearsdecoded andevaluationplan. BinaryTextAsset remainsAddressablesdependency untilassetrelease. Decode temporary packedAsset.bytes+decompressionMemoryStream+ToArray may inflate transientworkingmemory | NativeSupportPoseData.cs:9–18,24–27 |
| contact/floor buffers | fighterinstance arrays/dictionaries andsupportpointarrays, no staticallrostercache. Footrows cacheboneY peractor,contactpose arrays kept forreentry; destroyedwithactor | FighterView.cs:11–19,299–320; NativeSupportOverlay.cs:39–51; TestCandidates/CandidateFootGrounding.cs:5–26 |
| selectionHDR target | one RTcurrentresolution/AA; ReleaseSelectionRender releases/destroys onresize,BeginFight andPrototypeDestroy. Not retained intentionallythroughfight | FightSelection.cs:71,88–101; Prototype.cs:74,118 |
| filmic/GUI materials | filmicmaterialcreated lazily percamera, destroyedOnDisable; GUI wrapper disposedPrototypeDestroy | TestCandidates/FittedFilmicCamera.cs:7–20; PrototypeGuiTextures.cs:4–10; ColorCorrectGuiTexture.Dispose |
| shadowoverride | newfix never clones/owns materials; oldjournal cannot prove newfixinstalled. Previouslyclonedbaseonlymaterial wasperselectedactor anddestroyedwithcomponent, not all11cache | ShadowlessFighterMaterials.cs; newactual14/14tests in docs/testing/shadow-toggle-material-preservation-2026-10-19 |
| tinygenerated UItextures | backdrop/halo/circle/brand destroyedPrototypeDestroy. StaticcontrolIcons64²dictionary remainsappdomainresident boundedbyiconnames; intentionalcachecandidate, no evidence this explainsGiBfootprint | FightSelection.cs:144–146,179–186,238–240; Prototype.cs:118 |
| studioenvironment | smallResourcesconfig/reflectioncube+SH applicationlifetime; cameraprobe snapshots owned/restored onlyscope. Current128cube is small vscharactertextures | SelectionStudioLighting.cs/SelectionStudioLightingConfig; Resources/SelectionStudio |

## All11 resident distinction

Current authoring GameContent.asset isinline11prefabs/5arenas; loading thiscatalog CAN retainall11assetdependencygraphs even though only2areinstantiated. It is localreview/sourcecatalog. EmbeddedGameContent.asset isstreamed11 with emptydirectarrays; ContentBootstrap rejectsinline forpackagedembeddedlaunch. StreamingGameContent.asset currentlycontainsolder8addresses; it is not evidence currentembedded11catalog iswrong. Current HostFightReview scene hasdirectreviewcatalog reference; do not use itsEditorRSS asphone selectedresidencyproof. Need actuallatestiOSBuildLayout/loadedmetadataGUID to verify shippedgraph, not inference from filepresence inUnityproject/IPA.

Sourcecheck exampleproductionBoneHumanoidCandidate.prefab nativeentries haveoriginal:{fileID:0} andpackedpose refs, avoidingrawFBXsourceclipreference inthoseentries. This is oneactorcheck, not currentall11/fullcontrollerdependency audit. SharedAnimationClip/controller assets, meshCPUreadability, pertextureiOSformats/readability/mips andAddressablesbundleclosure need currentartifact-specific listing. Textureasset existence/on-diskFBXsize does not establishresidentbytes.

Portraitsourceimages are768²–1024²/1000×1200; isReadable0 observedfighter9meta. Intentionalall11portrait residency is muchsmallerthanfull11meshes/maps but actualiOScompressed/mipmemory not measuredhere. Do not silentlydownres them or alter originalmapbytes.

## Explicit unpromoted assets/gates

DenseRigifyReview standaloneoriginal~2M actor,27.6MBsourcekeybridge,3.3GB reviewHumantransport, copied99MBBOX andallUniqueDancesReviewvariants are NOT currentcatalogpromotion. Their presence inAssets does not mean loaded/shipped; assertlatestbuilddependencyclosure excludes them. Newcamera bounds arefiniteoriginalweight/ungroundedreviews, notproduction. DenseclothB, height/grounding andlargerIdlecamera remainunaccepted. No extra11uniquedancesaccepted/promotioninthisaudit.

## Next minimal memory attribution gate, no freeing patch yet

On an identifiedcurrentiPhonebuild log selectedaddresshandles, assetGUIDs, nativeActiveUsers/decodedcounts, liveFighterView/SMR/Texture/AnimationClip counts+Profiler.GetRuntimeMemorySizeLong perownedasset atsettledmetadata→pairwarm→fight→victory→newpair→destroy+deferredrelease. DistinguishUnityallocated/reserved/Mono/native/iOSfootprint/GPUunavailable. Use finite repeatedpair/reentry sequence andcompareeachassetidentity/lifetime; onlyexplicitunusedasset-unloadcheckpoint AFTERallleasesreleased, nevermidfight. Need sameiOSlayout toattribute sharedbundles/textureformats/meshreadability andverify noall11graph. Do not insertunloads ordisposeonDisable blindly: disabledopponentandpreviewreentry retain intentionalleases; actualownershiptests already enforce this.

No concrete unboundedall11load/leak proved by this staticaudit. Largecurrentselecteddependency/decodedmotion/textures andtransientdecode/GPUallocatorretention remain attributioncandidates. No heatfix/phoneacceptance claimed.
