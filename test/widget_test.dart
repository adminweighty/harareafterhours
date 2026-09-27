import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:harare_after_hours/main.dart';
import 'package:harare_after_hours/ui/game_home.dart';
import 'package:harare_after_hours/ui/unity_game_screen.dart';

void main() {
  testWidgets('opens directly into the city host without a menu', (
    WidgetTester tester,
  ) async {
    debugDefaultTargetPlatformOverride = TargetPlatform.linux;
    addTearDown(() => debugDefaultTargetPlatformOverride = null);
    await tester.pumpWidget(const HarareAfterHoursApp());
    expect(find.byType(UnityGameplayScreen), findsOneWidget);
    expect(
      tester
          .widget<UnityGameplayScreen>(find.byType(UnityGameplayScreen))
          .stayInCity,
      isTrue,
    );
    expect(find.text('Start mission'), findsNothing);
    expect(find.text('YOUR STORY'), findsNothing);
    expect(find.text('MISSION FLOW'), findsNothing);
    expect(find.text('Character'), findsNothing);
    expect(find.text('Missions'), findsNothing);
    await tester.pumpWidget(const SizedBox.shrink());
    debugDefaultTargetPlatformOverride = null;
  });

  testWidgets('opens the Character Studio from the player profile', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(const MaterialApp(home: GameHome()));

    await tester.ensureVisible(find.text('Character'));
    await tester.tap(find.text('Character'));
    await tester.pumpAndSettle();

    expect(find.text('CHARACTER STUDIO'), findsOneWidget);
    expect(find.text('Choose a photo'), findsOneWidget);
    expect(find.text('Rename character'), findsOneWidget);
  });

  testWidgets('starts gameplay without opening the briefing', (tester) async {
    debugDefaultTargetPlatformOverride = TargetPlatform.linux;
    addTearDown(() => debugDefaultTargetPlatformOverride = null);
    await tester.pumpWidget(const MaterialApp(home: GameHome()));
    await tester.ensureVisible(find.byKey(const Key('start-mission')));
    await tester.tap(find.byKey(const Key('start-mission')));
    await tester.pumpAndSettle();
    expect(find.byType(UnityGameplayScreen), findsOneWidget);
    expect(find.text('MISSION FLOW'), findsNothing);
    await tester.pumpWidget(const SizedBox.shrink());
    debugDefaultTargetPlatformOverride = null;
  });

  testWidgets('full mission details are optional', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: GameHome()));
    await tester.ensureVisible(find.text('Details'));
    await tester.tap(find.text('Details'));
    await tester.pumpAndSettle();
    expect(find.text('MISSION FLOW'), findsOneWidget);
  });

  for (final size in [
    const Size(320, 568),
    const Size(844, 390),
    const Size(1280, 800),
  ]) {
    testWidgets('city host fits $size with large text', (tester) async {
      debugDefaultTargetPlatformOverride = TargetPlatform.linux;
      addTearDown(() => debugDefaultTargetPlatformOverride = null);
      tester.view.physicalSize = size;
      tester.view.devicePixelRatio = 1;
      tester.platformDispatcher.textScaleFactorTestValue = 1.5;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
        tester.platformDispatcher.clearTextScaleFactorTestValue();
      });
      await tester.pumpWidget(const HarareAfterHoursApp());
      await tester.pumpAndSettle();
      expect(find.byType(UnityGameplayScreen), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox.shrink());
      debugDefaultTargetPlatformOverride = null;
    });
  }
}
