import 'dart:io';
import 'dart:ui' as ui;
import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:harare_after_hours/ui/game_field_guide.dart';
import 'package:harare_after_hours/ui/game_menu_visuals.dart';
import 'package:harare_after_hours/ui/game_session_menu.dart';
import 'package:harare_after_hours/ui/game_theme.dart';

void main() {
  testWidgets(
    'reduced motion settles immediately and loading actions stay disabled',
    (tester) async {
      final actions = <String>[];
      await tester.pumpWidget(
        MaterialApp(
          theme: GameTheme.dark,
          home: MediaQuery(
            data: const MediaQueryData(disableAnimations: true),
            child: Scaffold(
              body: GameSessionMenu(
                mainMenu: true,
                ready: false,
                characterReady: false,
                onAction: actions.add,
              ),
            ),
          ),
        ),
      );
      await tester.pump();
      await tester.ensureVisible(find.text('City is loading…'));
      await tester.tap(find.text('City is loading…'));
      await tester.tap(find.text('Change character'));
      expect(actions, isEmpty);
      expect(tester.binding.transientCallbackCount, 0);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('defeated main menu restarts instead of resuming a dead player', (
    tester,
  ) async {
    final actions = <String>[];
    await tester.pumpWidget(
      MaterialApp(
        theme: GameTheme.dark,
        home: Scaffold(
          body: GameSessionMenu(
            mainMenu: true,
            ready: true,
            needsRestart: true,
            characterReady: true,
            onAction: actions.add,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Continue game'), findsNothing);
    await tester.ensureVisible(find.text('Restart encounter'));
    await tester.tap(find.text('Restart encounter'));
    expect(actions, ['restart']);
  });

  for (final size in [const Size(320, 568), const Size(844, 390)]) {
    testWidgets('field guide categories and long text fit $size', (
      tester,
    ) async {
      await tester.binding.setSurfaceSize(size);
      tester.platformDispatcher.textScaleFactorTestValue = 1.5;
      addTearDown(() {
        tester.binding.setSurfaceSize(null);
        tester.platformDispatcher.clearTextScaleFactorTestValue();
      });
      await tester.pumpWidget(
        MaterialApp(
          theme: GameTheme.dark,
          home: const Scaffold(body: GameFieldGuide()),
        ),
      );
      expect(find.textContaining('Hold ▲ WALK'), findsOneWidget);
      for (final label in ['Drive', 'Survive', 'Your story', 'Move & aim']) {
        await tester.ensureVisible(find.text(label));
        await tester.tap(find.text(label));
        await tester.pumpAndSettle();
        expect(tester.takeException(), isNull);
      }
      expect(find.textContaining('toggles sprint'), findsNothing);
    });
  }

  testWidgets(
    'mission feedback shows actual payout, including no repeat payment',
    (tester) async {
      for (final payout in [0, 500]) {
        await tester.pumpWidget(
          MaterialApp(
            theme: GameTheme.dark,
            home: Scaffold(
              body: Center(
                child: SizedBox(
                  width: 300,
                  child: MissionSuccessCard(
                    score: 825,
                    stars: 3,
                    coins: payout,
                    xp: payout == 0 ? 0 : 100,
                  ),
                ),
              ),
            ),
          ),
        );
        await tester.pumpAndSettle();
        expect(find.text('825 PTS'), findsOneWidget);
        expect(find.byIcon(Icons.star_rounded), findsNWidgets(3));
        expect(
          find.textContaining(payout == 0 ? 'Result recorded' : '+500 coins'),
          findsOneWidget,
        );
        expect(tester.takeException(), isNull);
      }
    },
  );

  // Optional real-font captures for visual review, not platform-dependent goldens.
  final captureDir = Platform.environment['MENU_CAPTURE_DIR'];
  if (captureDir != null) {
    testWidgets('capture phone and tablet menu layouts', (tester) async {
      final fontPath = Platform.environment['MENU_CAPTURE_FONT'];
      if (fontPath != null) {
        final font = FontLoader('Roboto')
          ..addFont(
            Future.value(
              ByteData.sublistView(File(fontPath).readAsBytesSync()),
            ),
          );
        await font.load();
      }
      final icons = FontLoader('MaterialIcons')
        ..addFont(rootBundle.load('fonts/MaterialIcons-Regular.otf'));
      await icons.load();
      final boundaryKey = GlobalKey();
      for (final capture in <(String, Size, Widget)>[
        (
          'pause-landscape',
          const Size(844, 390),
          GameSessionMenu(
            mainMenu: false,
            ready: true,
            characterReady: true,
            onAction: (_) {},
          ),
        ),
        (
          'main-tablet',
          const Size(1100, 760),
          GameSessionMenu(
            mainMenu: true,
            ready: true,
            characterReady: true,
            onAction: (_) {},
          ),
        ),
        (
          'pause-portrait',
          const Size(390, 844),
          GameSessionMenu(
            mainMenu: false,
            ready: true,
            characterReady: true,
            onAction: (_) {},
          ),
        ),
        (
          'game-over',
          const Size(844, 390),
          GameOverPanel(onRestart: () {}, onMainMenu: () {}),
        ),
        ('guide', const Size(844, 390), const GameFieldGuide()),
      ]) {
        await tester.binding.setSurfaceSize(capture.$2);
        await tester.pumpWidget(
          MaterialApp(
            theme: GameTheme.dark,
            home: RepaintBoundary(
              key: boundaryKey,
              child: Scaffold(body: capture.$3),
            ),
          ),
        );
        await tester.pumpAndSettle();
        expect(tester.takeException(), isNull);
        await tester.runAsync(() async {
          final boundary =
              boundaryKey.currentContext!.findRenderObject()!
                  as RenderRepaintBoundary;
          final image = await boundary.toImage(pixelRatio: 2);
          final bytes = await image.toByteData(format: ui.ImageByteFormat.png);
          Directory(captureDir).createSync(recursive: true);
          File(
            '$captureDir/${capture.$1}.png',
          ).writeAsBytesSync(bytes!.buffer.asUint8List());
          image.dispose();
        });
      }
      await tester.binding.setSurfaceSize(null);
    });
  }
}
