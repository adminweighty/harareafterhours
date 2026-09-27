# First playable building/rescue slice

## Play

Walk to the signed storefront of **City Grocer**, **Sadza Kitchen**, **Big Apple** or **Private Lounge** and tap **ENTER** (keyboard E). Entry requires being outside the facade, on foot, with no active wanted level. These are the four existing venues in the original CBD; City Grocer is west of the central avenue near the garage district. Other buildings, including the Joina tower, are not newly enterable.

City Grocer has a fictional robbery encounter:

1. Enter and stop the two marked robbers using existing punch, kick or shooting controls. The HUD shows 0/2 → 2/2.
2. Approach the shopkeeper and tap RESCUE.
3. Walk back to the EXIT mat, keeping the shopkeeper close. They follow within 8 metres; if you run too far ahead, return to them.
4. Tap EXIT with the shopkeeper nearby. Gain +200 rescue points once, in addition to existing combat rewards. The rescue completion uses the existing saved award ledger and prevents replaying the reward.

You may leave before completing the rescue and try again. Existing main missions are preserved. Death/restart or other external player relocation clears the abandoned room. Quit/new sessions discard unfinished encounters but retain already saved points and rescue completion. The shopkeeper is a protected escort, not an additional damageable combat actor.

## Scope and implementation

This is an initial gameplay slice, not every building in the city or a complete dynamic-crime simulation. The rooms are compact, original modular layouts, not surveyed interiors of the named businesses; the fictional robbery does not imply an actual event at a real business. Entry uses an interaction transition to an isolated room rather than cutting holes in existing exterior meshes. Exit restores the exterior position and camera. The other three venues have exploration rooms; further venue-specific activities remain future work.

Only one room is instantiated at a time: shared cube geometry, simple box collision, three reusable room materials, emissive surfaces instead of added real-time lights, and existing character assets/animations. The two robbers use the existing health, telegraph, hit, knockdown and points systems. Room actors are removed from the director on exit. Mobile hardware profiling is still required; no FPS improvement is claimed.

## Sources applied

- [Xbox Accessibility Guideline 109: Objective clarity](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/109): persistent step-specific objectives, prerequisite counts, clear next actions and separation from the main mission.
- [Game Accessibility Guidelines: objective reminders](https://gameaccessibilityguidelines.com/indicate-allow-reminder-of-current-objectives-during-gameplay/): in-game reminders and control instructions available in the pause menu.

## Validation

Unity play-mode tests exercise exterior entry, four rooms, two robbers, defeat-gated rescue, escort distance, returning to the street, one-time persisted reward, main-mission preservation and cleanup. Flutter regression tests and static analysis cover the updated help text alongside the existing UI suite. The editor test restores the previous score record after execution.

All 31 Flutter tests passed; analysis clean. Extended Unity checks also passed actual escort movement, pause, and external-relocation cleanup. Rendered room preview: `gameplay/interiors/city-grocer-preview.png`; test report: `gameplay/interiors/validation.txt`. Unity framework and Flutter iOS simulator build succeeded and were installed without deleting simulator saves. The full rescue on a physical mobile device has not been verified.
