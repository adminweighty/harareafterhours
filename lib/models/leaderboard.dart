class LeaderboardEntry {
  const LeaderboardEntry({
    required this.rank,
    required this.playerName,
    required this.score,
    required this.xp,
    required this.completedMissions,
    required this.isCurrentPlayer,
  });

  final int rank;
  final String playerName;
  final int score;
  final int xp;
  final int completedMissions;
  final bool isCurrentPlayer;

  factory LeaderboardEntry.fromJson(Map<String, dynamic> json) {
    int number(String key) => (json[key] as num?)?.toInt() ?? 0;
    return LeaderboardEntry(
      rank: number('rank'),
      playerName: (json['playerName'] as String?)?.trim().isNotEmpty == true
          ? (json['playerName'] as String).trim()
          : 'Player',
      score: number('score'),
      xp: number('xp'),
      completedMissions: number('completedMissions'),
      isCurrentPlayer: json['isCurrentPlayer'] == true,
    );
  }
}

class LeaderboardResult {
  const LeaderboardResult({required this.entries, this.playerRank});

  final List<LeaderboardEntry> entries;
  final int? playerRank;

  factory LeaderboardResult.fromJson(Map<String, dynamic> json) {
    final rawEntries = json['entries'];
    final entries = <LeaderboardEntry>[];
    if (rawEntries is List) {
      for (final value in rawEntries) {
        if (value is Map) {
          entries.add(
            LeaderboardEntry.fromJson(Map<String, dynamic>.from(value)),
          );
        }
      }
    }
    return LeaderboardResult(
      entries: List<LeaderboardEntry>.unmodifiable(entries),
      playerRank: (json['playerRank'] as num?)?.toInt(),
    );
  }
}
