import 'package:flutter/material.dart';
import 'package:stacked/stacked.dart';

import '../data/campaign_data.dart';
import '../models/character_profile.dart';
import '../models/campaign_models.dart';
import '../viewmodels/campaign_viewmodel.dart';
import 'unity_game_screen.dart';
import 'mission_preview.dart';

const Color _ink = Color(0xFF07110F);
const Color _surface = Color(0xFF10201C);
const Color _raised = Color(0xFF172B25);
const Color _line = Color(0xFF284238);
const Color _muted = Color(0xFF9EAEA7);
const Color _white = Color(0xFFF3F7F2);
const TextStyle _labelStyle = TextStyle(
  color: _muted,
  fontSize: 10,
  fontWeight: FontWeight.w900,
  letterSpacing: 1.5,
);

class GameHome extends StatelessWidget {
  const GameHome({super.key});

  @override
  Widget build(BuildContext context) {
    return ViewModelBuilder<CampaignViewModel>.reactive(
      viewModelBuilder: () => CampaignViewModel(),
      builder: (BuildContext context, CampaignViewModel model, Widget? child) {
        return _GameShell(model: model);
      },
    );
  }
}

class _GameShell extends StatelessWidget {
  const _GameShell({required this.model});

  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) {
    final bool wide = MediaQuery.sizeOf(context).width >= 920;
    final List<Widget> pages = <Widget>[
      MissionPreview(
        mission: model.nextMission,
        completed: model.isCompleted(model.nextMission),
        onStart: () => _play(context, model.nextMission),
        onCharacter: () => model.selectTab(3),
        onMissions: () => model.selectTab(1),
        onDetails: () => _openBrief(context, model.nextMission),
      ),
      _Campaign(
        model: model,
        onMissionTap: (Mission mission) => _openBrief(context, mission),
      ),
      _CityMap(
        model: model,
        onMissionTap: (Mission mission) => _openBrief(context, mission),
      ),
      _Profile(model: model),
    ];

    return Scaffold(
      backgroundColor: _ink,
      body: Row(
        children: <Widget>[
          if (wide) _SideNav(model: model),
          Expanded(
            child: SafeArea(
              child: Column(
                children: <Widget>[
                  if (model.selectedTab != 0)
                    _TopBar(model: model, showBrand: !wide),
                  Expanded(
                    child: IndexedStack(
                      index: model.selectedTab,
                      children: pages,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
      bottomNavigationBar: wide ? null : _BottomNav(model: model),
    );
  }

  Future<void> _openBrief(BuildContext context, Mission mission) async {
    if (!model.isUnlocked(mission)) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Complete Level ${mission.level - 1} to unlock this mission.',
          ),
        ),
      );
      return;
    }

    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (BuildContext sheetContext) {
        return _MissionBrief(
          mission: mission,
          completed: model.isCompleted(mission),
          bestScore: model.bestScore(mission),
          onStart: () {
            Navigator.of(sheetContext).pop();
            _play(context, mission);
          },
        );
      },
    );
  }

  Future<void> _play(BuildContext context, Mission mission) async {
    await Navigator.of(context).push<void>(
      MaterialPageRoute<void>(
        builder: (BuildContext context) => UnityGameplayScreen(
          mission: mission,
          isReplay: model.isCompleted(mission),
          character: model.character,
          onMissionCompleted: (completedMission, result) {
            model.completeMission(completedMission, result);
          },
        ),
      ),
    );
    // Unity commits the result through onMissionCompleted and keeps the player
    // in free roam; leaving the city no longer carries the mission result.
    if (!context.mounted) return;
  }
}

class _SideNav extends StatelessWidget {
  const _SideNav({required this.model});

  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) {
    const List<(IconData, String)> items = <(IconData, String)>[
      (Icons.nightlight_round, 'Tonight'),
      (Icons.auto_stories_outlined, 'Campaign'),
      (Icons.map_outlined, 'City map'),
      (Icons.person_outline, 'Profile'),
    ];
    return Container(
      width: 232 * MediaQuery.textScalerOf(context).scale(1),
      decoration: const BoxDecoration(
        color: Color(0xFF0A1714),
        border: Border(right: BorderSide(color: _line)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          const Padding(
            padding: EdgeInsets.fromLTRB(23, 28, 22, 21),
            child: _Brand(),
          ),
          const Divider(height: 1, color: _line),
          const SizedBox(height: 15),
          for (int index = 0; index < items.length; index++)
            _NavItem(
              icon: items[index].$1,
              label: items[index].$2,
              active: model.selectedTab == index,
              onTap: () => model.selectTab(index),
            ),
          const Spacer(),
          const Padding(
            padding: EdgeInsets.all(18),
            child: _OfflineBadge(large: true),
          ),
        ],
      ),
    );
  }
}

class _NavItem extends StatelessWidget {
  const _NavItem({
    required this.icon,
    required this.label,
    required this.active,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 3),
      child: Material(
        color: active ? const Color(0xFF1B332A) : Colors.transparent,
        borderRadius: BorderRadius.circular(14),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(14),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
            child: Row(
              children: <Widget>[
                Icon(icon, color: active ? kLime : _muted),
                const SizedBox(width: 12),
                Text(
                  label,
                  style: TextStyle(
                    color: active ? _white : _muted,
                    fontWeight: active ? FontWeight.w900 : FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _BottomNav extends StatelessWidget {
  const _BottomNav({required this.model});

  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) {
    return NavigationBar(
      selectedIndex: model.selectedTab,
      onDestinationSelected: model.selectTab,
      height: 68,
      backgroundColor: const Color(0xFF0A1714),
      indicatorColor: const Color(0xFF1C342B),
      destinations: const <NavigationDestination>[
        NavigationDestination(
          icon: Icon(Icons.nightlight_round),
          label: 'Tonight',
        ),
        NavigationDestination(
          icon: Icon(Icons.auto_stories_outlined),
          label: 'Campaign',
        ),
        NavigationDestination(icon: Icon(Icons.map_outlined), label: 'City'),
        NavigationDestination(
          icon: Icon(Icons.person_outline),
          label: 'Profile',
        ),
      ],
    );
  }
}

class _TopBar extends StatelessWidget {
  const _TopBar({required this.model, required this.showBrand});

  final CampaignViewModel model;
  final bool showBrand;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(18, 12, 18, 10),
      child: Row(
        children: <Widget>[
          if (showBrand) const _Brand(compact: true),
          if (showBrand) const Spacer(),
          const _OfflineBadge(),
          const SizedBox(width: 10),
          _WalletPill(value: model.wallet),
          const SizedBox(width: 10),
          InkWell(
            onTap: () => model.selectTab(3),
            borderRadius: BorderRadius.circular(99),
            child: _CharacterPortrait(character: model.character, size: 38),
          ),
        ],
      ),
    );
  }
}

class _Brand extends StatelessWidget {
  const _Brand({this.compact = false});

  final bool compact;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        Container(
          width: compact ? 34 : 40,
          height: compact ? 34 : 40,
          decoration: BoxDecoration(
            color: kSunset,
            borderRadius: BorderRadius.circular(11),
          ),
          child: const Icon(Icons.nightlight_round, color: _ink),
        ),
        const SizedBox(width: 9),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            Text(
              'HARARE',
              style: TextStyle(
                fontSize: compact ? 12 : 14,
                fontWeight: FontWeight.w900,
                letterSpacing: 1.8,
              ),
            ),
            Text(
              'AFTER HOURS',
              style: TextStyle(
                color: kLime,
                fontSize: compact ? 9 : 10,
                fontWeight: FontWeight.w900,
                letterSpacing: 1.6,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _OfflineBadge extends StatelessWidget {
  const _OfflineBadge({this.large = false});

  final bool large;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: large ? 12 : 9, vertical: 7),
      decoration: BoxDecoration(
        color: const Color(0xFF162C24),
        border: Border.all(color: const Color(0xFF2A503F)),
        borderRadius: BorderRadius.circular(99),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          const Icon(Icons.cloud_done_outlined, color: kLime, size: 15),
          const SizedBox(width: 5),
          Flexible(
            child: Text(
              'OFFLINE',
              style: const TextStyle(
                color: kLime,
                fontSize: 10,
                fontWeight: FontWeight.w900,
                letterSpacing: .9,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _WalletPill extends StatelessWidget {
  const _WalletPill({required this.value});

  final int value;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: _surface,
        border: Border.all(color: _line),
        borderRadius: BorderRadius.circular(99),
      ),
      child: Row(
        children: <Widget>[
          const Icon(Icons.toll_outlined, color: kGold, size: 16),
          const SizedBox(width: 5),
          Text('$value', style: const TextStyle(fontWeight: FontWeight.w900)),
        ],
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.eyebrow, required this.title});

  final String eyebrow;
  final String title;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: <Widget>[
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Text(eyebrow, style: _labelStyle),
              const SizedBox(height: 4),
              Text(
                title,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _Campaign extends StatelessWidget {
  const _Campaign({required this.model, required this.onMissionTap});

  final CampaignViewModel model;
  final ValueChanged<Mission> onMissionTap;

  @override
  Widget build(BuildContext context) {
    const List<String> chapters = <String>[
      'Finding your feet',
      'Making connections',
      'Building a name',
      'After dark',
      'Your own place',
      'The big night',
    ];
    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 14, 18, 32),
      children: <Widget>[
        const Text('CAMPAIGN', style: _labelStyle),
        const SizedBox(height: 5),
        const Text(
          'A city worth returning to.',
          style: TextStyle(
            fontSize: 27,
            height: 1.1,
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 8),
        const Text(
          '30 authored missions · 6 connected districts · offline story progression',
          style: TextStyle(color: _muted, height: 1.4),
        ),
        const SizedBox(height: 20),
        _CampaignProgress(model: model),
        const SizedBox(height: 26),
        for (final String chapter in chapters) ...<Widget>[
          Text(
            chapter.toUpperCase(),
            style: const TextStyle(
              color: kLime,
              fontSize: 11,
              fontWeight: FontWeight.w900,
              letterSpacing: 1.3,
            ),
          ),
          const SizedBox(height: 10),
          for (final Mission mission in kMissions.where(
            (Mission m) => m.chapter == chapter,
          ))
            Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: _MissionTile(
                mission: mission,
                unlocked: model.isUnlocked(mission),
                completed: model.isCompleted(mission),
                score: model.bestScore(mission),
                onTap: () => onMissionTap(mission),
              ),
            ),
          const SizedBox(height: 14),
        ],
      ],
    );
  }
}

class _CampaignProgress extends StatelessWidget {
  const _CampaignProgress({required this.model});

  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: _line),
      ),
      child: Column(
        children: <Widget>[
          Row(
            children: <Widget>[
              const Text(
                'FESTIVAL ROAD',
                style: TextStyle(fontWeight: FontWeight.w900),
              ),
              const Spacer(),
              Text(
                '${model.completedCount} of 30',
                style: const TextStyle(
                  color: kLime,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ClipRRect(
            borderRadius: BorderRadius.circular(99),
            child: LinearProgressIndicator(
              value: model.completedCount / kMissions.length,
              minHeight: 9,
              backgroundColor: const Color(0xFF213A32),
              valueColor: const AlwaysStoppedAnimation<Color>(kLime),
            ),
          ),
          const SizedBox(height: 9),
          const Text(
            'Campaign missions never depend on purchases, watched ads, or a paid retry.',
            style: TextStyle(color: _muted, fontSize: 12),
          ),
        ],
      ),
    );
  }
}

class _MissionTile extends StatelessWidget {
  const _MissionTile({
    required this.mission,
    required this.unlocked,
    required this.completed,
    required this.score,
    required this.onTap,
  });

  final Mission mission;
  final bool unlocked;
  final bool completed;
  final int score;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final Color color = unlocked ? mission.accent : const Color(0xFF50645B);
    return Opacity(
      opacity: unlocked ? 1 : .55,
      child: Material(
        color: _surface,
        borderRadius: BorderRadius.circular(17),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(17),
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              children: <Widget>[
                Container(
                  width: 45,
                  height: 45,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: .15),
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: unlocked
                      ? Text(
                          mission.level.toString().padLeft(2, '0'),
                          style: TextStyle(
                            color: color,
                            fontWeight: FontWeight.w900,
                          ),
                        )
                      : const Icon(Icons.lock_outline, color: _muted, size: 19),
                ),
                const SizedBox(width: 13),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: <Widget>[
                      Text(
                        mission.title,
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      const SizedBox(height: 3),
                      Text(
                        unlocked
                            ? '${mission.district} · ${mission.activity}'
                            : 'Complete Level ${mission.level - 1} to unlock',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(color: _muted, fontSize: 12),
                      ),
                    ],
                  ),
                ),
                if (completed)
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: <Widget>[
                      const Icon(Icons.check_circle, color: kLime, size: 18),
                      const SizedBox(height: 4),
                      _Stars(score: score, size: 12),
                    ],
                  )
                else if (unlocked)
                  Icon(Icons.arrow_forward_ios_rounded, color: color, size: 16),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _CityMap extends StatelessWidget {
  const _CityMap({required this.model, required this.onMissionTap});

  final CampaignViewModel model;
  final ValueChanged<Mission> onMissionTap;

  @override
  Widget build(BuildContext context) {
    final District selected = kDistricts[model.selectedDistrict];
    final Mission? route = _nextDistrictRoute(model, selected);
    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 14, 18, 32),
      children: <Widget>[
        const Text('CITY MAP', style: _labelStyle),
        const SizedBox(height: 5),
        const Text(
          'Choose the route.\nKeep the city connected.',
          style: TextStyle(
            fontSize: 27,
            height: 1.1,
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 20),
        LayoutBuilder(
          builder: (BuildContext context, BoxConstraints constraints) {
            final double width = constraints.maxWidth > 680
                ? (constraints.maxWidth - 12) / 2
                : constraints.maxWidth;
            return Wrap(
              spacing: 12,
              runSpacing: 12,
              children: List<Widget>.generate(kDistricts.length, (int index) {
                final District district = kDistricts[index];
                return SizedBox(
                  width: width,
                  child: _DistrictMapTile(
                    district: district,
                    selected: index == model.selectedDistrict,
                    completed: district.levels
                        .where(model.completedLevels.contains)
                        .length,
                    onTap: () => model.selectDistrict(index),
                  ),
                );
              }),
            );
          },
        ),
        const SizedBox(height: 24),
        _DistrictDetail(
          district: selected,
          model: model,
          route: route,
          onMissionTap: onMissionTap,
        ),
      ],
    );
  }
}

Mission? _nextDistrictRoute(CampaignViewModel model, District district) {
  for (final int level in district.levels) {
    final Mission mission = kMissions[level - 1];
    if (model.isUnlocked(mission) && !model.isCompleted(mission)) {
      return mission;
    }
  }
  for (final int level in district.levels) {
    final Mission mission = kMissions[level - 1];
    if (model.isUnlocked(mission)) {
      return mission;
    }
  }
  return null;
}

class _DistrictMapTile extends StatelessWidget {
  const _DistrictMapTile({
    required this.district,
    required this.selected,
    required this.completed,
    required this.onTap,
  });

  final District district;
  final bool selected;
  final int completed;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? district.accent.withValues(alpha: .14) : _surface,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(18),
        child: Container(
          height: 134,
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(18),
            border: Border.all(
              color: selected ? district.accent.withValues(alpha: .7) : _line,
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Row(
                children: <Widget>[
                  Container(
                    width: 10,
                    height: 10,
                    decoration: BoxDecoration(
                      color: district.accent,
                      shape: BoxShape.circle,
                    ),
                  ),
                  const Spacer(),
                  Text(
                    '$completed / 5',
                    style: TextStyle(
                      color: district.accent,
                      fontWeight: FontWeight.w900,
                      fontSize: 12,
                    ),
                  ),
                ],
              ),
              const Spacer(),
              Text(
                district.name,
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                district.tagline,
                style: const TextStyle(color: _muted, fontSize: 12),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _DistrictDetail extends StatelessWidget {
  const _DistrictDetail({
    required this.district,
    required this.model,
    required this.route,
    required this.onMissionTap,
  });

  final District district;
  final CampaignViewModel model;
  final Mission? route;
  final ValueChanged<Mission> onMissionTap;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: _raised,
        borderRadius: BorderRadius.circular(21),
        border: Border.all(color: district.accent.withValues(alpha: .5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          _Pill(label: district.name.toUpperCase(), color: district.accent),
          const SizedBox(height: 13),
          Text(
            district.description,
            style: const TextStyle(
              fontSize: 17,
              height: 1.35,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 16),
          Wrap(
            spacing: 7,
            runSpacing: 7,
            children: district.levels.map((int level) {
              final Mission mission = kMissions[level - 1];
              final bool completed = model.isCompleted(mission);
              final bool unlocked = model.isUnlocked(mission);
              return Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 7,
                ),
                decoration: BoxDecoration(
                  color: completed
                      ? kLime.withValues(alpha: .15)
                      : unlocked
                      ? district.accent.withValues(alpha: .13)
                      : _ink,
                  border: Border.all(
                    color: completed ? kLime.withValues(alpha: .5) : _line,
                  ),
                  borderRadius: BorderRadius.circular(99),
                ),
                child: Text(
                  'L${level.toString().padLeft(2, '0')}',
                  style: TextStyle(
                    color: completed
                        ? kLime
                        : unlocked
                        ? _white
                        : _muted,
                    fontSize: 11,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              );
            }).toList(),
          ),
          if (route != null) ...<Widget>[
            const SizedBox(height: 20),
            const Text('CURRENT ROUTE', style: _labelStyle),
            const SizedBox(height: 5),
            Text(
              'L${route!.level.toString().padLeft(2, '0')} · ${route!.title}',
              style: const TextStyle(fontSize: 19, fontWeight: FontWeight.w900),
            ),
            const SizedBox(height: 11),
            FilledButton.icon(
              onPressed: () => onMissionTap(route!),
              icon: const Icon(Icons.arrow_forward),
              label: const Text('Open route'),
              style: FilledButton.styleFrom(
                backgroundColor: district.accent,
                foregroundColor: _ink,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class CharacterStudio extends StatelessWidget {
  const CharacterStudio({super.key, required this.model});

  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: model,
    builder: (context, child) => Scaffold(
      appBar: AppBar(title: const Text('Character')),
      body: ListView(
        padding: const EdgeInsets.all(18),
        children: <Widget>[_CharacterStudio(model: model)],
      ),
    ),
  );
}

class _Profile extends StatelessWidget {
  const _Profile({required this.model});

  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 14, 18, 32),
      children: <Widget>[
        const Text('PLAYER PROFILE', style: _labelStyle),
        const SizedBox(height: 14),
        _ProfileHero(model: model),
        const SizedBox(height: 22),
        const _SectionHeader(
          eyebrow: 'CHARACTER STUDIO',
          title: 'Make the night yours.',
        ),
        const SizedBox(height: 12),
        _CharacterStudio(model: model),
        const SizedBox(height: 22),
        const _SectionHeader(
          eyebrow: 'STORY STATE',
          title: 'Choices leave a trace.',
        ),
        const SizedBox(height: 12),
        _StoryState(model: model),
        const SizedBox(height: 22),
        const _SectionHeader(
          eyebrow: 'MUSIC DIRECTOR',
          title: 'Scene-aware cue plan',
        ),
        const SizedBox(height: 12),
        const _MusicPlan(),
        const SizedBox(height: 22),
        const _SectionHeader(
          eyebrow: 'CAMPAIGN SYNC',
          title: 'Keep the story when you switch devices.',
        ),
        const SizedBox(height: 12),
        _CloudSyncPanel(model: model),
      ],
    );
  }
}

class _ProfileHero extends StatelessWidget {
  const _ProfileHero({required this.model});

  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: <Color>[Color(0xFF1B392F), Color(0xFF10211D)],
        ),
        borderRadius: BorderRadius.circular(21),
        border: Border.all(color: const Color(0xFF315B48)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Row(
            children: <Widget>[
              _CharacterPortrait(character: model.character, size: 56),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      model.character.name.toUpperCase(),
                      style: const TextStyle(
                        fontSize: 22,
                        fontWeight: FontWeight.w900,
                        letterSpacing: 1.2,
                      ),
                    ),
                    Text(
                      model.endingLabel,
                      style: const TextStyle(
                        color: kLime,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ],
                ),
              ),
              _WalletPill(value: model.wallet),
            ],
          ),
          const SizedBox(height: 22),
          Row(
            children: <Widget>[
              Expanded(
                child: _ProfileMetric(
                  label: 'XP',
                  value: '${model.xp}',
                  color: kBlue,
                ),
              ),
              Expanded(
                child: _ProfileMetric(
                  label: 'MISSIONS',
                  value: '${model.completedCount}/30',
                  color: kLime,
                ),
              ),
              Expanded(
                child: _ProfileMetric(
                  label: 'PROGRESS',
                  value: '${model.campaignPercent}%',
                  color: kGold,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _CharacterStudio extends StatelessWidget {
  const _CharacterStudio({required this.model});

  static const List<_CharacterOption> _skinTones = <_CharacterOption>[
    _CharacterOption('Deep', Color(0xFF4D2B20)),
    _CharacterOption('Rich', Color(0xFF70402A)),
    _CharacterOption('Warm', Color(0xFF9D6347)),
    _CharacterOption('Golden', Color(0xFFC98E63)),
    _CharacterOption('Cool', Color(0xFF8D5B53)),
  ];

  static const List<_CharacterOption> _hairStyles = <_CharacterOption>[
    _CharacterOption('Close cut'),
    _CharacterOption('Braids'),
    _CharacterOption('Locs'),
    _CharacterOption('Curls'),
    _CharacterOption('Headwrap'),
  ];

  static const List<_CharacterOption> _outfits = <_CharacterOption>[
    _CharacterOption('Delivery fit'),
    _CharacterOption('Street classic'),
    _CharacterOption('Night shift'),
    _CharacterOption('Festival ready'),
  ];

  static const List<_CharacterOption> _accessories = <_CharacterOption>[
    _CharacterOption('No accessory'),
    _CharacterOption('Cap'),
    _CharacterOption('Glasses'),
    _CharacterOption('Headphones'),
  ];

  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) {
    final CharacterProfile character = model.character;

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: _line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              _CharacterPortrait(character: character, size: 92),
              const SizedBox(width: 15),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      character.name.toUpperCase(),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontSize: 19,
                        fontWeight: FontWeight.w900,
                        letterSpacing: .8,
                      ),
                    ),
                    const SizedBox(height: 4),
                    const Text(
                      'Your character, your version of Harare after hours.',
                      style: TextStyle(
                        color: _muted,
                        fontSize: 12,
                        height: 1.35,
                      ),
                    ),
                    const SizedBox(height: 7),
                    TextButton.icon(
                      onPressed: () => _renameCharacter(context),
                      icon: const Icon(Icons.edit_outlined, size: 16),
                      label: const Text('Rename character'),
                      style: TextButton.styleFrom(
                        foregroundColor: kLime,
                        padding: const EdgeInsets.symmetric(
                          horizontal: 0,
                          vertical: 5,
                        ),
                        textStyle: const TextStyle(fontWeight: FontWeight.w900),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 18),
          const Divider(color: _line),
          const SizedBox(height: 15),
          _CharacterOptionGroup(
            label: 'SKIN TONE',
            options: _skinTones,
            selected: character.skinTone,
            onSelected: model.updateSkinTone,
          ),
          const SizedBox(height: 15),
          _CharacterOptionGroup(
            label: 'HAIR',
            options: _hairStyles,
            selected: character.hairStyle,
            onSelected: model.updateHairStyle,
          ),
          const SizedBox(height: 15),
          _CharacterOptionGroup(
            label: 'OUTFIT',
            options: _outfits,
            selected: character.outfit,
            onSelected: model.updateOutfit,
          ),
          const SizedBox(height: 15),
          _CharacterOptionGroup(
            label: 'ACCESSORY',
            options: _accessories,
            selected: character.accessory,
            onSelected: model.updateAccessory,
          ),
          const SizedBox(height: 19),
          Wrap(
            spacing: 10,
            runSpacing: 9,
            children: <Widget>[
              FilledButton.icon(
                onPressed: model.isPickingCharacterPhoto
                    ? null
                    : () {
                        model.chooseCharacterPhoto();
                      },
                icon: model.isPickingCharacterPhoto
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: _ink,
                        ),
                      )
                    : const Icon(Icons.add_a_photo_outlined),
                label: Text(
                  character.hasPortrait ? 'Replace photo' : 'Choose a photo',
                ),
                style: FilledButton.styleFrom(
                  backgroundColor: kLime,
                  foregroundColor: _ink,
                  textStyle: const TextStyle(fontWeight: FontWeight.w900),
                ),
              ),
              if (character.hasPortrait)
                OutlinedButton.icon(
                  onPressed: model.clearCharacterPhoto,
                  icon: const Icon(Icons.delete_outline),
                  label: const Text('Remove photo'),
                  style: OutlinedButton.styleFrom(foregroundColor: _white),
                ),
            ],
          ),
          const SizedBox(height: 13),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              const Icon(Icons.privacy_tip_outlined, color: kGold, size: 18),
              const SizedBox(width: 9),
              Expanded(
                child: Text(
                  model.characterPhotoMessage,
                  style: const TextStyle(
                    color: _muted,
                    fontSize: 11,
                    height: 1.35,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Future<void> _renameCharacter(BuildContext context) async {
    final TextEditingController controller = TextEditingController(
      text: model.character.name,
    );
    final String? name = await showDialog<String>(
      context: context,
      builder: (BuildContext dialogContext) {
        return AlertDialog(
          title: const Text('Name your character'),
          content: TextField(
            controller: controller,
            autofocus: true,
            textCapitalization: TextCapitalization.words,
            maxLength: 24,
            decoration: const InputDecoration(
              hintText: 'Character name',
              counterText: '',
            ),
            onSubmitted: (String value) {
              Navigator.of(dialogContext).pop(value);
            },
          ),
          actions: <Widget>[
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () => Navigator.of(dialogContext).pop(controller.text),
              child: const Text('Save'),
            ),
          ],
        );
      },
    );
    controller.dispose();
    if (name != null) model.updateCharacterName(name);
  }
}

class _CharacterOptionGroup extends StatelessWidget {
  const _CharacterOptionGroup({
    required this.label,
    required this.options,
    required this.selected,
    required this.onSelected,
  });

  final String label;
  final List<_CharacterOption> options;
  final String selected;
  final ValueChanged<String> onSelected;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Text(label, style: _labelStyle),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: options
              .map(
                (_CharacterOption option) => ChoiceChip(
                  label: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: <Widget>[
                      if (option.color != null) ...<Widget>[
                        Container(
                          width: 11,
                          height: 11,
                          decoration: BoxDecoration(
                            color: option.color,
                            shape: BoxShape.circle,
                            border: Border.all(
                              color: _white.withValues(alpha: .35),
                            ),
                          ),
                        ),
                        const SizedBox(width: 6),
                      ],
                      Text(option.label),
                    ],
                  ),
                  selected: selected == option.label,
                  showCheckmark: false,
                  onSelected: (bool isSelected) {
                    if (isSelected) onSelected(option.label);
                  },
                  backgroundColor: _raised,
                  selectedColor: kLime.withValues(alpha: .2),
                  side: BorderSide(
                    color: selected == option.label
                        ? kLime.withValues(alpha: .72)
                        : _line,
                  ),
                  labelStyle: TextStyle(
                    color: selected == option.label ? kLime : _white,
                    fontSize: 11,
                    fontWeight: FontWeight.w800,
                  ),
                  visualDensity: VisualDensity.compact,
                ),
              )
              .toList(),
        ),
      ],
    );
  }
}

class _CharacterOption {
  const _CharacterOption(this.label, [this.color]);

  final String label;
  final Color? color;
}

class _CharacterPortrait extends StatelessWidget {
  const _CharacterPortrait({required this.character, required this.size});

  final CharacterProfile character;
  final double size;

  @override
  Widget build(BuildContext context) {
    final bool hasPortrait = character.hasPortrait;
    return Semantics(
      image: true,
      label: hasPortrait
          ? 'Custom portrait for ${character.name}'
          : 'Illustrated portrait for ${character.name}',
      child: SizedBox(
        width: size,
        height: size,
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: _raised,
            shape: BoxShape.circle,
            border: Border.all(color: kSunset.withValues(alpha: .75), width: 2),
          ),
          child: Padding(
            padding: const EdgeInsets.all(2),
            child: ClipOval(
              child: hasPortrait
                  ? Image.memory(
                      character.portraitBytes!,
                      fit: BoxFit.cover,
                      errorBuilder: (_, _, _) => _illustratedPortrait(),
                    )
                  : _illustratedPortrait(),
            ),
          ),
        ),
      ),
    );
  }

  Widget _illustratedPortrait() {
    final Color hairColor = switch (character.hairStyle) {
      'Braids' => const Color(0xFF21150F),
      'Locs' => const Color(0xFF291A12),
      'Curls' => const Color(0xFF342018),
      'Headwrap' => kViolet,
      _ => const Color(0xFF1C1513),
    };
    final Color outfitColor = switch (character.outfit) {
      'Street classic' => kViolet,
      'Night shift' => kBlue,
      'Festival ready' => kGold,
      _ => kSunset,
    };
    final double hairHeight = switch (character.hairStyle) {
      'Close cut' => size * .23,
      'Braids' => size * .34,
      'Locs' => size * .4,
      'Curls' => size * .36,
      'Headwrap' => size * .42,
      _ => size * .28,
    };
    final IconData? accessoryIcon = switch (character.accessory) {
      'Cap' => Icons.sports_baseball_outlined,
      'Glasses' => Icons.visibility_outlined,
      'Headphones' => Icons.headphones,
      _ => null,
    };

    return Stack(
      fit: StackFit.expand,
      children: <Widget>[
        ColoredBox(color: _skinToneColor(character.skinTone)),
        Positioned(
          top: 0,
          left: size * .08,
          right: size * .08,
          height: hairHeight,
          child: DecoratedBox(
            decoration: BoxDecoration(
              color: hairColor,
              borderRadius: BorderRadius.vertical(
                bottom: Radius.circular(size * .42),
              ),
            ),
          ),
        ),
        Positioned(
          left: size * .08,
          right: size * .08,
          bottom: -size * .14,
          height: size * .43,
          child: DecoratedBox(
            decoration: BoxDecoration(
              color: outfitColor,
              borderRadius: BorderRadius.vertical(
                top: Radius.circular(size * .22),
              ),
            ),
          ),
        ),
        if (accessoryIcon != null)
          Positioned(
            top: size * .29,
            left: 0,
            right: 0,
            child: Icon(
              accessoryIcon,
              color: character.accessory == 'Glasses' ? _ink : _white,
              size: size * .38,
            ),
          ),
      ],
    );
  }

  Color _skinToneColor(String skinTone) {
    return switch (skinTone) {
      'Rich' => const Color(0xFF70402A),
      'Warm' => const Color(0xFF9D6347),
      'Golden' => const Color(0xFFC98E63),
      'Cool' => const Color(0xFF8D5B53),
      _ => const Color(0xFF4D2B20),
    };
  }
}

class _ProfileMetric extends StatelessWidget {
  const _ProfileMetric({
    required this.label,
    required this.value,
    required this.color,
  });

  final String label;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Text(
          label,
          style: const TextStyle(
            color: _muted,
            fontSize: 9,
            fontWeight: FontWeight.w900,
            letterSpacing: 1,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          value,
          style: TextStyle(
            color: color,
            fontWeight: FontWeight.w900,
            fontSize: 18,
          ),
        ),
      ],
    );
  }
}

class _StoryState extends StatelessWidget {
  const _StoryState({required this.model});

  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: _line),
      ),
      child: Column(
        children: <Widget>[
          _StateLine(
            icon: Icons.groups_outlined,
            label: 'Crew trust',
            value: model.crewTrust,
            color: kLime,
          ),
          const Divider(color: _line, height: 25),
          _StateLine(
            icon: Icons.favorite_border,
            label: 'Community support',
            value: model.communitySupport,
            color: kRose,
          ),
          const Divider(color: _line, height: 25),
          const Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Icon(Icons.lock_outline, color: kGold, size: 19),
              SizedBox(width: 10),
              Expanded(
                child: Text(
                  'Evidence and campaign decisions are stable within a story slot.',
                  style: TextStyle(color: _muted, fontSize: 12, height: 1.35),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _StateLine extends StatelessWidget {
  const _StateLine({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
  });

  final IconData icon;
  final String label;
  final int value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: <Widget>[
        Icon(icon, color: color, size: 21),
        const SizedBox(width: 10),
        Expanded(
          child: Text(
            label,
            style: const TextStyle(fontWeight: FontWeight.w800),
          ),
        ),
        Text(
          '$value',
          style: TextStyle(
            color: color,
            fontWeight: FontWeight.w900,
            fontSize: 18,
          ),
        ),
      ],
    );
  }
}

class _Track {
  const _Track(this.title, this.scene, this.color);

  final String title;
  final String scene;
  final Color color;
}

class _MusicPlan extends StatelessWidget {
  const _MusicPlan();

  @override
  Widget build(BuildContext context) {
    const List<_Track> tracks = <_Track>[
      _Track('Still Dreaming', 'Cruising', kBlue),
      _Track('Six and Sevens', 'Fictional patrol pursuit', kSunset),
      _Track('Posa Posa', 'Indoor social scene', kViolet),
      _Track('Takabaka', 'Street dance crowd', kLime),
    ];
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: _line),
      ),
      child: Column(
        children: <Widget>[
          for (final _Track track in tracks)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Row(
                children: <Widget>[
                  Container(
                    width: 36,
                    height: 36,
                    decoration: BoxDecoration(
                      color: track.color.withValues(alpha: .15),
                      borderRadius: BorderRadius.circular(11),
                    ),
                    child: Icon(Icons.music_note, color: track.color, size: 19),
                  ),
                  const SizedBox(width: 11),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: <Widget>[
                        Text(
                          track.title,
                          style: const TextStyle(fontWeight: FontWeight.w800),
                        ),
                        Text(
                          track.scene,
                          style: const TextStyle(color: _muted, fontSize: 12),
                        ),
                      ],
                    ),
                  ),
                  const _Pill(label: 'PENDING', color: _muted),
                ],
              ),
            ),
          const Text(
            'Recordings, timing maps and rights status are not supplied. The prototype keeps the cue contract visible without playing or claiming those tracks.',
            style: TextStyle(color: _muted, fontSize: 12, height: 1.4),
          ),
        ],
      ),
    );
  }
}

class _CloudSyncPanel extends StatelessWidget {
  const _CloudSyncPanel({required this.model});

  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) {
    final Color statusColor = switch (model.syncState) {
      SyncState.local => kGold,
      SyncState.syncing => kBlue,
      SyncState.synced => kLime,
      SyncState.unavailable => kSunset,
    };
    final IconData statusIcon = switch (model.syncState) {
      SyncState.local => Icons.save_outlined,
      SyncState.syncing => Icons.sync,
      SyncState.synced => Icons.cloud_done_outlined,
      SyncState.unavailable => Icons.cloud_off_outlined,
    };
    final String statusLabel = switch (model.syncState) {
      SyncState.local => 'LOCAL',
      SyncState.syncing => 'SYNCING',
      SyncState.synced => 'SYNCED',
      SyncState.unavailable => 'UNAVAILABLE',
    };

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: statusColor.withValues(alpha: .42)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Container(
                padding: const EdgeInsets.all(9),
                decoration: BoxDecoration(
                  color: statusColor.withValues(alpha: .15),
                  borderRadius: BorderRadius.circular(11),
                ),
                child: model.isSyncing
                    ? SizedBox(
                        width: 19,
                        height: 19,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: statusColor,
                        ),
                      )
                    : Icon(statusIcon, color: statusColor, size: 20),
              ),
              const SizedBox(width: 11),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    const Text(
                      'GO CAMPAIGN API',
                      style: TextStyle(fontWeight: FontWeight.w900),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      model.syncMessage,
                      style: const TextStyle(
                        color: _muted,
                        fontSize: 12,
                        height: 1.35,
                      ),
                    ),
                  ],
                ),
              ),
              _Pill(label: statusLabel, color: statusColor),
            ],
          ),
          const SizedBox(height: 15),
          Text('PROFILE · ${model.profileId}', style: _labelStyle),
          const SizedBox(height: 4),
          Text(
            model.apiBaseUrl,
            style: const TextStyle(color: Color(0xFFD4DED8), fontSize: 12),
          ),
          const SizedBox(height: 15),
          Wrap(
            spacing: 9,
            runSpacing: 9,
            children: <Widget>[
              OutlinedButton.icon(
                onPressed: model.isSyncing ? null : model.loadFromCloud,
                icon: const Icon(Icons.cloud_download_outlined),
                label: const Text('Load cloud save'),
                style: OutlinedButton.styleFrom(foregroundColor: _white),
              ),
              FilledButton.icon(
                onPressed: model.isSyncing ? null : model.saveToCloud,
                icon: const Icon(Icons.cloud_upload_outlined),
                label: const Text('Save to cloud'),
                style: FilledButton.styleFrom(
                  backgroundColor: kLime,
                  foregroundColor: _ink,
                  textStyle: const TextStyle(fontWeight: FontWeight.w900),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          const Text(
            'Development service only: it stores an anonymous campaign snapshot. Add authentication and authorization before a public release.',
            style: TextStyle(color: _muted, fontSize: 11, height: 1.35),
          ),
        ],
      ),
    );
  }
}

