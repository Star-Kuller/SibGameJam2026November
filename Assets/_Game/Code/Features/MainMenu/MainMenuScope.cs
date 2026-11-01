using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.Input.Models;
using _Game.Code.Features.Input.Presenters;
using _Game.Code.Features.Input.View;
using _Game.Code.Features.MainMenu.Presenters;
using _Game.Code.Features.MainMenu.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Game.Code.Features.MainMenu
{
    public sealed class MainMenuScope : LifetimeScope
    {
        [SerializeField] private GatePrototypeSettings settings;
        [SerializeField] private GateAudioEvents audioEvents;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(settings);
            builder.RegisterInstance(audioEvents);
            builder.RegisterInstance(new PlayerSettingsModel(settings.mouseSensitivity));
            builder.Register<GateAudio>(Lifetime.Scoped);
            builder.RegisterComponentInHierarchy<PlayerInputView>();
            builder.RegisterComponentInHierarchy<InputSettingsView>();
            builder.RegisterComponentInHierarchy<MainMenuView>().AsSelf().As<ISettingsPanel>();
            builder.RegisterEntryPoint<InputSettingsPresenter>();
            builder.RegisterEntryPoint<MainMenuPresenter>();
        }
    }
}
