# Street-action gameplay — first playable phase

## Play

- Three robbery zones: First Street sidewalk (7.8,18), south block (-7.8,-25),
  east block (34,7.8). There is an 18-second spawn grace period.
- On foot, a nearby robber warns, approaches and attacks. RUN escapes; FIGHT
  (Q / right shoulder / touch button) strikes a nearby opponent in front of you.
- Three hits defeat a robber. Reward: 150 points plus any points recovered.
  The robber can steal up to 40 existing points. Balance cannot go negative.
- Two ZRP-marked patrol SUVs and a foot patrol use the existing shared car and
  humanoid models. Assaulting an officer or driving into a patrol car starts a
  pursuit. Repeated offences escalate to three wanted levels.
- Officers pursue on foot; patrols follow the city road junctions. Break sight
  and stay out of detection for 12 seconds to escape. Police search the last
  known position, not an invisible player's exact live location.
- Staying within an officer's arrest range fills a three-second capture meter.
  Run or fight before it fills. At capture, visible cuffs and an arm pose appear;
  movement, combat and boarding are locked for six seconds.
- Release clears wanted, restores health and charges up to 50 points. The
  mission stage is retained and its timer pauses while cuffed. No jail-break
  minigame is implemented in this phase.
- Health regenerates after 12 damage-free seconds. Defeat returns the player to
  a safe corner with a capped 25-point recovery cost and a grace period.
- Training beacons, driving, passenger rides, character customisation and
  mission objectives remain available. Defence against a robber does not
  create a wanted level; fighting police gives no points.

## Art and performance

Police livery is an original, fictionalised white/blue ZRP design on the shared
SUV, not an exact reconstruction of a real fleet vehicle or an official crest.
No Rockstar assets, music, maps or code are imported. Siren audio is synthesized
locally. The lightbar uses simple geometry, no added real-time lights. Police
cars retain the shared model LODs. Six encounter actors are capped (including
two initially hidden patrol officers), with sensing throttled to about 7 Hz.
Handcuff/punch/flinch poses are procedural additions to the shared humanoid rig,
not motion-captured combat animation. Pursuit uses a small road-junction graph
and obstacle checks, not a complete traffic/navigation simulation.

## Internet research and design decisions

- [Rockstar's official San Andreas manual](https://media.rockstargames.com/rockstargames-newsite/img/manuals/en_us/GTA_SA_PS3_MANUAL_ENG.pdf):
  reference for distinct on-foot/vehicle controls, attack, running and world
  activities. The rules and timing above are original game-design choices.
- [ZRP Operations](https://zrp.gov.zw/?p=7200): local organisational context;
  this game does not represent actual police procedures or endorsement.
- [Unity SphereCast](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics.SphereCast.html):
  obstruction checks for patrols and encounter actors.
- [Unity CharacterController](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/charactercontroller):
  collision-based movement while preserving the existing humanoid controller.

Later ideas, not implemented: a minimap search radius, witness calls, varied
robbery objectives, block/dodge stamina, reputation-based mission rewards,
police vehicle fleet variants, and authored arrest/combat animation clips.

## Validation

`HarareAfterHours.EditorTools.StreetActionValidation.Run` tests rewards once,
robbery and recovery, protected patrol seats, wanted escalation, pursuit
movement, sight obstruction, capture/cuffing, release, and escape. The test
restores the pre-test saved score. Results: `/private/tmp/harare-street-action/`.

Verified results are copied to `docs/gameplay/street-action/`: encounter tests
pass, along with the existing vehicle rig, driving, reversing, safe-exit,
passenger ride, ambient-car takeover and delivery-mission tests. That older
vehicle test now explicitly selects its starting mission stage, returns to
Tino's car for delivery (required by the current mission), and restores saves.
Review images show the ZRP vehicle and the cuffed player pose. No physical-phone
performance benchmark has been performed.

iPhone 16 Pro simulator: Unity export and native framework build succeeded;
Flutter launched successfully and the new health/free-roam HUD was verified
in the live city. The app is left running. Removed 2.4 GB of regenerable build
staging files; prior framework backup and validation evidence are retained.
Concurrent weapon/customisation work was preserved; this phase connects
street enemies to FIGHT, while existing pulse/fire target behaviour is unchanged.
