using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Game.Code.Editor
{
    [InitializeOnLoad]
    public static class BootstrapPlayMode
    {
        private const string BootstrapPath =
            "Assets/_Game/Scenes/Bootstrap.unity";

        private const string TargetSceneKey =
            "Game.Bootstrap.TargetScene";

        static BootstrapPlayMode()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            EditorApplication.delayCall += Configure;
        }

        private static void Configure()
        {
            var bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                BootstrapPath);

            if (bootstrap == null)
            {
                Debug.LogError(
                    $"Bootstrap scene not found: {BootstrapPath}");
                return;
            }

            EditorSceneManager.playModeStartScene = bootstrap;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            if (!SaveScenesBeforePlay())
            {
                SessionState.EraseString(TargetSceneKey);
                EditorApplication.isPlaying = false;
                return;
            }

            var currentScene = SceneManager.GetActiveScene();
            var path = currentScene.path;

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError(
                    "Save the active scene before entering Play Mode.");

                SessionState.EraseString(TargetSceneKey);
                EditorApplication.isPlaying = false;
                return;
            }

            if (path == BootstrapPath)
                path = string.Empty;

            SessionState.SetString(TargetSceneKey, path);
        }

        private static bool SaveScenesBeforePlay()
        {
            var hasChanges = false;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (!SceneManager.GetSceneAt(i).isDirty)
                    continue;

                hasChanges = true;
                break;
            }

            if (!hasChanges)
                return true;

            var shouldSave = EditorUtility.DisplayDialog(
                "Unsaved Scenes",
                "Bootstrap loads scenes from disk. " +
                "Save modified scenes before Play Mode?",
                "Save & Play",
                "Cancel");

            return shouldSave && EditorSceneManager.SaveOpenScenes();
        }
    }
}
