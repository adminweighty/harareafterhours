# Harare After Hours branded launch

The native Unity 6 splash and Unity logo are disabled in `unity_game/ProjectSettings/ProjectSettings.asset`. Company/product names are unchanged to preserve the existing save namespace. Re-export and rebuild Unity for each shipping platform; hot reload alone cannot change the native splash.

Flutter mounts the native game immediately beneath the branded loading overlay. The original logo fades and scales into place over 1.1 seconds, while loading proceeds concurrently. `world_ready` fades the overlay away in 280 ms without waiting for the entrance animation. There is no timed fake progress or mandatory Play tap. Menu remains accessible while loading. After 15 seconds a connection-check button retries the existing idempotent bridge handshake; it does not reload the world or reset saves. Reduced Motion uses a static logo and loading label. The main menu reuses the logo.

The iOS native launch background and Android legacy launch backgrounds match the dark green Flutter surface. The animated identity belongs to Flutter, not the static iOS launch storyboard. No gameplay, mission, combat, character, database or save rules were changed.

## Asset and generation

Final project asset: `assets/branding/harare-after-hours-logo.png` (1536 × 1024 RGBA PNG, approximately 1.6 MB). Generated using the built-in imagegen tool, not the CLI, with alpha preserved when copied into the project. The architectural skyline is a stylised identity, not a survey-accurate reconstruction. The live menu confirms the logo composites cleanly on the dark green surface.

### Initial prompt

Use case: logo-brand
Asset type: finished raster game logo for the existing mobile open-world game Harare After Hours.
Primary request: Design one original, premium, highly legible logo that feels like Harare's urban streets after dark. A confident custom condensed wordmark with an integrated architectural H emblem, subtle road negative space and a restrained city-roofline motif. Strong silhouette, balanced negative space, not a generic esports shield.
Text verbatim: "HARARE" on the dominant line and "AFTER HOURS" on the smaller line. Spell HARARE exactly H A R A R E. No other text.
Style: polished graphic identity, crisp edges, restrained warm-metal highlights, ivory lettering and warm peach-orange accents matching the existing game's #FF784D accent on dark green #07110F UI. Readable at phone width.
Composition: single centered complete emblem and wordmark, landscape 3:2 canvas, logo occupies most of canvas with safe padding on all sides.
Background: genuinely transparent PNG with preserved alpha, no scene, no rectangle, no checkerboard.
Constraints: original design only, no Unity logo, no GTA logo, no other brands, no watermark, no mockup, no tiny decorative text, no weapons or characters. Ready to animate using scale and opacity in Flutter.

### Final edit prompt

Edit this logo only. Preserve the excellent HARARE AFTER HOURS lettering, road-shaped H, ivory and orange materials and overall arrangement. Remove the cable-stayed bridge in the skyline because it does not represent Harare. Retain the other architectural silhouettes. Remove all background behind the logo and skyline, including the sun disc and all orange fog/glow outside the solid logo. Deliver a genuine transparent-background PNG with clean alpha edges; no solid black or checkerboard background. All letters and solid logo surfaces remain fully opaque. No new text. This is an isolated usable game logo asset, not a poster.

## Sources informing implementation

- [Unity pricing updates](https://unity.com/products/pricing-updates): Unity 6 splash screen is optional.
- [Unity splash-screen settings](https://docs.unity.com/en-us/engine/6000.7/manual/unity-editor/editor-settings-reference/comp-manager-group/class-player-settings-splash-screen): supported logo/splash settings.
- [Apple launching guidance](https://developer.apple.com/design/human-interface-guidelines/launching): keep the native launch lightweight and move into app content promptly.
- [Apple loading guidance](https://developer.apple.com/design/human-interface-guidelines/loading): communicate actual activity without inventing progress percentages.

## Checks

- Flutter analysis: no issues.
- 24 Flutter tests pass, including ready-gated reveal, slow-handshake retry, reduced-motion mode, portrait/landscape layouts and existing navigation/character tests.
- Unity simulator export and Xcode UnityFramework build succeeded. The rebuilt Flutter app starts into the city; live simulator checks verified the logo in the main menu and the pause/leave/continue path. The very short cold-start animation was covered by widget tests rather than frame-by-frame simulator recording. Physical-device and Android release testing remain separate checks.
