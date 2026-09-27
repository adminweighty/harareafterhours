# Cyan practice-target correction

The large cyan cylinder/sphere objects in the supplied simulator screenshot map to `CreateCombatChallenges` in `HarareAfterHoursBootstrap.cs`. They were intentionally generated primitives, not missing character/building assets. `CombatTarget.Configure`, flash recovery and respawn then overwrote every renderer with cyan plus emission, erasing the separate frame/core/base colours.

Changes:

- Replace six floating/rotating beacon models with grounded freestanding padded practice bags, rubber bases/supports and canvas-coloured reinforcement bands.
- Remove their six cyan point lights and idle emission. Keep the same positions, hit colliders, combat target identity, rewards and respawn timing.
- Preserve original materials and renderer property overrides. Apply only a brief hit-colour override, then restore the original appearance after the flash and after respawn. Avoid cloning materials for each hit renderer.
- Existing objective markers, vehicle materials, character textures and equipment are not globally recoloured. The violet/grey pulse weapon in the screenshot is a separate stylised equipment model, not a missing-texture shader.

The replacement is a low-poly practice prop, not a scanned leather asset. No third-party model or texture was downloaded for this fix.

Research:

- [Unity error and loading shaders](https://docs.unity3d.com/2023.2/Documentation/Manual/shader-error.html): error materials are magenta; cyan can also indicate a shader-loading placeholder. The exact matching geometry and explicit runtime cyan assignment established the cause here rather than colour alone.
- [Unity Renderer.SetPropertyBlock](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Renderer.SetPropertyBlock.html): temporary per-renderer property overrides without replacing the underlying material.

Validation covers all six meshes/materials, no target lights/emission, grounded appearance, hit flash restoration, damage, score reward, duplicate-hit protection and respawn. Logs and review capture: `/private/tmp/harare-practice-targets/`.

All target play-mode checks passed. Unity simulator export and native UnityFramework compilation succeeded. The editor review image and test result are copied to `docs/gameplay/practice-targets/`. The previous automatic-camera-follow fix remains in this build.

Live iPhone 16 Pro simulator verification confirmed the foreground and background cyan beacons are replaced by the brown/canvas practice props, with the game and HUD rendering normally. Flutter/Xcode launch succeeded (14.9 s build). App left running; regenerable temporary native-build copies removed.
