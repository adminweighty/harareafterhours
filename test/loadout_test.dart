import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:harare_after_hours/models/character_profile.dart';
import 'package:harare_after_hours/viewmodels/campaign_viewmodel.dart';
import 'package:harare_after_hours/ui/loadout_studio.dart';

void main() {
  test(
    'feedback settings survive profile copies and the Unity save contract',
    () {
      final model = CampaignViewModel();
      model.updateWeaponSound(false);
      model.updateReducedWeaponEffects(true);
      model.updateBloodEffects(false);
      model.updateWeapon('Pulse rifle');
      final restored = CharacterProfile.fromJson(model.character.toJson());
      expect(restored.muteWeaponAudio, isTrue);
      expect(restored.reducedWeaponEffects, isTrue);
      expect(restored.hideBloodEffects, isTrue);
      expect(restored.copyWith(outfit: 'Night shift').hideBloodEffects, isTrue);
      expect(CharacterProfile.fromJson({}).hideBloodEffects, isFalse);
      expect(restored.copyWith(outfit: 'Night shift').muteWeaponAudio, isTrue);
      expect(CharacterProfile.fromJson({}).reducedWeaponEffects, isFalse);
      model.dispose();
    },
  );
  test('loadout round trip preserves independent slots without photo data', () {
    final profile = CharacterProfile.initial.copyWith(
      weapon: 'Pulse rifle',
      headwear: 'Helmet',
      eyewear: 'Sunglasses',
      outfit: 'Festival ready',
    );
    final restored = CharacterProfile.fromJson(profile.toJson());
    expect(restored.toJson(), profile.toJson());
    expect(restored.hasPortrait, isFalse);
    expect(profile.copyWith(name: 'Maya').weapon, 'Pulse rifle');
  });

  test('migrates old accessory and tolerates missing optional save fields', () {
    expect(
      CharacterProfile.fromJson({'accessory': 'Glasses'}).eyewear,
      'Sunglasses',
    );
    expect(CharacterProfile.fromJson({'accessory': 'Cap'}).headwear, 'Cap');
    expect(CharacterProfile.fromJson({'name': 42}).name, 'Tari');
    expect(CharacterProfile.fromJson({}).weapon, 'Pulse pistol');
  });

  for (final size in [const Size(320, 568), const Size(844, 390)]) {
    testWidgets('loadout selects independent equipment at $size, large text', (
      tester,
    ) async {
      tester.view.physicalSize = size;
      tester.view.devicePixelRatio = 1;
      tester.platformDispatcher.textScaleFactorTestValue = 1.5;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
        tester.platformDispatcher.clearTextScaleFactorTestValue();
      });
      final model = CampaignViewModel();
      await tester.pumpWidget(MaterialApp(home: LoadoutStudio(model: model)));
      for (final option in [
        'Weapon:Pulse rifle',
        'Clothes:Festival ready',
        'Eyewear:Sunglasses',
        'Headwear:Helmet',
      ]) {
        final finder = find.byKey(ValueKey(option));
        await tester.ensureVisible(finder);
        await tester.pumpAndSettle();
        await tester.tap(finder);
        await tester.pumpAndSettle();
        expect(tester.widget<ChoiceChip>(finder).selected, isTrue);
      }
      expect(model.character.weapon, 'Pulse rifle');
      expect(model.character.eyewear, 'Sunglasses');
      expect(model.character.headwear, 'Helmet');
      expect(model.character.outfit, 'Festival ready');
      await tester.ensureVisible(find.text('Weapon feedback'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Weapon feedback'));
      await tester.pumpAndSettle();
      for (final title in [
        'Weapon sound',
        'Reduced effects',
        'Blood effects',
      ]) {
        final toggle = find.widgetWithText(SwitchListTile, title);
        await tester.ensureVisible(toggle);
        await tester.pumpAndSettle();
        await tester.tap(toggle);
        await tester.pumpAndSettle();
      }
      expect(model.character.muteWeaponAudio, isTrue);
      expect(model.character.reducedWeaponEffects, isTrue);
      expect(model.character.hideBloodEffects, isTrue);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox.shrink());
      model.dispose();
    });
  }
}
