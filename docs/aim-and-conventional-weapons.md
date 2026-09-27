# Touch combat and conventional weapons

The [official Rise of the Tomb Raider manual](https://www.feralinteractive.com/en/manuals/riseofthetombraider/latest/steam/) separates aiming and firing. This implementation adapts that interaction for mobile rather than reproducing proprietary UI or assets:

- AIM toggles shoulder aiming without shooting. Right-side look swipes are slower while aiming.
- FIRE automatically raises aim; an automatic rifle can be aimed by dragging its firing finger beyond the button. Finger ownership is retained until release, which stops shooting. The pistol remains tap-to-fire.
- MOVE remains independent. Aiming disables automatic camera recentering and reduces movement to 65% walking speed, with the character facing the camera's aim direction while sidestepping.
- Original procedural AK-style rifle silhouette: dark steel receiver/barrel, walnut stock/fore-end, segmented curved magazine and front sight. Compact Agent pistol: dark slide/frame, grip and sights. These are modest low-poly game models, not photorealistic licensed AK-47/007 replicas or extracted Tomb Raider assets.
- Existing save/bridge IDs `Pulse rifle` and `Pulse pistol` remain for compatibility; loadout labels show AK-style rifle and Agent pistol. Existing damage, scores, recoil, wall obstruction, blood preference and cooldowns are unchanged.

Verification: 31 Flutter tests passed, analyzer clean. Unity play-mode tests passed for movement vs firing, AIM without shooting, automatic aim on FIRE, drag outside FIRE while held, release-to-stop, pointer ownership, quit and restart. Physical-device ergonomics still require testing; simulator input cannot reproduce two physical thumbs accurately.

The Unity simulator framework and Flutter iOS app were rebuilt and installed successfully. The running simulator displayed the city, AIM ON shoulder view and conventional weapon with cartridge effects. Further UI automation stopped when the user interacted with the simulator.
