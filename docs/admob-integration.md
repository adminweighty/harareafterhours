# Google AdMob integration

The Flutter host initializes `google_mobile_ads` on Android and iOS, runs the Google User Messaging Platform consent flow before requesting an ad, and exposes **Privacy choices** from the game menu. A 320×50 banner appears only in the pause and city menus; active Unity gameplay remains unobstructed.

Development builds use Google's official sample application and banner IDs. They always request test creatives and cannot earn revenue. Do not publish with these IDs.

An Android banner unit imported from the MortalKombatQuiz project is stored in the ignored local `.admob.env`. That repository did not contain its matching AdMob application ID and had no iOS AdMob configuration, so the imported unit is not enabled in ordinary development builds.

## Production IDs required

Create the Android and iOS apps in AdMob, then configure:

- Android application ID in the ignored `.admob.env`; the release script supplies it to the manifest placeholder.
- iOS application ID in `ios/Runner/Info.plist` under `GADApplicationIdentifier`.
- Banner unit IDs at build time:

```sh
flutter build appbundle \
  --dart-define=ADMOB_ANDROID_BANNER_ID=ca-app-pub-…/…

flutter build ios --release \
  --dart-define=ADMOB_IOS_BANNER_ID=ca-app-pub-…/…
```

For an Android APK, copy `.admob.env.example` to `.admob.env`, add the matching Android application ID and banner unit, then run:

```sh
zsh tool/build_android_admob_release.sh
```

The script supplies the native application ID through a Gradle manifest placeholder and the banner unit through a Dart define. It refuses to build when either value is missing, which prevents an invalid app-ID/ad-unit pairing.

In AdMob, create the applicable European regulations and US state regulations messages under **Privacy & messaging**. UMP reads those messages using the native application ID. Confirm the privacy entry point and requests with Ad Inspector before release.

The iOS property list contains Google's current documented SKAdNetwork identifiers. Recheck that list when upgrading the ads SDK or adding mediation partners.

Official references:

- https://developers.google.com/admob/flutter/quick-start
- https://developers.google.com/admob/flutter/privacy
- https://developers.google.com/admob/flutter/banner
- https://developers.google.com/admob/flutter/test-ads
