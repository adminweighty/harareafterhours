# Physical iPhone deployment

## Latest shooter-controls update

The September 25 shooter-controls source and iPhone Unity framework have been
rebuilt. Installing this newer update is pending: host signing fails with an
Apple developer certificate-chain error (`errSecInternalComponent`), and the
iPhone was unavailable during the latest discovery check. The successful install
below refers to the earlier build, not this update. See
[shooter controls upgrade](shooter-controls-upgrade.md) for scope and test results.

## Verified on September 25, 2026

- Current Unity source exported and built successfully for `iphoneos`.
- `flutter build ios --debug --no-pub` completed with signing enabled.
- `codesign --verify --deep --strict build/ios/iphoneos/Runner.app` passed.
- The embedded Unity framework reports Mach-O platform `IOS`.
- Personal Team release build completed (347.3 MB); deep code-sign verification passed.
- Provisioning profile includes iPhone UDID `00008110-000E74320E82801E`.
- Installation on the physical iPhone succeeded using `devicectl`.
- After developer trust was completed, `devicectl` confirmed successful launch
  at 14:34 on September 25. The device became unavailable during the follow-up
  process check, so sustained runtime and gameplay validation remain pending.

Runner uses bundle ID `com.weighty.harareAfterHours` and team
`HQCMK42CYC` (Udean Mbano, Personal Team), explicitly selected by the user.
Automatic signing is explicit for Debug, Profile and Release. The
embedded Unity framework is signed by Runner's existing CodeSignOnCopy phase.

## Prepare after Unity changes or switching from Simulator

Close the Unity editor for this project, then run:

```bash
cd /Users/udeanmbano/StudioProjects/harareafterhours
zsh tool/prepare_ios_device.sh
```

The script exports current gameplay using Unity's Device SDK, builds
`UnityFramework` for `iphoneos`, checks the Mach-O platform is `IOS` (not
`IOSSIMULATOR`), and refreshes Flutter/CocoaPods dependencies. It preserves the
previous generated export in the temporary directory printed at startup. It does
not change the source game, install certificates, accept account agreements or
reset device trust. Build logs and temporary build products are retained there.
Paths can be overridden with `UNITY_EDITOR`, `UNITY_PROJECT`, and `FLUTTER_BIN`.

## Run on Udean's iPhone

```bash
cd /Users/udeanmbano/StudioProjects/harareafterhours
/Users/udeanmbano/Development/flutter/bin/flutter run \
  -d 00008110-000E74320E82801E lib/main.dart
```

Once preparation succeeds, Android Studio's normal Run action can also target
this iPhone. Do not run the simulator-preparation script before a device run: the
host currently embeds one active Unity framework, not a universal XCFramework.
To return to the simulator, use `zsh tool/run_ios_simulator.sh --prepare-only`
with Flutter on PATH; prepare the device again when switching back.

## Device/signing prerequisites

- Keep the phone unlocked and connected with a data-capable USB cable.
- Complete trust/pairing in Finder/Xcode and the prompts on the phone.
- Enable Developer Mode, restart and confirm activation on the phone.
- Open `ios/Runner.xcworkspace` in Xcode, select Runner → Signing & Capabilities
  and ensure the existing team is available under your signed-in Apple account.
- An unavailable device/pairing error cannot be fixed by a Flutter build setting.
  Do not reset privacy settings or remove pairing records as a first step.
- A signed device build needs a matching development provisioning profile. If
  Xcode asks to sign in, register the device or update a profile, complete that
  explicitly in Xcode before retrying.

For offline gameplay no backend is required. On an iPhone, `localhost` refers to
the phone, not the Mac; optional backend sync needs a reachable server address.

References: [Flutter iOS setup](https://docs.flutter.dev/platform-integration/ios/setup),
[Unity as a Library on iOS](https://docs.unity.com/en-us/engine/6000.7/manual/platform-specific/iphone/ios-developing/unityasa-library-ios).