class _Pill extends StatelessWidget {
  const _Pill({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .15),
        border: Border.all(color: color.withValues(alpha: .45)),
        borderRadius: BorderRadius.circular(99),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 9,
          fontWeight: FontWeight.w900,
          letterSpacing: .8,
        ),
      ),
    );
  }
}

class _Stars extends StatelessWidget {
  const _Stars({required this.score, this.size = 16});

  final int score;
  final double size;

  @override
  Widget build(BuildContext context) {
    final int stars = score >= 900
        ? 3
        : score >= 750
        ? 2
        : score > 0
        ? 1
        : 0;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: List<Widget>.generate(
        3,
        (int index) => Icon(
          index < stars ? Icons.star_rounded : Icons.star_outline_rounded,
          color: kGold,
          size: size,
        ),
      ),
    );
  }
}

class _MissionBrief extends StatelessWidget {
  const _MissionBrief({
    required this.mission,
    required this.completed,
    required this.bestScore,
    required this.onStart,
  });

  final Mission mission;
  final bool completed;
  final int bestScore;
  final VoidCallback onStart;

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: .79,
      minChildSize: .56,
      maxChildSize: .94,
      builder: (BuildContext context, ScrollController controller) {
        return Container(
          decoration: const BoxDecoration(
            color: Color(0xFF0D1B17),
            borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
          ),
          child: ListView(
            controller: controller,
            padding: const EdgeInsets.fromLTRB(22, 14, 22, 32),
            children: <Widget>[
              Center(
                child: Container(
                  width: 42,
                  height: 4,
                  decoration: BoxDecoration(
                    color: const Color(0xFF496258),
                    borderRadius: BorderRadius.circular(99),
                  ),
                ),
              ),
              const SizedBox(height: 22),
              Row(
                children: <Widget>[
                  _Pill(
                    label: 'LEVEL ${mission.level.toString().padLeft(2, '0')}',
                    color: mission.accent,
                  ),
                  const SizedBox(width: 8),
                  if (mission.adFree)
                    const _Pill(label: 'AD-FREE', color: kLime),
                  const Spacer(),
                  if (completed) _Stars(score: bestScore),
                ],
              ),
              const SizedBox(height: 14),
              Text(
                mission.title,
                style: const TextStyle(
                  fontSize: 30,
                  height: 1.05,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                mission.briefing,
                style: const TextStyle(color: Color(0xFFD5DED8), height: 1.45),
              ),
              const SizedBox(height: 20),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: <Widget>[
                  _Meta(icon: Icons.place_outlined, label: mission.district),
                  _Meta(
                    icon: Icons.schedule_outlined,
                    label: '${mission.timeOfDay} · ${mission.duration}',
                  ),
                  _Meta(
                    icon: Icons.directions_car_outlined,
                    label: mission.vehicle,
                  ),
                  _Meta(
                    icon: Icons.toll_outlined,
                    label: '${mission.coinReward} coins',
                  ),
                  _Meta(
                    icon: Icons.bolt_outlined,
                    label: '${mission.xpReward} XP',
                  ),
                ],
              ),
              const SizedBox(height: 21),
              const Text('MISSION FLOW', style: _labelStyle),
              const SizedBox(height: 11),
              for (int index = 0; index < mission.objectives.length; index++)
                Padding(
                  padding: const EdgeInsets.only(bottom: 11),
                  child: _BriefObjective(
                    index: index + 1,
                    text: mission.objectives[index],
                    accent: mission.accent,
                  ),
                ),
              _OptionalObjective(text: mission.optionalObjective),
              const SizedBox(height: 21),
              FilledButton.icon(
                onPressed: onStart,
                icon: Icon(completed ? Icons.replay : Icons.play_arrow_rounded),
                label: Text(completed ? 'Replay mission' : 'Start mission'),
                style: FilledButton.styleFrom(
                  minimumSize: const Size.fromHeight(52),
                  backgroundColor: mission.accent,
                  foregroundColor: _ink,
                  textStyle: const TextStyle(fontWeight: FontWeight.w900),
                ),
              ),
              if (completed) ...<Widget>[
                const SizedBox(height: 8),
                const Text(
                  'Replays update personal records and award 20% of base coins. First-completion XP does not repeat.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: _muted, fontSize: 12, height: 1.35),
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}

class _Meta extends StatelessWidget {
  const _Meta({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 8),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: _line),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Icon(icon, color: _muted, size: 14),
          const SizedBox(width: 5),
          Text(
            label,
            style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
          ),
        ],
      ),
    );
  }
}

class _BriefObjective extends StatelessWidget {
  const _BriefObjective({
    required this.index,
    required this.text,
    required this.accent,
  });

  final int index;
  final String text;
  final Color accent;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Container(
          width: 26,
          height: 26,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: accent.withValues(alpha: .16),
            shape: BoxShape.circle,
          ),
          child: Text(
            '$index',
            style: TextStyle(
              color: accent,
              fontWeight: FontWeight.w900,
              fontSize: 11,
            ),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(child: Text(text, style: const TextStyle(height: 1.35))),
      ],
    );
  }
}

