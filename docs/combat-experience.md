# Hostile encounters and one-hit defeat

Historical implementation note: the one-hit defeat rule below has been superseded by [life, counterattacks, punch and kick](combat-health-and-kicks.md). Current ordinary enemy hits cost 20 of 100 life, with post-hit protection; game over occurs at zero.

Reference: supplied `Harare_After_Hours_Open_World_Game_Script.md`, sections 7, 13 and 14. The user's latest one-hit Game Over request overrides the script's gradual-health failure threshold.

Implemented:

- Five hostile robbers across the original blocks, up from three; existing police and civilian systems retained.
- Nearby visible enemies carry HOSTILE labels with remaining hits and +50 reward information. Ordinary civilians and officers do not award these combat points.
- Three landed hits defeat a hostile. Existing weapon hit effects and blood-effect preference remain in use. Defeated hostile visuals fall sideways and stay down until an explicit encounter restart; this is a simple defeat pose, not a new motion-captured/ragdoll system.
- Enemy melee attacks have a 0.8-second warning. Moving out of reach avoids the strike; landing a hit interrupts it. Actual impact requires range, facing and clear line of sight. No attacks through walls.
- One valid hostile hit sets health to zero, locks controls and freezes simulation. Game Over explains the cause and offers Restart encounter or Main menu. Resume cannot bypass defeat.
- Restart restores enemies, clears pursuit and places the player at the safe corner with eight seconds to prepare. Existing mission stage, loadout and earned points remain. It does not erase the campaign or restart the entire app.
- Each named hostile awards +50 once through the existing persistent reward ledger, including across restarts. Repeat defeats explain that the reward was already collected. Practice bags retain their separate existing training rewards.

Not added in this pass: perfect-block/dodge scoring, dedicated block control, full trader-rescue mission rewards (+200/coins/reputation), reinforcement waves, new cinematic animations or ragdolls. Those script features are not represented as complete. Police arrest remains a separate existing system.

Sources:

- [Game Accessibility Guidelines: objective reminders](https://gameaccessibilityguidelines.com/indicate-allow-reminder-of-current-objectives-during-gameplay/): explicit objectives and accessible reminders.
- [Unity raycasts](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Physics.Raycast.html): geometry-aware hit/visibility checks.

Validation: deterministic combat checks passed for wind-up, movement evasion, close-wall blocking, one-hit Game Over, blocked Resume, explicit restart, grace period, reward cap and retained mission/points. The live play-mode street regression also passed police pursuit, protected police seats, capture, visible handcuffs, release/fine, escape and a real actor-driven attack causing Game Over followed by restart. Flutter Game Over layout/action tests cover 320×568 and 844×390 at 1.5× text scale.

All 20 Flutter tests passed and analysis found no issues. Unity simulator export and native UnityFramework build succeeded. These results establish functionality, not a physical-phone frame-rate or realism benchmark.

The rebuilt iPhone simulator app launched successfully (18.4 s Xcode build), visibly showing the new hostile scoring guidance. The full death/restart sequence was validated in Unity play mode and the Flutter panel independently in widget tests; it was not manually played end-to-end through simulator touch controls. App left running; only regenerable temporary native-build copies were removed.
