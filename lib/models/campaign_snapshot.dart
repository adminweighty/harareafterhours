class CampaignSnapshot {
  const CampaignSnapshot({
    required this.profileId,
    required this.playerName,
    required this.wallet,
    required this.xp,
    required this.crewTrust,
    required this.communitySupport,
    required this.completedLevels,
    required this.bestScores,
    required this.lastChoice,
    this.updatedAt,
  });

  final String profileId;
  final String playerName;
  final int wallet;
  final int xp;
  final int crewTrust;
  final int communitySupport;
  final List<int> completedLevels;
  final Map<int, int> bestScores;
  final String lastChoice;
  final String? updatedAt;

  factory CampaignSnapshot.fromJson(Map<String, dynamic> json) {
    final List<int> completed = <int>[];
    final Object? rawCompleted = json['completedLevels'];
    if (rawCompleted is List) {
      for (final Object? level in rawCompleted) {
        final int? parsed = _tryInt(level);
        if (parsed != null) completed.add(parsed);
      }
    }

    final Map<int, int> scores = <int, int>{};
    final Object? rawScores = json['bestScores'];
    if (rawScores is Map) {
      for (final MapEntry<Object?, Object?> entry in rawScores.entries) {
        final int? level = _tryInt(entry.key);
        final int? score = _tryInt(entry.value);
        if (level != null && score != null) scores[level] = score;
      }
    }

    return CampaignSnapshot(
      profileId: json['profileId'] as String? ?? '',
      playerName: json['playerName'] as String? ?? 'Player',
      wallet: _intOrZero(json['wallet']),
      xp: _intOrZero(json['xp']),
      crewTrust: _intOrZero(json['crewTrust']),
      communitySupport: _intOrZero(json['communitySupport']),
      completedLevels: List<int>.unmodifiable(completed),
      bestScores: Map<int, int>.unmodifiable(scores),
      lastChoice: json['lastChoice'] as String? ?? '',
      updatedAt: json['updatedAt'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'profileId': profileId,
      'playerName': playerName,
      'wallet': wallet,
      'xp': xp,
      'crewTrust': crewTrust,
      'communitySupport': communitySupport,
      'completedLevels': completedLevels,
      'bestScores': bestScores.map(
        (int level, int score) =>
            MapEntry<String, int>(level.toString(), score),
      ),
      'lastChoice': lastChoice,
      if (updatedAt != null) 'updatedAt': updatedAt,
    };
  }

  static int _intOrZero(Object? value) => _tryInt(value) ?? 0;

  static int? _tryInt(Object? value) {
    if (value is int) return value;
    if (value is num) return value.toInt();
    return int.tryParse(value?.toString() ?? '');
  }
}