class _OptionalObjective extends StatelessWidget {
  const _OptionalObjective({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: kGold.withValues(alpha: .1),
        borderRadius: BorderRadius.circular(13),
        border: Border.all(color: kGold.withValues(alpha: .34)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          const Icon(Icons.star_outline_rounded, color: kGold, size: 19),
          const SizedBox(width: 9),
          Expanded(
            child: RichText(
              text: TextSpan(
                style: const TextStyle(
                  color: _white,
                  height: 1.35,
                  fontSize: 13,
                ),
                children: <InlineSpan>[
                  const TextSpan(
                    text: 'OPTIONAL · ',
                    style: TextStyle(color: kGold, fontWeight: FontWeight.w900),
                  ),
                  TextSpan(text: text),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class MissionScreen extends StatefulWidget {
  const MissionScreen({
    super.key,
    required this.mission,
    required this.isReplay,
  });

  final Mission mission;
  final bool isReplay;

  @override
  State<MissionScreen> createState() => _MissionScreenState();
}

class _MissionScreenState extends State<MissionScreen> {
  int _completedObjectives = 0;
  int? _choiceIndex;
  bool _optionalCompleted = false;
  bool _cleanExecution = true;

  Mission get _mission => widget.mission;

  int get _score =>
      600 + 180 + (_cleanExecution ? 100 : 0) + (_optionalCompleted ? 100 : 0);

  int get _stars => _score >= 900
      ? 3
      : _score >= 750
      ? 2
      : 1;

  int get _coinPayout {
    if (widget.isReplay) return (_mission.coinReward * .2).floor();
    return (_mission.coinReward *
            (_stars == 3
                ? 1.5
                : _stars == 2
                ? 1.25
                : 1))
        .floor();
  }

  StoryImpact get _impact => _choiceIndex == 0
      ? _mission.choice.primaryImpact
      : _mission.choice.secondaryImpact;

  void _advanceObjective() {
    if (_completedObjectives < _mission.objectives.length) {
      setState(() => _completedObjectives += 1);
    }
  }

  void _commit() {
    Navigator.of(context).pop(
      MissionResult(
        score: _score,
        stars: _stars,
        coinPayout: _coinPayout,
        xpPayout: widget.isReplay ? 0 : _mission.xpReward,
        impact: _impact,
        choiceLabel: _choiceIndex == 0
            ? _mission.choice.primaryLabel
            : _mission.choice.secondaryLabel,
        wasReplay: widget.isReplay,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final double progress =
        (_completedObjectives + (_choiceIndex == null ? 0 : 1)) / 4;
    return Scaffold(
      backgroundColor: _ink,
      body: SafeArea(
        child: Column(
          children: <Widget>[
            Padding(
              padding: const EdgeInsets.fromLTRB(15, 10, 18, 12),
              child: Row(
                children: <Widget>[
                  IconButton(
                    onPressed: () => Navigator.of(context).pop(),
                    icon: const Icon(Icons.close),
                    tooltip: 'Leave mission',
                  ),
                  const SizedBox(width: 4),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: <Widget>[
                        Text(
                          'MISSION ${_mission.level.toString().padLeft(2, '0')}',
                          style: _labelStyle,
                        ),
                        Text(
                          _mission.title,
                          style: const TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                      ],
                    ),
                  ),
                  _Pill(
                    label: widget.isReplay ? 'REPLAY' : 'ACTIVE',
                    color: _mission.accent,
                  ),
                ],
              ),
            ),
            LinearProgressIndicator(
              value: progress,
              minHeight: 5,
              backgroundColor: const Color(0xFF1A3029),
              valueColor: AlwaysStoppedAnimation<Color>(_mission.accent),
            ),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.fromLTRB(18, 22, 18, 32),
                children: <Widget>[
                  _ActiveHeader(mission: _mission, progress: progress),
                  const SizedBox(height: 21),
                  const Text('OBJECTIVES', style: _labelStyle),
                  const SizedBox(height: 11),
                  for (
                    int index = 0;
                    index < _mission.objectives.length;
                    index++
                  )
                    Padding(
                      padding: const EdgeInsets.only(bottom: 10),
                      child: _ObjectiveCard(
                        index: index + 1,
                        text: _mission.objectives[index],
                        accent: _mission.accent,
                        complete: index < _completedObjectives,
                        active: index == _completedObjectives,
                      ),
                    ),
                  if (_completedObjectives <
                      _mission.objectives.length) ...<Widget>[
                    const SizedBox(height: 4),
                    FilledButton.icon(
                      onPressed: _advanceObjective,
                      icon: const Icon(Icons.check),
                      label: Text(
                        'Complete objective ${_completedObjectives + 1}',
                      ),
                      style: FilledButton.styleFrom(
                        minimumSize: const Size.fromHeight(52),
                        backgroundColor: _mission.accent,
                        foregroundColor: _ink,
                        textStyle: const TextStyle(fontWeight: FontWeight.w900),
                      ),
                    ),
                  ] else ...<Widget>[
                    const SizedBox(height: 12),
                    _ToggleTile(
                      icon: Icons.star_outline_rounded,
                      color: kGold,
                      title: 'Optional objective',
                      subtitle: _mission.optionalObjective,
                      value: _optionalCompleted,
                      onChanged: (bool value) =>
                          setState(() => _optionalCompleted = value),
                    ),
                    const SizedBox(height: 10),
                    _ToggleTile(
                      icon: Icons.shield_outlined,
                      color: kLime,
                      title: 'Clean execution',
                      subtitle: 'Keep the 100-point clean-execution component.',
                      value: _cleanExecution,
                      onChanged: (bool value) =>
                          setState(() => _cleanExecution = value),
                    ),
                    const SizedBox(height: 22),
                    const Text('STORY CHOICE', style: _labelStyle),
                    const SizedBox(height: 9),
                    Text(
                      _mission.choice.prompt,
                      style: const TextStyle(
                        fontSize: 17,
                        height: 1.3,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    const SizedBox(height: 11),
                    _ChoiceTile(
                      label: _mission.choice.primaryLabel,
                      impact: _mission.choice.primaryImpact,
                      selected: _choiceIndex == 0,
                      accent: _mission.accent,
                      onTap: () => setState(() => _choiceIndex = 0),
                    ),
                    const SizedBox(height: 9),
                    _ChoiceTile(
                      label: _mission.choice.secondaryLabel,
                      impact: _mission.choice.secondaryImpact,
                      selected: _choiceIndex == 1,
                      accent: _mission.accent,
                      onTap: () => setState(() => _choiceIndex = 1),
                    ),
                    if (_choiceIndex != null) ...<Widget>[
                      const SizedBox(height: 20),
                      _ResultCard(
                        score: _score,
                        coinPayout: _coinPayout,
                        xpPayout: widget.isReplay ? 0 : _mission.xpReward,
                        replay: widget.isReplay,
                      ),
                      const SizedBox(height: 12),
                      FilledButton.icon(
                        onPressed: _commit,
                        icon: const Icon(Icons.emoji_events_outlined),
                        label: const Text('Commit mission result'),
                        style: FilledButton.styleFrom(
                          minimumSize: const Size.fromHeight(54),
                          backgroundColor: kLime,
                          foregroundColor: _ink,
                          textStyle: const TextStyle(
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                      ),
                    ],
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ActiveHeader extends StatelessWidget {
  const _ActiveHeader({required this.mission, required this.progress});

  final Mission mission;
  final double progress;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: <Color>[
            mission.accent.withValues(alpha: .25),
            const Color(0xFF18302A),
          ],
        ),
        borderRadius: BorderRadius.circular(21),
        border: Border.all(color: mission.accent.withValues(alpha: .52)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Row(
            children: <Widget>[
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: _ink.withValues(alpha: .45),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(mission.icon, color: mission.accent),
              ),
              const Spacer(),
              Text(
                '${(progress * 100).round()}%',
                style: TextStyle(
                  color: mission.accent,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Text(mission.activity.toUpperCase(), style: _labelStyle),
          const SizedBox(height: 4),
          Text(
            mission.briefing,
            style: const TextStyle(
              fontSize: 16,
              height: 1.35,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 13),
          Text(
            'NOW PLAYING · ${mission.musicCue}',
            style: TextStyle(
              color: mission.accent,
              fontSize: 11,
              fontWeight: FontWeight.w900,
            ),
          ),
        ],
      ),
    );
  }
}

class _ObjectiveCard extends StatelessWidget {
  const _ObjectiveCard({
    required this.index,
    required this.text,
    required this.accent,
    required this.complete,
    required this.active,
  });

  final int index;
  final String text;
  final Color accent;
  final bool complete;
  final bool active;

  @override
  Widget build(BuildContext context) {
    final Color background = complete
        ? kLime.withValues(alpha: .1)
        : active
        ? accent.withValues(alpha: .13)
        : _surface;
    final Color border = complete
        ? kLime.withValues(alpha: .5)
        : active
        ? accent.withValues(alpha: .58)
        : _line;
    return AnimatedContainer(
      duration: const Duration(milliseconds: 200),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: border),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Container(
            width: 27,
            height: 27,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: complete
                  ? kLime
                  : active
                  ? accent
                  : const Color(0xFF29443A),
              shape: BoxShape.circle,
            ),
            child: complete
                ? const Icon(Icons.check, color: _ink, size: 16)
                : Text(
                    '$index',
                    style: TextStyle(
                      color: active ? _ink : _muted,
                      fontSize: 11,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
          ),
          const SizedBox(width: 11),
          Expanded(
            child: Text(
              text,
              style: TextStyle(
                color: complete ? const Color(0xFFC6D3CC) : _white,
                decoration: complete ? TextDecoration.lineThrough : null,
                fontWeight: active ? FontWeight.w800 : FontWeight.w600,
                height: 1.35,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ToggleTile extends StatelessWidget {
  const _ToggleTile({
    required this.icon,
    required this.color,
    required this.title,
    required this.subtitle,
    required this.value,
    required this.onChanged,
  });

  final IconData icon;
  final Color color;
  final String title;
  final String subtitle;
  final bool value;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    return SwitchListTile.adaptive(
      value: value,
      onChanged: onChanged,
      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 3),
      tileColor: color.withValues(alpha: .1),
      activeTrackColor: color,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(15),
        side: BorderSide(color: color.withValues(alpha: .38)),
      ),
      secondary: Icon(icon, color: color),
      title: Text(
        title,
        style: TextStyle(
          color: color,
          fontSize: 12,
          fontWeight: FontWeight.w900,
        ),
      ),
      subtitle: Text(
        subtitle,
        style: const TextStyle(color: _white, fontSize: 12, height: 1.3),
      ),
    );
  }
}

class _ChoiceTile extends StatelessWidget {
  const _ChoiceTile({
    required this.label,
    required this.impact,
    required this.selected,
    required this.accent,
    required this.onTap,
  });

  final String label;
  final StoryImpact impact;
  final bool selected;
  final Color accent;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? accent.withValues(alpha: .16) : _surface,
      borderRadius: BorderRadius.circular(15),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(15),
        child: Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(15),
            border: Border.all(color: selected ? accent : _line),
          ),
          child: Row(
            children: <Widget>[
              Icon(
                selected ? Icons.radio_button_checked : Icons.radio_button_off,
                color: selected ? accent : _muted,
              ),
              const SizedBox(width: 11),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      label,
                      style: const TextStyle(
                        fontWeight: FontWeight.w800,
                        height: 1.25,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      impact.label,
                      style: TextStyle(
                        color: selected ? accent : _muted,
                        fontSize: 11,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ResultCard extends StatelessWidget {
  const _ResultCard({
    required this.score,
    required this.coinPayout,
    required this.xpPayout,
    required this.replay,
  });

  final int score;
  final int coinPayout;
  final int xpPayout;
  final bool replay;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(17),
      decoration: BoxDecoration(
        color: const Color(0xFF153027),
        borderRadius: BorderRadius.circular(17),
        border: Border.all(color: const Color(0xFF3A7056)),
      ),
      child: Row(
        children: <Widget>[
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              const Text('MISSION RESULT', style: _labelStyle),
              const SizedBox(height: 4),
              Text(
                '$score / 1,000',
                style: const TextStyle(
                  fontSize: 21,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 3),
              _Stars(score: score),
            ],
          ),
          const Spacer(),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: <Widget>[
              Text(
                '+$coinPayout',
                style: const TextStyle(
                  color: kGold,
                  fontWeight: FontWeight.w900,
                  fontSize: 18,
                ),
              ),
              Text(
                replay ? 'replay coins' : 'coins',
                style: const TextStyle(color: _muted, fontSize: 11),
              ),
              const SizedBox(height: 4),
              Text(
                xpPayout > 0 ? '+$xpPayout XP' : 'No repeat XP',
                style: const TextStyle(
                  color: kLime,
                  fontSize: 12,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
