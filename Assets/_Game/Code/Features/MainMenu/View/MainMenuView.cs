using System;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Input.View;
using UnityEngine.UIElements;

namespace _Game.Code.Features.MainMenu.View
{
    public sealed class MainMenuView : DocumentSectionView, ISettingsPanel
    {
        private VisualElement _home;
        private VisualElement _settings;
        private Button _start;
        private Button _quit;
        private Button _openSettings;
        private Button _back;

        public event Action StartRequested;
        public event Action QuitRequested;
        public event Action SettingsClosed;

        protected override void BindElements()
        {
            _home = Element<VisualElement>("menu-home");
            _settings = Element<VisualElement>("settings-panel");
            _start = Element<Button>("start");
            _quit = Element<Button>("quit");
            _openSettings = Element<Button>("open-settings");
            _back = Element<Button>("settings-back");
            _start.clicked += OnStartRequested;
            _quit.clicked += Quit;
            _openSettings.clicked += OpenSettings;
            _back.clicked += CloseSettings;
            _start.schedule.Execute(_start.Focus);
        }

        protected override void UnbindElements()
        {
            _start.clicked -= OnStartRequested;
            _quit.clicked -= Quit;
            _openSettings.clicked -= OpenSettings;
            _back.clicked -= CloseSettings;
        }

        public void CloseSettings()
        {
            if (_settings.style.display == DisplayStyle.Flex)
            {
                _settings.style.display = DisplayStyle.None;
                _home.style.display = DisplayStyle.Flex;
                SettingsClosed?.Invoke();
                _openSettings.Focus();
            }
        }

        public void SetLoading(bool loading)
        {
            _start.SetEnabled(!loading);
            _quit.SetEnabled(!loading);
            _openSettings.SetEnabled(!loading);
            _start.text = loading ? "Загрузка…" : "Старт";
        }

        private void OpenSettings()
        {
            _home.style.display = DisplayStyle.None;
            _settings.style.display = DisplayStyle.Flex;
            _back.Focus();
        }

        private void OnStartRequested()
        {
            StartRequested?.Invoke();
        }

        private void Quit()
        {
            QuitRequested?.Invoke();
        }
    }
}
