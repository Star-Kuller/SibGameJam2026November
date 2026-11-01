using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Game.Code.Core.Scenes
{
    public static class SceneNavigation
    {
        public const string MainMenuPath = "Assets/_Game/Scenes/MainMenu.unity";
        public const string GatePath = "Assets/_Game/Scenes/GatePrototype.unity";

        private static bool _loading;

        public static async UniTask ChangeTo(string path, Scene current)
        {
            if (_loading)
            {
                await UniTask.WaitUntil(() => !_loading);
            }

            _loading = true;
            var roots = current.IsValid() ? current.GetRootGameObjects() : Array.Empty<UnityEngine.GameObject>();
            var active = new bool[roots.Length];

            for (var index = 0; index < roots.Length; index++)
            {
                active[index] = roots[index].activeSelf;
                if (roots[index].GetComponentInChildren<Camera>() == null)
                {
                    roots[index].SetActive(false);
                }
            }

            try
            {
                await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                var loaded = SceneManager.GetSceneByPath(path);

                if (!loaded.IsValid() || !loaded.isLoaded)
                {
                    throw new InvalidOperationException("Scene could not be loaded: " + path);
                }

                foreach (var root in roots)
                {
                    root.SetActive(false);
                }

                SceneManager.SetActiveScene(loaded);

                if (current.IsValid() && current.isLoaded)
                {
                    await SceneManager.UnloadSceneAsync(current);
                }
            }
            catch
            {
                for (var index = 0; index < roots.Length; index++)
                {
                    if (roots[index] != null)
                    {
                        roots[index].SetActive(active[index]);
                    }
                }

                throw;
            }
            finally
            {
                _loading = false;
            }
        }
    }
}
