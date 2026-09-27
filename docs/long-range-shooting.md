# Long-range shooting fix

The rifle's hitscan range was hard-limited to 40 metres and the pistol to 28 metres in `PlayerCombat.Equip`, so increasing the city size left visible targets outside bullet reach. Rifle range is now 250 metres; pistol range is 100 metres. These are gameplay limits, not a real-world ballistics simulation.

The existing camera-to-target and muzzle-to-impact raycasts remain intact, including nearest-obstacle selection and the chest-to-muzzle obstruction check. Damage, cooldowns, saves and the main thread's weapon/control changes are untouched.

Unity batch validation `HarareAfterHours.EditorTools.LongRangeShotValidation.Run` passed against the actual `TryAttack` method: both weapons hit a capsule target at 60 m and just inside their maximum range; targets beyond each maximum remain unharmed; a wall blocks both; an offset shoulder camera converges on the distant target. This is an isolated physics test, not a live street-NPC/device playtest. Log: `/private/tmp/harare-long-range-validation.log`.

Reference: [Unity 6 Physics.Raycast](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics.Raycast.html) documents `maxDistance` as the collision-query distance limit. Range changes follow that behavior without bypassing obstruction checks.

This side-conversation patch changes Unity source only. The embedded Unity framework must be re-exported/rebuilt by the main workflow before the installed Flutter app includes the fix; the running simulator was not replaced here.
