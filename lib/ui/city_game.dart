import 'package:flutter/material.dart';
import 'package:stacked/stacked.dart';

import '../viewmodels/campaign_viewmodel.dart';
import 'loadout_studio.dart';
import 'leaderboard_sheet.dart';
import 'unity_game_screen.dart';

/// Unity owns the opening, objectives and free roam. Stacked retains the
/// character profile and campaign services without putting a menu in front.
class CityGame extends StatelessWidget {
  const CityGame({super.key});

  @override
  Widget build(BuildContext context) {
    return ViewModelBuilder<CampaignViewModel>.reactive(
      viewModelBuilder: CampaignViewModel.new,
      onViewModelReady: (model) => model.initialize(),
      builder: (context, model, child) => UnityGameplayScreen(
        mission: model.nextMission,
        isReplay: model.isCompleted(model.nextMission),
        character: model.character,
        stayInCity: true,
        onMissionCompleted: model.completeMission,
        onCharacterRestored: model.restoreCharacter,
        onCustomize: () => showModalBottomSheet<void>(
          context: context,
          isScrollControlled: true,
          useSafeArea: true,
          builder: (_) => SizedBox(
            height: MediaQuery.sizeOf(context).height * .9,
            child: LoadoutStudio(model: model),
          ),
        ),
        onLeaderboard: () => showModalBottomSheet<void>(
          context: context,
          isScrollControlled: true,
          useSafeArea: true,
          builder: (_) => FractionallySizedBox(
            heightFactor: .94,
            child: LeaderboardSheet(model: model),
          ),
        ),
      ),
    );
  }
}
