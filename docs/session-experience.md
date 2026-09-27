# Pause, leave and character-editing experience

- Always-visible labelled **Menu** control, including while Unity loads.
- Scrollable pause menu: Resume game, Change character, Controls & tips, Graphics, Leave city. Character loading is explained rather than hiding the entry.
- Leave city asks for confirmation and opens a main menu over the retained, paused Unity session. Continue game resumes the same world; no scene rebuild, mission reset or app-process termination is used.
- Main menu also offers character editing and control instructions. This is an in-app exit from gameplay, not a force-quit button. Closing through the phone's Home/app switcher uses the game's existing saved progress on next launch; exact world position is not newly persisted by this change.
- Appearance/photo editing is promoted to the top of Change character; Apply and return is fixed at the bottom. Changes sync through the existing Unity profile bridge after the Stacked rebuild.
- System Back opens pause instead of silently disposing gameplay. Backgrounding pauses; returning requires an explicit resume when no other menu was already open.
- Controls help covers movement, automatic camera, vehicles, objectives and the avenue time trial.

Validation: all 18 Flutter tests passed, including portrait/landscape menus with large text, disabled profile-loading behaviour, and independent character equipment selection. Flutter analysis reports no issues. No Unity gameplay or backend changes were required.

Live iPhone simulator verification passed: Menu → Change character → Night shift → Apply and return visibly updated the player; Menu → Leave city → confirmation opened the main menu; editing there returned to the main menu without resuming; Continue restored the same mission (Meet Tino, 33 m) and score (120). The original Street classic outfit was restored. Going Home and reopening the app displayed Game paused as intended. A subsequent Material ancestor fix resolves Flutter's warning about hidden main-menu ink effects; all 18 tests and analysis passed again and the final build launched. Simulator is left running. Physical Android back gestures and OS-terminated session restoration were not tested.

Sources:

- [Apple in-game menus](https://developer.apple.com/design/human-interface-guidelines/menus): prioritise common actions, clear labels, readable controls and native touch interaction.
- [Apple multitasking](https://developer.apple.com/design/human-interface-guidelines/multitasking): pause active gameplay when switching apps and preserve context.
- [Flutter PopScope](https://api.flutter.dev/flutter/widgets/PopScope-class.html): intercept back navigation and handle blocked pops.
- [Flutter lifecycle observer](https://api.flutter.dev/flutter/widgets/WidgetsBindingObserver/didChangeAppLifecycleState.html): react to foreground/background changes.
