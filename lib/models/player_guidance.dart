/// Read-only guidance from Unity; never grants progress or unlocks missions.
class PlayerGuidance {
  const PlayerGuidance({
    this.objective = 'Connecting to your city',
    this.instruction =
        'Your current objective will appear here when the city is ready.',
    this.availability = 'Story available: M01. M02–M30 are not playable yet.',
  });
  final String objective, instruction, availability;

  static PlayerGuidance? parse(Map<String, dynamic> payload) {
    if (payload['version'] != 1) return null;
    final objective = payload['objective'];
    final instruction = payload['instruction'];
    final availability = payload['availability'];
    if (objective is! String ||
        instruction is! String ||
        availability is! String ||
        objective.trim().isEmpty ||
        instruction.trim().isEmpty) {
      return null;
    }
    return PlayerGuidance(
      objective: objective,
      instruction: instruction,
      availability: availability,
    );
  }
}
