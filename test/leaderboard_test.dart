import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:harare_after_hours/models/leaderboard.dart';
import 'package:harare_after_hours/models/player_identity.dart';
import 'package:harare_after_hours/services/campaign_sync_service.dart';
import 'package:harare_after_hours/services/player_identity_service.dart';
import 'package:harare_after_hours/ui/game_theme.dart';
import 'package:harare_after_hours/ui/leaderboard_sheet.dart';
import 'package:harare_after_hours/viewmodels/campaign_viewmodel.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  test('leaderboard parses ranking and current player', () {
    final result = LeaderboardResult.fromJson({
      'playerRank': 2,
      'entries': [
        {
          'rank': 1,
          'playerName': 'Maya',
          'score': 980,
          'xp': 100,
          'completedMissions': 1,
          'isCurrentPlayer': false,
        },
        {
          'rank': 2,
          'playerName': 'Tari',
          'score': 920,
          'xp': 100,
          'completedMissions': 1,
          'isCurrentPlayer': true,
        },
      ],
    });

    expect(result.playerRank, 2);
    expect(result.entries.first.playerName, 'Maya');
    expect(result.entries.last.isCurrentPlayer, isTrue);
  });

  test(
    'campaign service sends opaque identity when loading leaderboard',
    () async {
      late Uri requested;
      final service = CampaignSyncService(
        baseUrl: 'https://scores.example',
        client: MockClient((request) async {
          requested = request.url;
          return http.Response(jsonEncode({'entries': <Object>[]}), 200);
        }),
      );

      await service.loadLeaderboard(
        profileId: 'email_1234567890abcdef',
        limit: 20,
      );
      expect(requested.path, '/v1/leaderboard');
      expect(requested.queryParameters['profileId'], 'email_1234567890abcdef');
      expect(requested.queryParameters['limit'], '20');
      service.dispose();
    },
  );

  test('player identity model keeps email separate from public name', () {
    const identity = PlayerIdentity(
      profileId: 'email_hash',
      displayName: 'Nyasha',
      email: 'nyasha@example.com',
    );
    expect(identity.displayName, 'Nyasha');
    expect(identity.hasEmail, isTrue);
    expect(identity.profileId, isNot(contains('@')));
  });

  test('email identity persists locally as an opaque profile id', () async {
    SharedPreferences.setMockInitialValues({});
    final service = PlayerIdentityService();
    const current = PlayerIdentity(
      profileId: 'player_local',
      displayName: 'Tari',
    );
    final saved = await service.save(
      current: current,
      displayName: '  Nyasha  Moyo ',
      email: 'Nyasha@Example.com',
    );
    expect(saved.displayName, 'Nyasha Moyo');
    expect(saved.profileId, startsWith('email_'));
    expect(saved.profileId, isNot(contains('@')));

    final restored = await service.load();
    expect(restored.email, 'nyasha@example.com');
    expect(restored.profileId, saved.profileId);
  });

  testWidgets('player can publish identity and see ranked scores', (
    tester,
  ) async {
    SharedPreferences.setMockInitialValues({});
    Map<String, dynamic>? published;
    final service = CampaignSyncService(
      baseUrl: 'https://scores.example',
      client: MockClient((request) async {
        if (request.method == 'PUT') {
          published = jsonDecode(request.body) as Map<String, dynamic>;
          return http.Response(request.body, 200);
        }
        return http.Response(
          jsonEncode({
            'playerRank': 2,
            'entries': [
              {
                'rank': 1,
                'playerName': 'Maya',
                'score': 980,
                'xp': 100,
                'completedMissions': 1,
                'isCurrentPlayer': false,
              },
              {
                'rank': 2,
                'playerName': 'Nyasha',
                'score': 920,
                'xp': 100,
                'completedMissions': 1,
                'isCurrentPlayer': true,
              },
            ],
          }),
          200,
        );
      }),
    );
    final model = CampaignViewModel(syncService: service);
    addTearDown(model.dispose);
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(
      MaterialApp(
        theme: GameTheme.dark,
        home: Scaffold(body: LeaderboardSheet(model: model)),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Top game scores'), findsOneWidget);
    expect(find.text('Maya'), findsOneWidget);

    await tester.enterText(
      find.byKey(const ValueKey('leaderboard-player-name')),
      'Nyasha',
    );
    await tester.enterText(
      find.byKey(const ValueKey('leaderboard-player-email')),
      'nyasha@example.com',
    );
    await tester.ensureVisible(find.text('Save identity & publish score'));
    await tester.tap(find.text('Save identity & publish score'));
    await tester.pumpAndSettle();

    expect(published?['playerName'], 'Nyasha');
    expect(published?['profileId'], startsWith('email_'));
    expect(jsonEncode(published), isNot(contains('nyasha@example.com')));
    expect(find.textContaining('ranked #2'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
