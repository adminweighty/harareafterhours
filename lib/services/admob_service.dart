import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:google_mobile_ads/google_mobile_ads.dart';

/// Owns the Google Mobile Ads and UMP consent lifecycle.
///
/// Test ad units are the default. Production builds can replace them with
/// `--dart-define=ADMOB_ANDROID_BANNER_ID=...` and
/// `--dart-define=ADMOB_IOS_BANNER_ID=...` after the matching native app IDs
/// are installed in AndroidManifest.xml and Info.plist.
class AdMobService {
  AdMobService._();

  static final AdMobService instance = AdMobService._();

  static const String androidTestBannerId =
      'ca-app-pub-3940256099942544/6300978111';
  static const String iosTestBannerId =
      'ca-app-pub-3940256099942544/2934735716';

  final ValueNotifier<bool> canRequestAds = ValueNotifier<bool>(false);
  final ValueNotifier<bool> privacyOptionsRequired = ValueNotifier<bool>(false);

  bool _started = false;
  bool _sdkInitialized = false;

  static bool get isSupported =>
      !kIsWeb &&
      (defaultTargetPlatform == TargetPlatform.android ||
          defaultTargetPlatform == TargetPlatform.iOS);

  static String get bannerAdUnitId {
    if (defaultTargetPlatform == TargetPlatform.android) {
      return const String.fromEnvironment(
        'ADMOB_ANDROID_BANNER_ID',
        defaultValue: androidTestBannerId,
      );
    }
    return const String.fromEnvironment(
      'ADMOB_IOS_BANNER_ID',
      defaultValue: iosTestBannerId,
    );
  }

  Future<void> initialize() async {
    if (!isSupported || _started) return;
    _started = true;
    final complete = Completer<void>();
    var finished = false;

    Future<void> finish() async {
      if (finished) return;
      finished = true;
      await _refreshPrivacyState();
      if (canRequestAds.value && !_sdkInitialized) {
        _sdkInitialized = true;
        await MobileAds.instance.initialize();
      }
      if (!complete.isCompleted) complete.complete();
    }

    ConsentInformation.instance.requestConsentInfoUpdate(
      ConsentRequestParameters(),
      () {
        unawaited(
          ConsentForm.loadAndShowConsentFormIfRequired((_) {
            unawaited(finish());
          }),
        );
      },
      (_) => unawaited(finish()),
    );
    await complete.future;
  }

  Future<void> showPrivacyOptions() async {
    if (!isSupported) return;
    await ConsentForm.showPrivacyOptionsForm((_) {
      unawaited(_refreshPrivacyState());
    });
  }

  Future<void> _refreshPrivacyState() async {
    canRequestAds.value = await ConsentInformation.instance.canRequestAds();
    privacyOptionsRequired.value =
        await ConsentInformation.instance
            .getPrivacyOptionsRequirementStatus() ==
        PrivacyOptionsRequirementStatus.required;
  }
}
