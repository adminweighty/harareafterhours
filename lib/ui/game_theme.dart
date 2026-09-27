import 'package:flutter/material.dart';

/// Shared Flutter design tokens inspired by the game's night-city identity.
abstract final class GameTheme {
  static const ink = Color(0xFF070F12);
  static const surface = Color(0xFF122126);
  static const amber = Color(0xFFFFBA78);
  static const ivory = Color(0xFFF6F1E5);

  static ThemeData get dark {
    final colors =
        ColorScheme.fromSeed(
          seedColor: amber,
          brightness: Brightness.dark,
          surface: surface,
        ).copyWith(
          primary: amber,
          onPrimary: ink,
          secondary: const Color(0xFF9ADBC6),
          onSecondary: ink,
          secondaryContainer: const Color(0xFF294D40),
          onSecondaryContainer: ivory,
          primaryContainer: const Color(0xFF674122),
          onPrimaryContainer: ivory,
          surfaceContainerLowest: ink,
          surfaceContainerLow: const Color(0xFF1B3028),
          surfaceContainer: const Color(0xFF20372D),
          surfaceContainerHigh: const Color(0xFF294438),
          surfaceContainerHighest: const Color(0xFF324F42),
          onSurface: ivory,
          onSurfaceVariant: const Color(0xFFB6C6BE),
          outline: const Color(0xFF61756A),
          outlineVariant: const Color(0xFF344A3F),
        );
    final shape = RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(16),
    );
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      colorScheme: colors,
      scaffoldBackgroundColor: ink,
      textTheme: ThemeData.dark().textTheme.apply(
        bodyColor: ivory,
        displayColor: ivory,
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: surface,
        foregroundColor: ivory,
        surfaceTintColor: Colors.transparent,
        centerTitle: false,
      ),
      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: surface,
        surfaceTintColor: Colors.transparent,
        modalBarrierColor: Colors.black.withValues(alpha: .72),
        showDragHandle: true,
        dragHandleColor: colors.outline,
        clipBehavior: Clip.antiAlias,
        constraints: const BoxConstraints(maxWidth: 640),
        shape: RoundedRectangleBorder(
          borderRadius: const BorderRadius.vertical(top: Radius.circular(28)),
          side: BorderSide(color: colors.outlineVariant),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size(48, 54),
          shape: shape,
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(48, 52),
          shape: shape,
          side: BorderSide(color: colors.outline),
        ),
      ),
      iconButtonTheme: IconButtonThemeData(
        style: IconButton.styleFrom(
          minimumSize: const Size(48, 48),
          foregroundColor: colors.primary,
        ),
      ),
      listTileTheme: ListTileThemeData(
        iconColor: colors.primary,
        textColor: ivory,
        shape: shape,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
        minVerticalPadding: 12,
        tileColor: colors.surfaceContainerLow,
      ),
      chipTheme: ChipThemeData(
        selectedColor: colors.primaryContainer,
        side: BorderSide(color: colors.outlineVariant),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: colors.secondaryContainer,
        contentTextStyle: TextStyle(color: colors.onSecondaryContainer),
        shape: shape,
      ),
      dividerTheme: DividerThemeData(color: colors.outlineVariant, space: 24),
    );
  }
}

/// One sheet treatment for confirmations and short help content.
class GameActionSheet extends StatelessWidget {
  const GameActionSheet({
    super.key,
    required this.title,
    required this.message,
    required this.confirmLabel,
    this.cancelLabel = 'Cancel',
    this.destructive = false,
  });
  final String title, message, confirmLabel, cancelLabel;
  final bool destructive;

  @override
  Widget build(BuildContext context) => SafeArea(
    top: false,
    child: SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(
        24,
        4,
        24,
        24 + MediaQuery.viewInsetsOf(context).bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  title,
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
              ),
              IconButton(
                tooltip: 'Close',
                onPressed: () => Navigator.pop(context, false),
                icon: const Icon(Icons.close),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            message,
            style: TextStyle(
              color: Theme.of(context).colorScheme.onSurfaceVariant,
              height: 1.5,
            ),
          ),
          const SizedBox(height: 24),
          FilledButton(
            style: destructive
                ? FilledButton.styleFrom(
                    backgroundColor: Theme.of(
                      context,
                    ).colorScheme.errorContainer,
                    foregroundColor: Theme.of(
                      context,
                    ).colorScheme.onErrorContainer,
                  )
                : null,
            onPressed: () => Navigator.pop(context, true),
            child: Text(confirmLabel),
          ),
          const SizedBox(height: 8),
          OutlinedButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(cancelLabel),
          ),
        ],
      ),
    ),
  );
}
