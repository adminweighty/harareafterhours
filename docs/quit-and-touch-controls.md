# Quit and touch controls — September 24, 2026

## Fixes

- Quit is distinct from Leave city. Leave keeps a paused session; Quit sends `end_session`, waits for Unity's save acknowledgement, stops gameplay/audio and pauses the native engine. Flutter displays a separate **Game closed** screen. Start game resumes the engine and reloads the scene using existing saved progression/loadout, not the abandoned encounter.
- Android still dismisses its activity. iOS does not force-kill the process: use the Home gesture to leave the app. No `exit(0)` or private API is used. Saved progression is retained; exact street position is not newly persisted.
- Raw mouse firing is disabled while custom touch controls own input. A movement/camera/button click cannot also become a weapon click.
- Left MOVE controls movement; right-side swipes control camera direction. Fingers retain their initial action while dragged across other buttons. FIRE remains independent and can be held with movement for automatic weapons.
- The former VIEW button is explicitly AIM / AIM ON when carrying a firearm, and CAMERA otherwise. AIM toggles a shoulder camera without firing. Walking uses the wider following camera. The passive target appears only while aiming or briefly after firing; it is not a joystick or camera button.
- Pausing, changing equipment, entering a vehicle or closing the session clears aiming. Punch, kick, health and existing mission systems remain intact.

## Checks

- 29 Flutter tests passed; static analysis clean. New tests cover the closed screen and restart button at portrait/landscape sizes with 150% text.
- Unity `InputQuitValidation.Run` passed: joystick plus held mouse cannot fire; separate FIRE works concurrently; AIM does not fire; pointer ownership survives cross-button dragging; quit clears input/aim, pauses time/audio and acknowledges saving; late resume is ignored; restart creates a new scene instance.
- Rebuilt the native framework and ran on iPhone 16 Pro / iOS 18.6 simulator: Menu → Quit → confirmation reaches Game closed; Start game renders Joina again with 280 saved points; AIM displays AIM ON and the reticle/shoulder camera without firing, AIM off removes the reticle; movement input does not trigger a shot; Menu still opens and Stay in game cancels quit successfully. Android activity dismissal still requires an Android device/emulator check.

## Sources and application

- [Apple: programmatically quitting iOS applications](https://developer.apple.com/library/archive/qa/qa1561/_index.html): no supported graceful process-termination API; use an explicit session-ended state rather than `exit`.
- [Flutter SystemNavigator.pop](https://api.flutter.dev/flutter/services/SystemNavigator/pop.html): Android activity removal differs from iOS controller dismissal.
- [Unity input/UI FAQ](https://docs.unity3d.com/cn/2023.1/Manual/UIE-faq-event-and-input-system.html): separate gameplay input from UI-owned input. These controls use custom IMGUI rather than uGUI, so the fix guards the raw mouse path and retains pointer ownership instead of applying an unrelated EventSystem check.
- [Game Accessibility Guidelines: large, well-spaced controls](https://gameaccessibilityguidelines.com/ensure-interactive-elements-virtual-controls-are-large-and-well-spaced-particularly-on-small-or-touch-screens/): retain separated touch regions and explicit action labels.
