import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../models/campaign_models.dart';

/// Lightweight entry screen: preview first, one tap to play, details on demand.
class MissionPreview extends StatelessWidget {
  const MissionPreview({
    super.key,
    required this.mission,
    required this.completed,
    required this.onStart,
    required this.onCharacter,
    required this.onMissions,
    required this.onDetails,
  });

  final Mission mission;
  final bool completed;
  final VoidCallback onStart, onCharacter, onMissions, onDetails;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) => SingleChildScrollView(
        padding: const EdgeInsets.all(12),
        child: ConstrainedBox(
          constraints: BoxConstraints(
            minHeight: math.max(560, constraints.maxHeight - 24),
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(24),
            child: Stack(
              children: [
                Positioned.fill(
                  child: Image.asset(
                    'assets/previews/harare-city.png',
                    fit: BoxFit.cover,
                    alignment: const Alignment(.35, 0),
                    excludeFromSemantics: true,
                    errorBuilder: (_, _, _) => const ColoredBox(
                      color: Color(0xFF213C35),
                      child: Center(child: Icon(Icons.location_city, size: 96)),
                    ),
                  ),
                ),
                const Positioned.fill(
                  child: DecoratedBox(
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topCenter,
                        end: Alignment.bottomCenter,
                        stops: [0, .28, .57, 1],
                        colors: [
                          Color(0xA607110F),
                          Color(0x1007110F),
                          Color(0xDF07110F),
                          Color(0xFF07110F),
                        ],
                      ),
                    ),
                  ),
                ),
                ConstrainedBox(
                  constraints: BoxConstraints(
                    minHeight: math.max(560, constraints.maxHeight - 24),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(22),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'HARARE / AFTER HOURS',
                          style: TextStyle(
                            fontWeight: FontWeight.w900,
                            fontSize: 12,
                            letterSpacing: 2,
                          ),
                        ),
                        const SizedBox(height: 140),
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'MISSION ${mission.level.toString().padLeft(2, '0')}',
                              style: TextStyle(
                                color: mission.accent,
                                fontWeight: FontWeight.w900,
                                letterSpacing: 2,
                              ),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              mission.title,
                              style: const TextStyle(
                                fontSize: 36,
                                height: 1.05,
                                fontWeight: FontWeight.w900,
                              ),
                            ),
                            const SizedBox(height: 12),
                            Wrap(
                              spacing: 14,
                              runSpacing: 8,
                              children: [
                                _Detail(Icons.place_outlined, mission.district),
                                _Detail(mission.icon, mission.activity),
                                _Detail(Icons.schedule, mission.duration),
                              ],
                            ),
                            const SizedBox(height: 18),
                            Text(
                              mission.objectives.isEmpty
                                  ? mission.activity
                                  : mission.objectives.first,
                              style: const TextStyle(fontSize: 16, height: 1.4),
                            ),
                            const SizedBox(height: 22),
                            SizedBox(
                              width: double.infinity,
                              child: FilledButton.icon(
                                key: const Key('start-mission'),
                                onPressed: onStart,
                                icon: Icon(
                                  completed
                                      ? Icons.replay
                                      : Icons.play_arrow_rounded,
                                ),
                                label: Text(
                                  completed
                                      ? 'Replay mission'
                                      : 'Start mission',
                                ),
                                style: FilledButton.styleFrom(
                                  backgroundColor: mission.accent,
                                  foregroundColor: const Color(0xFF07110F),
                                  minimumSize: const Size.fromHeight(56),
                                  textStyle: const TextStyle(
                                    fontSize: 17,
                                    fontWeight: FontWeight.w900,
                                  ),
                                ),
                              ),
                            ),
                            const SizedBox(height: 8),
                            Wrap(
                              spacing: 8,
                              runSpacing: 4,
                              children: [
                                OutlinedButton.icon(
                                  onPressed: onCharacter,
                                  icon: const Icon(Icons.person_outline),
                                  label: const Text('Character'),
                                ),
                                OutlinedButton.icon(
                                  onPressed: onMissions,
                                  icon: const Icon(Icons.grid_view_rounded),
                                  label: const Text('Missions'),
                                ),
                                TextButton(
                                  onPressed: onDetails,
                                  child: const Text('Details'),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ],
                    ),
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

class _Detail extends StatelessWidget {
  const _Detail(this.icon, this.label);
  final IconData icon;
  final String label;
  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      Icon(icon, size: 16, color: const Color(0xFFC7F36B)),
      const SizedBox(width: 5),
      Text(label, style: const TextStyle(color: Color(0xFFD3DDD7))),
    ],
  );
}
