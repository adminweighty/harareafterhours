import 'package:flutter/material.dart';
import 'game_menu_visuals.dart';
import 'game_theme.dart';
import '../models/player_guidance.dart';

class GameOverPanel extends StatelessWidget {
  const GameOverPanel({
    super.key,
    required this.onRestart,
    required this.onMainMenu,
  });
  final VoidCallback onRestart, onMainMenu;
  @override
  Widget build(BuildContext context) => _SessionStatePanel(
    eyebrow: 'ONE MORE NIGHT',
    title: 'GAME OVER',
    icon: Icons.shield_outlined,
    message:
        'Regroup. Get back on your feet.\nBreak enemy attacks with PUNCH or KICK, and use cover to recover.',
    children: [
      MenuActionCard(
        icon: Icons.restart_alt,
        title: 'Restart encounter',
        subtitle: 'Return to the safe corner',
        primary: true,
        onTap: onRestart,
      ),
      const SizedBox(height: 12),
      OutlinedButton(onPressed: onMainMenu, child: const Text('Main menu')),
      const SizedBox(height: 16),
      const Text(
        'Mission progress and earned points stay saved. Previously rewarded enemies do not award points again.',
        style: TextStyle(color: Color(0xFFA6BBBD), fontSize: 13, height: 1.5),
      ),
    ],
  );
}

class GameClosedPanel extends StatelessWidget {
  const GameClosedPanel({super.key, required this.onStart});
  final VoidCallback onStart;
  @override
  Widget build(BuildContext context) => _SessionStatePanel(
    eyebrow: 'UNTIL NEXT TIME',
    title: 'Game closed',
    icon: Icons.nightlight_round,
    message:
        'The city will be waiting.\nGameplay and audio have stopped. Your saved progress is kept.',
    children: [
      MenuActionCard(
        icon: Icons.play_arrow_rounded,
        title: 'Start game',
        subtitle: 'A fresh session. Your saved story.',
        primary: true,
        onTap: onStart,
      ),
      const SizedBox(height: 16),
      const Text(
        'On iPhone, swipe up from the bottom to return Home.',
        style: TextStyle(color: Color(0xFFA6BBBD), fontSize: 13, height: 1.5),
      ),
    ],
  );
}

class _SessionStatePanel extends StatelessWidget {
  const _SessionStatePanel({
    required this.eyebrow,
    required this.title,
    required this.message,
    required this.icon,
    required this.children,
  });
  final String eyebrow, title, message;
  final IconData icon;
  final List<Widget> children;
  @override
  Widget build(BuildContext context) => Material(
    color: GameTheme.ink,
    child: NightCityBackdrop(
      child: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(28),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: MenuReveal(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Align(
                      alignment: Alignment.centerLeft,
                      child: Icon(icon, size: 48, color: GameTheme.amber),
                    ),
                    const SizedBox(height: 24),
                    MenuEyebrow(eyebrow),
                    const SizedBox(height: 12),
                    Text(
                      title,
                      style: const TextStyle(
                        fontSize: 40,
                        fontWeight: FontWeight.w900,
                        letterSpacing: -1,
                      ),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      message,
                      style: const TextStyle(
                        height: 1.6,
                        color: Color(0xFFBDD1CE),
                      ),
                    ),
                    const SizedBox(height: 28),
                    ...children,
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    ),
  );
}

/// The live session stays paused while this responsive menu is visible.
class GameSessionMenu extends StatelessWidget {
  const GameSessionMenu({
    super.key,
    required this.mainMenu,
    required this.ready,
    required this.characterReady,
    required this.onAction,
    this.missionTitle = 'City Wakes / Wrong Delivery',
    this.district = 'Harare CBD',
    this.needsRestart = false,
    this.difficulty = 'Intermediate',
    this.guidance = const PlayerGuidance(),
    this.sponsor,
  });
  final bool mainMenu, ready, characterReady, needsRestart;
  final ValueChanged<String> onAction;
  final String missionTitle, district, difficulty;
  final PlayerGuidance guidance;
  final Widget? sponsor;

