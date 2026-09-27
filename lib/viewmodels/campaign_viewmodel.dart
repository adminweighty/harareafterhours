import 'dart:typed_data';

import 'package:stacked/stacked.dart';

import '../data/campaign_data.dart';
import '../models/character_profile.dart';
import '../models/campaign_models.dart';
import '../models/campaign_snapshot.dart';
import '../services/character_photo_service.dart';
import '../services/campaign_sync_service.dart';

enum SyncState { local, syncing, synced, unavailable }

class CampaignViewModel extends BaseViewModel {
  CampaignViewModel({
    CampaignSyncService? syncService,
    CharacterPhotoService? characterPhotoService,
  }) : _syncService = syncService ?? CampaignSyncService(),
       _characterPhotoService =
           characterPhotoService ?? CharacterPhotoService();

  static const String _profileId = String.fromEnvironment(
    'CAMPAIGN_PROFILE_ID',
    defaultValue: 'tari-demo',
  );

  final CampaignSyncService _syncService;
  final CharacterPhotoService _characterPhotoService;
  final Set<int> _completedLevels = <int>{};
  final Map<int, int> _bestScores = <int, int>{};

  int _wallet = 500;
  int _xp = 0;
  int _crewTrust = 0;
  int _communitySupport = 0;
  int _selectedTab = 0;
  int _selectedDistrict = 0;
  CharacterProfile _character = CharacterProfile.initial;
  bool _isPickingCharacterPhoto = false;
  String _characterPhotoMessage =
      'Your portrait stays on this device for this session and is not sent to the campaign save.';
  String _lastChoice = 'Your story starts at the delivery office.';
  SyncState _syncState = SyncState.local;
  String _syncMessage =
      'Local story only. Save when the campaign service is running.';

  Set<int> get completedLevels => Set<int>.unmodifiable(_completedLevels);
  int get wallet => _wallet;
  int get xp => _xp;
  int get crewTrust => _crewTrust;
  int get communitySupport => _communitySupport;
  int get selectedTab => _selectedTab;
  int get selectedDistrict => _selectedDistrict;
  CharacterProfile get character => _character;
  bool get isPickingCharacterPhoto => _isPickingCharacterPhoto;
  String get characterPhotoMessage => _characterPhotoMessage;
  String get lastChoice => _lastChoice;
  String get profileId => _profileId;
  String get syncMessage => _syncMessage;
  SyncState get syncState => _syncState;
  bool get isSyncing => _syncState == SyncState.syncing;
  String get apiBaseUrl => _syncService.baseUrl;
  int get completedCount => _completedLevels.length;
  int get campaignPercent => (completedCount / kMissions.length * 100).round();
  bool get campaignComplete => completedCount == kMissions.length;

  Mission get nextMission {
    for (final Mission mission in kMissions) {
      if (!_completedLevels.contains(mission.level)) {
        return mission;
      }
    }
    return kMissions.last;
  }

  bool isUnlocked(Mission mission) {
    return mission.level == 1 || _completedLevels.contains(mission.level - 1);
  }

  bool isCompleted(Mission mission) => _completedLevels.contains(mission.level);

  int bestScore(Mission mission) => _bestScores[mission.level] ?? 0;

  void selectTab(int index) {
    if (_selectedTab == index) return;
    _selectedTab = index;
    notifyListeners();
  }

  void selectDistrict(int index) {
    if (_selectedDistrict == index) return;
    _selectedDistrict = index;
    notifyListeners();
  }

  void updateCharacterName(String name) {
    final String cleanedName = name.trim();
    if (cleanedName.isEmpty || cleanedName == _character.name) return;
    _character = _character.copyWith(name: cleanedName);
    notifyListeners();
  }

  void updateSkinTone(String skinTone) {
    if (skinTone == _character.skinTone) return;
    _character = _character.copyWith(skinTone: skinTone);
    notifyListeners();
  }

  void updateHairStyle(String hairStyle) {
    if (hairStyle == _character.hairStyle) return;
    _character = _character.copyWith(hairStyle: hairStyle);
    notifyListeners();
  }

  void updateOutfit(String outfit) {
    if (outfit == _character.outfit) return;
    _character = _character.copyWith(outfit: outfit);
    notifyListeners();
  }

  void updateAccessory(String accessory) {
    if (accessory == _character.accessory) return;
    _character = _character.copyWith(
      accessory: accessory,
      headwear: accessory == 'Cap' || accessory == 'Headphones'
          ? accessory
          : 'None',
      eyewear: accessory == 'Glasses' ? 'Sunglasses' : 'None',
    );
    notifyListeners();
  }

  void restoreCharacter(CharacterProfile character) {
    _character = character.copyWith(portraitBytes: _character.portraitBytes);
    notifyListeners();
  }

  void updateWeapon(String weapon) {
    _character = _character.copyWith(weapon: weapon);
    notifyListeners();
  }

  void updateWeaponSound(bool enabled) {
    _character = _character.copyWith(muteWeaponAudio: !enabled);
    notifyListeners();
  }

