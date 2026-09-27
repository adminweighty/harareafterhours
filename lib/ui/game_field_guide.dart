import 'package:flutter/material.dart';
import 'game_menu_visuals.dart';
import 'game_theme.dart';

/// Short, searchable-by-category instructions matching the shipped controls.
class GameFieldGuide extends StatefulWidget {
  const GameFieldGuide({super.key});
  @override
  State<GameFieldGuide> createState() => _GameFieldGuideState();
}

class _GameFieldGuideState extends State<GameFieldGuide> {
  int _selected = 0;
  static const _sections = <(String, IconData, List<(String, String)>)>[
    (
      'Move & aim',
      Icons.open_with_rounded,
      [
        (
          'Walk straight. Turn deliberately.',
          'Hold ▲ WALK to move forward. ▼ BACK reverses without turning around. Left and right TURN rotate you and the camera. Hold WALK with a turn to steer. Slide your thumb between arrows to change direction.',
        ),
        (
          'Sprint only when you choose',
          'Hold SPRINT while walking forward; release it to walk. CROUCH while sprinting starts a slide. Otherwise it crouches. STAND or JUMP rises when there is room overhead.',
        ),
        (
          'Line up the shot',
          'Tap AIM for precision. Keep a visible enemy near the centre target. Solid walls and cover block bullets, even when a red marker is visible. Enemies can be hit at 50 metres and farther within weapon range.',
        ),
        (
          'Fire & reload',
          'Hold FIRE for the rifle; tap for each pistol shot. You can move while firing. Tap the weapon panel to switch equipment. RELOAD refills 30 rifle rounds or 12 pistol rounds; switching weapons does not refill the magazine.',
        ),
        (
          'Find your view',
          'Drag unused screen space to look around. The view button switches between the full-character camera and the closer camera. Menu → Control settings lets you move buttons and adjust size, opacity, look sensitivity and aim-on-fire.',
        ),
      ],
    ),
    (
      'Survive',
      Icons.shield_outlined,
      [
        (
          'Read the red markers',
          'Red markers identify hostiles. BEHIND COVER means the target is obstructed; reposition before firing. OFFSCREEN means turn to find them. Civilians give no combat points.',
        ),
        (
          'Break their attack',
          'PUNCH is quick. KICK has more reach and damage but recovers more slowly. Both interrupt attacks. Defeating a marked hostile awards 50 points once.',
        ),
        (
          'Recover in safety',
          'Watch the LIFE bar. Leave nearby hostiles and avoid damage for 12 seconds to begin recovery. At zero life, Restart encounter returns you to the safe corner and preserves mission progress and earned points.',
        ),
        (
          'Choose your effects',
          'Change character → Weapon feedback controls weapon sound, reduced effects and blood effects. Headwear is cosmetic; it does not provide armour.',
        ),
      ],
    ),
    (
      'Drive',
      Icons.directions_car_outlined,
      [
        (
          'Get behind the wheel',
          'Stand beside a stopped car. USE takes the driver seat; RIDE takes a passenger seat. The controls change when you enter.',
        ),
        (
          'Accelerate, steer, stop',
          'Hold the accelerator to gain speed and use left/right to steer. Brake before tight corners and before exiting. A passenger cannot accelerate or steer.',
        ),
        (
          'Find the circuit',
          'Drive north along the central avenue. Stop at the circuit start and tap RACE.',
        ),
      ],
    ),
    (
      'Your story',
      Icons.route_outlined,
      [
        (
          'Follow the objective',
          'Use the objective and distance at the top of the screen. Approach a contact, then tap TALK or USE when prompted. M01 begins with Rudo and the delivery.',
        ),
        (
          'Bring them home',
          'Enter the signed doors at City Grocer, Sadza Kitchen, The Velvet Room or Private Lounge. At City Grocer, stop both robbers, approach the shopkeeper and tap RESCUE. Walk slowly together to the marked EXIT, then tap EXIT for the one-time rescue reward.',
        ),
        (
          'Keep exploring',
          'Only M01 is currently playable. M02–M30, including The Velvet Room story, are not available yet. Completing M01 opens free roam. The completion card reports the score and rewards confirmed by the city; previously paid rewards are not paid again.',
        ),
        (
          'Take a break',
          'Menu pauses the city. Leave city keeps this session paused. Quit game ends gameplay and audio. On iPhone, use the Home gesture to close the app. Reopening uses saved progress.',
        ),
      ],
    ),
  ];

  @override
  Widget build(BuildContext context) => SafeArea(
    child: SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(24, 8, 24, 28),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    MenuEyebrow('THE STREET GUIDE'),
                    SizedBox(height: 8),
                    Text(
                      'Controls & tips',
                      style: TextStyle(
                        fontSize: 28,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
              ),
              IconButton(
                tooltip: 'Close guide',
                onPressed: () => Navigator.pop(context),
                icon: const Icon(Icons.close),
              ),
            ],
          ),
          const SizedBox(height: 20),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: List.generate(
              _sections.length,
              (i) => ChoiceChip(
                selected: _selected == i,
                showCheckmark: false,
                avatar: Icon(_sections[i].$2, size: 17),
                label: Text(_sections[i].$1),
                onSelected: (_) => setState(() => _selected = i),
              ),
            ),
          ),
          const SizedBox(height: 20),
          AnimatedSwitcher(
            duration: MediaQuery.disableAnimationsOf(context)
                ? Duration.zero
                : const Duration(milliseconds: 180),
            child: Column(
              key: ValueKey(_selected),
              children: [
                for (final tip in _sections[_selected].$3)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: Container(
                      padding: const EdgeInsets.all(18),
                      decoration: BoxDecoration(
                        color: const Color(0xFF192D31),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Text(
                            tip.$1,
                            style: const TextStyle(
                              color: GameTheme.amber,
                              fontWeight: FontWeight.w700,
                              fontSize: 16,
                            ),
                          ),
                          const SizedBox(height: 8),
                          Text(
                            tip.$2,
                            style: const TextStyle(
                              height: 1.55,
                              color: Color(0xFFD4E0DE),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
              ],
            ),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Got it'),
          ),
        ],
      ),
    ),
  );
}