  @override
  Widget build(BuildContext context) => NightCityBackdrop(
    child: SafeArea(
      child: LayoutBuilder(
        builder: (context, constraints) {
          final landscape = constraints.maxWidth > constraints.maxHeight;
          final compactLandscape = landscape && constraints.maxHeight < 560;
          final wide =
              constraints.maxWidth >= 700 &&
              MediaQuery.textScalerOf(context).scale(1) < 1.4;
          return SingleChildScrollView(
            padding: EdgeInsets.symmetric(
              horizontal: wide ? 28 : 20,
              vertical: compactLandscape ? 12 : 20,
            ),
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 1160),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Container(
                      padding: EdgeInsets.symmetric(
                        horizontal: compactLandscape ? 12 : 16,
                        vertical: compactLandscape ? 7 : 10,
                      ),
                      decoration: BoxDecoration(
                        color: const Color(0xD9102023),
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: const Color(0xFF294245)),
                        boxShadow: const [
                          BoxShadow(
                            color: Color(0x55000000),
                            blurRadius: 24,
                            offset: Offset(0, 10),
                          ),
                        ],
                      ),
                      child: Row(
                        children: [
                          Container(
                            width: 34,
                            height: 34,
                            decoration: BoxDecoration(
                              color: GameTheme.amber.withValues(alpha: .14),
                              shape: BoxShape.circle,
                              border: Border.all(
                                color: GameTheme.amber.withValues(alpha: .45),
                              ),
                            ),
                            child: const Icon(
                              Icons.nightlight_round,
                              size: 17,
                              color: GameTheme.amber,
                            ),
                          ),
                          const SizedBox(width: 11),
                          const Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                MenuEyebrow('HARARE / AFTER HOURS'),
                                SizedBox(height: 2),
                                Text(
                                  'CITY STORIES AFTER DARK',
                                  style: TextStyle(
                                    color: Color(0xFF8FA9A7),
                                    fontSize: 9,
                                    letterSpacing: 1.1,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          if (!mainMenu)
                            IconButton.filledTonal(
                              tooltip: 'Close menu',
                              onPressed: () => onAction('resume'),
                              icon: const Icon(Icons.close_rounded),
                            )
                          else
                            Tooltip(
                              message: 'City session',
                              child: Container(
                                width: 34,
                                height: 34,
                                decoration: BoxDecoration(
                                  color: const Color(0xFF1B3032),
                                  shape: BoxShape.circle,
                                  border: Border.all(
                                    color: const Color(0xFF355053),
                                  ),
                                ),
                                child: const Icon(
                                  Icons.location_city_rounded,
                                  size: 17,
                                  color: Color(0xFFA0B8B5),
                                ),
                              ),
                            ),
                        ],
                      ),
                    ),
                    SizedBox(height: compactLandscape ? 12 : 20),
                    if (wide)
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            flex: 5,
                            child: _hero(context, compact: compactLandscape),
                          ),
                          SizedBox(width: compactLandscape ? 20 : 32),
                          Expanded(
                            flex: 6,
                            child: _actions(context, compact: compactLandscape),
                          ),
                        ],
                      )
                    else ...[
                      _hero(context, compact: false),
                      const SizedBox(height: 24),
                      _actions(context, compact: false),
                    ],
                    const SizedBox(height: 24),
                    if (sponsor != null) ...[
                      Center(child: sponsor!),
                      const SizedBox(height: 24),
                    ],
                    const Text(
                      'YOUR CITY. YOUR STORY.',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: Color(0xFF7F9A9B),
                        fontSize: 10,
                        letterSpacing: 3,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      ),
    ),
  );

