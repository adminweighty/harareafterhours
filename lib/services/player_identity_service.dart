import 'dart:convert';
import 'dart:math';

import 'package:crypto/crypto.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../models/player_identity.dart';

class PlayerIdentityException implements Exception {
  const PlayerIdentityException(this.message);

  final String message;
}

class PlayerIdentityService {
  static const _profileIdKey = 'leaderboard.profile_id';
  static const _displayNameKey = 'leaderboard.display_name';
  static const _emailKey = 'leaderboard.email';

  Future<PlayerIdentity> load({
    String fallbackName = 'Tari',
    String? fallbackProfileId,
  }) async {
    final preferences = await SharedPreferences.getInstance();
    final storedId = preferences.getString(_profileIdKey)?.trim();
    final storedName = preferences.getString(_displayNameKey)?.trim();
    final storedEmail = preferences.getString(_emailKey)?.trim() ?? '';
    final identity = PlayerIdentity(
      profileId: storedId?.isNotEmpty == true
          ? storedId!
          : (fallbackProfileId?.trim().isNotEmpty == true
                ? fallbackProfileId!.trim()
                : _anonymousProfileId()),
      displayName: storedName?.isNotEmpty == true
          ? storedName!
          : fallbackName.trim(),
      email: storedEmail,
    );
    await _write(preferences, identity);
    return identity;
  }

  Future<PlayerIdentity> save({
    required PlayerIdentity current,
    required String displayName,
    required String email,
  }) async {
    final cleanedName = displayName.trim().replaceAll(RegExp(r'\s+'), ' ');
    final cleanedEmail = email.trim().toLowerCase();
    if (cleanedName.length < 2 || cleanedName.length > 24) {
      throw const PlayerIdentityException(
        'Player name must contain 2–24 characters.',
      );
    }
    if (cleanedEmail.isNotEmpty && !_looksLikeEmail(cleanedEmail)) {
      throw const PlayerIdentityException('Enter a valid email address.');
    }
    final identity = PlayerIdentity(
      profileId: cleanedEmail.isEmpty
          ? current.profileId
          : _emailProfileId(cleanedEmail),
      displayName: cleanedName,
      email: cleanedEmail,
    );
    await _write(await SharedPreferences.getInstance(), identity);
    return identity;
  }

  static bool _looksLikeEmail(String value) => RegExp(
    r'^[^\s@]+@[^\s@]+\.[^\s@]+$',
    caseSensitive: false,
  ).hasMatch(value);

  static String _emailProfileId(String email) {
    final digest = sha256.convert(utf8.encode(email)).toString();
    return 'email_${digest.substring(0, 48)}';
  }

  static String _anonymousProfileId() {
    final random = Random.secure();
    final bytes = List<int>.generate(18, (_) => random.nextInt(256));
    return 'player_${base64UrlEncode(bytes).replaceAll('=', '')}';
  }

  static Future<void> _write(
    SharedPreferences preferences,
    PlayerIdentity identity,
  ) async {
    await Future.wait([
      preferences.setString(_profileIdKey, identity.profileId),
      preferences.setString(_displayNameKey, identity.displayName),
      preferences.setString(_emailKey, identity.email),
    ]);
  }
}
