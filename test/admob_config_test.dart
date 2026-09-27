import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:harare_after_hours/services/admob_service.dart';

void main() {
  tearDown(() => debugDefaultTargetPlatformOverride = null);

  test(
    'Android uses the official Google test banner until release IDs exist',
    () {
      debugDefaultTargetPlatformOverride = TargetPlatform.android;
      expect(AdMobService.isSupported, isTrue);
      expect(AdMobService.bannerAdUnitId, AdMobService.androidTestBannerId);
    },
  );

  test('iOS uses the official Google test banner until release IDs exist', () {
    debugDefaultTargetPlatformOverride = TargetPlatform.iOS;
    expect(AdMobService.isSupported, isTrue);
    expect(AdMobService.bannerAdUnitId, AdMobService.iosTestBannerId);
  });

  test('desktop does not initialize or reserve space for AdMob', () {
    debugDefaultTargetPlatformOverride = TargetPlatform.linux;
    expect(AdMobService.isSupported, isFalse);
  });
}
