#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    [InitializeOnLoad]
    public static class AudioMenuValidation
    {
        private const string SessionKey = "Harare.AudioMenuQA";
        private static double _deadline;

        static AudioMenuValidation()
        {
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            Preserve(HarareArtLibrary.SoundMutedSaveKey);
            Preserve(HarareArtLibrary.ShuffleSaveKey);
            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(SessionKey, false) || !EditorApplication.isPlaying) return;
            if (_deadline == 0) _deadline = EditorApplication.timeSinceStartup + 90;
            if (Time.time < 3 && EditorApplication.timeSinceStartup < _deadline) return;

            try
            {
                Check(EditorApplication.timeSinceStartup < _deadline, "Audio menu validation timed out");
                HarareAfterHoursBootstrap game = HarareAfterHoursBootstrap.Instance;
                Check(game != null && game.ArtLibrary != null && game.Bridge != null, "World audio services load");
                HarareArtLibrary audio = game.ArtLibrary;
                Check(audio.TrackCount >= 2 && !string.IsNullOrWhiteSpace(audio.CurrentTrackName), "Soundtrack catalogue loads");

                game.Bridge.OnFlutterMessage("{\"type\":\"set_audio_muted\",\"payload\":\"true\"}");
                Check(audio.SoundMuted && AudioListener.volume == 0f, "Master mute silences every Unity source");
                Check(PlayerPrefs.GetInt(HarareArtLibrary.SoundMutedSaveKey, 0) == 1, "Mute preference persists");

                game.Bridge.OnFlutterMessage("{\"type\":\"set_audio_shuffle\",\"payload\":\"false\"}");
                Check(!audio.ShuffleEnabled, "Shuffle can be disabled");
                game.Bridge.OnFlutterMessage("{\"type\":\"set_audio_shuffle\",\"payload\":\"true\"}");
                Check(audio.ShuffleEnabled && PlayerPrefs.GetInt(HarareArtLibrary.ShuffleSaveKey, 0) == 1, "Shuffle preference persists");

                string previousTrack = audio.CurrentTrackName;
                game.Bridge.OnFlutterMessage("{\"type\":\"randomize_soundtrack\"}");
                Check(audio.CurrentTrackName != previousTrack, "New mix selects a different track");
                Check(game.Bridge.LastEvent.Contains("audio_state") && game.Bridge.LastEvent.Contains(audio.CurrentTrackName), "Flutter receives live audio state");

                game.Bridge.OnFlutterMessage("{\"type\":\"set_audio_muted\",\"payload\":\"false\"}");
                Check(!audio.SoundMuted && AudioListener.volume == 1f, "Sound restores immediately");
                Finish(0, "PASS: catalogue, master mute, persistent shuffle, different-track randomization and Flutter audio state.");
            }
            catch (Exception exception)
            {
                Finish(1, exception.ToString());
            }
        }

        private static void Preserve(string key)
        {
            SessionState.SetBool(SessionKey + key + ".exists", PlayerPrefs.HasKey(key));
            SessionState.SetInt(SessionKey + key + ".value", PlayerPrefs.GetInt(key, 0));
        }

        private static void Restore(string key)
        {
            if (SessionState.GetBool(SessionKey + key + ".exists", false))
                PlayerPrefs.SetInt(key, SessionState.GetInt(SessionKey + key + ".value", 0));
            else
                PlayerPrefs.DeleteKey(key);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Finish(int code, string report)
        {
            Restore(HarareArtLibrary.SoundMutedSaveKey);
            Restore(HarareArtLibrary.ShuffleSaveKey);
            PlayerPrefs.Save();
            AudioListener.volume = 1f;
            Time.timeScale = 1f;
            File.WriteAllText("/private/tmp/harare-audio-menu-validation.txt", report);
            Debug.Log(report);
            SessionState.EraseBool(SessionKey);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(code);
        }
    }
}
#endif
