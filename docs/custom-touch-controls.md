# Custom touch controls and perspective

Implemented September 24, 2026 in the existing Unity scene and Flutter host.

- Menu → **Control settings**, while on foot, pauses gameplay and opens a draggable HUD editor. Confirm saves locally; Cancel restores the previous layout; Reset restores defaults. Overlapping/off-screen controls cannot be saved. Look sensitivity and Y-axis inversion are adjustable.
- FPP/TPP switches between an eye-level first-person camera and the existing third-person camera. Vehicles retain their driving camera. First-person hides the player's body/head but preserves the equipped weapon; this does not add a separately animated first-person arms rig.
- Any unused safe-screen area accepts drag-to-look. Buttons, the movement joystick and Flutter's menu retain finger ownership. Moving a joystick finger over FIRE never shoots.
- The weapon panel cycles existing equipment and shows remaining magazine rounds. Rifle: 30 rounds, 2.1-second reload. Pistol: 12 rounds, 1.5-second reload. Reserve ammunition is currently unlimited. Reload blocks firing; switching cancels reload without refilling magazines. Magazine counts are session-local.
- Existing LIFE bar, contextual USE/ENTER/RESCUE/EXIT, jump, punch and kick remain. This is a reference-inspired control set, not a reproduction of the supplied game's art.
- Movement accelerates/decelerates smoothly, feeds actual speed into the existing humanoid blend tree, and supports a 120 ms jump buffer and 100 ms edge grace. Existing jump rise/fall/landing clips are retained. No new motion-capture assets were downloaded.

## Sources informing implementation

- [Microsoft Xbox accessibility: input](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/107): customizable touch positions, sensitivity and inversion.
- [Unity Blend Trees](https://docs.unity3d.com/6000.0/Documentation/Manual/class-BlendTree.html): smooth animation blending based on movement parameters.

## Verification

Unity input/session regression covers perspective toggles, editor pause/cancel, magazine consumption, firing blocked during reload, switching without free ammunition, independent touch ownership, and quit/restart. Shared-character play-mode checks pass walk 1.8 m/s, sprint 5.5 m/s, jump/landing at approximately 1.25 m apex, no double jump, camera-relative direction change and face alignment. Flutter menu tests cover the control-settings action at portrait/landscape sizes and large text.

Physical-device multi-touch and mobile performance still require device testing. Saved normalized layouts may need adjustment after changing screen orientation.

Additional input regression checks inject native touchscreen phases to reposition RELOAD while paused and verify free-area swipes update camera yaw in both perspective modes. Simulator visual checks confirmed the health/ammo HUD, first-person weapon view, magazine depletion and timed reload completion.

The final simulator build is installed. Automated simulator drag gestures did not visibly move controls or rotate the view, so end-to-end drag behavior is **not visually verified**, despite passing injected-input regression checks. Confirm on a physical touch device before release.
