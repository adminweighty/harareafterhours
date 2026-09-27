import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'game_theme.dart';

/// Finite motion: menus settle instead of continuously consuming frames.
/// System Reduce Motion bypasses translations and decorative animation.
class MenuReveal extends StatelessWidget {
  const MenuReveal({super.key, required this.child, this.order = 0});
  final Widget child;
  final int order;

  @override
  Widget build(BuildContext context) {
    if (MediaQuery.disableAnimationsOf(context)) return child;
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: 1),
      duration: Duration(milliseconds: 320 + order * 75),
      curve: Curves.easeOutCubic,
      child: child,
      builder: (_, value, child) => Opacity(
        opacity: value,
        child: Transform.translate(
          offset: Offset(0, 18 * (1 - value)),
          child: child,
        ),
      ),
    );
  }
}

class NightCityBackdrop extends StatelessWidget {
  const NightCityBackdrop({super.key, required this.child});
  final Widget child;

  @override
  Widget build(BuildContext context) => Stack(
    fit: StackFit.expand,
    children: [
      Positioned.fill(
        child: ExcludeSemantics(
          child: RepaintBoundary(
            child: TweenAnimationBuilder<double>(
              tween: Tween(begin: 0, end: 1),
              duration: MediaQuery.disableAnimationsOf(context)
                  ? Duration.zero
                  : const Duration(milliseconds: 1400),
              curve: Curves.easeOutCubic,
              builder: (_, value, _) =>
                  CustomPaint(painter: _CityMapPainter(value)),
            ),
          ),
        ),
      ),
      child,
    ],
  );
}

class _CityMapPainter extends CustomPainter {
  const _CityMapPainter(this.progress);
  final double progress;
  @override
  void paint(Canvas canvas, Size size) {
    canvas.clipRect(Offset.zero & size);
    canvas.drawRect(Offset.zero & size, Paint()..color = GameTheme.ink);
    final center = Offset(size.width * .16, size.height * .52);
    canvas.drawRect(
      Offset.zero & size,
      Paint()
        ..shader =
            const RadialGradient(
              colors: [Color(0xFF213D3A), GameTheme.ink],
            ).createShader(
              Rect.fromCircle(center: center, radius: size.longestSide * .7),
            ),
    );
    canvas.save();
    canvas.translate(size.width * .2, size.height * .45);
    canvas.rotate(-.3);
    final block = Paint()
      ..color = const Color(0xFF2E4B47).withValues(alpha: .3);
    final line = Paint()
      ..color = const Color(0xFF74B9AA).withValues(alpha: .09)
      ..strokeWidth = 1;
    for (var x = -12; x < 20; x++) {
      for (var y = -14; y < 18; y++) {
        canvas.drawRRect(
          RRect.fromRectAndRadius(
            Rect.fromLTWH(x * 80.0, y * 54.0, 62, 37),
            const Radius.circular(3),
          ),
          block,
        );
      }
      canvas.drawLine(
        Offset(x * 80.0 - 9, -900),
        Offset(x * 80.0 - 9, 1100),
        line,
      );
    }
    final path = Path()
      ..moveTo(-300, 235)
      ..lineTo(-89, 235)
      ..lineTo(-89, 73)
      ..lineTo(231, 73)
      ..lineTo(231, -197)
      ..lineTo(600, -197);
    final route = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2
      ..color = GameTheme.amber.withValues(alpha: .25);
    canvas.drawPath(path, route);
    final metric = path.computeMetrics().first;
    final position = metric
        .getTangentForOffset(metric.length * (.15 + .6 * progress))!
        .position;
    canvas.drawCircle(
      position,
      13,
      Paint()..color = GameTheme.amber.withValues(alpha: .08),
    );
    canvas.drawCircle(
      position,
      4,
      Paint()..color = GameTheme.amber.withValues(alpha: .6),
    );
    for (var i = 0; i < 4; i++) {
      canvas.drawCircle(
        Offset(-89 + i * 80, 73),
        3,
        Paint()..color = const Color(0xFF76CCBA),
      );
    }
    canvas.restore();
    canvas.drawRect(
      Offset.zero & size,
      Paint()
        ..shader = const LinearGradient(
          begin: Alignment.centerLeft,
          end: Alignment.centerRight,
          colors: [Color(0x10070F12), Color(0xEA070F12)],
        ).createShader(Offset.zero & size),
    );
  }

  @override
  bool shouldRepaint(_CityMapPainter oldDelegate) =>
      oldDelegate.progress != progress;
}

