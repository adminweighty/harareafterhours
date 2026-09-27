# City-first startup

The Flutter entry point now opens `CityGame`, a Stacked host for Unity. The
city is the first playable screen. There is no mission preview, start button,
Flutter mission header, or completion sheet in this route. Pause offers Resume
and the existing Character Studio, including local photos.

## Opening mission

The opening follows sections 3, 6, 8 and 10 of the supplied
`Harare_After_Hours_Open_World_Game_Script.md`, with the user's direct-city
startup instruction taking precedence over the script's main menu. The opening
is now at 18:30, following the requested evening setting, with warm low sunlight,
a dusk sky and illuminated street lamps.

1. Spawn on foot in evening Harare. Tino's short message introduces the garage.
2. Follow the visible marker and real distance. Talk to Tino using USE/E.
3. Receive the keys (+100 points); enter the marked car (+150 points).
4. Drive to the marked checkpoint and slow below 8 km/h. Completing the drive
   earns +750 points, or +550 after a significant collision.
5. Stay in the same city in free roam. Save the mission stage and points locally.

The test-drive checkpoint and point values are implementation choices to make
the script's driving introduction playable; they are not quoted script values.
The existing world is compact, so objective distances are measured, not the
script's illustrative fixed 300 m. The optional trader confrontation, 20-car
selection and subsequent 29 scripted missions are not implemented by this
startup change. Existing traffic, combat challenges and car interactions remain.

Unity owns the HUD and local progression. Mission rewards are idempotent across
reloads. This local Unity save is not yet reconciled with the Go campaign API.
Flutter only dismisses loading after a real `world_ready` event. Native mobile
builds use the plugin's direct message path so IL2CPP stripping cannot remove
the reflection-only callback. Android and physical iPhone exports must be
regenerated separately; the Simulator framework cannot run on a physical phone.

## UX references

- Microsoft XAG 101: readable HUD, objective text and contextual prompts:
  https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101
- Microsoft XAG 102: contrast between HUD information and a changing world:
  https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/102

Applied here as a compact high-contrast points/objective panel, safe-area-aware
placement, screen-scaled text, short contextual hints, and temporary dialogue.
This is not a claim of full XAG compliance.

## Verification

- `flutter analyze` and `flutter test`.
- `CityStartupValidation.Run` in a disposable `/private/tmp/harare-city-start.*`
  Unity project copy: first objective, key gating, conversation distance, real
  vehicle entry, braking requirement, physical checkpoint trigger, reward
  deduplication, saved points/mission state and continued free roam.
- The validation uses its own company/product identity to isolate save data.

The Unity editor source is `unity`; Flutter hot reload
does not update its embedded Unity framework.
