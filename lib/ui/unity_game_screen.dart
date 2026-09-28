import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_unity_widget_2/flutter_unity_widget_2.dart';

import '../models/character_profile.dart';
import '../models/player_guidance.dart';
import '../models/campaign_models.dart';
import '../data/campaign_data.dart';
import '../services/admob_service.dart';
import 'admob_banner.dart';
import 'game_session_menu.dart';
import 'game_launch_overlay.dart';
import 'game_theme.dart';
import 'game_menu_visuals.dart';
import 'game_field_guide.dart';

@visibleForTesting
MissionResult missionResultFromUnityPayload({
  required Mission mission,
  required Map<String, dynamic> payload,
  required bool isReplay,
}) {
  final serviceRoute = payload['openingRoute'] == 'service';
  final hasAuthoredChoice = payload['choiceLabel'] is String;
  return MissionResult(
    score: (payload['score'] as num?)?.toInt() ?? 0,
    stars: (payload['stars'] as num?)?.toInt() ?? 1,
    coinPayout: isReplay ? 0 : (payload['coins'] as num?)?.toInt() ?? 0,
    xpPayout: isReplay ? 0 : (payload['xp'] as num?)?.toInt() ?? 0,
    impact: hasAuthoredChoice
        ? StoryImpact(
            label: payload['choiceLabel'] as String,
            crewTrust: (payload['crewTrust'] as num?)?.toInt() ?? 0,
            communitySupport:
                (payload['communitySupport'] as num?)?.toInt() ?? 0,
          )
        : serviceRoute
        ? mission.choice.secondaryImpact
        : mission.choice.primaryImpact,
    choiceLabel: hasAuthoredChoice
        ? payload['choiceLabel'] as String
        : serviceRoute
        ? mission.choice.secondaryLabel
        : mission.choice.primaryLabel,
    wasReplay: isReplay,
  );
}

class UnityGameplayScreen extends StatefulWidget {
  const UnityGameplayScreen({
    super.key,
    required this.mission,
    required this.isReplay,
    required this.character,
    this.stayInCity = false,
    this.onCustomize,
    this.onLeaderboard,
    this.onCharacterRestored,
    this.onMissionCompleted,
  });

  final Mission mission;
  final bool isReplay;
  final CharacterProfile character;
  final bool stayInCity;
  final Future<void> Function()? onCustomize;
  final Future<void> Function()? onLeaderboard;
  final ValueChanged<CharacterProfile>? onCharacterRestored;
  final void Function(Mission mission, MissionResult result)?
  onMissionCompleted;

  @override
  State<UnityGameplayScreen> createState() => _UnityGameplayScreenState();
}

