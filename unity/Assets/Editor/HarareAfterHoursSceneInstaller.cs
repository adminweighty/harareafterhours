#if UNITY_EDITOR
using HarareAfterHours;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class HarareAfterHoursSceneInstaller
{
    [MenuItem("Harare After Hours/Create 3D Gameplay Scene")]
    private static void CreateGameplayScene()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Install(scene);
    }

    private static void Install(Scene scene)
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Remove the template listener in edit mode. The gameplay bootstrap
        // owns its listener, which prevents a noisy duplicate-listener warning
        // whenever Play Mode starts.
        foreach (AudioListener listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(listener);
        }

        if (Object.FindFirstObjectByType<HarareAfterHoursBootstrap>() != null)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        GameObject root = new("Harare After Hours 3D");
        root.AddComponent<HarareAfterHoursBootstrap>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Harare After Hours 3D scene installed. Press Play to start the first delivery.");
    }
}
#endif
