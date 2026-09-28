# Harare After Hours v3.1 production roadmap

This roadmap turns `Harare_After_Hours_Game_Script_v3_1` into a sequence of playable, testable deliveries. The script is the design reference. Repository behavior and validation results decide whether a task is complete.

## Delivery rules

- Execute tasks in the order below. A task advances only after its acceptance check passes.
- Extend the current Flutter host and Unity world. Flutter owns campaign presentation and persistence; Unity owns the live 3D mission.
- Keep first-completion rewards idempotent. Retrying, restoring a checkpoint, or replaying cannot pay the same first-completion reward twice.
- Every mission delivery includes mobile controls, checkpoint recovery, Unity-to-Flutter results, and an iPhone device build.
- Use development blockouts honestly where licensed final models, music, likenesses, or vehicle rights are still pending.

## Ordered execution queue

| Order | Task | Deliverable and acceptance evidence | Status |
| --- | --- | --- | --- |
| 00 | Audit v3.1 against the current build | Map the 30-mission script and acceptance gates to current Flutter/Unity systems. Identify reusable driving, combat, venue, racing, police, roster, and save components. | Complete |
| 01 | Campaign contract and M01 `City Wakes / Wrong Delivery` | Flutter selects M01 in Unity. The player meets Rudo, receives the envelope, pursues the satchel thief by the market or service route, recovers the satchel directly or with Tino, returns it to Chipo, earns one 500-coin/100-XP result, and remains in free roam. Collection and yard checkpoints survive restart. | Complete |
| 02 | M04 firearm tutorial | Add safe tutorial briefing, aim/fire/reload, finite marked threats, phone recovery, checkpoint restore, terminal encounter states, and one committed result. Validate direct and support routes on touch controls. | Queued |
| 03 | M12 burglary and crew support | Make the garage burglary a real protected-area encounter. A second crew member performs a visible support task; actor ownership, objective item state, enemy resolution, and retry all restore together. | Queued |
| 04 | M16 physical venue entry | Walk from the city into each authored venue area, load the packaged content variant, apply the public weapon policy, support presentation skip, and preserve story access. | Queued |
| 05 | M17 alarm and extraction | Build alarm, evacuation, threat clearance, passenger/cargo extraction, and all-clear transition. Driving time must change the venue outcome. | Queued |
| 06 | M13 racing | Run one repeatable circuit with grip and drift setups, readable starts/corners/recovery, fair rivals, mobile steering assistance, and measurable handling differences. | Queued |
| 07 | M29 boss and M30 finale handoff | Deliver finite boss phases, terminal resolution, extraction-to-stage handoff, assigned performer actions, late-arrival variation, ending flags, and post-campaign free roam. | Queued |
| 08 | Fill the remaining campaign in story order | Author M02–M03, M05–M11, M14–M15, and M18–M28 using the proven mission patterns. Verify all 30 base rewards total 58,500 coins and 13,875 XP. | Playable blockout complete; bespoke encounter pass queued |
| 09 | Character identity and crew pass | Put every approved character into an in-world role, keep face/outfit identity across exploration, dialogue, vehicles, combat, and performance, and prevent duplicate people, self-calls, or simultaneous conflicting assignments. Refine rigs and mobile LODs as each role becomes playable. | Queued |
| 10 | Production hardening | Profile sustained 30 FPS on named phones, audit save migration, content variant, rights manifests, audio availability, accessibility, store disclosures, regression tests, archive build, and device install. | Queued |

## Task 01 implementation checklist

- [x] Replace the obsolete test-drive opening with the v3.1 M01 state machine.
- [x] Save `definitionVersion`, checkpoint, route, optional poster, score state, and reward commit locally.
- [x] Add Rudo, the runner, Tino, Chipo, the satchel interaction, route feedback, and objective marker.
- [x] Send the selected Flutter mission into Unity after the world is ready.
- [x] Send a versioned result back to Flutter and update campaign progress once while Unity continues free roam.
- [x] Add an automated Unity M01 validation and Flutter bridge/result tests.
- [x] Install and launch the standalone release build on the registered iPhone.

## Full-campaign playable blockout — 27 September 2026

- M02–M05 now run as a bespoke Act I chain: visible binary choices, market and service routes, multi-stop driving, a finite brawler/firearm encounter, checkpoint recovery, and one-time results.
- M06–M30 now receive their authored title, district, activity, vehicle, objectives, rewards, and both story consequences from Flutter. Unity composes those contracts into a four-phase live mission with contact, travel, pressure/evidence, and final-choice gameplay instead of returning `not_implemented_yet`.
- Completing a result changes the already-open Unity session to the next unlocked mission. Replay results are clamped to zero coins and XP in Flutter even if a stale native payload reports a payout.
- `ActOneCampaignValidation` uses editor-only save/reward keys. It completed M02 in the live scene, verified its Flutter result and once-only reward, then selected M03–M30 in order and checked every authored objective contract without modifying real saves.
- Flutter analysis passed and all 43 tests passed. Unity iOS export, native `UnityFramework`, signed Flutter release build, physical-iPhone install, launch, and running-process confirmation passed.

This milestone makes the 30-mission story progression playable as a development blockout. Tasks 02–07 and 09–10 remain the acceptance gates for bespoke set pieces, character-specific staging, final art/audio, performance, accessibility, and store-ready production quality.

### Task 01 evidence — 26 September 2026

- Unity `M01CampaignValidation`: passed the full Rudo → theft → service route → recovery → Chipo flow, one-result commit, Flutter payload, and yard checkpoint restore.
- Flutter mission-result tests: 3 passed.
- Flutter static analysis: no issues.
- Unity device export and native `UnityFramework`: passed; Mach-O platform is `IOS`, minimum iOS 15.0.
- Flutter/Xcode debug device build: passed at `build/ios/iphoneos/Runner.app` for `com.weighty.harareafterhours`. Local deep signature verification still reports the existing `CSSMERR_TP_NOT_TRUSTED` certificate-chain condition.
- Physical device gate: release build installed and launched on `Udean’s iPhone` (iPhone 13 Pro Max); Xcode confirmed the `Runner` process remained active.

### M01 realism pass

- Replaced the single-cube placeholder with a layered leather satchel, flap, clasp and shoulder strap.
- The satchel now moves visibly from Rudo to the runner, then to the player, and disappears only after Chipo receives it.
- Runner movement now accelerates and slows, applies grounded gravity explicitly, turns at a bounded rate, reacts to side collisions and changes direction briefly to escape obstacles.
- Rudo, Tino and Chipo turn naturally toward a nearby player and return to their resting orientation when the player leaves.
- Farai’s optional poster now has readable in-world schedule text.
- Preserved the mobile lighting budget: no new shadow-casting lights, fullscreen effects or continuous physics bodies were added.

Implementation follows Unity’s `CharacterController.Move` contract, which requires the caller to apply gravity, and uses the existing damped Animator speed blending. The presentation keeps dynamic shadows and additional lights constrained because Unity’s URP mobile guidance identifies shadow distance, cascades, additional-light shadows and soft shadows as major costs.

## Definition of complete

A task is complete only when its gameplay can be performed in the live Unity world, its required state restores after interruption, its reward cannot duplicate, the automated checks pass, and the current iPhone build launches. Documentation, UI labels, static images, and cinematic stand-ins do not satisfy a gameplay task.
