class PlayerIdentity {
  const PlayerIdentity({
    required this.profileId,
    required this.displayName,
    this.email = '',
  });

  final String profileId;
  final String displayName;
  final String email;

  bool get hasEmail => email.isNotEmpty;
}
