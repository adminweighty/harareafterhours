using System;
using UnityEngine;

namespace HarareAfterHours
{
    public enum GameDifficulty
    {
        Beginner,
        Intermediate,
        Expert
    }

    /// <summary>Runtime combat tuning that is small enough to persist as a player preference.</summary>
    public readonly struct DifficultyProfile
    {
        public readonly int EnemyHits;
        public readonly float IncomingDamage;
        public readonly float HealthRecovery;
        public readonly float EngageRange;
        public readonly float DisengageRange;
        public readonly float EnemySpeed;
        public readonly float MeleeWindup;
        public readonly float MeleeCooldown;
        public readonly float RangedWindup;
        public readonly float RangedCooldown;
        public readonly int StreetEnemyCount;
        public readonly int MissionEnemyBonus;

        public DifficultyProfile(int enemyHits, float incomingDamage, float healthRecovery,
            float engageRange, float disengageRange, float enemySpeed, float meleeWindup,
            float meleeCooldown, float rangedWindup, float rangedCooldown,
            int streetEnemyCount, int missionEnemyBonus)
        {
            EnemyHits = enemyHits;
            IncomingDamage = incomingDamage;
            HealthRecovery = healthRecovery;
            EngageRange = engageRange;
            DisengageRange = disengageRange;
            EnemySpeed = enemySpeed;
            MeleeWindup = meleeWindup;
            MeleeCooldown = meleeCooldown;
            RangedWindup = rangedWindup;
            RangedCooldown = rangedCooldown;
            StreetEnemyCount = streetEnemyCount;
            MissionEnemyBonus = missionEnemyBonus;
        }
    }

    public static class GameDifficultySettings
    {
        private const string PreferenceKey = "game_difficulty_v1";
        private static GameDifficulty? _current;

        public static GameDifficulty Current
        {
            get
            {
                if (_current.HasValue) return _current.Value;
                int saved = Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, (int)GameDifficulty.Intermediate), 0, 2);
                _current = (GameDifficulty)saved;
                return _current.Value;
            }
        }

        public static DifficultyProfile Profile
        {
            get
            {
                switch (Current)
                {
                    case GameDifficulty.Beginner:
                        return new DifficultyProfile(2, .65f, 4.5f, 10f, 21f, 2.65f, 1.05f, 1.65f, 1.15f, 2.35f, 8, 0);
                    case GameDifficulty.Expert:
                        return new DifficultyProfile(4, 1.35f, 1.8f, 16f, 31f, 3.55f, .58f, .85f, .62f, 1.25f, 14, 2);
                    default:
                        return new DifficultyProfile(3, 1f, 3f, 12f, 25f, 3.1f, .8f, 1.2f, .85f, 1.8f, 11, 1);
                }
            }
        }

        public static bool Apply(string value)
        {
            GameDifficulty parsed;
            if (!Enum.TryParse(value, true, out parsed) || !Enum.IsDefined(typeof(GameDifficulty), parsed)) return false;
            _current = parsed;
            PlayerPrefs.SetInt(PreferenceKey, (int)parsed);
            PlayerPrefs.Save();
            return true;
        }
    }
}
