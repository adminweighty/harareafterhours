# Harare environment — area 01: First Street reference frontage

Source Unity project: `unity/unity_game`. Existing production scene: `Assets/Scenes/SampleScene.unity`.

[Before, pavement view](first-street/before-pavement.png) · [After, pavement view](first-street/after-pavement.png) · [After, corner view](first-street/after-corner.png)

## Reference analysis and scope

All 20 supplied images were inspected. They are very wide, low-resolution crops (approximately 836 × 188 pixels); several contain borders, captions or fragments of adjacent images. They are used as visual references, not facade textures. They do not provide a survey, precise dimensions, complete roofs or rear elevations. Different views sometimes conflict. Exact real-world placement cannot be established from these images alone.

The existing city is a seeded procedural grid of 36 unnamed buildings, with no building-to-location registry. Existing photo panels are not reliable evidence of a building's identity. The first match is therefore a deliberate reference-to-plot assignment, not a claim that the previous box already represented a particular real building.

| References | Visible architectural/environment cues | Existing correspondence and status |
| --- | --- | --- |
| 01–02, Mbuya Nehanda statue | Raised single bronze figure, rectangular stone plinth, office towers, signals, mature trees | No corresponding model. Central junction is only a candidate, not a verified placement. Needs sculpted model and consistent reference. |
| 03–04, Copacabana | Long low terminal fascia, open bays, white kombis, vendor umbrellas, rounded high-rise behind | No terminal or rounded tower. Candidate northwest commercial block; queued. |
| 05–06, Big Apple exterior | Dark long fascia, red serif lettering, glazed entrances, number 143, stone upper facade | No identified model. Queue a shopfront module adaptation in a later block. |
| 07, Big Apple interior | Dark club room, tables, performance area and coloured lighting | No interior correspondence. Separate future interior work; do not infer exterior dimensions from it. |
| 08–09, Private Lounge | Black/gold fascia, horizontal metal slats, glazed entrance, planters; dark furnished interior | No corresponding building/interior. Queued. |
| 10, First Street day | Tan concrete/stone framing, repeated recessed glazing, covered shop pavement, visible Bata sign, blue street sign, street vendors | **Area 01:** existing seeded plot `(18,18)` assigned to this frontage. Only one building is replaced. |
| 11, First Street night | Dense retail, covered pedestrian frontage, reflective pavement, kombis, illuminated small signs | Supports pavement/retail character. Camera and storefronts differ from 10; not treated as an exact reverse angle or justification for moving roads. Night wetness/lighting deferred. |
| 12, Jason Moyo | Tall slab offices, palm-lined avenue, Stanbic fascia, banner poles | No identified counterpart. Existing central road is only a candidate; queued. |
| 13, Samora Machel | Broad divided avenue, long skyline, paired lamps, large billboard | Existing road grid lacks the visible median. Requires a later road/traffic review, not a change in this area. |
| 14, intersection | Two bronze figures, plinth inscription and signals among office towers | Conflicts with the single figure/raised arm in 01–02. Do not combine these into an invented monument. Sculptor needs authoritative consistent views. |
| 15–16, Mbuya Nehanda Street | Close retail fronts, recessed glazing, concrete bands, OK/TECNO signs, traffic and night venues | No exact building match. Queued; signs in 16 differ from 05–06, so do not assume one Big Apple elevation. |
| 17, Avondale | Low tiled-roof shopping centre, central arch, glazed entry, palms, parking | No corresponding arch or centre. Queued landmark; silhouette can be modular, roof junctions may benefit from Blender. |
| 18, Borrowdale | Low modern retail, landscaping, stone locality wall, broad driveway | Existing photo showroom is not a matching building. Queued separate district frontage. |
| 19, Mbare Musika | Market hall sign, weathered concrete upper floors, stalls, umbrellas, produce and kombis | Existing mission market is the nearest semantic match. Preserve its checkpoint; refine it in a later area. |
| 20, The Avenues | Street-facing bar fronts, dark fascia/gold script, trees and busy kerb | No identified counterpart. Queued. |

Anonymous background towers and partially occluded shops in these crops are not individually identifiable. They remain unmodified pending their area pass. This report does not claim the entire reference set has been reconstructed.

## Building changed

One prototype building at `(17.8968, 0, 19.6303)` metres. Its original footprint, approximately `14.789 × 10.0182 m`, and centre are retained. Original height was approximately `7.1157 m`; the reference-led first pass uses a 3.9 m retail floor, three 3.25 m upper storeys and a small flat parapet (14.25 m total). Total floor count and roof are provisional because the photograph cuts them off.

Added street-facing concrete framing, recessed glazing, window mullions and sills, ground-floor shop glazing, double-door details, covered arcades, supporting pillars, parapet and Bata lettering. The side/rear elevations repeat the visible architectural vocabulary conservatively. Shop doors remain closed: this pass does not introduce playable interiors.

