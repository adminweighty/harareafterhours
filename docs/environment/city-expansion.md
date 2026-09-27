# Reference-led city expansion and graphics pass

## What this pass delivers

The original game, CBD plots, First Street/Bata landmark, shops, characters, missions, police, traffic and touch controls remain in place. A separate `Connected Harare avenue expansion` root adds roads and architecture around them.

- Supported ground: **1,200 × 1,200 metres**, nine times the preceding 400 × 400 m ground area. This is the supported play area, not 1.44 km² of fully detailed city.
- Connected road network: outer avenues at ±420 m, cross avenues at ±210 m, and extensions of the central roads. Original roads inside the CBD are unchanged. Outer roads are 16 m wide; central extensions retain 12 m width.
- **197 building instances** use three original reference-derived facade modules: ribbon-window offices, CBD shop blocks, and lower garden-office blocks. The outer districts are modular approximations, not 197 individually reconstructed real buildings.
- Avondale-inspired shopping entrance: arched cream infill, twin entrance towers, glazed doorway framing, arcade columns, pitched roof forms, four low-poly palms, shade trees, paved forecourt, lowered driveway access and five static parked vehicles. It is a recognisable first-pass silhouette, not a finished exact model or accessible shopping interior.
- Original driving and pedestrian populations remain unchanged. New districts do not yet have full pedestrian/traffic population or AI racing opponents.

## Racing

The Avenue Circuit is an opt-in **3,980 m time trial**. From the original CBD, drive north on the central road to the marked start at `(0, 110)`; stop and tap **RACE**. Follow the next checkpoint gate and the HUD direction/distance. The route heads north, around the outer avenues, then returns to the start.

Checkpoints must be crossed in order. Leaving the current road corridor for more than five seconds, changing vehicles, or a large position reset ends the attempt. END RACE cancels without exiting the car. Best time is saved locally; the first completion awards 500 points through existing progression. Campaign mission state remains separate. This is local arcade gameplay, not server-authoritative anti-cheat.

## Reference comparison and remaining work

| Supplied images | Interpretation / treatment |
|---|---|
| 01–02, 14: Mbuya Nehanda/intersection | Important landmark; sculpture, pedestal and surrounding towers still require dedicated modelling. Do not substitute an unrelated statue. |
| 03–04: Copacabana | Terminus canopy, kombi concentration and rounded tower identified; exact reconstruction remains pending. |
| 05–09, 16, 20: nightlife | Existing Big Apple/Private Lounge exteriors retained. Interiors, night lighting, wet streets and crowds are not implemented in this pass. |
| 10–11: First Street | Existing Bata/reference block preserved, including its previous photographic materials and vendor details. |
| 12–15: CBD/avenues | Window ribbons, concrete floor bands, upright facade divisions, shop glazing and street proportions informed new modular blocks. New roads are fictional game connections, not GIS-aligned Harare roads. |
| 17: Avondale | Main landmark worked on in this pass. Visible arch, towers, arcades and palms were prioritised. Depth, wing length and forecourt dimensions are inferred. Generic CAFÉ / GRILL labels are fictional supporting signs, not asserted tenants. |
| 18: Borrowdale | Lower office blocks and planted frontage informed the outer district module; not an exact Borrowdale reconstruction. |
| 19: Mbare | Existing market retained. Accurate produce stalls, crowds and kombi models remain a separate market pass. |

The supplied images are small, cropped reference strips (836 × 188 pixels). They do not provide measurable rear/side elevations or a complete street plan. Exact reconstruction requires higher-resolution originals, multiple angles, dimensions and/or surveyed map data. Landmark sculpture, unique roof detail and close-up shop assets should receive a dedicated Blender/FBX pass; modular prefabs are isolated to make later replacement straightforward.

## Graphics

Pause → **Balanced graphics** or **High graphics**. The choice persists locally.

| Setting | Balanced | High |
|---|---|---|
| Render scale | 0.85 | 1.0 |
| Anti-aliasing | FXAA (single-sample target) | FXAA (single-sample target) |
| Main-light shadow map | 2048, two cascades | 2048, two cascades |
| Shadow distance | 55 m | 90 m |
| Camera view distance | 520 m | 700 m |

Both use soft main-light shadows, ACES tone mapping, restrained colour grading and low-intensity bloom. Existing photographic asphalt/concrete are reused. A CC0 photographic sky supplies clouds and global reflection data; it is a sky-only image, not another city's architecture. Global rendering changes affect existing cars and characters too, but their meshes/rigs are not rebuilt by this pass.

Live iPhone simulator testing exposed an MSAA attachment-sample mismatch in Flutter's embedded Unity surface. Both presets now use a single-sample native target and camera FXAA, avoiding that incompatible MSAA path.

