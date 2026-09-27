import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:harare_after_hours/ui/game_theme.dart';

void main() {
  testWidgets(
    'Game theme uses the logo palette and comfortable touch targets',
    (tester) async {
      final theme = GameTheme.dark;
      expect(theme.colorScheme.primary, GameTheme.amber);
      expect(theme.bottomSheetTheme.backgroundColor, GameTheme.surface);
      expect(
        theme.filledButtonTheme.style!.minimumSize!.resolve({})!.height,
        greaterThanOrEqualTo(48),
      );
    },
  );

  for (final size in [const Size(320, 568), const Size(844, 390)]) {
    testWidgets('Quit sheet can cancel and confirm at $size with large text', (
      tester,
    ) async {
      await tester.binding.setSurfaceSize(size);
      tester.platformDispatcher.textScaleFactorTestValue = 1.5;
      addTearDown(() {
        tester.binding.setSurfaceSize(null);
        tester.platformDispatcher.clearTextScaleFactorTestValue();
      });
      bool? result;
      await tester.pumpWidget(
        MaterialApp(
          theme: GameTheme.dark,
          home: Builder(
            builder: (context) => Scaffold(
              body: TextButton(
                onPressed: () async {
                  result = await showModalBottomSheet<bool>(
                    context: context,
                    isScrollControlled: true,
                    useSafeArea: true,
                    builder: (_) => const GameActionSheet(
                      title: 'Quit game?',
                      message:
                          'Your city stays paused. Use the Home gesture to close the app on iPhone.',
                      confirmLabel: 'Quit game',
                      cancelLabel: 'Stay in game',
                      destructive: true,
                    ),
                  );
                },
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      );
      for (final label in ['Stay in game', 'Quit game']) {
        await tester.tap(find.text('Open'));
        await tester.pumpAndSettle();
        await tester.ensureVisible(find.text(label));
        await tester.pumpAndSettle();
        await tester.tap(find.text(label));
        await tester.pumpAndSettle();
        expect(result, label == 'Quit game');
        expect(tester.takeException(), isNull);
      }
    });
  }
}
