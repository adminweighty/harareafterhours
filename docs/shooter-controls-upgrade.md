# Shooter controls and encounters — September 25, 2026

## Implemented in the existing Unity project

- Original translucent circular HUD symbols with short labels; no copied COD artwork.
- Compact objective, bottom health strip and weapon panel; omitted squad/score panels.
- Separate hip-fire and Aim. Optional aim-on-fire in SETUP; firing no longer forces slow ADS by default.
- Independent movement, look and fire fingers. Unclaimed screen space supports looking on either side; buttons and the stick retain finger ownership.
- FPP/TPP switch, weapon cycling, magazine reload, punch, kick, jump, sprint and crouch/slide.
- Control editor with drag positioning, individual size/opacity, sensitivity, invert Y, stick sprint and aim-on-fire. Confirm saves; Cancel restores; Reset uses the new defaults. Overlapping/off-screen layouts are rejected.
- Layout v2 is stored separately from legacy layout v1, which is not deleted.
- Accelerated analog walk/jog/sprint, armed strafing, buffered jump, ceiling collision handling and grounded landing parameters.
- Imported the existing humanoid library's crouch idle/walk clips. Reversed grounded gait when backpedalling; lower-body directional posing retains upper-body aiming. Sliding uses an additive pose, not a new motion-captured clip.
- Timed sprint slide with a shorter collision capsule, collision stop, cooldown and crouched finish. Standing/jumping checks headroom.
- Armed robbers at the east block and Joina approach. They chase, warn, sample an aim point, shoot against world collision, and can be interrupted or defeated. Moving after the warning can dodge; solid cover blocks shots.
- Shots deal 15 HP, melee 20 HP, with existing hit-recovery protection and game over only at zero HP. Existing blood-visibility preferences remain handled by the blood-effects system.

Unity source remains `unity`. Pre-edit scripts were backed up to `/private/tmp/harare-shooter-backup`.

## Checks and limitations

The final rendered play-mode suite passed layout save/cancel, simultaneous sprint/fire/look (including camera rotation), slide/stand collision height, crouch animation parameters, jump/landing, perspective switching, enemy damage, cover, defeat and backward-gait direction. The existing touch suite passed finger ownership, three-finger move/look/jump, release, pause, focus loss and input locking. Flutter's 31 tests passed and `flutter analyze` reported no issues.

Visual Unity-editor checks confirmed weapon switching, FPP switching, control dragging, individual size/opacity previews, and Cancel restoring the layout. Options now open separately from the drag-layout view to prevent overlaps in short landscape windows. The animation checks exposed and fixed a bootstrap-order bug: the shared visual was created before its player controller and never resolved that reference again.

These checks do not establish COD-level animation quality, phone frame rate, physical touch comfort or final visual fidelity. Slide and directional poses need device visual review; they are not bespoke captured strafing/slide animations. No prone, vaulting, aim assist or multiplayer systems were added.

The final updated iPhone framework export succeeded. The unsigned iOS release build also succeeded (345.6 MB), verifying host compilation and packaging. Signing failed on both attempts with `errSecInternalComponent` and “unable to build chain to self-signed root” for the Personal Team developer certificate. The phone was unavailable during discovery. No certificate trust settings were changed. The current `build/ios/iphoneos/Runner.app` is unsigned and cannot be installed until signing is repaired. Do not confuse the previously installed app with this update.

## Research used

- [Activision: COD Mobile controls](https://blog.activision.com/call-of-duty/2019-10/Getting-a-Grip-on-the-Call-of-Duty-Mobile-Controls): independent hip-fire/ADS, editable HUD size/opacity, sprint/crouch/slide and camera-mode controls informed the interaction design.
- [Unity: CharacterController.Move](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html): collision-constrained movement and explicit gravity handling.
- [Unity: animation blend trees](https://docs.unity.com/en-us/engine/6000.7/manual/animation-section/animation-mecanim/animation-animator-controller/animation-state-machines/animation-blend-trees): blend locomotion from speed instead of switching abruptly between clips.
- [Unity: state speed parameters](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/animations/animatorstate/speedparameteractive): grounded-state playback direction.