This does **not** establish Xbox-equivalent graphics or performance. It does not include ray tracing, a full high-detail asset library, baked city-wide lighting, or baked occlusion. Runtime city construction must first be converted into stable bakeable scene cells before Unity's baked occlusion/lightmapping workflow can be applied reliably. The present optimization is shared geometry/materials, combined facade meshes, three building LODs, culling and a single mesh for added road markings. No additional real-time street lights were added.

## Assets / locations

- Editor generator: `unity/Assets/Editor/CityExpansionInstaller.cs`
- Runtime: `ExpandedCity`, `CityTimeTrial`, `CityGraphics` in the existing Unity `Assets/Scripts` folder.
- Prefabs/settings: `Assets/Resources/HarareEnvironment/CityExpansion/` — AvenueOffice, CBDShopBlock, GardenOffice, AvondaleEntrance, AvondalePalm, AvenueJacaranda, Balanced, High, CityGrade and PhotographicSky.
- Combined meshes/materials/credits: `Assets/Environment/CityExpansion/`.
- New materials include avenue glass, ivory frames, weathered roof, sandstone trim, palm fronds, depth-tested lettering and photographic sky. Existing concrete/asphalt/tree materials are reused; no existing asset was deleted.
- Source sky: `Assets/Environment/Textures/PhotographicSky/kloofendal_48d_partly_cloudy_puresky_2k.hdr`.
- Pre-edit script backups, build logs and validation output: `/private/tmp/harare-city-expansion/`.

## Research and licensed source

- [Unity mobile art optimization](https://unity.com/how-to/mobile-game-optimization-tips-part-1): silhouette-first modelling, LOD, mesh combination, texture budgets and the distinction between automatic frustum culling and baked occlusion.
- [Unity mobile graphics guidance](https://unity.com/blog/games/optimize-your-mobile-game-performance-expert-tips-on-graphics-and-assets): lighting cost, lightmaps/probes and mobile-specific tradeoffs.
- [Unity URP quality settings](https://docs.unity.com/en-us/engine/6000.0/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp/quality-settings-location): render scale, MSAA and shadow settings.
- [Unity camera anti-aliasing](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/api/UnityEngine.Rendering.Universal.AntialiasingMode.html): FXAA as a post-processing anti-aliasing pass.
- [Unity URP post-processing](https://docs.unity.com/en-us/engine/6000.6/manual/post-processing-and-full-screen-effects/urp/integration-with-post-processing): restrained mobile post-processing instead of expensive full-screen effects.
- [Unity cubemap workflow](https://docs.unity.com/en-us/engine/6000.6/manual/materials-and-shaders/textures/class-cubemap/create): HDR cubemap import and reflection convolution.
- [Poly Haven photographic sky](https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky), Greg Zaal / Jarod Guest, [CC0](https://polyhaven.com/license). Unmodified 2K source, imported as a mipmapped 512-face cubemap. It is not a Harare sky survey.

## Verification

Expansion play-mode checks passed 803 asphalt/clearance samples over the circuit, ordered checkpoint progression, best-time saving, first-finish reward, retry/cancel and unchanged campaign state. Race completion uses a deterministic movement fixture, not a claim that a human drove the full lap. Editor frame sample before the final photographic-sky pass: median 16.69 ms, p95 16.72 ms; this is not a physical-phone benchmark.

Final post-sky expansion validation passed, including all 803 road samples and race checks. Editor median 16.70 ms / p95 16.71 ms. Ground/walking/jumping, original car and pedestrian counts, First Street walking clearance and photographic surfaces also passed after separating the five parked scenery cars from driveable vehicles. Review images and text reports are in `docs/environment/city-expansion/`.

After the live MSAA issue, the complete expansion validator passed again with explicit assertions for a single-sample pipeline, camera MSAA disabled and FXAA enabled in both presets. Latest editor frame sample: median 16.69 ms / p95 16.72 ms. Flutter analysis passed without issues. Native simulator verification is a separate check from these editor tests; no physical-device GPU/thermal benchmark has been performed.

Final iOS simulator export and native UnityFramework build succeeded. Flutter launched the rebuilt app on iPhone 16 Pro / iOS 18.6 (Xcode app build 14.7 s). The live city displayed the photographic sky and extended buildings without the previous red render-pass errors. Both High and Balanced were selected from the actual pause menu and their saved preference values verified; gameplay resumed. The simulator is left running with Balanced selected. Camera gestures and a complete human-driven race were not validated in this final smoke test. The existing `flutter_unity_widget_2` Swift Package Manager warning remains non-fatal and requires a separate plugin migration.
