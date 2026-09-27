# Player loadout

Pause → Loadout → pick equipment → Back to city. No startup menu is added.

- Weapons: Unarmed, Pulse pistol (28 m, 0.38 s cooldown), Pulse rifle (40 m,
  0.18 s cooldown), Baton (2.9 m melee, 3 integrity damage). Weapons affect the
  existing training targets. This loadout change does not add ranged damage
  to the separate street-encounter system.
- Clothes: four fabric colourways on the existing rigged suit, preserving its
  original diffuse texture. These are not four different garment meshes.
- Eyewear: None / Sunglasses. Headwear: None / Cap / Helmet / Headphones.
  Headwear and eyewear are independent; helmets are cosmetic, not armour.
- The weapon follows the hand and hides while seated; cosmetics follow the head.
- Unity persists the normalized profile under `Harare.Loadout.v1` on this
  device. Flutter waits for the saved profile rather than replacing it with
  defaults at startup. No backend dependency; no photo bytes in this save.
- Appearance and an optional local portrait remain under Appearance & photo.

## Internet sources and assets

- [Kenney Blaster Kit 2.1](https://kenney.nl/assets/blaster-kit), CC0. Two FBX
  models and the palette are included under Unity's Resources/HarareEquipment,
  with the original License.txt. Runtime materials use URP.
- [Microsoft XAG 112](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112)
  informed consistent category controls, visible selected checkmarks, and
  single-direction scrolling/reflow at larger text sizes.
- [Unity humanoid bone API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animator.GetBoneTransform.html)
  is used to attach cosmetics to the animated head and weapon to the hand.

Headwear and baton meshes are locally generated lightweight prototype geometry.

## Weapon presentation update

- Equipping a pistol/rifle brings the camera closer over the shoulder and raises
  the weapon. Humanoid IK supports the rifle with both hands; vehicle steering
  and arrested poses keep priority. Armed-player animation is not culled.
- Ranged weapons have brighter textured materials for evening readability.
- Tap FIRE for the pistol; hold FIRE for the rifle. Drag the clear middle-right
  part of the screen to aim (HUD and bottom controls are excluded).
- Small muzzle flash, muzzle-origin tracer, recoil, synthetic pulse audio and
  a center reticle with confirmed-hit feedback. No camera shake or full-screen flash.
- Pause → Loadout → Weapon feedback provides saved sound/reduced-effects choices.
  Reduced effects removes muzzle flash, impact spark, weapon recoil and sway.
- Camera aim is checked against the physical muzzle path so nearby walls block
  shots. Ranged damage now reaches robber/officer encounter actors as well as
  training beacons. Mission cast NPCs are unchanged.
- All sound and effect geometry is generated locally; no additional licensed
  third-party media. The existing Kenney model license is unchanged.
- The embedded view also accepts a single-pointer IMGUI aiming fallback when
  Input System touch deltas are unavailable, with double-processing protection.

Simulator verification shows the closer view, raised textured weapon, reticle,
HOLD FIRE control, and working feedback switches. Automated drags did not
operate either Unity aiming or the Flutter settings list in this Simulator
session, so touch aiming and held-fire gestures still require a hands-on
device/simulator check. This limitation is not treated as a passing UI test.
Sound and reduced-effects selections also survived reinstall and relaunch.
After verification, weapon sound and normal effects were restored and the
simulator was left in the city with the equipped rifle.

Research used [Unity's humanoid IK guidance](https://docs.unity.com/en-us/engine/6000.3/manual/animation-section/animation-mecanim/avatar-creationand-setup/inverse-kinematics),
[Epic's examples of muzzle, projectile and impact feedback](https://dev.epicgames.com/documentation/fortnite/star-wars-visual-effects-in-fortnite),
and [Microsoft's motion/distraction accessibility guidance](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/117).
Epic's licensed assets were not downloaded or reused.

## Character-hit feedback

This update changes Unity source and the Flutter preference bridge. The existing
installed simulator app has not been replaced in this side conversation; it
needs a fresh Unity export/framework and Flutter build to show these changes.

Verified: Flutter analysis and all 13 tests pass. Isolated Unity play-mode checks
pass actual ranged character hits, wall blocking, officer wanted consequences,
audio playback/listener, downed-hit suppression, effect pool cap, saved toggle,
immediate clearing, and reduced effects; existing loadout checks still pass.
The rendered preview was inspected. Speaker audibility on the phone/simulator
has not been verified. The cloned editor also logs its existing SearchDatabase
index exception; the gameplay checks finish successfully with exit code zero.

- Pistol/rifle sounds combine a short noise transient, low-frequency body,
  and the existing electronic tone. Local generated audio needs no download.
- Successful shots on street encounter actors create red droplet bursts,
  bone-following surface marks (12 seconds), and ground stains (18 seconds).
  Player damage also produces blood. Downed actors, blocked shots, misses,
  and training targets do not create character-hit blood.
- Three accepted shots down an encounter actor. Existing recovery, points,
  and officer wanted-level consequences remain in effect.
- Pause → Loadout → Weapon feedback → Blood effects is a saved toggle.
  Turning it off clears existing effects; reduced effects uses fewer droplets.
- A shared 72-item geometry pool bounds effect count; effects have no colliders.
  These are stylized surface marks, not skinned texture painting or dismemberment.
- Audio playback follows [Unity's PlayOneShot API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSource.PlayOneShot.html).
  Unity's [particle burst API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ParticleSystem.Emit.html)
  was reviewed; this implementation instead reuses small mesh droplets so it
  can share the existing runtime materials without a new particle shader.

## Verification

- `flutter analyze` and 13 Flutter tests: clean/pass, including large-text
  loadout layout, independent equipment selection, feedback switch interaction,
  and saved feedback preferences.
- Unity `HarareAfterHours.EditorTools.LoadoutValidation.Run` in an isolated
  disposable project: passes model import, bone attachments, simultaneous
  helmet/sunglasses, four distinct fabric tints, save/restore, bridge response,
  invalid-data handling, pistol/rifle range, cooldown, paused attack guard,
  baton/unarmed damage, and vehicle holster/fire guard.
- Expanded Unity checks also pass raised-hand IK, muzzle-origin tracer,
  confirmed-hit/flash feedback, nearby-wall blocking, mute/reduced effects,
  and input-lock protection. Front, side and shoulder views were rendered.
- Front and side render checks used to correct headgear fit and model direction.
- Simulator build/install succeeded. In-app Pause → Loadout selection of Pulse
  rifle, Street classic, Sunglasses and Helmet visibly applied in the city;
  all four selections were confirmed after a full app relaunch.
