import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:harare_after_hours/ui/game_session_menu.dart';

void main() {
  for (final size in [const Size(320, 568), const Size(844, 390)]) {
    testWidgets('closed session can start again at $size', (tester) async {
      tester.view.physicalSize = size;
      tester.view.devicePixelRatio = 1;
      tester.platformDispatcher.textScaleFactorTestValue = 1.5;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
        tester.platformDispatcher.clearTextScaleFactorTestValue();
      });
      var starts = 0;
      await tester.pumpWidget(
        MaterialApp(home: GameClosedPanel(onStart: () => starts++)),
      );
      expect(find.text('Game closed'), findsOneWidget);
      expect(find.text('Continue game'), findsNothing);
      await tester.ensureVisible(find.text('Start game'));
      await tester.tap(find.text('Start game'));
      expect(starts, 1);
      expect(tester.takeException(), isNull);
    });
  }
  for (final size in [const Size(320, 568), const Size(844, 390)]) {
    testWidgets('Game Over actions fit $size with large text', (tester) async {
      tester.view.physicalSize = size;
      tester.view.devicePixelRatio = 1;
      tester.platformDispatcher.textScaleFactorTestValue = 1.5;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
        tester.platformDispatcher.clearTextScaleFactorTestValue();
      });
      int restarts = 0, menus = 0;
      await tester.pumpWidget(
        MaterialApp(
          home: GameOverPanel(
            onRestart: () => restarts++,
            onMainMenu: () => menus++,
          ),
        ),
      );
      expect(find.text('GAME OVER'), findsOneWidget);
      for (final label in ['Restart encounter', 'Main menu']) {
        await tester.ensureVisible(find.text(label));
        await tester.pumpAndSettle();
        await tester.tap(find.text(label));
      }
      expect(restarts, 1);
      expect(menus, 1);
      expect(tester.takeException(), isNull);
    });
  }
  for (final size in [const Size(320, 568), const Size(844, 390)]) {
    for (final mainMenu in [false, true]) {
      testWidgets('session menu $mainMenu accessible at $size', (tester) async {
        tester.view.physicalSize = size;
        tester.view.devicePixelRatio = 1;
        tester.platformDispatcher.textScaleFactorTestValue = 1.5;
        addTearDown(() {
          tester.view.resetPhysicalSize();
          tester.view.resetDevicePixelRatio();
          tester.platformDispatcher.clearTextScaleFactorTestValue();
        });
        final actions = <String>[];
        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              body: GameSessionMenu(
                mainMenu: mainMenu,
                ready: true,
                characterReady: true,
                onAction: actions.add,
              ),
            ),
          ),
        );
        for (final label in [
          mainMenu ? 'Continue game' : 'Resume game',
          'Change character',
          'Controls & tips',
          'Top scores',
          if (!mainMenu) 'Control settings',
          if (!mainMenu) 'Leave city',
          'Quit game',
        ]) {
          await tester.ensureVisible(find.text(label));
          await tester.tap(find.text(label));
          await tester.pumpAndSettle();
        }
        expect(actions, [
          'resume',
          'character',
          'help',
          'leaderboard',
          if (!mainMenu) 'controls',
          if (!mainMenu) 'leave',
          'quit',
        ]);
        expect(tester.takeException(), isNull);
      });
    }
  }
  testWidgets('loading profile is explained, leaving remains available', (
    tester,
  ) async {
    final actions = <String>[];
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: GameSessionMenu(
            mainMenu: false,
            ready: false,
            characterReady: false,
            onAction: actions.add,
          ),
        ),
      ),
    );
    expect(find.text('Waiting for your saved character…'), findsOneWidget);
    await tester.tap(find.text('Change character'));
    expect(actions, isEmpty);
    await tester.ensureVisible(find.text('Leave city'));
    await tester.tap(find.text('Leave city'));
    expect(actions, ['leave']);
  });

  testWidgets('game level selector emits the selected difficulty', (
    tester,
  ) async {
    final actions = <String>[];
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: GameSessionMenu(
            mainMenu: true,
            ready: true,
            characterReady: true,
            difficulty: 'Intermediate',
            onAction: actions.add,
          ),
        ),
      ),
    );
    await tester.ensureVisible(find.text('Expert'));
    await tester.tap(find.text('Expert'));
    expect(actions, ['difficulty_expert']);
  });

  testWidgets('sound deck exposes mute, shuffle and a different mix', (
    tester,
  ) async {
    final actions = <String>[];
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: GameSessionMenu(
            mainMenu: true,
            ready: true,
            characterReady: true,
            soundMuted: true,
            shuffleSoundtrack: false,
            currentTrack: 'Still dreaming',
            onAction: actions.add,
          ),
        ),
      ),
    );

    expect(find.text('NIGHTWAVE AUDIO'), findsOneWidget);
    expect(find.text('Still dreaming'), findsOneWidget);
    for (final label in ['Sound off', 'Shuffle off', 'New mix']) {
      await tester.ensureVisible(find.text(label));
      await tester.tap(find.text(label));
    }
    expect(actions, [
      'audio_toggle',
      'audio_shuffle_toggle',
      'audio_randomize',
    ]);
    expect(tester.takeException(), isNull);
  });
}
