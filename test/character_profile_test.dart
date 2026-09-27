import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:harare_after_hours/models/character_profile.dart';

void main() {
  test(
    'keeps visual choices while replacing and clearing a local portrait',
    () {
      final Uint8List photo = Uint8List.fromList(<int>[1, 2, 3, 4]);
      final CharacterProfile customised = CharacterProfile.initial.copyWith(
        name: 'Maya',
        skinTone: 'Golden',
        hairStyle: 'Curls',
        outfit: 'Festival ready',
        accessory: 'Headphones',
        portraitBytes: photo,
      );

      expect(customised.name, 'Maya');
      expect(customised.skinTone, 'Golden');
      expect(customised.hairStyle, 'Curls');
      expect(customised.outfit, 'Festival ready');
      expect(customised.accessory, 'Headphones');
      expect(customised.hasPortrait, isTrue);
      expect(customised.portraitBytes, same(photo));

      final CharacterProfile withoutPhoto = customised.copyWith(
        clearPortrait: true,
      );
      expect(withoutPhoto.hasPortrait, isFalse);
      expect(withoutPhoto.name, 'Maya');
      expect(withoutPhoto.outfit, 'Festival ready');
    },
  );
}
