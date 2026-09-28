import 'dart:async';

import 'package:flutter/material.dart';

import '../models/leaderboard.dart';
import '../viewmodels/campaign_viewmodel.dart';
import 'game_menu_visuals.dart';
import 'game_theme.dart';

class LeaderboardSheet extends StatefulWidget {
  const LeaderboardSheet({super.key, required this.model});

  final CampaignViewModel model;

  @override
  State<LeaderboardSheet> createState() => _LeaderboardSheetState();
}

class _LeaderboardSheetState extends State<LeaderboardSheet> {
  late final TextEditingController _nameController;
  late final TextEditingController _emailController;
  String? _formError;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(text: widget.model.playerName);
    _emailController = TextEditingController(text: widget.model.playerEmail);
    unawaited(_load());
  }

  Future<void> _load() async {
    await widget.model.initialize();
    if (!mounted) return;
    if (_nameController.text == 'Tari' && widget.model.playerName != 'Tari') {
      _nameController.text = widget.model.playerName;
    }
    if (_emailController.text.isEmpty && widget.model.playerEmail.isNotEmpty) {
      _emailController.text = widget.model.playerEmail;
    }
    await widget.model.refreshLeaderboard();
  }

  @override
  void dispose() {
    _nameController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _publish() async {
    FocusScope.of(context).unfocus();
    final error = await widget.model.updatePlayerIdentity(
      name: _nameController.text,
      email: _emailController.text,
    );
    if (!mounted) return;
    setState(() => _formError = error);
    if (error == null) {
      await widget.model.refreshLeaderboard(publish: true);
    }
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: widget.model,
    builder: (context, _) => Material(
      color: GameTheme.ink,
      child: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) => SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 28),
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 760),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      children: [
                        const Icon(
                          Icons.emoji_events_rounded,
                          color: GameTheme.amber,
                          size: 30,
                        ),
                        const SizedBox(width: 12),
                        const Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              MenuEyebrow('CITY RANKINGS'),
                              SizedBox(height: 2),
                              Text(
                                'Top game scores',
                                style: TextStyle(
                                  fontSize: 24,
                                  fontWeight: FontWeight.w900,
                                ),
                              ),
                            ],
                          ),
                        ),
                        IconButton.filledTonal(
                          tooltip: 'Close rankings',
                          onPressed: () => Navigator.pop(context),
                          icon: const Icon(Icons.close_rounded),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    _identityCard(),
                    const SizedBox(height: 16),
                    Row(
                      children: [
                        const Expanded(child: MenuEyebrow('LEADERBOARD')),
                        IconButton(
                          tooltip: 'Refresh scores',
                          onPressed: widget.model.isLeaderboardLoading
                              ? null
                              : widget.model.refreshLeaderboard,
                          icon: const Icon(Icons.refresh_rounded),
                        ),
                      ],
                    ),
                    Text(
                      widget.model.leaderboardMessage,
                      style: const TextStyle(
                        color: Color(0xFFAFC4C1),
                        height: 1.4,
                      ),
                    ),
                    const SizedBox(height: 10),
                    if (widget.model.isLeaderboardLoading)
                      const LinearProgressIndicator(minHeight: 3)
                    else if (widget.model.leaderboard.isEmpty)
                      _emptyBoard()
                    else
                      ...widget.model.leaderboard.map(_scoreRow),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    ),
  );

  Widget _identityCard() => Container(
    padding: const EdgeInsets.all(16),
    decoration: BoxDecoration(
      gradient: const LinearGradient(
        colors: [Color(0xFF172C30), Color(0xFF201B31)],
      ),
      borderRadius: BorderRadius.circular(18),
      border: Border.all(color: const Color(0xFF466064)),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            const Expanded(
              child: Text(
                'Your player identity',
                style: TextStyle(fontSize: 17, fontWeight: FontWeight.w800),
              ),
            ),
            Text(
              '${widget.model.totalBestScore} PTS',
              style: const TextStyle(
                color: GameTheme.amber,
                fontWeight: FontWeight.w900,
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        TextField(
          key: const ValueKey('leaderboard-player-name'),
          controller: _nameController,
          maxLength: 24,
          textInputAction: TextInputAction.next,
          decoration: const InputDecoration(
            labelText: 'Player name',
            hintText: 'Name shown on the leaderboard',
            prefixIcon: Icon(Icons.person_outline_rounded),
          ),
        ),
        const SizedBox(height: 8),
        TextField(
          key: const ValueKey('leaderboard-player-email'),
          controller: _emailController,
          keyboardType: TextInputType.emailAddress,
          autocorrect: false,
          textCapitalization: TextCapitalization.none,
          decoration: const InputDecoration(
            labelText: 'Email identity (optional)',
            hintText: 'Use the same email on another device',
            prefixIcon: Icon(Icons.alternate_email_rounded),
          ),
        ),
        if (_formError != null) ...[
          const SizedBox(height: 8),
          Text(_formError!, style: const TextStyle(color: Color(0xFFFF9B92))),
        ],
        const SizedBox(height: 12),
        FilledButton.icon(
          onPressed: widget.model.isLeaderboardLoading ? null : _publish,
          icon: const Icon(Icons.cloud_upload_outlined),
          label: const Text('Save identity & publish score'),
        ),
        const SizedBox(height: 8),
        const Text(
          'Only your player name is public. Your email stays on this device; the server receives a one-way identity hash.',
          style: TextStyle(color: Color(0xFF8FA9A7), fontSize: 11, height: 1.4),
        ),
      ],
    ),
  );

  Widget _emptyBoard() => Container(
    padding: const EdgeInsets.all(18),
    decoration: BoxDecoration(
      color: const Color(0xA0142528),
      borderRadius: BorderRadius.circular(14),
      border: Border.all(color: const Color(0xFF304B4D)),
    ),
    child: Column(
      children: [
        const Icon(Icons.query_stats_rounded, color: Color(0xFF91AAA8)),
        const SizedBox(height: 8),
        Text(
          'Your local best: ${widget.model.totalBestScore} points',
          textAlign: TextAlign.center,
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
      ],
    ),
  );

  Widget _scoreRow(LeaderboardEntry entry) {
    final rankColor = switch (entry.rank) {
      1 => const Color(0xFFFFD56A),
      2 => const Color(0xFFC4D0D6),
      3 => const Color(0xFFD99A6C),
      _ => const Color(0xFF8EAAA7),
    };
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(
        color: entry.isCurrentPlayer
            ? GameTheme.amber.withValues(alpha: .12)
            : const Color(0xC0122225),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: entry.isCurrentPlayer
              ? GameTheme.amber.withValues(alpha: .65)
              : const Color(0xFF2D4749),
        ),
      ),
      child: Row(
        children: [
          SizedBox(
            width: 42,
            child: Text(
              '#${entry.rank}',
              style: TextStyle(
                color: rankColor,
                fontSize: 17,
                fontWeight: FontWeight.w900,
              ),
            ),
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  entry.isCurrentPlayer
                      ? '${entry.playerName} · YOU'
                      : entry.playerName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
                Text(
                  '${entry.completedMissions} missions · ${entry.xp} XP',
                  style: const TextStyle(
                    color: Color(0xFF93AAA8),
                    fontSize: 11,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 10),
          Text(
            '${entry.score} PTS',
            style: const TextStyle(
              color: GameTheme.amber,
              fontWeight: FontWeight.w900,
            ),
          ),
        ],
      ),
    );
  }
}
