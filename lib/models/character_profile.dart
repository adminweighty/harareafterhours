import 'dart:typed_data';

/// Player-controlled visual choices for the campaign protagonist.
///
/// A portrait is deliberately kept out of [CampaignSnapshot]. It is personal
/// media, so this first implementation holds it only in the running app.
class CharacterProfile {
  const CharacterProfile({
    required this.name,
    required this.skinTone,
    required this.hairStyle,
    required this.outfit,
    required this.accessory,
    this.weapon = 'Pulse pistol',
    this.headwear = 'None',
    this.eyewear = 'None',
    this.muteWeaponAudio = false,
    this.reducedWeaponEffects = false,
    this.hideBloodEffects = false,
    this.portraitBytes,
  });

  static const CharacterProfile initial = CharacterProfile(
    name: 'Tari',
    skinTone: 'Deep',
    hairStyle: 'Close cut',
    outfit: 'Delivery fit',
    accessory: 'No accessory',
  );

  final String name;
  final String skinTone;
  final String hairStyle;
  final String outfit;
  final String accessory;
  final String weapon;
  final String headwear;
  final String eyewear;
  final bool muteWeaponAudio;
  final bool reducedWeaponEffects;
  final bool hideBloodEffects;
  final Uint8List? portraitBytes;

  bool get hasPortrait => portraitBytes != null;

  /// Local Unity save/bridge contract. Personal photos are never serialized.
  Map<String, dynamic> toJson() => {
    'name': name,
    'skinTone': skinTone,
    'hairStyle': hairStyle,
    'outfit': outfit,
    'accessory': accessory,
    'weapon': weapon,
    'headwear': headwear,
    'eyewear': eyewear,
    'muteWeaponAudio': muteWeaponAudio,
    'reducedWeaponEffects': reducedWeaponEffects,
    'hideBloodEffects': hideBloodEffects,
  };

  static CharacterProfile fromJson(Map<String, dynamic> json) {
    String value(String key, String fallback) =>
        json[key] is String && (json[key] as String).trim().isNotEmpty
        ? (json[key] as String).trim()
        : fallback;
    final accessory = value('accessory', initial.accessory);
    return CharacterProfile(
      name: value('name', initial.name),
      skinTone: value('skinTone', initial.skinTone),
      hairStyle: value('hairStyle', initial.hairStyle),
      outfit: value('outfit', initial.outfit),
      accessory: accessory,
      weapon: value('weapon', 'Pulse pistol'),
      headwear: value(
        'headwear',
        accessory == 'Cap' || accessory == 'Headphones' ? accessory : 'None',
      ),
      eyewear: value('eyewear', accessory == 'Glasses' ? 'Sunglasses' : 'None'),
      muteWeaponAudio: json['muteWeaponAudio'] == true,
      reducedWeaponEffects: json['reducedWeaponEffects'] == true,
      hideBloodEffects: json['hideBloodEffects'] == true,
    );
  }

  CharacterProfile copyWith({
    String? name,
    String? skinTone,
    String? hairStyle,
    String? outfit,
    String? accessory,
    String? weapon,
    String? headwear,
    String? eyewear,
    bool? muteWeaponAudio,
    bool? reducedWeaponEffects,
    bool? hideBloodEffects,
    Uint8List? portraitBytes,
    bool clearPortrait = false,
  }) {
    return CharacterProfile(
      name: name ?? this.name,
      skinTone: skinTone ?? this.skinTone,
      hairStyle: hairStyle ?? this.hairStyle,
      outfit: outfit ?? this.outfit,
      accessory: accessory ?? this.accessory,
      weapon: weapon ?? this.weapon,
      headwear: headwear ?? this.headwear,
      eyewear: eyewear ?? this.eyewear,
      muteWeaponAudio: muteWeaponAudio ?? this.muteWeaponAudio,
      reducedWeaponEffects: reducedWeaponEffects ?? this.reducedWeaponEffects,
      hideBloodEffects: hideBloodEffects ?? this.hideBloodEffects,
      portraitBytes: clearPortrait ? null : portraitBytes ?? this.portraitBytes,
    );
  }
}
