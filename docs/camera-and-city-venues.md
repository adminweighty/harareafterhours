# Navigation camera and storefront pass

## Scope

Incremental changes to the existing Unity city, not a replacement project. Existing building positions, randomized dimensions, mission progression, police, traffic, character and weapon systems are retained.

### Camera

- Closer walking view with forward framing and automatic follow behind walking/running turns. Movement is camera-relative at gesture start; its basis is stable while held so automatic camera yaw cannot cause circular steering.
- Driving camera follows the car's heading after a 1.8-second manual-look grace period, with speed-dependent forward framing and a gradual 65–72 degree field of view.
- VIEW button or C optionally recentres immediately; walking/running no longer requires it. Right-stick, mouse and touch orbit remain available, with a 0.65-second on-foot manual-look grace period before automatic follow resumes during movement.
- Vehicle entry/exit clears stale camera smoothing and starts behind the new target.
- Collision checks ignore the occupied vehicle hierarchy and test the final smoothed camera position against obstacles.
- Existing ranged-weapon shoulder view and reticle remain intact.

### City additions

Four reusable, code-built storefront modules attach to existing buildings without moving them:

| Location | Existing plot | Reference / assumption |
|---|---|---|
| Big Apple | (18, -22), west-facing | Supplied photo 05: dark fascia, red accent, glazing and entrance canopy |
| Private Lounge | (18, 58), west-facing | Supplied photo 08: dark fascia, warm trim and horizontal gold screening |
| City Grocer | (-22, 18), east-facing | Fictional supporting shop, not an asserted real business |
| Sadza Kitchen | (-22, -22), east-facing | Fictional supporting takeaway, not an asserted real business |

These are game-world placements, not surveyed Harare coordinates. The low-resolution references support entrance styling, not exact architectural reconstruction. Existing major building envelopes and First Street/Bata landmark are unchanged.

Walk up to the entrance and press USE / E to check in. First discovery awards 50 points per location, persisted through the existing progression system; repeat visits cannot farm points. Police heat prevents check-in, and main mission interactions retain priority. No real purchases or external transactions are involved.

This pass does **not** implement accessible interiors, dancing, venue management, new music sequencing, or the script's full nightlife mission chain. Those still need a dedicated interior/content pass. Venue materials and sign lettering are generated at runtime; repeated cube geometry is shared, materials are reused within each storefront, distant storefronts are culled with LOD Groups, and no real-time lights are added.

## Research used

- [Unity Cinemachine Third Person Follow](https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/api/Unity.Cinemachine.CinemachineThirdPersonFollow.html): camera offset, damping and obstacle-handling concepts. The existing camera is patched; no Cinemachine dependency was added.
- [Unity automatic heading recentering](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.6/manual/CinemachineBodyOrbitalTransposer.html): follow the target heading with configurable wait and recenter speed. These principles are applied to the existing custom rig, without adding another camera system.
- [Unity mobile graphics and asset optimization](https://unity.com/blog/games/optimize-your-mobile-game-performance-expert-tips-on-graphics-and-assets): lightweight geometry, material reuse and visibility culling.

## Verification

`HarareAfterHours.EditorTools.CameraVenueValidation.Run` checks storefront approach clearance, one-time rewards, unchanged main mission state, camera obstacle clearance and car self-collision exclusion. It restores the saved points record after testing. Screenshots and logs are in `/private/tmp/harare-camera-venues`.

Editor checks do not establish physical-device frame rate. The embedded Flutter app must use a rebuilt UnityFramework to display these changes.

Completed checks: camera/venue play-mode validation passed; ground, walking, jumping, original vehicle/NPC counts and daylight presentation regression passed. Ground-test editor frame sample: median 16.69 ms, p95 16.75 ms (not a phone benchmark). The Unity iOS simulator export compiled successfully. Review images are saved in `docs/gameplay/camera-venues/`. Final simulator interaction is blocked while the Mac is locked.

Native `UnityFramework` build succeeded and was copied into `ios/UnityLibrary/UnityFramework.framework`. The previous framework and pre-edit script copies remain recoverable in `/private/tmp/harare-camera-venues/before/`.

## Automatic turn-follow fix — 23 September 2026

Removed the explicit on-foot automatic-camera exclusion. Walking/running now smoothly follows the player's heading, with a 0.65-second manual-look grace period. Held movement keeps a stable yaw reference to prevent camera/movement feedback loops; a fresh gesture uses the current view direction. Car follow, obstacle collision and optional VIEW/C reset remain available.

The extended play-mode validator passed left/right/back/forward alignment, straight held-input displacement, manual-look priority, wall collision, car heading and own-car collision exclusion, plus the existing venue/mission checks. These are deterministic Update/LateUpdate fixtures, not a physical-phone gesture benchmark. Results: `docs/gameplay/camera-venues/auto-follow-validation.txt`. Unity simulator export and native framework compilation both passed.

The rebuilt Flutter app launched successfully on the iPhone 16 Pro simulator and rendered the game without a visible error overlay. Automated simulator drags did not produce an observable movement result, so sustained touch-turn behavior still needs a human/device check; the alignment assertions above are editor fixtures. The app was left running. Temporary native build copies were removed; source and validation logs remain.

Full Flutter simulator build also passed: `flutter build ios --simulator --debug --no-pub` produced `build/ios/iphonesimulator/Runner.app` (Xcode build 47.2 seconds). This is a build verification, not a live simulator interaction check. Regenerable native staging/DerivedData folders from this pass were removed after success; backups and evidence were retained.
