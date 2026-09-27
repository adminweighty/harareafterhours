# Harare After Hours mobile icons

Generated with the built-in imagegen tool, using the existing game logo as a branding reference. The original in-game logo is unchanged.

## Deliverables

- `master.png`: original generated square artwork.
- `foreground.png`: generated transparent cutout for adaptive/themed Android icons.
- `app-store-1024.png`: opaque 1024×1024 sRGB store/app icon.
- `google-play-512.png`: opaque 512×512 sRGB Play listing icon.
- iOS: all 15 unique PNG sizes referenced by `ios/Runner/Assets.xcassets/AppIcon.appiconset/Contents.json` (19 iPhone, iPad and marketing entries).
- Android: five legacy launcher densities (48/72/96/144/192px), five adaptive foreground densities (108/162/216/324/432px), API 26 adaptive XML and API 33 themed/monochrome XML. The OS applies the launcher shape and themed tint; no baked-in corner mask.

Run `swift tool/generate_app_icons.swift` from the Flutter project root to regenerate the platform PNGs on macOS. It validates dimensions, opaque iOS outputs and real foreground transparency; measures the foreground's alpha silhouette and fits it inside a 64dp circle, within Android's 66dp safe zone. No new Flutter package is required. PNGs are native resources and need no Flutter asset declaration.

## Exact generation prompts

Master (reference: `assets/branding/harare-after-hours-logo.png`):

> Use case: logo-brand. Create a new square mobile app icon for Harare After Hours, derived from the supplied image as branding reference only. A single bold sculpted ivory-and-amber capital H with dark emerald bevels, a tiny road motif through its lower central negative space, and a simplified Harare tower crown/skyline above its crossbar. Premium action-game emblem, exceptionally legible at 48 pixels, restrained amber metallic highlights, strong silhouette. Background full-bleed opaque very dark forest green (#07110F), subtle amber halo immediately behind emblem. Centered composition, emblem inside middle 72% of square, generous clear corners for platform masks. No words other than the H, no full title, no small lettering, no border, no baked rounded corners, no phone mockup. Deliver one polished 1024x1024 square app icon.

Foreground (edit target: generated master):

> Use case: background-extraction. Android adaptive launcher foreground asset. Keep this exact ivory/amber/emerald H, road and Harare tower skyline emblem, its shape, colors and proportions unchanged. Remove only the full dark background and all diffuse amber halo outside the emblem. True transparent alpha background, not a checkerboard drawing, no opaque rectangle, no ground shadow. Clean crisp cutout around the entire emblem and open negative spaces. Keep the emblem centered on the same square canvas with generous transparent outer margins. Do not add words or change the logo.

## Platform references

Verification: generated/validated 27 PNGs; iOS simulator debug build passed and the new icon was visually verified on the iPhone 16 Pro Home Screen. Android resources compiled and linked with AAPT2 against API 36 using a minimal test manifest (the app manifest normally requires Gradle substitution). This is a resource check, not an Android gameplay/runtime test.

- [Apple asset-catalog app icons](https://developer.apple.com/documentation/xcode/configuring-your-app-icon): retain the existing catalog workflow and its iPhone/iPad slots.
- [Android adaptive icons](https://developer.android.com/develop/ui/compose/system/icon_design_adaptive): separate foreground/background, safe-zone padding and monochrome support.

The native Unity splash and in-game controls are not changed by launcher icon generation. Newer optional Apple layered Icon Composer appearances are not supplied; the standard PNG catalog remains the configured app icon.
