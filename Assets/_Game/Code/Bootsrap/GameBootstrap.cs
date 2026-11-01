using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Cysharp.Threading.Tasks;

namespace _Game.Code.Bootsrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int defaultSceneIndex = 1;

        private const string TargetSceneKey =
            "Game.Bootstrap.TargetScene";

        
        private async UniTaskVoid Start()
        {
            Debug.Log("[Bootstrap] Bootstrap...");

            try
            {
                var targetScene = GetTargetScene();

                if (string.IsNullOrEmpty(targetScene))
                {
                    Debug.LogError(
                        $"Invalid startup scene index: {defaultSceneIndex}");
                    return;
                }

                Debug.Log($"[Bootstrap] Loading scene: {targetScene}");

#if UNITY_EDITOR
                var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                    targetScene,
                    new LoadSceneParameters(LoadSceneMode.Additive));
#else
        var operation = SceneManager.LoadSceneAsync(
            targetScene,
            LoadSceneMode.Additive);
#endif

                if (operation == null)
                {
                    Debug.LogError($"Failed to load: {targetScene}");
                    return;
                }

                await operation;

                var scene = SceneManager.GetSceneByPath(targetScene);

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    Debug.LogError($"Scene not loaded: {targetScene}");
                    return;
                }

                if (!SceneManager.SetActiveScene(scene))
                {
                    Debug.LogError($"Cannot activate: {targetScene}");
                    return;
                }

                Debug.Log($"[Bootstrap] Started scene: {scene.name}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }


        private string GetTargetScene()
        {
#if UNITY_EDITOR
            var editorScene = SessionState.GetString(
                TargetSceneKey,
                string.Empty);

            SessionState.EraseString(TargetSceneKey);

            if (!string.IsNullOrEmpty(editorScene))
                return editorScene;
#endif

            return SceneUtility.GetScenePathByBuildIndex(
                defaultSceneIndex);
        }
    }
}
