# Life, counterattacks, punch and kick

This supersedes the earlier one-hit game-over rule.

## Player rules

- Start/restart with 100 life. Ordinary hostile punches cost 20 life: five separated successful hits defeat a full-health player without regeneration.
- After damage, 1.5 seconds of protection blocks additional hits from all enemies. Movement and attacks remain available; no forced player stun or control lock occurs before defeat.
- Health uses a filled bar and numeric LIFE value. During protection the HUD says HIT BACK NOW. Low life changes colour, but colour is not the only indicator.
- After 12 seconds without damage, with no active undefeated hostile within 16 metres, life recovers at 3 per second up to 100.
- Game over, paused time and input lock occur only at zero life. Restart retains earned points and mission state, restores life and preserves the existing eight-second preparation grace.
- Defeat cancels pending melee contact and clears punch/kick posing, preventing an old attack from landing after restart.
- Existing arrest, vehicle and pause protections remain intact.

## Attacks

| Action | Input | Contact delay | Reach | Enemy damage | Recovery |
|---|---|---:|---:|---:|---:|
| Punch | PUNCH / Q / right shoulder | 0.12 s | 2.25 m normally | 1 of 3 life units | 0.68 s |
| Kick | KICK / K / left shoulder | 0.22 s | 2.6 m | 2 of 3 life units | 0.95 s |

Punch and kick share recovery so alternating buttons cannot bypass it. Kicks use a bounded additive right-leg pose on the existing humanoid rig; they do not replace locomotion or require an external animation asset. Both attacks require facing/range and clear line of sight, interrupt enemy wind-up, and retain the existing visible knockdown and one-time +50 hostile reward. Kicks stagger surviving targets for 1.1 seconds; punches for 0.85 seconds. Three punches or one kick plus one punch defeat an ordinary marked hostile.

The existing weapon FIRE/HIT action remains, as do jump, ride, interact and camera controls. KICK occupies the unused lower-left action slot; FIGHT is labelled PUNCH. Driving/passenger controls are unchanged. Pause, focus changes and input locking clear queued kicks.

## Hostile identification

- Every live robber has a pulsing red chevron above their head, including robbery-room enemies created later in the mission flow.
- The marker follows the character's head, appears only within the existing encounter sight range, and never reveals an enemy through solid cover.
- Civilians and officers have no red threat marker. A robber's marker hides when they are down, preserving the existing defeat and recovery rules.
- The indicator uses one shared mesh and material, no real-time lights, and no screen-space overlay so it remains suitable for the mobile build.

## Checks

- Updated `CombatExperienceValidation`: real enemy attack leaves 80 life, immediate duplicate damage ignored, five separated hits trigger defeat, restart and save preservation, wall/movement evasion, reward protection.
- Updated `StreetActionValidation`: live robber receives a red overhead marker, it sits above the head and hides after defeat; the established pursuit and encounter checks remain.
- Extended `MeleeKnockdownValidation`: kick contact delay/pose, wall blocking, longer reach, two-unit damage, shared cooldown, kick+punch knockdown, touch hit-testing and queued-input reset; existing punch/fall/recovery checks retained.
- Both Unity validations passed, including pending-contact cancellation. Flutter: 27 tests passed; static analysis found no issues. Final Unity export, native framework and Flutter simulator builds succeeded. Visual inspection on iPhone 16 Pro confirmed the numbered LIFE bar and separate PUNCH/KICK buttons with no overlap. The simulator app was left running.
- Damage, timing and range values are initial design choices, not numbers prescribed by the references. Physical-device playtesting is still needed to tune difficulty and kick presentation across character outfits.

## Research applied

- [Game Accessibility Guidelines: large, spaced virtual controls](https://gameaccessibilityguidelines.com/ensure-interactive-elements-virtual-controls-are-large-and-well-spaced-particularly-on-small-or-touch-screens/) informed separate labelled hit targets and retaining their existing size/gaps.
- [Game Accessibility Guidelines: do not rely on colour alone](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/) informed numeric life and recovery text alongside the bar.
- [Unity 6 WaitForSeconds](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/WaitForSeconds.html) and [Time.timeScale](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-timeScale.html) informed scaled-time attack contact/recovery so pausing cannot let enemies keep damaging the player.

No downloaded third-party models or animations were added.
