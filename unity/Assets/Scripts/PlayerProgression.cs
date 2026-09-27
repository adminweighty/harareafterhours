using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    public enum CombatAction
    {
        Pulse,
        Fight,
    }

    /// <summary>
    /// Keeps a small, local score loop for the playable slice. Scores are
    /// forwarded to Flutter so a host app can persist them later, while Unity
    /// remains fully playable offline.
    /// </summary>
    public sealed class PlayerProgression : MonoBehaviour
    {
        [SerializeField] private float comboWindowSeconds = 6f;

        private FlutterGameBridge _bridge;
        private float _comboExpiresAt;
        public const string SaveKey = "Harare.Points.v1";
        public static string ActiveSaveKey
        {
            get
            {
#if UNITY_EDITOR
                if (UnityEditor.SessionState.GetBool(MissionDirector.ValidationSessionKey, false) ||
                    UnityEditor.SessionState.GetBool(ActOneCampaign.ValidationSessionKey, false)) return SaveKey + ".validation";
#endif
                return SaveKey;
            }
        }
        private readonly HashSet<string> _missionAwards = new();

        [Serializable]
        private sealed class ScoreSave
        {
            public int points;
            public int challenges;
            public string[] awards;
        }

        public int Points { get; private set; }
        public bool HasMissionAward(string id) => _missionAwards.Contains(id);
        public int Combo { get; private set; }
        public int ChallengesCompleted { get; private set; }

        public void Configure(FlutterGameBridge bridge)
        {
            _bridge = bridge;
            try
            {
                ScoreSave saved = JsonUtility.FromJson<ScoreSave>(PlayerPrefs.GetString(ActiveSaveKey, "{}"));
                if (saved != null)
                {
                    Points = Mathf.Max(0, saved.points);
                    ChallengesCompleted = Mathf.Max(0, saved.challenges);
                    if (saved.awards != null) foreach (string award in saved.awards) _missionAwards.Add(award);
                }
            }
            catch (ArgumentException) { Debug.LogWarning("Starting a new local points record."); }
            PublishScore("ready");
        }

        private void Update()
        {
            if (Combo > 0 && Time.time > _comboExpiresAt)
            {
                Combo = 0;
                PublishScore("combo_expired");
            }
        }

        public void AwardChallenge(CombatAction action)
        {
            int basePoints = action == CombatAction.Pulse ? 100 : 150;
            int multiplier = Mathf.Clamp(1 + Combo / 3, 1, 4);

            Points += basePoints * multiplier;
            Combo++;
            ChallengesCompleted++;
            _comboExpiresAt = Time.time + comboWindowSeconds;
            Save();
            PublishScore(action == CombatAction.Pulse ? "pulse" : "fight");
        }

        public void AwardMissionStep(string id, int points)
        {
            if (string.IsNullOrEmpty(id) || points <= 0 || !_missionAwards.Add(id)) return;
            Points += points;
            Save();
            PublishScore(id);
        }

        private void Save()
        {
            PlayerPrefs.SetString(ActiveSaveKey, JsonUtility.ToJson(new ScoreSave
            {
                points = Points, challenges = ChallengesCompleted,
                awards = new List<string>(_missionAwards).ToArray(),
            }));
            PlayerPrefs.Save();
        }
        public void AwardStreetEncounter(int points)
        {
            Points+=Mathf.Clamp(points,0,250);ChallengesCompleted++;Save();PublishScore("street_defence");
        }
        public int SpendPoints(int maximum,string reason)
        {
            int amount=Mathf.Min(Points,Mathf.Max(0,maximum));Points-=amount;Combo=0;Save();PublishScore(reason);return amount;
        }

        public void RegisterMiss()
        {
            if (Combo == 0) return;
            Combo = 0;
            PublishScore("combo_reset");
        }

        private void PublishScore(string action)
        {
            _bridge?.Publish(
                "score_updated",
                $"{{\"points\":{Points},\"combo\":{Combo},\"challenges\":{ChallengesCompleted},\"action\":\"{action}\"}}"
            );
        }
    }
}
