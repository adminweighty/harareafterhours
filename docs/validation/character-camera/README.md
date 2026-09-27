# Street characters and camera follow — 27 September 2026

Normal armed locomotion previously forced the player's facing to the camera and suppressed follow whenever a ranged weapon was equipped. Facing and follow now use aiming state instead. The movement reference stays fixed during a held input to avoid steering feedback loops; manual camera look retains its grace period.

Character rendering retains the cheapest mesh until the camera far plane (no final LOD cutoff). Skinned bounds are padded once for runtime poses, dynamic occlusion culling is disabled for characters, and animators keep their transforms updated offscreen. Mesh frustum culling and the three existing LOD meshes remain active; no per-frame skinned-bounds recalculation is added.

The brown objects in the supplied phone screenshot were six street practice bags, not character meshes. The bootstrap no longer spawns them. Red HUD labels now distinguish OFFSCREEN, BEHIND COVER and DISTANT THIEF from visible nearby threats. World-space red enemy markers remain. Sight checks are cached for 0.15 seconds and use a reusable raycast buffer.

Validation:
- 49 active character instances and 147 skinned meshes passed material, visibility and LOD checks.
- All 24 cast appearances rendered at all three LODs; pixel checks confirmed each character in every gallery capture. LOD0 and LOD2 were also visually reviewed.
- Pistol and rifle left/right/down/up movement: body turns, camera follows, held direction stays straight. Explicit aiming retains camera-facing strafing.
- No CombatTarget practice bags instantiated.
- Normal street rendering reviewed; black sky and readable character lighting remain.
- Tests preserve local preferences and mission/progression values. Captures are from Unity Editor, not the phone.

Known separate observation: an ambient vehicle appears airborne in the street capture. This change does not modify vehicle physics.

Unity references: https://docs.unity3d.com/cn/2022.1/Manual/class-SkinnedMeshRenderer.html and https://docs.unity3d.com/es/current/ScriptReference/Renderer-allowOcclusionWhenDynamic.html

Deployment: Unity release export, native framework and Flutter release build succeeded. Installed and launched on the connected iPhone 13 Pro Max; CoreDevice confirmed Runner still running (PID 5978). Build logs: `/private/tmp/harare-ios-device.icmNHz/`. Native screen appearance and frame rate were not measured.
