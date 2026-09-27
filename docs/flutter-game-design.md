# Flutter game design and quit flow

`lib/ui/game_theme.dart` centralises midnight green surfaces, warm amber actions, ivory text, secondary mint accents, component shapes, contrast colours and minimum 48-point action targets. Main menu, pause navigation, character loadout, game-over screen, chips, buttons and feedback inherit these tokens. Sheets share a rounded, clipped surface, a visible drag handle and a 640-point maximum width.

Pause now has an explicit close control. Leave-city confirmation, control help and quit confirmation use a shared scrollable action-sheet component with safe-area and keyboard padding. Primary and cancellation actions remain reachable with large text and landscape layouts.

Quit is available from pause and main menu, including during loading. Cancellation does not change the session. The updated flow ends the session and stops Unity/audio, then shows a distinct Game closed screen; Start game reloads the city using saved progression. On Android, `SystemNavigator.pop()` closes the Android activity; this is not a process kill. On iOS, leaving the app itself uses the Home gesture. There is no `exit(0)`, private iOS API, forced crash or data deletion. See [quit and touch controls](quit-and-touch-controls.md) for the updated acknowledgement flow and verification.

## Design references

- [Apple sheets](https://developer.apple.com/design/human-interface-guidelines/sheets): clear dismissal and focused tasks.
- [Apple buttons](https://developer.apple.com/design/human-interface-guidelines/buttons): clear action hierarchy and prominent primary actions.
- [Flutter SystemNavigator.pop](https://api.flutter.dev/flutter/services/SystemNavigator/pop.html): platform-specific screen dismissal rather than terminating the process.

## Verification

27 Flutter tests pass, covering menu quit dispatch, confirmation/cancellation, large-text portrait/landscape sheets and the existing character/launch UI tests. Flutter static analysis is clean. iOS simulator appearance and quit navigation checked separately; Android activity dismissal still needs a device/emulator run.
