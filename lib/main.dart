import 'dart:async';

import 'package:flutter/material.dart';

import 'services/admob_service.dart';
import 'ui/city_game.dart';
import 'ui/game_theme.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  unawaited(AdMobService.instance.initialize());
  runApp(const HarareAfterHoursApp());
}

class HarareAfterHoursApp extends StatelessWidget {
  const HarareAfterHoursApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Harare After Hours',
      debugShowCheckedModeBanner: false,
      theme: GameTheme.dark,
      home: const CityGame(),
    );
  }
}