class _UnityGameplayScreenState extends State<UnityGameplayScreen>
    with WidgetsBindingObserver {
  // Claim each pointer inside the native game immediately, including concurrent
  // joystick/camera touches. Flutter's pause overlay stays above this view.
  static final _gameGestures = <Factory<OneSequenceGestureRecognizer>>{
    Factory<OneSequenceGestureRecognizer>(EagerGestureRecognizer.new),
  };
  UnityWidgetController? _unity;
  Timer? _readinessPoll;
  Timer? _resultTimer;
  final _guidance = ValueNotifier<PlayerGuidance>(const PlayerGuidance());
  final _audioRevision = ValueNotifier<int>(0);
  MissionResult? _latestResult;
  bool _ready = false;
  bool _pauseOpen = false;
  bool _profileRequested = false;
  bool _profileRestored = false;
  bool _atMainMenu = false;
  bool _foreground = true;
  bool _gameOver = false;
  bool _sessionEnded = false;
  bool _missionSelected = false;
  String _difficulty = 'Intermediate';
  bool _soundMuted = false;
  bool _shuffleSoundtrack = true;
  String _currentTrack = 'Harare night mix';
  final Set<int> _completedResultSequences = <int>{};
  int _launchVersion = 0;
  Completer<void>? _sessionEndAck;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    // The Unity HUD is laid out as a two-thumb landscape controller. Lock the
    // gameplay route so iOS never compresses the D-pad and combat stack into a
    // portrait viewport while the rest of the Flutter campaign remains portrait.
    unawaited(
      SystemChrome.setPreferredOrientations(const <DeviceOrientation>[
        DeviceOrientation.landscapeLeft,
        DeviceOrientation.landscapeRight,
      ]),
    );
  }

  @override
  void didUpdateWidget(UnityGameplayScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.mission.missionId == widget.mission.missionId) return;
    _missionSelected = false;
    if (_ready) unawaited(_syncMission());
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    _foreground = state == AppLifecycleState.resumed;
    if (_sessionEnded) {
      unawaited(_unity?.pause());
      return;
    }
    if (!_foreground) {
      unawaited(_post('pause'));
    } else if (_ready && !_pauseOpen && !_atMainMenu) {
      unawaited(_showPause());
    }
  }

  bool get _isSupportedHost =>
      !kIsWeb &&
      (defaultTargetPlatform == TargetPlatform.iOS ||
          defaultTargetPlatform == TargetPlatform.android);

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _readinessPoll?.cancel();
    _resultTimer?.cancel();
    _guidance.dispose();
    _audioRevision.dispose();
    _unity?.dispose();
    unawaited(
      SystemChrome.setPreferredOrientations(const <DeviceOrientation>[
        DeviceOrientation.portraitUp,
      ]),
    );
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (!_isSupportedHost) {
      return const Scaffold(
        body: Center(
          child: Padding(
            padding: EdgeInsets.all(24),
            child: Text(
              'Play Harare on iPhone or Android.',
              textAlign: TextAlign.center,
            ),
          ),
        ),
      );
    }
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, result) {
        if (!didPop) {
          if (_atMainMenu) {
            unawaited(_mainMenuAction('quit'));
          } else {
            unawaited(_showPause());
          }
        }
      },
      child: Scaffold(
        backgroundColor: const Color(0xFF07110F),
        body: Stack(
          fit: StackFit.expand,
          children: [
            AbsorbPointer(
              absorbing: _sessionEnded || _atMainMenu || _gameOver,
              child: UnityWidget(
                gestureRecognizers: _gameGestures,
                onUnityCreated: _onUnityCreated,
                onUnityMessage: _onUnityMessage,
                fullscreen: true,
                runImmediately: true,
                unloadOnDispose: false,
                useAndroidViewSurface: true,
                placeholder: const ColoredBox(color: Color(0xFF07110F)),
              ),
            ),
            // Unity supplies points, objectives and controls: no duplicated HUD.
            GameLaunchOverlay(
              key: ValueKey(_launchVersion),
              ready: _ready,
              onRetry: () => unawaited(_post('request_world_ready')),
            ),
            if (_latestResult != null &&
                !_atMainMenu &&
                !_gameOver &&
                !_sessionEnded)
              Positioned(
                top: 12,
                left: 100,
                right: 100,
                child: SafeArea(
                  child: IgnorePointer(
                    child: Center(
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 400),
                        child: Semantics(
                          liveRegion: true,
                          child: MissionSuccessCard(
                            key: ValueKey(_latestResult),
                            score: _latestResult!.score,
                            stars: _latestResult!.stars,
                            coins: _latestResult!.coinPayout,
                            xp: _latestResult!.xpPayout,
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            if (!_atMainMenu && !_gameOver && !_sessionEnded)
              SafeArea(
                child: Align(
                  alignment: Alignment.topRight,
                  child: Padding(
                    padding: const EdgeInsets.all(8),
                    child: FilledButton.tonalIcon(
                      onPressed: _showPause,
                      icon: const Icon(Icons.pause_rounded),
                      label: const Text('Menu'),
                      style: FilledButton.styleFrom(
                        minimumSize: const Size(48, 48),
                      ),
                    ),
                  ),
                ),
              ),
            if (_atMainMenu && !_sessionEnded)
              Material(
                color: const Color(0xFF07110F),
                child: ValueListenableBuilder<PlayerGuidance>(
                  valueListenable: _guidance,
                  builder: (_, guide, _) => ValueListenableBuilder<int>(
                    valueListenable: _audioRevision,
                    builder: (_, _, _) => GameSessionMenu(
                      guidance: guide,
                      missionTitle: widget.mission.title,
                      district: widget.mission.district,
                      mainMenu: true,
                      needsRestart: _gameOver,
                      ready: _ready,
                      characterReady:
                          _profileRestored && widget.onCustomize != null,
                      sponsor: const AdMobBanner(),
                      difficulty: _difficulty,
                      soundMuted: _soundMuted,
                      shuffleSoundtrack: _shuffleSoundtrack,
                      currentTrack: _currentTrack,
                      onAction: (action) => unawaited(_mainMenuAction(action)),
                    ),
                  ),
                ),
              ),
            if (_gameOver && !_atMainMenu)
              GameOverPanel(
                onRestart: () => _post('restart_encounter'),
                onMainMenu: () => setState(() => _atMainMenu = true),
              ),
            if (_sessionEnded)
              GameClosedPanel(onStart: () => unawaited(_startNewSession())),
          ],
        ),
      ),
    );
  }

  void _onUnityCreated(UnityWidgetController controller) {
    _unity = controller;
    if (_sessionEnded) {
      unawaited(controller.pause());
      return;
    }
    controller.resume();
    _pollReadiness();
  }

  void _pollReadiness() {
    _readinessPoll?.cancel();
    // Retry an idempotent request; elapsed time is not evidence of readiness.
    _readinessPoll = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted && !_ready) unawaited(_post('request_world_ready'));
      if (mounted && _ready && !_profileRestored) {
        unawaited(_post('request_character_profile'));
      }
    });
    unawaited(_post('request_world_ready'));
  }

  Future<void> _post(String type, [String payload = '']) async {
    await _unity?.postMessage(
      'FlutterBridge',
      'OnFlutterMessage',
      jsonEncode({'type': type, 'payload': payload}),
    );
  }

  Future<void> _syncCharacter() async {
    await _post('set_character_profile', jsonEncode(widget.character.toJson()));
    final portrait = widget.character.portraitBytes;
    if (portrait != null && portrait.isNotEmpty) {
      await _post('set_character_photo', base64Encode(portrait));
    }
  }

  Future<void> _syncMission() async {
    if (_missionSelected) return;
    _missionSelected = true;
    await _post(
      'set_active_mission',
      jsonEncode({
        'missionId': widget.mission.missionId,
        'level': widget.mission.level,
        'definitionVersion': widget.mission.definitionVersion,
        'isReplay': widget.isReplay,
        'unlocked': true,
        'title': widget.mission.title,
        'district': widget.mission.district,
        'activity': widget.mission.activity,
        'vehicle': widget.mission.vehicle,
        'coinReward': widget.mission.coinReward,
        'xpReward': widget.mission.xpReward,
        'objectives': widget.mission.objectives,
        'primaryLabel': widget.mission.choice.primaryLabel,
        'secondaryLabel': widget.mission.choice.secondaryLabel,
        'primaryTrust': widget.mission.choice.primaryImpact.crewTrust,
        'primaryCommunity':
            widget.mission.choice.primaryImpact.communitySupport,
        'secondaryTrust': widget.mission.choice.secondaryImpact.crewTrust,
        'secondaryCommunity':
            widget.mission.choice.secondaryImpact.communitySupport,
      }),
    );
  }

  void _onUnityMessage(dynamic rawMessage) {
    try {
      final event = jsonDecode(rawMessage.toString()) as Map<String, dynamic>;
      if (event['type'] == 'session_ended') {
        if (_sessionEndAck?.isCompleted == false) _sessionEndAck!.complete();
        return;
      }
      if (_sessionEnded) return;
      if (event['type'] == 'player_guidance' && mounted) {
        final payload =
            jsonDecode(event['payload'] as String) as Map<String, dynamic>;
        final guide = PlayerGuidance.parse(payload);
        if (guide != null) _guidance.value = guide;
      }
      if (event['type'] == 'open_guidance' && mounted) unawaited(_showPause());
      if (event['type'] == 'game_over' && mounted) {
        setState(() => _gameOver = true);
      }
      if (event['type'] == 'encounter_restarted' && mounted) {
        setState(() {
          _gameOver = false;
          _atMainMenu = false;
        });
        if (!_foreground) unawaited(_post('pause'));
      }
      if (event['type'] == 'world_ready' && mounted) {
        final payload =
            jsonDecode(event['payload'] as String) as Map<String, dynamic>;
        final reportedDifficulty = payload['difficulty'] as String?;
        setState(() {
          _ready = true;
          if (reportedDifficulty != null) _difficulty = reportedDifficulty;
        });
        unawaited(_syncMission());
        unawaited(_post('request_guidance'));
        unawaited(_post('request_audio_state'));
        if (!_profileRequested) {
          _profileRequested = true;
          // Unity restores the local loadout first. Never overwrite it with
          // Flutter's default profile during the initial bridge handshake.
          unawaited(_post('request_character_profile'));
          if (!_pauseOpen && !_atMainMenu && _foreground) {
            unawaited(_post('resume'));
          }
        }
      }
      if (event['type'] == 'difficulty_changed' && mounted) {
        final payload =
            jsonDecode(event['payload'] as String) as Map<String, dynamic>;
        final reportedDifficulty = payload['difficulty'] as String?;
        if (reportedDifficulty != null) {
          setState(() => _difficulty = reportedDifficulty);
        }
      }
      if (event['type'] == 'audio_state' && mounted) {
        final payload =
            jsonDecode(event['payload'] as String) as Map<String, dynamic>;
        _soundMuted = payload['muted'] == true;
        _shuffleSoundtrack = payload['shuffle'] != false;
        final track = payload['track'] as String?;
        if (track != null && track.trim().isNotEmpty) _currentTrack = track;
        _audioRevision.value++;
      }
      if (event['type'] == 'weapon_changed' && mounted) {
        _profileRestored = false;
        unawaited(_post('request_character_profile'));
      }
      if (event['type'] == 'character_profile' &&
          mounted &&
          !_profileRestored) {
        final profile = CharacterProfile.fromJson(
          jsonDecode(event['payload'] as String) as Map<String, dynamic>,
        );
        widget.onCharacterRestored?.call(profile);
        setState(() => _profileRestored = true);
        _readinessPoll?.cancel();
      }
      if (event['type'] == 'mission_complete' && mounted) {
        final payload =
            jsonDecode(event['payload'] as String) as Map<String, dynamic>;
        final missionId = payload['missionId'] as String?;
        final matching = kMissions.where((item) => item.missionId == missionId);
        if (matching.isNotEmpty) {
          final completedMission = matching.first;
          final sequence = (payload['sequence'] as num?)?.toInt() ?? 0;
          if (_completedResultSequences.add(sequence)) {
            final result = missionResultFromUnityPayload(
              mission: completedMission,
              payload: payload,
              isReplay: widget.isReplay,
            );
            widget.onMissionCompleted?.call(completedMission, result);
            _resultTimer?.cancel();
            setState(() => _latestResult = result);
            _resultTimer = Timer(const Duration(seconds: 6), () {
              if (mounted) setState(() => _latestResult = null);
            });
          }
        }
      }
      if (event['type'] == 'mission_unavailable' && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Complete the previous mission first. Your current checkpoint is still saved.',
            ),
          ),
        );
      }
    } on FormatException {
      // Other Unity packages can publish unrelated messages.
    } on TypeError {
      // Ignore unrelated payload shapes.
    }
  }

  Future<void> _showPause() async {
    if (_pauseOpen || _sessionEnded || _gameOver || !mounted) return;
    _pauseOpen = true;
    try {
      await _post('pause');
      if (!mounted) return;
      final action = await showGeneralDialog<String>(
        context: context,
        barrierLabel: 'Paused city',
        transitionDuration: MediaQuery.disableAnimationsOf(context)
            ? Duration.zero
            : const Duration(milliseconds: 220),
        pageBuilder: (menuContext, _, _) => Material(
          color: GameTheme.ink,
          child: ValueListenableBuilder<PlayerGuidance>(
            valueListenable: _guidance,
            builder: (_, guide, _) => ValueListenableBuilder<int>(
              valueListenable: _audioRevision,
              builder: (_, _, _) => GameSessionMenu(
                guidance: guide,
                mainMenu: false,
                missionTitle: widget.mission.title,
                district: widget.mission.district,
                ready: _ready,
                characterReady: _profileRestored && widget.onCustomize != null,
                sponsor: const AdMobBanner(),
                difficulty: _difficulty,
                soundMuted: _soundMuted,
                shuffleSoundtrack: _shuffleSoundtrack,
                currentTrack: _currentTrack,
                onAction: (value) {
                  if (_isAudioAction(value)) {
                    unawaited(_handleAudioAction(value));
                  } else {
                    Navigator.pop(menuContext, value);
                  }
                },
              ),
            ),
          ),
        ),
        transitionBuilder: (_, animation, _, child) =>
            FadeTransition(opacity: animation, child: child),
      );
      if (!mounted) return;
      if (action == 'leave') {
        final leave = await showModalBottomSheet<bool>(
          context: context,
          isScrollControlled: true,
          useSafeArea: true,
          builder: (context) => const GameActionSheet(
            title: 'Leave the city?',
            message:
                'The city stays paused in the main menu. Continue game returns to this session without restarting.',
            confirmLabel: 'Leave city',
            cancelLabel: 'Keep playing',
          ),
        );
        if (leave == true && mounted) setState(() => _atMainMenu = true);
      }
      if (action == 'quit' && mounted) await _quitGame();
      if (action == 'help' && mounted) await _showControls();
      if (action == 'controls' && mounted) await _post('edit_controls');
      if (action == 'graphics_balanced' || action == 'graphics_high') {
        await _post(
          'set_graphics',
          action == 'graphics_high' ? 'High' : 'Balanced',
        );
      }
      if (action?.startsWith('difficulty_') == true) {
        await _setDifficulty(action!.substring('difficulty_'.length));
      }
      if (action == 'character' && mounted) await _customize();
      if (action == 'leaderboard' && mounted) {
        await widget.onLeaderboard?.call();
      }
      if (action == 'privacy') {
        await AdMobService.instance.showPrivacyOptions();
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Could not complete that action. Please try again.'),
          ),
        );
      }
    } finally {
      _pauseOpen = false;
      if (mounted &&
          !_atMainMenu &&
          !_gameOver &&
          !_sessionEnded &&
          _foreground) {
        await _post('resume');
      }
    }
  }

  Future<void> _customize() async {
    await widget.onCustomize?.call();
    // Let the Stacked rebuild deliver the latest immutable profile first.
    await WidgetsBinding.instance.endOfFrame;
    if (mounted) {
      await _syncCharacter();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Character updated'),
            duration: Duration(seconds: 2),
          ),
        );
      }
    }
  }

  Future<void> _mainMenuAction(String action) async {
    if (_pauseOpen) return;
    _pauseOpen = true;
    try {
      if (action == 'restart' && _ready && _gameOver) {
        await _post('restart_encounter');
      } else if (action == 'resume' && _ready && !_gameOver) {
        await _post('resume');
        if (mounted) setState(() => _atMainMenu = false);
      } else if (action == 'character' && _profileRestored) {
        await _customize();
      } else if (action == 'help') {
        await _showControls();
      } else if (action == 'leaderboard') {
        await widget.onLeaderboard?.call();
      } else if (action == 'privacy') {
        await AdMobService.instance.showPrivacyOptions();
      } else if (_isAudioAction(action)) {
        await _handleAudioAction(action);
      } else if (action.startsWith('difficulty_')) {
        await _setDifficulty(action.substring('difficulty_'.length));
      } else if (action == 'quit') {
        await _quitGame();
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Could not complete that action. Please try again.'),
          ),
        );
      }
    } finally {
      _pauseOpen = false;
    }
  }

  bool _isAudioAction(String action) =>
      action == 'audio_toggle' ||
      action == 'audio_shuffle_toggle' ||
      action == 'audio_randomize';

  Future<void> _handleAudioAction(String action) async {
    if (!_ready) return;
    switch (action) {
      case 'audio_toggle':
        return _post('set_audio_muted', (!_soundMuted).toString());
      case 'audio_shuffle_toggle':
        return _post('set_audio_shuffle', (!_shuffleSoundtrack).toString());
      case 'audio_randomize':
        return _post('randomize_soundtrack');
    }
  }

  Future<void> _setDifficulty(String value) async {
    final normalized = switch (value.toLowerCase()) {
      'beginner' => 'Beginner',
      'expert' => 'Expert',
      _ => 'Intermediate',
    };
    if (mounted) setState(() => _difficulty = normalized);
    await _post('set_difficulty', normalized);
  }

  Future<void> _quitGame() async {
    if (_sessionEnded) return;
    final android = !kIsWeb && defaultTargetPlatform == TargetPlatform.android;
    final confirmed = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (context) => GameActionSheet(
        title: 'Quit game?',
        confirmLabel: 'Quit game',
        cancelLabel: 'Stay in game',
        destructive: true,
        message: android
            ? 'Close the game screen. Your saved progress stays; reopening starts a new city session.'
            : 'End this city session and stop gameplay and audio. Your saved progress stays. iPhone apps close using the Home gesture; this button will show Game closed.',
      ),
    );
    if (confirmed != true || !mounted) return;
    if (_ready) {
      _sessionEndAck = Completer<void>();
      await _post('end_session');
      await _sessionEndAck!.future.timeout(const Duration(seconds: 5));
    }
    await _unity?.pause();
    if (!mounted) return;
    _readinessPoll?.cancel();
    setState(() {
      _atMainMenu = true;
      _sessionEnded = true;
    });
    if (android) await SystemNavigator.pop();
  }

  Future<void> _startNewSession() async {
    if (_pauseOpen || !_sessionEnded) return;
    _pauseOpen = true;
    final hadWorld = _ready;
    try {
      await _unity?.resume();
      if (!mounted) return;
      setState(() {
        _sessionEnded = false;
        _atMainMenu = false;
        _gameOver = false;
        _ready = false;
        _launchVersion++;
        _profileRequested = false;
        _profileRestored = false;
        _missionSelected = false;
        _completedResultSequences.clear();
        _guidance.value = const PlayerGuidance();
      });
      if (hadWorld) await _post('restart_session');
      _pollReadiness();
    } catch (_) {
      if (mounted) {
        setState(() {
          _sessionEnded = true;
          _atMainMenu = true;
        });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Could not start the city. Please try again.'),
          ),
        );
      }
    } finally {
      _pauseOpen = false;
    }
  }

  Future<void> _showControls() => showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    builder: (context) => const GameFieldGuide(),
  );
}
