import 'package:flutter_test/flutter_test.dart';
import 'package:harare_after_hours/data/campaign_data.dart';
import 'package:harare_after_hours/models/campaign_models.dart';
import 'package:harare_after_hours/ui/unity_game_screen.dart';

void main() {
  test('M01 exposes the v3.1 campaign identity and recovery objectives', () {
    final mission = kMissions.first;

    expect(mission.missionId, 'M01');
    expect(mission.definitionVersion, 31);
    expect(mission.title, contains('Wrong Delivery'));
    expect(
      mission.objectives,
      contains('Pursue the runner by the market lane or service route.'),
    );
    expect(mission.coinReward, 500);
    expect(mission.xpReward, 100);
  });

  test('Unity M01 result maps into one Flutter campaign result', () {
    final MissionResult result = missionResultFromUnityPayload(
      mission: kMissions.first,
      isReplay: false,
      payload: <String, dynamic>{
        'missionId': 'M01',
        'definitionVersion': 31,
        'score': 925,
        'stars': 3,
        'coins': 500,
        'xp': 100,
        'openingRoute': 'service',
      },
    );

    expect(result.score, 925);
    expect(result.stars, 3);
    expect(result.coinPayout, 500);
    expect(result.xpPayout, 100);
    expect(result.choiceLabel, 'Service route');
    expect(result.wasReplay, isFalse);
  });

  test('reconciled or replay result cannot invent missing rewards', () {
    final MissionResult result = missionResultFromUnityPayload(
      mission: kMissions.first,
      isReplay: true,
      payload: <String, dynamic>{
        'score': 800,
        'stars': 2,
        // Even a stale or restored Unity payload cannot repay a replay.
        'coins': 500,
        'xp': 100,
        'openingRoute': 'market',
      },
    );

    expect(result.coinPayout, 0);
    expect(result.xpPayout, 0);
    expect(result.choiceLabel, 'Market route');
    expect(result.wasReplay, isTrue);
  });

  test('authored later-mission result carries its selected consequence', () {
    final MissionResult result = missionResultFromUnityPayload(
      mission: kMissions[1],
      isReplay: false,
      payload: <String, dynamic>{
        'missionId': 'M02',
        'score': 900,
        'stars': 3,
        'coins': 600,
        'xp': 125,
        'choiceLabel': 'Return it directly',
        'crewTrust': 0,
        'communitySupport': 1,
      },
    );
    expect(result.choiceLabel, 'Return it directly');
    expect(result.impact.communitySupport, 1);
    expect(result.coinPayout, 600);
    expect(result.xpPayout, 125);
  });
}