The local pavement fills the existing setback behind the unchanged 12 m roads, with 180 mm paving, kerbs, paving joints, a First Street sign, a small vendor table and one broad-canopy tree. Their precise positions and dimensions are inferred within the existing plot. Road centre lines, intersections, traffic paths, mission checkpoint, player start and nearby challenge beacons are preserved. Shared street-light defects were also corrected: assigning the light's local position used to move the entire pole to the world origin, and cylinder scaling made the poles nearly 12 m tall with lamps midway up. Child light anchors now leave all 21 poles at their originally intended street positions. Six-metre poles use a simple horizontal arm and lantern; the existing light count is unchanged.

## Added assets and organisation

Under `Assets/Environment/`: Buildings, Roads, Props, Vegetation, Signs, Materials, Textures, Prefabs/Modules, ReferenceImages and Reports. All supplied reference images are retained outside Resources and are not referenced by the new runtime prefab.

Runtime assembly: `Assets/Resources/HarareEnvironment/FirstStreetCorner.prefab`. Reusable source prefabs: OfficeWindow, Shopfront, ArcadePillar, Parapet, Kerb and VendorTable. The assembly's per-material meshes are generated from the same module functions; source module edits require updating/rebuilding the generator, not automatic propagation through nested prefab instances.

Fourteen shared surface materials: Stone, Concrete, Recess, Glass, GlassAlternate, Metal, Paving, PavingJoint, Kerb, StreetBlue, SignWhite, Wood, Leaf and LeafLight. Sign lettering shares Unity's font atlas. No photographic textures, new real-time lights, mesh colliders or high-resolution material maps are added.

Building and tree each have three LODs. Paving joints, table and lettering cull at distance. Small shapes are combined by material to avoid a draw call per window or paving line. Surface materials permit GPU instancing when reused; this does not claim that every draw is instanced. Static building surfaces are marked for occlusion and the installed scene is baked; the runtime prefab fallback retains geometry/LOD but cannot supply baked static occlusion by itself.

Rebuild with `Harare After Hours > Environment > Install First Street Reference Block`. It changes its own generated assets, preserves scene prefab overrides and refuses to overwrite an unpacked block. Future imported FBX geometry has a named replacement socket at the plot's origin; use metres, +Y up and +Z north. No external modelling is required for this first facade pass. The statue still needs a proper sculpted model; no substitute statue was generated.

## Verification

Unity editor compilation, installation and occlusion bake succeeded. The seeded layout comparison shows exactly one removed prototype building; the other 35 positions and dimensions match the baseline to four decimal places.

The final Unity iOS simulator export and Xcode UnityFramework compilation succeeded. The new framework was embedded into the existing Flutter project; `flutter run` built, installed and launched the application on iPhone 16 Pro (`31E54A3C-F97D-4405-9275-67BF0A3F8FB9`). The Mac remained locked, so computer-use access could not verify the live in-app mission view. The visual evidence here comes from the actual Unity scene rendered in Play Mode. No claim is made that a simulator walkthrough or physical-device profiling was completed.

Play-mode validation passed: a single installed block; valid materials and LOD references; clear carriageways; closed-shop collision; player walks more than 3.5 m along the new pavement with normal movement input; USE enters the existing vehicle and advances the mission; throttle moves it; USE exits and restores the character controller. Teleporting the vehicle to the existing market checkpoint triggers mission completion. This verifies the trigger, not an end-to-end driven route.

All 24 cast roles, 23 NPCs, three traffic vehicles, combat/progression services, Flutter bridge object, HUD and playing music remained present. At least 20 NPCs and all traffic vehicles moved during the observation interval. No runtime errors or exceptions occurred. The test checks bridge presence, not a Flutter round trip or every combat/audio interaction.

Building LOD0/1/2: **4,836 / 1,908 / 492 triangles**, with **8 / 7 / 5 renderers**. Tree LODs: 512 / 192 / 72 triangles. The detailed pavement-joint mesh has 996 triangles in one renderer and culls at distance. The whole added block uses 12 box colliders, no mesh colliders and no new real-time lights. Mesh/asset budget details are saved in Unity's `Assets/Environment/Reports/FirstStreet-asset-budget.txt`.

An editor sample of 180 frames measured median 16.70 ms and p95 16.71 ms, with 295 MiB reported Unity allocated memory for the complete editor scene. These are editor observations at the existing 60 fps target, not a physical-mobile GPU benchmark or a measurement of the block's incremental memory cost. Physical iPhone/Android GPU, thermal and memory profiling remains outstanding.

The reference roofline, total floor count, rear walls and exact pavement layout remain provisional. Higher-resolution full-height building views and alternate angles would permit a more faithful next refinement. The other areas in the reference catalogue remain queued.

Occlusion follows Unity's [static occlusion setup](https://docs.unity.com/en-us/engine/6000.0/manual/cameras/occlusion-culling/getting-started): the replacement is saved into the scene before baking. Existing runtime-created city objects were not converted wholesale to baked scene geometry.
