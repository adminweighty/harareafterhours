import 'package:flutter/material.dart';

class StoryImpact {
  const StoryImpact({
    required this.label,
    this.crewTrust = 0,
    this.communitySupport = 0,
  });

  final String label;
  final int crewTrust;
  final int communitySupport;
}

class MissionChoice {
  const MissionChoice({
    required this.prompt,
    required this.primaryLabel,
    required this.secondaryLabel,
    required this.primaryImpact,
    required this.secondaryImpact,
  });

  final String prompt;
  final String primaryLabel;
  final String secondaryLabel;
  final StoryImpact primaryImpact;
  final StoryImpact secondaryImpact;
}

class Mission {
  const Mission({
    required this.level,
    required this.chapter,
    required this.title,
    required this.district,
    required this.timeOfDay,
    required this.duration,
    required this.coinReward,
    required this.xpReward,
    required this.icon,
    required this.accent,
    required this.activity,
    required this.vehicle,
    required this.briefing,
    required this.objectives,
    required this.optionalObjective,
    required this.choice,
    required this.musicCue,
    this.adFree = false,
  });

  final int level;
  final String chapter;
  final String title;
  final String district;
  final String timeOfDay;
  final String duration;
  final int coinReward;
  final int xpReward;
  final IconData icon;
  final Color accent;
  final String activity;
  final String vehicle;
  final String briefing;
  final List<String> objectives;
  final String optionalObjective;
  final MissionChoice choice;
  final String musicCue;
  final bool adFree;

  String get missionId => 'M${level.toString().padLeft(2, '0')}';
  int get definitionVersion => 31;
}

class MissionResult {
  const MissionResult({
    required this.score,
    required this.stars,
    required this.coinPayout,
    required this.xpPayout,
    required this.impact,
    required this.choiceLabel,
    required this.wasReplay,
  });

  final int score;
  final int stars;
  final int coinPayout;
  final int xpPayout;
  final StoryImpact impact;
  final String choiceLabel;
  final bool wasReplay;
}

class District {
  const District({
    required this.name,
    required this.tagline,
    required this.description,
    required this.accent,
    required this.levels,
  });

  final String name;
  final String tagline;
  final String description;
  final Color accent;
  final List<int> levels;
}
