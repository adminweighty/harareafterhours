# Joina City opening — incremental landmark pass

## What changed

The existing Unity project now builds a Joina City block on the connected avenue, north of the original CBD. No scene, controller, character asset, mission enum value or existing city asset was deleted. Conflicting generic building plots are reserved for the landmark. This is an additive playable area, not a replacement game.

- First visit starts on the Joina approach pavement with the selected player already standing and controllable.
- The camera initially frames the landmark. Moving or looking releases that framing into the existing following camera; there is no forced cutscene or text wall.
- The opening uses a wider portrait field of view, then returns to the normal gameplay field of view. Opening hints describe the contact objective, and the actual touchscreen USE button becomes TALK near the contact.
- Objective: **Meet the contact at Joina City**. Within three metres, USE/TALK completes the welcome and awards 25 points once.
- The original saved campaign stage resumes afterward, including already completed campaigns. Tino's vehicle and campaign progression remain gated until the welcome finishes; other vehicles remain usable.
- Restart returns to Joina while the welcome is pending. Later launches follow the previous campaign startup behavior.
- Six pavement pedestrians, two slow traffic vehicles and one usable parked SUV make the approach playable. The existing four-kilometre time trial, police, combat, character customization and pause/quit systems remain in the project.

## Reference interpretation

All four supplied September 23 Joina images informed the design: elevated daylight for massing; night for warm retail lighting and glazing accents; sunset driving for the palm-lined road and frontage; on-foot daylight for the initial objective and landmark-facing arrival.

Implemented features: cream textured podium, glazed shop arcade, stone office tower with repeated recessed windows, staggered half-round blue glass wings and horizontal bands, fluted roof drum and disc crown, Joina lettering, panel seams, roof railing, palm rows, benches, planters, city banners, crossing paint and wayfinding.

The images are generated concepts and disagree on surrounding street labels/layout. The landmark's own newsletter identifies **Jason Moyo / Julius Nyerere**. Existing game road coordinates are deliberately retained; the new block is not a geographically exact map. Dimensions (approximately 60 × 48 m podium and 100 m crown height) are inferred game-scale proportions, not a survey. Generic shop category signs are used instead of asserting unverified tenants.

This is a first geometry/art pass, **not pixel-identical photorealism**. It does not reproduce the night image's wet reflections, enormous crowds, detailed interiors or every adjacent building. Rear elevations are simplified. A production-quality custom Blender landmark, authored façade PBR maps, detailed interiors, baked lighting and real-device profiling remain work for a later art pass.

## Assets and performance

Unity implementation: `Assets/Scripts/JoinaCityBlock.cs`.

- Procedurally generated reusable cube components, half-round wing meshes and a shared palm crown mesh; no new imported model or prefab files.
- Runtime materials: textured limestone cloned from the existing world-projected WallCream surface, blue reflective glazing, charcoal metal, pale bands, restrained emissive shop trim, bark and foliage. Existing pavement/asphalt/paint materials are reused.
- Meshes combined by material within the landmark, not across the whole city. Fine façade details and signs have separate distance culling. Simplified box/capsule collision; no extra real-time lights.
- Existing mobile graphics presets, FXAA and **MSAA disabled** are preserved for the embedded Unity renderer.
- This runtime block does not claim baked occlusion culling. A future baked/prefab conversion is needed for authored occlusion and lightmaps.
- Generated meshes/materials are released with the block. Test renders are diagnostic, not measured phone frame rates.

## Verification

`HarareAfterHours.EditorTools.JoinaCityValidation.Run` checks spawn, first objective, interaction distance, material support, LOD presence, absence of additional lights, completion persistence, preservation of an existing completed campaign, one-time reward and existing race length. It snapshots/restores campaign/welcome/progression preferences and saves scene renders.

`JoinaCityValidation.RunFresh` additionally checks the unobstructed pavement route with capsule casts, the campaign-car gate and the transition from the Joina welcome into Tino's keys mission. Both saved-campaign and fresh-start checks passed. Reports and preview renders are in `docs/gameplay/joina/`.

Flutter regression suite: 27 tests passed; static analysis: no issues. The existing melee/knockdown regression also passed with the new district present (delayed hits, wall occlusion, falling, one-time score and recovery/reset). The final Unity simulator export and native framework build succeeded, and Flutter launched successfully on iPhone 16 Pro (iOS 18.6). Visual inspection confirmed the full tower framing, selected player, Joina objective and welcome-specific touch hint in portrait. Full physical-device thermal/frame-time profiling is not covered by these tests.

The first native link attempt reported `errno=28` (space unavailable). A retry with 25 GB available succeeded using the compiled objects; no personal files were deleted. The plugin still emits its existing Swift Package Manager support warning, which did not prevent this build.

## Internet sources

- [Joina City official site](https://joinacity.co.zw/) — shopping/office identity and landmark context.
- [Joina's Insider, official newsletter](https://joinacity.co.zw/wp-content/uploads/2024/07/Joinas-Insider-Vol2Iss02.pdf) — street-corner identification. PDF page rendering timed out; no architectural measurements were inferred from unread pages.
- [Official tenant directory](https://directory.joinacity.co.zw/) — retail/service variety; not used to invent a precise exterior tenant layout.
- [Unity 6: combine meshes manually](https://docs.unity3d.com/6000.0/Documentation/Manual/combining-meshes.html) — bounded mesh combination and its loss of individual culling.
- [Unity 6: LOD Group](https://docs.unity3d.com/6000.0/Documentation/Manual/class-LODGroup.html) — distance-dependent detail/culling.
- [Unity URP performance configuration](https://docs.unity.cn/Manual/urp/configure-for-better-performance.html) — restrained lights/reflections and profiling requirements.

No web-sourced proprietary models, game assets or photographs were downloaded into the game.
