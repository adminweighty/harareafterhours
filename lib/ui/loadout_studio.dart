import 'package:flutter/material.dart';

import '../viewmodels/campaign_viewmodel.dart';
import 'game_home.dart';
import 'game_menu_visuals.dart';

/// All categories share the same single-selection, wrap-to-fit interaction.
class LoadoutStudio extends StatelessWidget {
  const LoadoutStudio({super.key, required this.model});
  final CampaignViewModel model;

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: model,
    builder: (context, _) => Scaffold(
      backgroundColor: Theme.of(context).colorScheme.surface,
      appBar: AppBar(
        title: const Text('Change character'),
        leading: IconButton(
          tooltip: 'Apply and return',
          icon: const Icon(Icons.close),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      bottomNavigationBar: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
          child: FilledButton.icon(
            icon: const Icon(Icons.check),
            label: const Text('Apply and return'),
            onPressed: () => Navigator.pop(context),
          ),
        ),
      ),
      body: MenuReveal(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
          children: [
            const MenuEyebrow('CHARACTER / LOADOUT'),
            const SizedBox(height: 12),
            Text(
              model.character.name,
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 8),
            const Text(
              'Changes apply when you return. Your mission stays paused.',
            ),
            const SizedBox(height: 12),
            FilledButton.tonalIcon(
              icon: const Icon(Icons.face),
              label: const Text('Appearance & photo'),
              onPressed: () => showModalBottomSheet<void>(
                context: context,
                isScrollControlled: true,
                useSafeArea: true,
                builder: (context) => SizedBox(
                  height: MediaQuery.sizeOf(context).height * .9,
                  child: CharacterStudio(model: model),
                ),
              ),
            ),
            const SizedBox(height: 20),
            _choices(
              'Weapon',
              ['Unarmed', 'Pulse pistol', 'Pulse rifle', 'Baton'],
              model.character.weapon,
              model.updateWeapon,
            ),
            _choices(
              'Clothes',
              [
                'Delivery fit',
                'Street classic',
                'Night shift',
                'Festival ready',
              ],
              model.character.outfit,
              model.updateOutfit,
            ),
            _choices(
              'Eyewear',
              ['None', 'Sunglasses'],
              model.character.eyewear,
              model.updateEyewear,
            ),
            _choices(
              'Headwear',
              ['None', 'Cap', 'Helmet', 'Headphones'],
              model.character.headwear,
              model.updateHeadwear,
            ),
            const Text(
              'Headwear is cosmetic. Attacking officers raises your wanted level.',
              style: TextStyle(fontSize: 13),
            ),
            const SizedBox(height: 16),
            ExpansionTile(
              title: const Text('Weapon feedback'),
              tilePadding: EdgeInsets.zero,
              children: [
                SwitchListTile(
                  title: const Text('Weapon sound'),
                  value: !model.character.muteWeaponAudio,
                  onChanged: model.updateWeaponSound,
                ),
                SwitchListTile(
                  title: const Text('Reduced effects'),
                  subtitle: const Text('No muzzle flash or weapon kick'),
                  value: model.character.reducedWeaponEffects,
                  onChanged: model.updateReducedWeaponEffects,
                ),
                SwitchListTile(
                  title: const Text('Blood effects'),
                  subtitle: const Text('Character splashes and ground stains'),
                  value: !model.character.hideBloodEffects,
                  onChanged: model.updateBloodEffects,
                ),
              ],
            ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    ),
  );

  Widget _choices(
    String label,
    List<String> options,
    String selected,
    ValueChanged<String> onSelected,
  ) => Padding(
    padding: const EdgeInsets.only(bottom: 20),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 4,
          children: options
              .map(
                (option) => ChoiceChip(
                  key: ValueKey('$label:$option'),
                  label: Text(switch (option) {
                    'Pulse rifle' => 'AK-style rifle',
                    'Pulse pistol' => 'Agent pistol',
                    _ => option,
                  }),
                  selected: option == selected,
                  showCheckmark: true,
                  materialTapTargetSize: MaterialTapTargetSize.padded,
                  onSelected: (checked) {
                    if (checked) onSelected(option);
                  },
                ),
              )
              .toList(),
        ),
      ],
    ),
  );
}