class MenuEyebrow extends StatelessWidget {
  const MenuEyebrow(this.text, {super.key, this.color = GameTheme.amber});
  final String text;
  final Color color;
  @override
  Widget build(BuildContext context) => Text(
    text,
    style: TextStyle(
      color: color,
      fontSize: 10,
      fontWeight: FontWeight.w800,
      letterSpacing: 2,
    ),
  );
}

class MenuActionCard extends StatefulWidget {
  const MenuActionCard({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
    this.primary = false,
    this.compact = false,
  });
  final IconData icon;
  final String title, subtitle;
  final VoidCallback? onTap;
  final bool primary, compact;
  @override
  State<MenuActionCard> createState() => _MenuActionCardState();
}

class _MenuActionCardState extends State<MenuActionCard> {
  bool _hover = false, _pressed = false;
  @override
  Widget build(BuildContext context) {
    final enabled = widget.onTap != null;
    final color = widget.primary ? GameTheme.ink : GameTheme.ivory;
    final reduce = MediaQuery.disableAnimationsOf(context);
    return Semantics(
      button: true,
      enabled: enabled,
      child: AnimatedScale(
        scale: _pressed ? .985 : 1,
        duration: reduce ? Duration.zero : const Duration(milliseconds: 120),
        child: AnimatedContainer(
          duration: reduce ? Duration.zero : const Duration(milliseconds: 160),
          decoration: BoxDecoration(
            color: widget.primary
                ? (enabled ? GameTheme.amber : const Color(0xFF596468))
                : (_hover ? const Color(0xFF253B3D) : const Color(0xEE152428)),
            borderRadius: BorderRadius.circular(widget.compact ? 14 : 18),
            border: Border.all(
              color: widget.primary
                  ? GameTheme.amber.withValues(alpha: .5)
                  : (_hover
                        ? const Color(0xFF78B2A9)
                        : const Color(0xFF304347)),
            ),
          ),
          child: Material(
            color: Colors.transparent,
            child: InkWell(
              borderRadius: BorderRadius.circular(widget.compact ? 14 : 18),
              onTap: widget.onTap,
              onHover: (v) => setState(() => _hover = v),
              onHighlightChanged: (v) => setState(() => _pressed = v),
              child: Padding(
                padding: EdgeInsets.symmetric(
                  horizontal: widget.compact ? 14 : (widget.primary ? 20 : 16),
                  vertical: widget.compact ? 12 : (widget.primary ? 20 : 16),
                ),
                child: Row(
                  children: [
                    Icon(
                      widget.icon,
                      color: enabled ? color : color.withValues(alpha: .45),
                      size: widget.compact ? 21 : 25,
                    ),
                    SizedBox(width: widget.compact ? 10 : 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            widget.title,
                            style: TextStyle(
                              color: enabled
                                  ? color
                                  : color.withValues(alpha: .5),
                              fontSize: widget.compact
                                  ? (widget.primary ? 17 : 14)
                                  : (widget.primary ? 20 : 15),
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          SizedBox(height: widget.compact ? 2 : 4),
                          Text(
                            widget.subtitle,
                            style: TextStyle(
                              color: widget.primary
                                  ? color.withValues(alpha: .8)
                                  : const Color(0xFFA6BBBD),
                              fontSize: widget.compact ? 11 : 12,
                              height: 1.4,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    Icon(
                      Icons.arrow_forward_rounded,
                      size: 18,
                      color: color.withValues(alpha: enabled ? .8 : .3),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Shared success feedback, driven only by a confirmed Unity result.
class MissionSuccessCard extends StatelessWidget {
  const MissionSuccessCard({
    super.key,
    required this.score,
    required this.stars,
    required this.coins,
    required this.xp,
  });
  final int score, stars, coins, xp;
  @override
  Widget build(BuildContext context) => MenuReveal(
    child: Material(
      color: const Color(0xFA122629),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: const BorderSide(color: Color(0xFF72CDB6)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const MenuEyebrow('MISSION COMPLETE', color: Color(0xFF8CE2C9)),
            const SizedBox(height: 8),
            Wrap(
              spacing: 12,
              runSpacing: 8,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                Text(
                  '$score PTS',
                  style: const TextStyle(
                    fontSize: 24,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: List.generate(
                    3,
                    (i) => Icon(
                      i < math.min(stars, 3)
                          ? Icons.star_rounded
                          : Icons.star_outline_rounded,
                      color: GameTheme.amber,
                      size: 22,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              coins > 0 || xp > 0
                  ? '+$coins coins  /  +$xp XP  •  Free roam unlocked'
                  : 'Result recorded  •  Free roam unlocked',
              style: const TextStyle(fontSize: 12, color: Color(0xFFBDD3D0)),
            ),
          ],
        ),
      ),
    ),
  );
}
