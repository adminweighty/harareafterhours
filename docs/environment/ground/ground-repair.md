# Reference-led ground and environment surfaces

## Scope

This increment improves the existing game's ground and surface presentation, not a replacement project or a complete reconstruction of all 20 locations. Street centres, building plots, the First Street corner geometry, player/NPC controllers, vehicles, missions, Flutter UI and backend are preserved.

The supplied folder already existed in Unity's reference library. For this surface pass, `10_First_Street_Day.png`, `12_Jason_Moyo_Avenue.png` and `19_Mbare_Musika_Market.png` provided the paving, asphalt, weathered concrete and dusty-earth palette. Their small perspective views are not scan-quality material maps. The new atlas is an AI-generated interpretation, not direct photogrammetry and not proof that every building matches a photograph.

## Implemented

- Continuous 400 × 400 metre collision ground replaces the 150 metre plane that ended before the 156 metre roads. Ground top remains at y=0. Distant invisible boundaries at ±198 metres prevent walking off the edge; existing routes and missions remain well inside them.
- Textured asphalt, paving, soil and concrete, with world-position projection measured in metres. Scaled meshes no longer stretch a single texture across a whole street.
- Existing 12 metre roads retain their centres. Pavement segments stop at intersections instead of forming raised strips across traffic lanes. Kerbs and two starting-junction crossings were added; road paint is non-emissive and decorative, without collider bumps.
- The First Street paving/stone/kerb materials use the shared surface library; background building colours and daytime windows are less saturated. Building geometry has not been replaced.
- Full street photographs are no longer pasted over cube façades. Large location, character and car reference-board galleries are opt-in with `HarareArtLibrary.Build(true)` for art review. Images and review code remain; normal `Build()` still starts music.
- One existing directional light now casts daylight shadows. No new real-time lights were added. Slightly stronger distance haze softens the unfinished outskirts.

## Assets and implementation

The built-in image-generation tool created [harare-surfaces-atlas.png](harare-surfaces-atlas.png). The complete prompt is retained in [generation-prompt.txt](generation-prompt.txt). No original reference image was overwritten.

Unity project: `unity/unity_game`.

- `Assets/Environment/Textures/HarareSurfaceAtlas.png`: unchanged generated atlas, imported by Unity as four independent texture-array slices.
- `Assets/Environment/Shaders/HarareWorldSurface.shader`: URP-lit world-space surface shader with shadows, fog, shared material parameters and instancing variants.
- `Assets/Resources/HarareEnvironment/Surfaces/`: Asphalt, Paving, Soil, Concrete, four wall tints, RoadPaint and CrossingPaint materials.
- `Assets/Scripts/GroundEnvironment.cs`: ground, roads, segmented pavements, kerbs, markings and outer collision boundaries.
- `Assets/Editor/GroundSurfaceInstaller.cs`: reproducible texture-array import and material setup. Run `HarareAfterHours.EditorTools.GroundSurfaceInstaller.Install`.
- `Assets/Editor/GroundGameplayValidation.cs`: ground/collision/texture checks, movement checks and scene captures.

The imported array has four 512px slices, mipmaps, trilinear filtering and 4× anisotropy. iOS/Android use ASTC 6×6 settings; CPU readability is disabled after installation. Mirror wrapping avoids hard colour seams in the generated edges. This is an albedo/material pass, not scanned normal/displacement mapping. Existing environment LODs remain unchanged. Ground/kerb meshes are simple boxes with shared materials; no tessellation is introduced.

## Verification and limits

The ground validation checks support at 16 locations outside the old ground, clear intersection collision, the distant boundary, texture-array layers/mipmaps, working shaders, four vehicles and 23 pedestrians. It also exercises normal walking, jumping and landing outside the old city footprint. See `validation.txt` and the before/after images alongside this report.

The existing [city regression](city-regression.txt) passed: First Street collision and LODs, all 24 character roles, 23 moving pedestrians, three moving ambient cars, 21 streetlights, music, combat/progression services and delivery completion. The [vehicle regression](vehicle-regression.txt) passed: acceleration, steering, reverse/braking, suspension, driver and passenger seating, safe exits, traffic-car takeover and mission completion. The city editor sample was 16.69ms median / 16.71ms p95, with 261 MiB allocated for the whole scene; these are not mobile-device performance measurements.

All eight Flutter tests passed after updating `assets/previews/harare-city.png` with the new Unity pavement render. Subsequently, concurrent edits at 16:22 changed `lib/main.dart`, added `lib/ui/city_game.dart`, and changed `lib/ui/unity_game_screen.dart` to start directly in Unity. Those edits were preserved, not authored or reverted by this environment task; the earlier Flutter test result does not validate that later startup work. Visual comparisons: [before pavement](before-pavement.png), [after pavement](after-pavement.png), [starting junction](after-start.png), [extended ground](after-ground.png).

The Unity iOS export, UnityFramework Xcode build and Flutter simulator build succeeded. Live iPhone 16 Pro simulator inspection confirmed asphalt, paving, plaster, kerbs and crossings render correctly without missing/pink materials. The separately changed Flutter startup left its loading indicator visible over the running city; that startup integration needs its own verification. This task did not overwrite the concurrent startup files. The game was left running with the debugger detached.

Only this task's regenerable Xcode DerivedData and framework staging directories were cleaned after installation (about 2.4 GiB). The active framework, prior-framework backup, original references, generated atlas, source code and review renders remain.

The city still has unfinished background block buildings and sparse street dressing. Realistic individual landmarks, accurate traffic signage, trees and detailed shop fronts require subsequent reference-by-reference modelling; this pass does not claim photorealism or exact reconstruction. The new ground is flat because the selected photographs do not establish terrain elevations. The outer boundary is a safety limit, not a newly modelled district. Physical-device GPU/battery profiling remains outstanding.

Initial runtime/material backups are retained at `/private/tmp/harare-ground-repair/before`.

## Internet research

Unity recommends tiled material layers for terrain surfaces, including albedo and optional normal maps: [Terrain Layers](https://docs.unity.com/en-us/engine/6000.3/manual/creating-environments/script-terrain/terrain-textures/class-terrain-layer). For the existing flat mesh city, a shared world-space material retains the gameplay collision layout without introducing a new Terrain heightfield.

Unity's [2D texture arrays](https://docs.unity.com/en-us/engine/6000.7/manual/materials-and-shaders/textures/class-texture2darray/texture-arrays-introduction) keep independently sampled materials in one texture resource. Its [flipbook import settings](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/textureimportersettings/flipbookcolumns) divide an atlas into array slices. [Texture import settings](https://docs.unity.com/en-us/engine/6000.7/manual/materials-and-shaders/textures/textures-reference/texture-type-default) document mipmaps, wrapping and filtering. These are the primary-source basis for this implementation.
