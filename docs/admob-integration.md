# Google AdMob integration

The Flutter host initializes `google_mobile_ads` on Android and iOS, runs the Google User Messaging Platform consent flow before requesting an ad, and exposes **Privacy choices** from the game menu. A 320×50 banner appears only in the pause and city menus; active Unity gameplay remains unobstructed.

The Android and iOS native projects contain their matching Harare Nights AdMob
application IDs. Ordinary development builds still use Google's official sample
banner IDs, so local testing cannot generate invalid production traffic.

The MortalKombatQuiz repository contained one Android banner unit but no matching
AdMob application ID and no iOS configuration. That unmatched unit is not used.
The ignored local `.admob.env` contains the matching Harare Nights Android and iOS
application and banner IDs created in AdMob.

## Production builds

Build Android with the matching native app and banner unit:

```sh
zsh tool/build_android_admob_release.sh
```

Build iOS after refreshing the Unity device framework:

```sh
zsh tool/prepare_ios_device.sh
zsh tool/build_ios_admob_release.sh
```

The scripts load the ignored `.admob.env`, supply the production banner unit through
a Dart define, and refuse to build when required values are missing. The iOS script
also checks that the configured application ID matches `Info.plist`.

In AdMob, create the applicable European regulations and US state regulations messages under **Privacy & messaging**. UMP reads those messages using the native application ID. Confirm the privacy entry point and requests with Ad Inspector before release.

The iOS property list contains Google's current documented SKAdNetwork identifiers. Recheck that list when upgrading the ads SDK or adding mediation partners.

Official references:

- https://developers.google.com/admob/flutter/quick-start
- https://developers.google.com/admob/flutter/privacy
- https://developers.google.com/admob/flutter/banner
- https://developers.google.com/admob/flutter/test-ads