  Widget _hero(BuildContext context, {required bool compact}) => MenuReveal(
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
          decoration: BoxDecoration(
            color: const Color(0xFF243C3B),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                ready ? Icons.pause_circle_outline : Icons.hourglass_empty,
                size: 14,
                color: const Color(0xFF98D9C7),
              ),
              const SizedBox(width: 7),
              Text(
                ready ? 'SESSION PAUSED' : 'CONNECTING TO CITY',
                style: const TextStyle(
                  color: Color(0xFFB7DCD3),
                  fontSize: 10,
                  letterSpacing: 1.2,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
        SizedBox(height: compact ? 12 : 18),
        Text(
          mainMenu ? 'The night\nis yours.' : 'Game paused',
          style: TextStyle(
            fontSize: compact ? (mainMenu ? 38 : 34) : (mainMenu ? 48 : 42),
            height: 1.04,
            letterSpacing: compact ? -1 : -1.7,
            fontWeight: FontWeight.w900,
            color: GameTheme.ivory,
          ),
        ),
        SizedBox(height: compact ? 8 : 14),
        Text(
          mainMenu
              ? 'Find your next story in the streets of Harare.'
              : 'Take a breath. The city can wait.',
          style: TextStyle(
            color: const Color(0xFFACC1BE),
            fontSize: compact ? 13 : 15,
            height: 1.5,
          ),
        ),
        SizedBox(height: compact ? 12 : 22),
        Container(
          padding: EdgeInsets.all(compact ? 12 : 16),
          decoration: BoxDecoration(
            color: const Color(0xD914272A),
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: const Color(0xFF304B4D)),
            boxShadow: const [
              BoxShadow(
                color: Color(0x33000000),
                blurRadius: 20,
                offset: Offset(0, 8),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    width: compact ? 30 : 36,
                    height: compact ? 30 : 36,
                    decoration: BoxDecoration(
                      color: GameTheme.amber.withValues(alpha: .14),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Icon(
                      Icons.route_rounded,
                      size: 19,
                      color: GameTheme.amber,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        MenuEyebrow(district.toUpperCase()),
                        const SizedBox(height: 3),
                        Text(
                          missionTitle,
                          maxLines: compact ? 1 : 2,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            fontSize: compact ? 14 : 16,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              SizedBox(height: compact ? 10 : 14),
              const Divider(height: 1, color: Color(0xFF315052)),
              SizedBox(height: compact ? 10 : 14),
              const MenuEyebrow('WHAT NEXT?'),
              SizedBox(height: compact ? 5 : 8),
              Text(
                guidance.objective,
                style: TextStyle(
                  fontSize: compact ? 15 : 18,
                  fontWeight: FontWeight.w700,
                  height: 1.3,
                ),
              ),
              SizedBox(height: compact ? 5 : 8),
              Text(
                guidance.instruction,
                maxLines: compact ? 2 : null,
                overflow: compact ? TextOverflow.ellipsis : null,
                style: TextStyle(
                  color: const Color(0xFFC0D4D0),
                  fontSize: compact ? 12 : 14,
                  height: 1.5,
                ),
              ),
              SizedBox(height: compact ? 6 : 10),
              Text(
                guidance.availability,
                maxLines: compact ? 1 : null,
                overflow: compact ? TextOverflow.ellipsis : null,
                style: TextStyle(
                  color: GameTheme.amber,
                  fontSize: compact ? 10 : 12,
                  height: 1.4,
                ),
              ),
            ],
          ),
        ),
        SizedBox(height: compact ? 12 : 20),
        const Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            _ControlHint('▲', 'Walk'),
            _ControlHint('◀ ▶', 'Turn'),
            _ControlHint('HOLD', 'Sprint'),
          ],
        ),
      ],
    ),
  );

  Widget _actions(BuildContext context, {required bool compact}) {
    final characterCard = MenuActionCard(
      compact: compact,
      icon: Icons.person_outline_rounded,
      title: 'Change character',
      subtitle: characterReady
          ? 'Your look. Your loadout.'
          : 'Waiting for your saved character…',
      onTap: characterReady ? () => onAction('character') : null,
    );
    final tipsCard = MenuActionCard(
      compact: compact,
      icon: Icons.explore_outlined,
      title: 'Controls & tips',
      subtitle: 'Movement, driving and combat guide',
      onTap: () => onAction('help'),
    );

    return Container(
      padding: EdgeInsets.all(compact ? 12 : 16),
      decoration: BoxDecoration(
        color: const Color(0xD90C191C),
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: const Color(0xFF294245)),
        boxShadow: const [
          BoxShadow(
            color: Color(0x66000000),
            blurRadius: 30,
            offset: Offset(0, 14),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(4, 1, 4, 9),
            child: Row(
              children: [
                const Expanded(child: MenuEyebrow('CITY MENU')),
                Text(
                  difficulty.toUpperCase(),
                  style: const TextStyle(
                    color: Color(0xFF8FA9A7),
                    fontSize: 9,
                    letterSpacing: 1.2,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
          MenuReveal(
            order: 1,
            child: MenuActionCard(
              compact: compact,
              icon: Icons.play_arrow_rounded,
              title: ready
                  ? (needsRestart
                        ? 'Restart encounter'
                        : (mainMenu ? 'Continue game' : 'Resume game'))
                  : 'City is loading…',
              subtitle: needsRestart
                  ? 'Return to the last safe point'
                  : 'Back to the streets',
              primary: true,
              onTap: ready
                  ? () => onAction(needsRestart ? 'restart' : 'resume')
                  : null,
            ),
          ),
          SizedBox(height: compact ? 8 : 10),
          MenuReveal(
            order: 2,
            child: compact
                ? Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(child: characterCard),
                      const SizedBox(width: 8),
                      Expanded(child: tipsCard),
                    ],
                  )
                : Column(
                    children: [
                      characterCard,
                      const SizedBox(height: 10),
                      tipsCard,
                    ],
                  ),
          ),
          SizedBox(height: compact ? 8 : 10),
          MenuReveal(
            order: 3,
            child: _DifficultySelector(
              compact: compact,
              selected: difficulty,
              enabled: ready,
              onSelected: (level) =>
                  onAction('difficulty_${level.toLowerCase()}'),
            ),
          ),
          if (!mainMenu) ...[
            SizedBox(height: compact ? 8 : 10),
            MenuReveal(
              order: 4,
              child: MenuActionCard(
                compact: compact,
                icon: Icons.tune_rounded,
                title: 'Control settings',
                subtitle: 'Button layout, aim & sensitivity',
                onTap: ready ? () => onAction('controls') : null,
              ),
            ),
            SizedBox(height: compact ? 6 : 8),
            Material(
              color: const Color(0x9915282B),
              clipBehavior: Clip.antiAlias,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(14),
                side: const BorderSide(color: Color(0xFF30474A)),
              ),
              child: Theme(
                data: Theme.of(
                  context,
                ).copyWith(dividerColor: Colors.transparent),
                child: ExpansionTile(
                  dense: compact,
                  visualDensity: compact ? VisualDensity.compact : null,
                  tilePadding: const EdgeInsets.symmetric(horizontal: 14),
                  title: const Text(
                    'Graphics',
                    style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700),
                  ),
                  subtitle: const Text(
                    'Balanced for mobile',
                    style: TextStyle(fontSize: 11),
                  ),
                  leading: const Icon(Icons.auto_awesome_rounded, size: 21),
                  children: [
                    ListTile(
                      dense: true,
                      title: const Text('Balanced graphics'),
                      subtitle: const Text(
                        'Smooth play · lower rendering cost',
                      ),
                      enabled: ready,
                      onTap: ready ? () => onAction('graphics_balanced') : null,
                    ),
                    ListTile(
                      dense: true,
                      title: const Text('High graphics'),
                      subtitle: const Text('Sharper image · more GPU use'),
                      enabled: ready,
                      onTap: ready ? () => onAction('graphics_high') : null,
                    ),
                  ],
                ),
              ),
            ),
          ],
          SizedBox(height: compact ? 6 : 10),
          Wrap(
            spacing: 8,
            runSpacing: 4,
            alignment: WrapAlignment.end,
            children: [
              if (!mainMenu)
                OutlinedButton.icon(
                  onPressed: () => onAction('leave'),
                  icon: const Icon(Icons.west_rounded, size: 17),
                  label: const Text('Leave city'),
                ),
              TextButton.icon(
                onPressed: () => onAction('quit'),
                icon: const Icon(Icons.power_settings_new, size: 17),
                label: const Text('Quit game'),
              ),
              TextButton.icon(
                onPressed: () => onAction('privacy'),
                icon: const Icon(Icons.privacy_tip_outlined, size: 17),
                label: const Text('Privacy'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ControlHint extends StatelessWidget {
  const _ControlHint(this.keyLabel, this.label);
  final String keyLabel, label;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    decoration: BoxDecoration(
      color: const Color(0xAA12272A),
      borderRadius: BorderRadius.circular(8),
      border: Border.all(color: const Color(0xFF304D4D)),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (keyLabel == '▲')
          const Icon(Icons.arrow_drop_up, size: 18, color: GameTheme.amber)
        else if (keyLabel == '◀ ▶')
          const Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.arrow_left, size: 16, color: GameTheme.amber),
              Icon(Icons.arrow_right, size: 16, color: GameTheme.amber),
            ],
          )
        else
          Text(
            keyLabel,
            style: const TextStyle(
              color: GameTheme.amber,
              fontSize: 11,
              fontWeight: FontWeight.w800,
            ),
          ),
        const SizedBox(width: 6),
        Text(
          label,
          style: const TextStyle(color: Color(0xFFC1D2CF), fontSize: 11),
        ),
      ],
    ),
  );
}

class _DifficultySelector extends StatelessWidget {
  const _DifficultySelector({
    required this.compact,
    required this.selected,
    required this.enabled,
    required this.onSelected,
  });

  final bool compact;
  final String selected;
  final bool enabled;
  final ValueChanged<String> onSelected;

  static const levels = <String>['Beginner', 'Intermediate', 'Expert'];

  @override
  Widget build(BuildContext context) => Container(
    padding: EdgeInsets.all(compact ? 12 : 14),
    decoration: BoxDecoration(
      gradient: const LinearGradient(
        colors: [Color(0xE6162C2E), Color(0xE6102023)],
        begin: Alignment.topLeft,
        end: Alignment.bottomRight,
      ),
      borderRadius: BorderRadius.circular(14),
      border: Border.all(color: const Color(0xFF2B4848)),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            const Icon(
              Icons.sports_martial_arts_rounded,
              size: 20,
              color: GameTheme.amber,
            ),
            const SizedBox(width: 9),
            const Expanded(
              child: Text(
                'Game level',
                style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800),
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: GameTheme.amber.withValues(alpha: .12),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Text(
                selected.toUpperCase(),
                style: const TextStyle(
                  color: GameTheme.amber,
                  fontSize: 8,
                  letterSpacing: .8,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
          ],
        ),
        SizedBox(height: compact ? 3 : 6),
        Text(
          switch (selected) {
            'Beginner' => 'More recovery · slower enemies · two-hit takedowns',
            'Expert' => 'Faster threats · longer pursuit · four-hit takedowns',
            _ => 'Balanced combat · three-hit takedowns',
          },
          style: const TextStyle(
            color: Color(0xFFAFC6C2),
            fontSize: 12,
            height: 1.35,
          ),
        ),
        SizedBox(height: compact ? 8 : 11),
        LayoutBuilder(
          builder: (context, constraints) {
            final narrow = constraints.maxWidth < 235;
            final choiceWidth = narrow
                ? constraints.maxWidth
                : (constraints.maxWidth - 16) / 3;
            return Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final level in levels)
                  SizedBox(
                    width: choiceWidth,
                    child: _DifficultyChoice(
                      label: level,
                      selected: selected == level,
                      enabled: enabled,
                      compact: compact,
                      onTap: () => onSelected(level),
                    ),
                  ),
              ],
            );
          },
        ),
      ],
    ),
  );
}

class _DifficultyChoice extends StatelessWidget {
  const _DifficultyChoice({
    required this.label,
    required this.selected,
    required this.enabled,
    required this.compact,
    required this.onTap,
  });

  final String label;
  final bool selected, enabled, compact;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: selected ? const Color(0xFF33504A) : const Color(0xFF172D30),
    borderRadius: BorderRadius.circular(10),
    child: InkWell(
      onTap: enabled ? onTap : null,
      borderRadius: BorderRadius.circular(10),
      child: Container(
        constraints: BoxConstraints(minHeight: compact ? 48 : 56),
        padding: EdgeInsets.symmetric(
          horizontal: compact ? 7 : 9,
          vertical: compact ? 7 : 9,
        ),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(10),
          border: Border.all(
            color: selected ? GameTheme.amber : const Color(0xFF365052),
          ),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              selected
                  ? Icons.radio_button_checked
                  : Icons.radio_button_unchecked,
              size: compact ? 16 : 18,
              color: selected ? GameTheme.amber : const Color(0xFF8FA9A6),
            ),
            SizedBox(height: compact ? 3 : 5),
            Text(
              label,
              maxLines: 2,
              textAlign: TextAlign.center,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: enabled ? GameTheme.ivory : const Color(0xFF718583),
                fontSize: compact ? 10 : 11,
                height: 1.05,
                fontWeight: selected ? FontWeight.w800 : FontWeight.w600,
              ),
            ),
          ],
        ),
      ),
    ),
  );
}
