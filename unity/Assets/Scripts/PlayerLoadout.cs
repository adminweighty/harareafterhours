using System;
using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>Device-local equipment save; never stores personal photo bytes.</summary>
    public sealed class PlayerLoadout : MonoBehaviour
    {
        public const string SaveKey = "Harare.Loadout.v1";
        public const string StarterWeaponKey = "Harare.StarterSidearm.v1";
        private ThirdPersonController _player;
        private PlayerEquipmentVisual _visual;
        private AvatarProfilePayload _profile;
        public string ProfileJson => JsonUtility.ToJson(_profile);
        public void CycleWeapon()
        {
            if(_profile==null||_player.InputLocked||_player.IsInVehicle)return;
            string[] choices={"Pulse pistol","Pulse rifle","Baton","Unarmed"};
            int next=(Array.IndexOf(choices,_profile.weapon)+1)%choices.Length;
            _profile.weapon=choices[next];ApplyJson(ProfileJson);
            FlutterGameBridge.Instance?.Publish("weapon_changed","{}");
        }

        public void Initialize(ThirdPersonController player)
        {
            _player = player;
            _visual = GetComponent<PlayerEquipmentVisual>();
            if (_visual == null) _visual = gameObject.AddComponent<PlayerEquipmentVisual>();
            _visual.Initialize(player);
            string saved = PlayerPrefs.GetString(SaveKey, "{\"weapon\":\"Pulse pistol\"}");
            if (!ApplyJson(saved, false)) ApplyJson("{\"weapon\":\"Pulse pistol\"}", false);
            // One-time compatibility upgrade for saves created when the game
            // silently defaulted to Unarmed. The player can still cycle back.
            if(!PlayerPrefs.HasKey(StarterWeaponKey))
            {
                if(_profile.weapon=="Unarmed")
                {
                    _profile.weapon="Pulse pistol";
                    ApplyJson(ProfileJson,false);
                }
                PlayerPrefs.SetString(SaveKey,ProfileJson);
                PlayerPrefs.SetInt(StarterWeaponKey,1);
                PlayerPrefs.Save();
            }
        }

        public bool ApplyJson(string json, bool save = true)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 4096) return false;
            try
            {
                var profile = JsonUtility.FromJson<AvatarProfilePayload>(json);
                if (profile == null) return false;
                profile.name = string.IsNullOrWhiteSpace(profile.name) ? "Tari" : profile.name.Trim();
                if (profile.name.Length > 24) profile.name = profile.name[..24];
                profile.skinTone = Choice(profile.skinTone, "Deep", "Rich", "Warm", "Golden", "Cool");
                profile.hairStyle = Choice(profile.hairStyle, "Close cut", "Braids", "Locs", "Curls", "Headwrap");
                profile.outfit = Choice(profile.outfit, "Delivery fit", "Street classic", "Night shift", "Festival ready");
                // Migrate the earlier, single accessory slot only when new slots are absent.
                if (string.IsNullOrEmpty(profile.headwear))
                    profile.headwear = profile.accessory is "Cap" or "Headphones" ? profile.accessory : "None";
                if (string.IsNullOrEmpty(profile.eyewear))
                    profile.eyewear = profile.accessory == "Glasses" ? "Sunglasses" : "None";
                profile.headwear = Choice(profile.headwear, "None", "Cap", "Helmet", "Headphones");
                profile.eyewear = Choice(profile.eyewear, "None", "Sunglasses");
                profile.weapon = Choice(profile.weapon, "Pulse pistol", "Pulse rifle", "Baton", "Unarmed");
                profile.accessory = Choice(profile.accessory, "No accessory", "Cap", "Glasses", "Headphones");
                _profile = profile;
                _player.Avatar.ApplyProfile(profile);
                _visual.Apply(profile);
                var blood = GetComponent<BloodHitEffects>();
                if (blood == null) blood = gameObject.AddComponent<BloodHitEffects>();
                blood.Configure(profile.hideBloodEffects, profile.reducedWeaponEffects);
                _player.GetComponent<PlayerCombat>()?.Equip(profile.weapon);
                if (save)
                {
                    PlayerPrefs.SetString(SaveKey, ProfileJson);
                    PlayerPrefs.Save();
                }
                return true;
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"Invalid loadout ignored: {exception.Message}");
                return false;
            }
        }

        private static string Choice(string value, params string[] values)
        {
            foreach (string option in values)
                if (string.Equals(value?.Trim(), option, StringComparison.OrdinalIgnoreCase)) return option;
            return values[0];
        }
    }
}
