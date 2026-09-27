import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';

/// Branding runs alongside native loading; readiness, never a timer, opens play.
class GameLaunchOverlay extends StatefulWidget {
  const GameLaunchOverlay({
    super.key,
    required this.ready,
    required this.onRetry,
  });
  final bool ready;
  final VoidCallback onRetry;

  @override
  State<GameLaunchOverlay> createState() => _GameLaunchOverlayState();
}

class _GameLaunchOverlayState extends State<GameLaunchOverlay>
    with SingleTickerProviderStateMixin {
  late final AnimationController _reveal = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1800),
  );
  Timer? _slowLoad;
  bool _waiting = false;
  bool _hidden = false;

  @override
  void initState() {
    super.initState();
    // A fast native handshake must not skip the branding animation.
    _hidden = false;
    _reveal.addStatusListener((status) {
      if (status == AnimationStatus.completed && mounted) setState(() {});
    });
    _armSlowLoad();
  }

  void _armSlowLoad() {
    _slowLoad?.cancel();
    _waiting = false;
    _slowLoad = Timer(const Duration(seconds: 15), () {
      if (mounted && !widget.ready) setState(() => _waiting = true);
    });
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (MediaQuery.disableAnimationsOf(context)) {
      _reveal.value = 1;
    } else if (!_reveal.isCompleted && !_reveal.isAnimating) {
      _reveal.forward();
    }
  }

  @override
  void didUpdateWidget(GameLaunchOverlay oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.ready) _slowLoad?.cancel();
    if (!widget.ready && oldWidget.ready) {
      _hidden = false;
      _armSlowLoad();
      if (MediaQuery.disableAnimationsOf(context)) {
        _reveal.value = 1;
      } else {
        _reveal.forward(from: 0);
      }
    }
  }

  @override
  void dispose() {
    _slowLoad?.cancel();
    _reveal.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_hidden) return const SizedBox.shrink();
    final reducedMotion = MediaQuery.disableAnimationsOf(context);
    final opening = widget.ready && (_reveal.isCompleted || reducedMotion);
    if (opening && reducedMotion) return const SizedBox.shrink();
    return IgnorePointer(
      ignoring: opening,
      child: AnimatedOpacity(
        opacity: opening ? 0 : 1,
        duration: reducedMotion
            ? Duration.zero
            : const Duration(milliseconds: 280),
        onEnd: () {
          if (mounted && widget.ready) setState(() => _hidden = true);
        },
        child: Material(
          color: const Color(0xFF07110F),
          child: Stack(
            fit: StackFit.expand,
            children: [
              ExcludeSemantics(
                child: RepaintBoundary(
                  child: AnimatedBuilder(
                    animation: _reveal,
                    builder: (_, _) => CustomPaint(
                      painter: _CityArrivalPainter(_reveal.value),
                    ),
                  ),
                ),
              ),
              SafeArea(
                child: LayoutBuilder(
                  builder: (context, constraints) => SingleChildScrollView(
                    child: ConstrainedBox(
                      constraints: BoxConstraints(
                        minHeight: constraints.maxHeight,
                      ),
                      child: Padding(
                        padding: const EdgeInsets.fromLTRB(24, 72, 24, 32),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            FadeTransition(
                              opacity: _reveal.drive(
                                CurveTween(curve: Curves.easeOut),
                              ),
                              child: ScaleTransition(
                                scale: _reveal.drive(
                                  Tween(begin: 0.9, end: 1.0).chain(
                                    CurveTween(curve: Curves.easeOutCubic),
                                  ),
                                ),
                                child: GameLogo(
                                  height: (constraints.maxHeight * 0.38).clamp(
                                    100,
                                    280,
                                  ),
                                ),
                              ),
                            ),
                            const SizedBox(height: 28),
                            const Text(
                              'YOUR CITY. YOUR STORY.',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                color: Colors.white70,
                                fontSize: 11,
                                letterSpacing: 3,
                              ),
                            ),
                            const SizedBox(height: 14),
                            const Text(
                              'HARARE CBD',
                              style: TextStyle(
                                color: Color(0xFFFFAE87),
                                fontSize: 12,
                                letterSpacing: 4,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            const SizedBox(height: 18),
                            const Text(
                              'Preparing your city…',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 18,
                              ),
                            ),
                            const SizedBox(height: 16),
                            SizedBox(
                              width: 180,
                              child: reducedMotion
                                  ? const Text(
                                      'Loading',
                                      textAlign: TextAlign.center,
                                    )
                                  : const LinearProgressIndicator(
                                      minHeight: 2,
                                      color: Color(0xFFFF784D),
                                      backgroundColor: Color(0xFF29332F),
                                      semanticsLabel: 'Loading city',
                                    ),
                            ),
                            if (_waiting) ...[
                              const SizedBox(height: 24),
                              const Text(
                                'Still connecting to the city. You can open Menu while you wait.',
                                textAlign: TextAlign.center,
                                style: TextStyle(color: Colors.white70),
                              ),
                              TextButton(
                                onPressed: widget.onRetry,
                                child: const Text('Check connection'),
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Lightweight code-native scenery: no video decoding alongside Unity startup.
class _CityArrivalPainter extends CustomPainter {
  const _CityArrivalPainter(this.progress);
  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    final horizon = size.height * .70;
    final center = Offset(size.width / 2, horizon);
    final glow = Paint()
      ..shader =
          RadialGradient(
            colors: [
              const Color(0xFF9E652B).withValues(alpha: .22 * progress),
              const Color(0x0007110F),
            ],
          ).createShader(
            Rect.fromCircle(
              center: Offset(size.width / 2, size.height * .4),
              radius: size.longestSide * .55,
            ),
          );
    canvas.drawRect(Offset.zero & size, glow);
    final buildings = Paint()
      ..color = const Color(0xFF18332B).withValues(alpha: .6 * progress);
    for (var i = 0; i < 16; i++) {
      final width = size.width / 16;
      final height = (35 + (i * 37 % 90)) * progress;
      canvas.drawRect(
        Rect.fromLTWH(i * width, horizon - height, width - 3, height),
        buildings,
      );
    }
    final lines = Paint()
      ..color = const Color(0xFFFFB565).withValues(alpha: .16 * progress)
      ..strokeWidth = 1;
    for (var i = -4; i <= 4; i++) {
      canvas.drawLine(
        center + Offset(i * 12, 0),
        Offset(size.width / 2 + i * size.width * .30, size.height),
        lines,
      );
    }
    for (var i = 0; i < 7; i++) {
      final t = ((i + progress * 1.5) / 7) % 1;
      final y = horizon + math.pow(t, 2) * (size.height - horizon);
      canvas.drawLine(Offset(0, y), Offset(size.width, y), lines);
    }
    final light = Paint()
      ..color = const Color(
        0xFFFFBD71,
      ).withValues(alpha: math.sin(progress * math.pi).clamp(0, 1) * .5)
      ..strokeWidth = 2;
    canvas.drawLine(
      Offset(size.width * progress - 70, horizon),
      Offset(size.width * progress + 70, horizon),
      light,
    );
  }

  @override
  bool shouldRepaint(_CityArrivalPainter oldDelegate) =>
      oldDelegate.progress != progress;
}

class GameLogo extends StatelessWidget {
  const GameLogo({super.key, this.height});
  final double? height;

  @override
  Widget build(BuildContext context) => ConstrainedBox(
    constraints: const BoxConstraints(maxWidth: 560),
    child: Image.asset(
      'assets/branding/harare-after-hours-logo.png',
      height: height,
      fit: BoxFit.contain,
      semanticLabel: 'Harare After Hours',
      filterQuality: FilterQuality.medium,
      errorBuilder: (_, error, stack) => const Text(
        'HARARE\nAFTER HOURS',
        textAlign: TextAlign.center,
        style: TextStyle(
          color: Colors.white,
          fontSize: 32,
          fontWeight: FontWeight.w900,
        ),
      ),
    ),
  );
}
