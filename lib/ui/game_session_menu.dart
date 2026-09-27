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
          final wide =
              constraints.maxWidth >= 720 &&
              MediaQuery.textScalerOf(context).scale(1) < 1.4;
          return SingleChildScrollView(
            padding: EdgeInsets.all(wide ? 28 : 20),
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 1100),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      children: [
                        const Expanded(
                          child: MenuEyebrow('HARARE / AFTER HOURS'),
                        ),
                        if (!mainMenu)
                          IconButton(
                            tooltip: 'Close menu',
                            onPressed: () => onAction('resume'),
                            icon: const Icon(Icons.close_rounded),
                          )
                        else
                          const Padding(
                            padding: EdgeInsets.all(12),
                            child: MenuEyebrow(
                              'CITY SESSION',
                              color: Color(0xFF8FAFAA),
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    if (wide)
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(flex: 5, child: _hero(context)),
                          const SizedBox(width: 40),
                          Expanded(flex: 6, child: _actions(context)),
                        ],
                      )
                    else ...[
                      _hero(context),
                      const SizedBox(height: 28),
                      _actions(context),
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

  Widget _hero(BuildContext context) => MenuReveal(
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
        const SizedBox(height: 20),
        Text(
          mainMenu ? 'The night\nis yours.' : 'Game paused',
          style: TextStyle(
            fontSize: mainMenu ? 52 : 42,
            height: 1.04,
            letterSpacing: -1.7,
            fontWeight: FontWeight.w900,
            color: GameTheme.ivory,
          ),
        ),
        const SizedBox(height: 16),
        Text(
          mainMenu
              ? 'Find your next story in the streets of Harare.'
              : 'Take a breath. The city can wait.',
          style: const TextStyle(
            color: Color(0xFFACC1BE),
            fontSize: 15,
            height: 1.5,
          ),
        ),
        const SizedBox(height: 26),
        Container(
          padding: const EdgeInsets.fromLTRB(16, 4, 4, 4),
          decoration: const BoxDecoration(
            border: Border(left: BorderSide(color: GameTheme.amber, width: 2)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const MenuEyebrow('WHAT NEXT?'),
              const SizedBox(height: 8),
              Text(
                guidance.objective,
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w700,
                  height: 1.3,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                guidance.instruction,
                style: const TextStyle(
                  color: Color(0xFFC0D4D0),
                  fontSize: 14,
                  height: 1.5,
                ),
              ),
              const SizedBox(height: 12),
              Text(
                guidance.availability,
                style: const TextStyle(
                  color: GameTheme.amber,
                  fontSize: 12,
                  height: 1.4,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 26),
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

  Widget _actions(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      MenuReveal(
        order: 1,
        child: MenuActionCard(
          icon: Icons.play_arrow_rounded,
          title: ready
              ? (needsRestart
                    ? 'Restart encounter'
                    : (mainMenu ? 'Continue game' : 'Resume game'))
              : 'City is loading…',
          subtitle: 'Back to the streets',
          primary: true,
          onTap: ready
              ? () => onAction(needsRestart ? 'restart' : 'resume')
              : null,
        ),
      ),
      const SizedBox(height: 12),
      MenuReveal(
        order: 2,
        child: MenuActionCard(
          icon: Icons.person_outline_rounded,
          title: 'Change character',
          subtitle: characterReady
              ? 'Your look. Your loadout.'
              : 'Waiting for your saved character…',
          onTap: characterReady ? () => onAction('character') : null,
        ),
      ),
      const SizedBox(height: 10),
      MenuReveal(
        order: 3,
        child: _DifficultySelector(
          selected: difficulty,
          enabled: ready,
          onSelected: (level) => onAction('difficulty_${level.toLowerCase()}'),
        ),
      ),
      const SizedBox(height: 10),
      MenuReveal(
        order: 4,
        child: MenuActionCard(
          icon: Icons.explore_outlined,
          title: 'Controls & tips',
          subtitle: 'Move with confidence. Make every shot count.',
          onTap: () => onAction('help'),
        ),
      ),
      if (!mainMenu) ...[
        const SizedBox(height: 10),
        MenuReveal(
          order: 4,
          child: MenuActionCard(
            icon: Icons.tune_rounded,
            title: 'Control settings',
            subtitle: 'Button layout, aim & sensitivity',
            onTap: ready ? () => onAction('controls') : null,
          ),
        ),
        const SizedBox(height: 10),
        Theme(
          data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
          child: ExpansionTile(
            tilePadding: const EdgeInsets.symmetric(horizontal: 16),
            title: const Text(
              'Graphics',
              style: TextStyle(fontSize: 15, fontWeight: FontWeight.w700),
            ),
            leading: const Icon(Icons.graphic_eq_rounded, size: 22),
            children: [
              ListTile(
                title: const Text('Balanced graphics'),
                subtitle: const Text('Lower rendering cost'),
                enabled: ready,
                onTap: ready ? () => onAction('graphics_balanced') : null,
              ),
              const SizedBox(height: 6),
              ListTile(
                title: const Text('High graphics'),
                subtitle: const Text('Sharper image · more GPU use'),
                enabled: ready,
                onTap: ready ? () => onAction('graphics_high') : null,
              ),
            ],
          ),
        ),
      ],
      const SizedBox(height: 12),
      Wrap(
        spacing: 10,
        runSpacing: 8,
        children: [
          if (!mainMenu)
            OutlinedButton.icon(
              onPressed: () => onAction('leave'),
              icon: const Icon(Icons.west_rounded, size: 18),
              label: const Text('Leave city'),
            ),
          TextButton.icon(
            onPressed: () => onAction('quit'),
            icon: const Icon(Icons.power_settings_new, size: 18),
            label: const Text('Quit game'),
          ),
          TextButton.icon(
            onPressed: () => onAction('privacy'),
            icon: const Icon(Icons.privacy_tip_outlined, size: 18),
            label: const Text('Privacy choices'),
          ),
        ],
      ),
    ],
  );
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
    required this.selected,
    required this.enabled,
    required this.onSelected,
  });

  final String selected;
  final bool enabled;
  final ValueChanged<String> onSelected;

  static const levels = <String>['Beginner', 'Intermediate', 'Expert'];

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(16),
    decoration: BoxDecoration(
      color: const Color(0xCC102326),
      borderRadius: BorderRadius.circular(16),
      border: Border.all(color: const Color(0xFF2B4848)),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Row(
          children: [
            Icon(Icons.sports_martial_arts_rounded, size: 21),
            SizedBox(width: 10),
            Expanded(
              child: Text(
                'Game level',
                style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800),
              ),
            ),
          ],
        ),
        const SizedBox(height: 6),
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
        const SizedBox(height: 12),
        LayoutBuilder(
          builder: (context, constraints) {
            final narrow = constraints.maxWidth < 420;
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
    required this.onTap,
  });

  final String label;
  final bool selected, enabled;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: selected ? const Color(0xFF33504A) : const Color(0xFF172D30),
    borderRadius: BorderRadius.circular(10),
    child: InkWell(
      onTap: enabled ? onTap : null,
      borderRadius: BorderRadius.circular(10),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(10),
          border: Border.all(
            color: selected ? GameTheme.amber : const Color(0xFF365052),
          ),
        ),
        child: Row(
          children: [
            Icon(
              selected
                  ? Icons.radio_button_checked
                  : Icons.radio_button_unchecked,
              size: 18,
              color: selected ? GameTheme.amber : const Color(0xFF8FA9A6),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                label,
                maxLines: 2,
                style: TextStyle(
                  color: enabled ? GameTheme.ivory : const Color(0xFF718583),
                  fontWeight: selected ? FontWeight.w800 : FontWeight.w600,
                ),
              ),
            ),
          ],
        ),
      ),
    ),
  );
}
