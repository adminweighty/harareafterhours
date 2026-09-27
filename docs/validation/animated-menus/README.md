# Animated menus and gameplay feedback

The live city menu now uses a full-screen responsive layout, an illustrated street-grid backdrop, amber primary actions, and consistent action cards. Pause, return-to-game, game-over and closed-session screens share this treatment. Character loadout opens with the same short entrance motion. The game continues to launch directly into the city.

Motion includes a finite route-light sweep, fade/slide entrances, button press feedback, menu fade transitions and field-guide category transitions. System Reduce Motion removes decorative movement. Animation subtrees are cached where appropriate and decorative painting is isolated in a RepaintBoundary; there is no looping menu animation.

Gameplay-related changes:
- Updated WALK/BACK/TURN and held-sprint instructions replace obsolete analog-stick/toggle instructions.
- The categorized field guide explains aiming, obstructed/offscreen enemies, reloading, driving, recovery and missions.
- Confirmed Unity mission results produce a six-second, touch-through card with actual score, stars and payouts. Existing sequence deduplication and reward logic are preserved; this card does not award currency.
- A defeated player's main menu offers Restart encounter instead of resuming a dead character. Closing other menu flows cannot resume an ended or defeated session.

Validation: the full Flutter suite passed 40 tests including the optional visual capture case (39 normal tests). Subsequent loadout changes passed all five loadout tests; final field-guide changes passed the six menu design/capture tests. Static analysis reported no issues. Tests cover loading-state disabled actions, correct restart routing, reduced motion settling, real/zero payout text, categories at narrow and landscape sizes with 150% text, existing menu actions, reward-result parsing, character customization and startup.

Captures are rendered Flutter widgets at 390×844, 844×390 and 1100×760. Visual review used a locally available Arial font and the bundled Material icon font; installed iOS text uses the platform font. Captures are not screenshots from the physical phone. On-device frame time and native Unity end-to-end menu interaction require hands-on verification.

Sources:
- [Apple motion guidance](https://developer.apple.com/design/human-interface-guidelines/motion)
- [Flutter animation overview](https://docs.flutter.dev/ui/animations/overview)
- [Flutter rendering best practices](https://docs.flutter.dev/perf/best-practices)

Deployment: Flutter iOS release build succeeded, installed and launched on Udean’s iPhone. Apple devicectl confirmed Runner process 6320 running after launch. Build log: `/private/tmp/harare-menu-deploy.log`. This release reuses the previously validated Unity framework; no Unity asset or movement changes were made for this menu update.
