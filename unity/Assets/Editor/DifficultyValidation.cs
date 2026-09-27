#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

namespace HarareAfterHours.EditorTools
{
    public static class DifficultyValidation
    {
        public static void Run()
        {
            int exitCode = 0;
            string previous = GameDifficultySettings.Current.ToString();
            string report;
            try
            {
                Check(GameDifficultySettings.Apply("Beginner"), "Beginner can be selected");
                DifficultyProfile beginner = GameDifficultySettings.Profile;
                Check(beginner.EnemyHits == 2 && beginner.IncomingDamage < 1f && beginner.MeleeWindup > .8f,
                    "Beginner eases toughness, damage and reaction time");

                Check(GameDifficultySettings.Apply("Intermediate"), "Intermediate can be selected");
                DifficultyProfile intermediate = GameDifficultySettings.Profile;
                Check(intermediate.EnemyHits == 3 && Math.Abs(intermediate.IncomingDamage - 1f) < .001f,
                    "Intermediate preserves the original balance");

                Check(GameDifficultySettings.Apply("Expert"), "Expert can be selected");
                DifficultyProfile expert = GameDifficultySettings.Profile;
                Check(expert.EnemyHits == 4 && expert.IncomingDamage > 1f &&
                      expert.EngageRange > intermediate.EngageRange && expert.MeleeWindup < intermediate.MeleeWindup,
                    "Expert increases toughness, threat range, damage and cadence");
                Check(!GameDifficultySettings.Apply("Impossible"), "Unknown levels are rejected");
                report = "PASS: Beginner, Intermediate and Expert persistence and combat tuning validated.";
            }
            catch (Exception exception)
            {
                exitCode = 1;
                report = "FAIL: " + exception;
            }
            finally
            {
                GameDifficultySettings.Apply(previous);
            }

            Directory.CreateDirectory("/private/tmp/harare-difficulty");
            File.WriteAllText("/private/tmp/harare-difficulty/validation.txt", report);
            UnityEngine.Debug.Log(report);
            EditorApplication.Exit(exitCode);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
