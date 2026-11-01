using System;
using _Game.Code.Core.Scenes;
using _Game.Code.Features.Audio;
using _Game.Code.Features.Input.View;
using _Game.Code.Features.MainMenu.View;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace _Game.Code.Features.MainMenu.Presenters
{
    public sealed class MainMenuPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly MainMenuView _view;
        private readonly PlayerInputView _input;
        private readonly GateAudio _audio;
        private readonly GateAudioEvents _sounds;
        private bool _loading;

        public MainMenuPresenter(MainMenuView view, PlayerInputView input, GateAudio audio, GateAudioEvents sounds)
        {
            _view = view;
            _input = input;
            _audio = audio;
            _sounds = sounds;
        }

        public void Initialize()
        {
            _view.StartRequested += Start;
            _view.QuitRequested += Quit;
            _input.CaptureCursor(false);
        }

        public void Tick()
        {
            _audio.ReleaseFinished();

            if (_input.Read().PausePressed && !_loading)
            {
                _view.CloseSettings();
            }
        }

        public void Dispose()
        {
            _view.StartRequested -= Start;
            _view.QuitRequested -= Quit;
            _audio.Dispose();
        }

        private void Start()
        {
            StartAsync().Forget();
        }

        private async UniTaskVoid StartAsync()
        {
            if (_loading)
            {
                return;
            }

            _loading = true;
            _view.SetLoading(true);
            _audio.Play(_sounds.button, Vector3.zero, true);

            try
            {
                _input.enabled = false;
                await SceneNavigation.ChangeTo(SceneNavigation.GatePath, _view.gameObject.scene);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _input.enabled = true;
                _loading = false;
                _view.SetLoading(false);
            }
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
