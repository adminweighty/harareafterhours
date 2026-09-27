# Bullet and blood feedback

## Changes

- Removed the rifle's cyan, full-path beam. Both firearms now use a warm, thin travelling streak no longer than 0.65 metres. Cosmetic travel uses 300 m/s; damage remains the existing immediate raycast, not a ballistic simulation.
- Shortened the muzzle flash to 35 ms and reduced its size. Eight reusable brass casing objects eject beside the weapon and expire or stop rendering at obstacles. No per-shot casing allocation or rigidbody simulation.
- Replaced the descending electronic tone with a procedural noise crack and low-frequency report. This is synthesized audio, not a recorded firearm sample.
- Separated world-surface contact from successful combat damage: walls can show a brief impact without falsely confirming a character hit.
- Character hits retain flinch/down reactions and produce smaller gravity-driven red droplets. Droplets raycast along their motion and can become surface marks. Rounded irregular shared stain geometry replaces spiky shapes; the material responds to scene lighting.
- Blood remains capped at 72 pooled objects. The existing blood toggle immediately clears/hides it. Reduced effects hides tracer, muzzle flash and casing ejection and reduces blood droplets. Mute remains effective.
- Existing weapon save identifiers (including “Pulse rifle”), damage, hostile rewards, police consequences, missions and game-over/restart behaviour are unchanged. The existing weapon models are not replaced by this effects update.

## Validation

Unity play-mode regression covers actual ranged character damage, wall obstruction, blood emission, bounded pool reuse, hidden/reduced blood, downed-hit guard, police consequences, hit confirmation, mute, tracer colour, muzzle origin and short tracer length. The test restores editor loadout/points preferences. Visual preview is a controlled editor fixture, not a physical-device performance benchmark.

Final checks passed. The Unity export and native framework build succeeded, and the rebuilt Flutter app was installed and visually checked opening into the existing city on iPhone 16 Pro Simulator. Frame-by-frame gunfire appearance on a physical iPhone remains a manual QA check. Saved simulator points remained 180 at launch. Evidence: `docs/gameplay/bullet-effects/validation.txt` and `blood-preview.png`.

## Primary references

- [Unity LineRenderer.SetPosition](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/LineRenderer.SetPosition.html): moving the two endpoints of the short streak.
- [Unity Physics.Raycast](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics.Raycast.html): world contacts and hit obstruction.
- [Unity object pooling](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Pool.ObjectPool_1.html): reuse rather than repeatedly creating/destroying effects. This implementation uses fixed-size arrays rather than Unity's generic pool.