  void updateReducedWeaponEffects(bool reduced) {
    _character = _character.copyWith(reducedWeaponEffects: reduced);
    notifyListeners();
  }

  void updateBloodEffects(bool enabled) {
    _character = _character.copyWith(hideBloodEffects: !enabled);
    notifyListeners();
  }

  void updateHeadwear(String headwear) {
    _character = _character.copyWith(headwear: headwear);
    notifyListeners();
  }

  void updateEyewear(String eyewear) {
    _character = _character.copyWith(eyewear: eyewear);
    notifyListeners();
  }

  Future<void> chooseCharacterPhoto() async {
    if (_isPickingCharacterPhoto) return;
    _isPickingCharacterPhoto = true;
    _characterPhotoMessage = 'Opening your photo library…';
    notifyListeners();

    try {
      final Uint8List? portraitBytes = await _characterPhotoService
          .choosePortrait();
      if (portraitBytes == null) {
        _characterPhotoMessage = 'Photo selection cancelled.';
      } else {
        _character = _character.copyWith(portraitBytes: portraitBytes);
        _characterPhotoMessage =
            'Portrait added locally. It is not uploaded to PostgreSQL or the campaign API.';
      }
    } catch (_) {
      _characterPhotoMessage =
          'We could not read that photo. Try another image from your library.';
    } finally {
      _isPickingCharacterPhoto = false;
      notifyListeners();
    }
  }

  void clearCharacterPhoto() {
    if (!_character.hasPortrait) return;
    _character = _character.copyWith(clearPortrait: true);
    _characterPhotoMessage = 'Local portrait removed.';
    notifyListeners();
  }

  void completeMission(Mission mission, MissionResult result) {
    if (!result.wasReplay) {
      _completedLevels.add(mission.level);
      _xp += result.xpPayout;
    }
    _wallet += result.coinPayout;
    _bestScores[mission.level] =
        (_bestScores[mission.level] ?? 0) < result.score
        ? result.score
        : _bestScores[mission.level]!;
    _crewTrust += result.impact.crewTrust;
    _communitySupport += result.impact.communitySupport;
    _lastChoice = result.choiceLabel;
    _syncState = SyncState.local;
    _syncMessage = 'Local changes waiting to sync.';
    notifyListeners();
  }

  Future<void> saveToCloud() async {
    if (isSyncing) return;
    _syncState = SyncState.syncing;
    _syncMessage = 'Saving campaign to the cloud…';
    notifyListeners();

    try {
      final CampaignSnapshot saved = await _syncService.save(_snapshot());
      _applySnapshot(saved);
      _syncState = SyncState.synced;
      _syncMessage = 'Cloud save updated${_timestampSuffix(saved.updatedAt)}.';
    } on CampaignSyncException catch (error) {
      _syncState = SyncState.unavailable;
      _syncMessage = error.message;
    } finally {
      notifyListeners();
    }
  }

  Future<void> loadFromCloud() async {
    if (isSyncing) return;
    _syncState = SyncState.syncing;
    _syncMessage = 'Loading campaign from the cloud…';
    notifyListeners();

    try {
      final CampaignSnapshot? snapshot = await _syncService.load(_profileId);
      if (snapshot == null) {
        _syncState = SyncState.local;
        _syncMessage = 'No cloud save yet for $_profileId.';
      } else {
        _applySnapshot(snapshot);
        _syncState = SyncState.synced;
        _syncMessage =
            'Cloud save loaded${_timestampSuffix(snapshot.updatedAt)}.';
      }
    } on CampaignSyncException catch (error) {
      _syncState = SyncState.unavailable;
      _syncMessage = error.message;
    } finally {
      notifyListeners();
    }
  }

  String get endingLabel {
    if (campaignComplete && _crewTrust >= 5) {
      return 'Independent Headliner';
    }
    if (campaignComplete) return 'Room to Grow';
    if (_crewTrust >= 5) return 'Trusted Operator';
    return 'New Arrival';
  }

  CampaignSnapshot _snapshot() {
    return CampaignSnapshot(
      profileId: _profileId,
      wallet: _wallet,
      xp: _xp,
      crewTrust: _crewTrust,
      communitySupport: _communitySupport,
      completedLevels: _completedLevels.toList()..sort(),
      bestScores: Map<int, int>.from(_bestScores),
      lastChoice: _lastChoice,
    );
  }

  void _applySnapshot(CampaignSnapshot snapshot) {
    _wallet = snapshot.wallet;
    _xp = snapshot.xp;
    _crewTrust = snapshot.crewTrust;
    _communitySupport = snapshot.communitySupport;
    _completedLevels
      ..clear()
      ..addAll(snapshot.completedLevels);
    _bestScores
      ..clear()
      ..addAll(snapshot.bestScores);
    _lastChoice = snapshot.lastChoice.isEmpty
        ? 'Your story starts at the delivery office.'
        : snapshot.lastChoice;
  }

  String _timestampSuffix(String? updatedAt) {
    return updatedAt == null || updatedAt.isEmpty ? '' : ' at $updatedAt';
  }

  @override
  void dispose() {
    _syncService.dispose();
    super.dispose();
  }
}
