# Shared character repair — 23 September 2026

All 24 cast roles now use the same fitted Character01 mesh, proportions, Humanoid rig, materials and locomotion controller. This patches the existing Unity project at `unity`; it does not replace the city or Flutter application.

## What was wrong

- The old Blender export removed shape keys without applying their combined shape. The body reverted to its unfitted Basis while the clothing, hair, eyes and skeleton retained the fitted proportions. This caused the detached-looking face, eyes, hair and hands.
- Skin, eye and hair texture nodes were not connected to their Unity materials.
- Three independent skeletons were instantiated for the three LODs.
- Every Animator was disabled; a sine-wave script rotated limbs instead of playing locomotion clips.
- Per-character scale changes and unrigged primitive costumes distorted the common base.
- RUN only supplied walking input; there was no touch JUMP control.

## Implemented

- Bake the fitted body and facial shape before removing shape keys (`apply_mix=True`); retain UVs and skin weights. The original editable Blender master is untouched.
- Preserve garment subdivision before mobile reduction for smoother knee, hip and sleeve deformation.
- One calibrated skeleton and Animator per character; all three LOD renderers reference those same bones. Map body, toes and fingers, enforce the reference T-pose, and correct the animation forward axis.
- Explicit URP skin, eyes, brows, hair, clothing, normal-map and shoe material assignments. Avoid multiplying an already-dark skin texture by another dark colour.
- Shared geometry at uniform scale. Cast differences are surface/material palette variants, not separate body models or rigid costume primitives.
- Authored idle, walk, run, jump-rise, airborne and landing animation states. Movement, collisions and gravity remain owned by the existing CharacterController; animation root motion is disabled.
- Match locomotion playback to measured contact speed on the fitted rig. The forward-running clip is used for both jogging and running; the downloaded sprint take is retained but not used because its in-place contact timing was inconsistent.
- Mobile RUN requests sprint and preserves directional input. Mobile JUMP is added. Keyboard Shift/Space and gamepad left-stick click/north button also control sprint/jump; existing USE bindings remain unchanged.
- Vehicle visibility respects LOD selection, and stale jump/vertical velocity is cleared on vehicle entry/exit.
- User-photo input remains available as an optional chest-mounted portrait badge, not a floating rectangle covering the repaired face. This is not photo-to-3D facial reconstruction.

## Previews

- [Original broken export](previews/before-front.png)
- [Repaired base in idle](previews/after-front.png)
- [Face close-up](previews/after-face.png)
- [Walking](previews/walk-14.png)
- [Running](previews/run-14.png)
- [Jump pose](previews/jump-rise-14.png)

Pose captures use Unity's evaluated skinning, baked temporarily for deterministic editor screenshots. No character screenshot was AI-generated or retouched. Jump preview shows the pose at a fixed review origin; actual airborne height is tested separately.

## Verification

- [Asset and pose checks](asset-validation.txt): all 24 instances share the exact same base mesh; one Animator each; valid Humanoid/finger mappings; shared LOD bones; diffuse maps assigned; correct facing; authored foot motion; no root-motion drift.
- [Live movement checks](play-validation.txt): walking **1.80 m/s**, sprint **5.50 m/s**, jump apex **1.44 m**, successful landing, no midair double-jump, input release and reverse-facing checks pass.
- [Existing-city regression](city-regression.txt): 23 NPCs and three traffic vehicles retained and moving; pavement collision, vehicle entry/driving/exit, mission completion, HUD, combat/progression services, audio and bridge retained; no runtime exceptions.
- Final editor sample: median **16.70 ms**, p95 **16.71 ms** over 180 frames, 286 MiB editor allocation. These are not physical-device GPU or memory measurements.
- LOD triangle counts: **18,725 / 10,631 / 5,137**. Four skin weights per vertex; shared texture/material assets; distance LOD culling retained.

## Main files

- Unity runtime: `Assets/Scripts/CharacterRoster.cs`, `ThirdPersonController.cs`, `MobileControls.cs`, `GameplayHud.cs`.
- Shared prefab and model exports: `Assets/Resources/HarareCharacters/Character01.prefab`, `Character01_LOD0/1/2.fbx`.
- Shared material and animation assets: `Assets/Characters/Materials/`, `Assets/Characters/Animations/`.
- Repeatable installer: `Assets/Editor/SharedCharacterInstaller.cs`.
- Review/tests: `CharacterRepairReview.cs`, `SharedCharacterValidation.cs`, `SharedCharacterPlayValidation.cs`, and the existing First Street gameplay regression.
- Blender exporter: `tool/character_repair/export_shared_base.py` in the Flutter repository. Its source is `character-prototype/v2/delivery/Character01_Editable.blend`; output is `/private/tmp/harare-character-repair/repaired`.
- Original mesh/code backup from this repair: `/private/tmp/harare-character-repair/before` (temporary recovery files, not permanent version control).

## Sources and scope

Animations come from [Quaternius Universal Animation Library Standard](https://quaternius.com/packs/universalanimationlibrary.html), downloaded through its [official itch.io page](https://quaternius.itch.io/universal-animation-library), under CC0. The licence is retained at `Assets/Characters/AnimationSource/Quaternius-License.txt`. The base remains the existing MakeHuman/MPFB-derived character; no restricted Renderpeople source was used.

Rigging and controller implementation follows Unity's [Humanoid avatar configuration](https://docs.unity3d.com/6000.0/Documentation/Manual/ConfiguringtheAvatar.html), [blend trees](https://docs.unity3d.com/6000.0/Documentation/Manual/class-BlendTree.html) and [root-motion guidance](https://docs.unity3d.com/6000.0/Documentation/Manual/RootMotion.html). The editor installer invokes Unity's own Enforce T-Pose operation; this editor-only reflection is tied to the installed Unity 6000.3 API and must be rechecked when upgrading Unity.

This is one shared body/face/hair/clothing geometry set with material-colour variations, not 24 separately painted facial textures or distinct hairstyle meshes. Facial expression animation, lip-sync, photo-to-face reconstruction and terrain-adaptive foot placement were not added. Physical iPhone/Android performance still needs device profiling; the editor checks do not establish production readiness or visual perfection.

## App build

Unity iOS simulator export, Xcode UnityFramework build and Flutter debug build all succeeded. The app was launched on the existing iPhone 16 Pro simulator (`31E54A3C-F97D-4405-9275-67BF0A3F8FB9`). The first mission was opened, and live UI checks visibly confirmed the repaired standing character, animated NPCs, the touch JUMP action and turning to face the camera. The mission is left open; no mission completion was submitted from this simulator check.

The existing `flutter_unity_widget_2` Swift Package Manager compatibility warning remains; it did not prevent this build. The backend was offline during the simulator check, so this does not verify online features.

The generated scratch Xcode build directories were removed after installation to avoid retaining duplicate gigabyte-sized build products. Source assets, reports, previews and the pre-repair recovery backup are retained.
