# Touch navigation and control design

## Controls

- Left thumb: floating analog stick. Small deflection walks slowly; larger deflection walks faster. Push beyond the outer ring to run. Release to stop.
- Right-side free space: swipe to look or aim. A finger remains assigned to its original control, so swiping across buttons cannot trigger them.
- On foot: HIT/FIRE, FIGHT, JUMP, USE/TALK/VISIT, RIDE, VIEW. Automatic weapons use HOLD FIRE; other attacks trigger on press.
- Driving: the left-side D-pad steers; hold the clearly labelled **ACCEL** pedal to move, **REV** to back up and **BRAKE** to stop. Up/down remain alternate throttle controls. ACCEL and REV together give neutral throttle. Existing safe-exit speed checks remain enforced.
- Passenger: STOP requests a drop-off; EXIT retains the existing safe-exit checks.
- The camera automatically follows walking/running turns, including left, right and turning back. VIEW is now only an optional instant recenter. Keyboard and gamepad controls remain available.
- Automatic follow uses smooth, frame-rate-independent yaw interpolation. A manual camera swipe takes priority for 0.65 seconds on foot (1.8 seconds in a car). A held movement gesture keeps its initial world reference so camera recentering cannot steer the player in circles; release and push again to use the new camera heading. Manual camera look still rotates the movement reference intentionally.

The control layout respects screen safe areas. Rounded, translucent buttons show pressed feedback; the main action is highlighted. The mission/health panels are more compact to preserve the street view. Geometry-based UI uses existing Unity rendering rather than downloaded image assets or an additional UI package.

## Input safety

Independent finger ownership supports movement, camera and actions together. Gameplay polling uses Unity Input System Enhanced Touch, which retains each active contact across input updates; a 13% thumbstick dead zone suppresses drift and diagonal movement is clamped. Touch state is cleared on pause, focus loss, component disable, arrest/input lock, screen/safe-area changes and vehicle-mode transitions. Mouse/IMGUI fallback supports simulator and editor interaction. The old directional HUD remains a fallback when the new component is disabled.

## Internet references used

- [Apple: Design great interfaces for handheld games](https://developer.apple.com/videos/play/meet-with-apple/243/) — touch-native thumbsticks, integrating sprint, clear control labels and pressed feedback.
- [Apple: Game controls](https://developer.apple.com/design/human-interface-guidelines/game-controls) — virtual touch controls alongside other input methods.
- [Unity: OnScreenStick](https://docs.unity.cn/Packages/com.unity.inputsystem%401.8/api/UnityEngine.InputSystem.OnScreen.OnScreenStick.html) — dynamic-origin stick and isolated-input considerations. This project retains its custom input buffer; it does not synthesize a virtual gamepad.
- [Unity: Enhanced Touch](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/Touch.html) — use Enhanced Touch for frame-by-frame touch polling.
- [Unity: WheelCollider steering](https://docs.unity3d.com/kr/6000.0/ScriptReference/WheelCollider-steerAngle.html) and [motor torque](https://docs.unity3d.com/kr/6000.0/ScriptReference/WheelCollider-motorTorque.html) — speed-sensitive steering and torque/braking behavior retained in the vehicle controller.
- [Flutter: EagerGestureRecognizer](https://api.flutter.dev/flutter/gestures/EagerGestureRecognizer-class.html) — the embedded Unity view now explicitly claims touch sequences, including concurrent fingers. Flutter's pause overlay remains above the game view.

## Verification

`TouchControlsValidation.Run` passed analog/dead-zone checks, button hit-region checks, live character movement driven through the pointer router, simultaneous move/look/jump routing, sprint, no slide-to-fire, release and cancellation checks. It does not simulate physical capacitive touch hardware. Physical-device ergonomics, final visual layout, and live driving gestures remain to be checked; the Mac was locked during this pass.

Code: `unity/Assets/Scripts/TouchGameplayControls.cs`, with integrations in `GameplayHud`, `ThirdPersonCameraRig`, and `MobileControls`. Logs and pre-edit backups: `/private/tmp/harare-touch-controls/`.

Existing vehicle regression also passed with the new controller enabled: acceleration, steering, reverse/braking, safe exits, passenger ride/drop-off, ambient-car takeover, delivery mission and pedestrian count. These exercise vehicle gameplay but do not substitute for a physical multi-touch driving test. Saved test reports are in `docs/gameplay/touch-controls/`.

Build verification completed: Unity simulator export and native framework build passed; Flutter analysis reported no issues for `unity_game_screen.dart`; formatting check passed; the complete Flutter debug simulator app built successfully (Xcode 26.8 seconds). The new framework is in `ios/UnityLibrary/UnityFramework.framework`, with the previous version backed up under `/private/tmp/harare-touch-controls/before/`. Regenerable staging/DerivedData folders were cleaned after success; source backups and logs remain. The app was built, not live gesture-tested or relaunched while the Mac was locked.

## Driving repair — September 26, 2026

The driver HUD exposes separate ACCEL, REV and BRAKE controls beside the left/right D-pad. The driving hint stays visible while seated. `TouchControlsValidation.Run` now boards the mission vehicle and verifies every driver control has a unique safe hit region, ACCEL plus LEFT produces simultaneous throttle and steering, the actual WheelCollider vehicle moves and turns, REV produces reverse input, and BRAKE reaches the vehicle controller.
