# Fighting and visible enemy knockdowns

Tap FIGHT near a facing enemy; with Unarmed selected, FIRE/HIT also punches. Punches alternate arms with the other hand held in guard. Contact is evaluated 0.12 seconds after the press, near full extension, rather than immediately. Range, facing, line of sight, cooldown, input lock and vehicle restrictions remain in force. Each landed non-final hit interrupts the attack and shows remaining hits. Three hits defeat a hostile; +50 points remain one-time per marked hostile.

`EnemyKnockdown` animates a fall over 0.65 seconds. It checks four directions for clearance, samples the ground and uses a one-time baked posed mesh to fit the final body position rather than relying on padded imported animation bounds. It temporarily disables the standing controller and locomotion animator. Defeated hostiles stay down until encounter restart; officers retain their temporary knockdown and a short recovery transition. Restart restores the original rig pose, animator and collision. Ranged defeats also use this fall.

This is a controlled procedural knockdown using the existing shared models, not a full physics ragdoll or a new motion-capture asset. Ground fitting is designed for streets/pavements; tight corners and steep slopes still deserve manual device QA. The reusable mesh is baked once per knockdown, not every frame. No dynamic rigidbodies/joints are introduced.

## Primary sources used

- [Unity Animator/root motion](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/animator/applyrootmotion): avoid simultaneous root-motion and manual-transform ownership during the fall.
- [Unity Quaternion.Slerp](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Quaternion.Slerp.html): interpolate the fall and recovery rotations.
- [Unity SphereCastAll](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics.SphereCastAll.html): inspect clearance around a falling actor.

## Checks

The staged play-mode test exercises the real unarmed attack path, contact delay, flinch, wall obstruction, third-hit fall, disabled standing capsule, one-time points, persistent down state, restart restoration and officer recovery. It restores the editor loadout/points preferences afterward. Preview and final results are stored in `docs/gameplay/melee/` when verified.

Final results: melee/knockdown and shooting/blood regression checks pass; all 27 Flutter tests pass and static analysis is clean. Unity export and native iOS framework builds succeeded. The updated app was installed and visually verified opening into the city on iPhone 16 Pro Simulator, with saved points retained. The grounded fall preview is an editor test fixture; physical-device fight timing, performance, tight corners and slope behaviour still need manual playtesting.
