import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:harare_after_hours/models/player_guidance.dart';
import 'package:harare_after_hours/ui/game_session_menu.dart';
import 'package:harare_after_hours/ui/game_theme.dart';

void main() {
  test('guidance requires supported version and usable text', () {
    final payload = <String, dynamic>{
      'version': 1,
      'objective': 'Return to Chipo',
      'instruction': 'Follow the gold marker and tap RETURN.',
      'availability': 'M01 only',
    };
    expect(PlayerGuidance.parse(payload)!.objective, 'Return to Chipo');
    expect(PlayerGuidance.parse({...payload, 'version': 2}), isNull);
    expect(PlayerGuidance.parse({...payload, 'instruction': ''}), isNull);
    expect(PlayerGuidance.parse({...payload, 'instruction': 12}), isNull);
    expect(PlayerGuidance.parse({}), isNull);
  });
  for (final size in [const Size(320, 568), const Size(844, 390)]) {
    testWidgets('current next step updates in an already open menu at $size', (
      tester,
    ) async {
      await tester.binding.setSurfaceSize(size);
      tester.platformDispatcher.textScaleFactorTestValue = 1.5;
      addTearDown(() {
        tester.binding.setSurfaceSize(null);
        tester.platformDispatcher.clearTextScaleFactorTestValue();
      });
      final guide = ValueNotifier(
        const PlayerGuidance(
          objective: 'Talk to Rudo',
          instruction:
              'Follow the gold marker to Rudo. Tap TALK to accept the delivery.',
        ),
      );
      await tester.pumpWidget(
        MaterialApp(
          theme: GameTheme.dark,
          home: Scaffold(
            body: ValueListenableBuilder<PlayerGuidance>(
              valueListenable: guide,
              builder: (_, value, _) => GameSessionMenu(
                mainMenu: false,
                ready: true,
                characterReady: true,
                onAction: (_) {},
                guidance: value,
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('WHAT NEXT?'), findsOneWidget);
      expect(find.text('Talk to Rudo'), findsOneWidget);
      guide.value = const PlayerGuidance(
        objective: 'Return to EXIT · continue outside',
        instruction:
            'This is The Velvet Room lounge preview. M16–M17 are not playable yet.\n\nOutside: Return the satchel to Chipo. Follow the gold marker and tap RETURN.',
      );
      await tester.pumpAndSettle();
      expect(find.text('Talk to Rudo'), findsNothing);
      expect(
        find.textContaining('Return the satchel to Chipo'),
        findsOneWidget,
      );
      expect(find.textContaining('M02–M30 are not playable'), findsOneWidget);
      await tester.ensureVisible(find.text('Resume game'));
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox());
      guide.dispose();
    });
  }
}
