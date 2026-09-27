import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:harare_after_hours/ui/game_launch_overlay.dart';

void main() {
  testWidgets('Already-ready native world still shows launch branding', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(home: GameLaunchOverlay(ready: true, onRetry: () {})),
    );
    expect(find.byType(GameLogo), findsOneWidget);
    await tester.pumpAndSettle();
    expect(find.byType(GameLogo), findsNothing);
    await tester.pumpWidget(const SizedBox());
  });
  testWidgets('Fast city waits for branding, restarting replays splash', (
    tester,
  ) async {
    Widget screen(bool ready) => MaterialApp(
      home: GameLaunchOverlay(ready: ready, onRetry: () {}),
    );
    await tester.pumpWidget(screen(false));
    await tester.pump(const Duration(milliseconds: 100));
    await tester.pumpWidget(screen(true));
    expect(find.byType(GameLogo), findsOneWidget);
    await tester.pumpAndSettle();
    expect(find.byType(GameLogo), findsNothing);
    await tester.pumpWidget(screen(false));
    await tester.pump(const Duration(seconds: 16));
    expect(find.text('Check connection'), findsOneWidget);
    expect(find.byType(GameLogo), findsOneWidget);
    await tester.pumpWidget(const SizedBox());
  });
  for (final size in [const Size(320, 568), const Size(844, 390)]) {
    testWidgets('Logo loads and opens city only when ready at $size', (
      tester,
    ) async {
      await tester.binding.setSurfaceSize(size);
      addTearDown(() => tester.binding.setSurfaceSize(null));
      Widget launch(bool ready) => MaterialApp(
        home: Scaffold(
          body: Stack(
            fit: StackFit.expand,
            children: [
              const Text('City'),
              GameLaunchOverlay(ready: ready, onRetry: () {}),
            ],
          ),
        ),
      );
      await tester.pumpWidget(launch(false));
      await tester.pump(const Duration(seconds: 2));
      expect(find.byType(GameLogo), findsOneWidget);
      expect(find.text('Preparing your city…'), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(launch(true));
      await tester.pumpAndSettle();
      expect(find.byType(GameLogo), findsNothing);
      expect(find.text('City'), findsOneWidget);
    });
  }

  testWidgets(
    'Slow load offers a real handshake retry without fake readiness',
    (tester) async {
      var retries = 0;
      await tester.pumpWidget(
        MaterialApp(
          home: GameLaunchOverlay(ready: false, onRetry: () => retries++),
        ),
      );
      await tester.pump(const Duration(seconds: 16));
      await tester.ensureVisible(find.text('Check connection'));
      await tester.tap(find.text('Check connection'));
      expect(retries, 1);
      expect(find.text('Preparing your city…'), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    },
  );

  testWidgets('Reduced motion uses a still logo and no animated progress', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: MediaQuery(
          data: const MediaQueryData(
            disableAnimations: true,
            textScaler: TextScaler.linear(1.5),
          ),
          child: GameLaunchOverlay(ready: false, onRetry: () {}),
        ),
      ),
    );
    await tester.pump();
    expect(find.byType(LinearProgressIndicator), findsNothing);
    expect(
      tester.widget<ScaleTransition>(find.byType(ScaleTransition)).scale.value,
      1,
    );
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
  });
}
