using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.GateEncounter.Presenters;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Gates.Models;
using _Game.Code.Features.Gates.Presenters;
using _Game.Code.Features.Gates.View;
using _Game.Code.Features.Input.Models;
using _Game.Code.Features.Input.Presenters;
using _Game.Code.Features.Input.View;
using _Game.Code.Features.Presentation.View;
using _Game.Code.Features.Survivors.Models;
using _Game.Code.Features.Survivors.Presenters;
using _Game.Code.Features.Survivors.View;
using _Game.Code.Features.Turret.Presenters;
using _Game.Code.Features.Turret.View;
using _Game.Code.Features.Zombies.Models;
using _Game.Code.Features.Zombies.Presenters;
using _Game.Code.Features.Zombies.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Game.Code.Features.GateEncounter
{
    public sealed class GateEncounterScope : LifetimeScope
    {
        [SerializeField] private GatePrototypeSettings settings;
        [SerializeField] private GateAudioEvents audioEvents;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(settings);
            builder.RegisterInstance(audioEvents);

            builder.RegisterInstance(new GateEncounterModel());
            builder.RegisterInstance(new GateModel(settings.positionHealth));
            builder.RegisterInstance(new ColonyModel(settings.startingResidents, settings.populationCapacity));
            builder.RegisterInstance(new CombatStatisticsModel());
            builder.RegisterInstance(new PlayerSettingsModel(settings.mouseSensitivity));
            builder.RegisterInstance(new SpawnScheduleModel(
                settings.firstSpawnDelay,
                settings.spawnInterval,
                settings.shiftDuration));

            builder.Register<GateAudio>(Lifetime.Scoped);
            builder.Register<ZombiePresenter>(Lifetime.Scoped);
            builder.Register<OutcomePresenter>(Lifetime.Scoped);

            builder.RegisterComponentInHierarchy<PlayerInputView>();
            builder.RegisterComponentInHierarchy<TurretView>();
            builder.RegisterComponentInHierarchy<GateEncounterView>();
            builder.RegisterComponentInHierarchy<GateHudView>();
            builder.RegisterComponentInHierarchy<SessionMenuView>();
            builder.RegisterComponentInHierarchy<NotificationView>();
            builder.RegisterComponentInHierarchy<InputSettingsView>();
            builder.RegisterComponentInHierarchy<CctvOverlayView>();
            builder.RegisterComponentInHierarchy<GateControlsView>();
            builder.RegisterComponentInHierarchy<GateWorldView>();
            builder.RegisterComponentInHierarchy<PopulationHudView>();
            builder.RegisterComponentInHierarchy<ZombieSceneView>();
            builder.RegisterComponentInHierarchy<SurvivorSceneView>();

            builder.RegisterEntryPoint<SurvivorPresenter>().AsSelf();
            builder.RegisterEntryPoint<GatePresenter>().AsSelf();
            builder.RegisterEntryPoint<InputSettingsPresenter>().AsSelf();
            builder.RegisterEntryPoint<TurretPresenter>().AsSelf();
            builder.RegisterEntryPoint<GateEncounterPresenter>().AsSelf();
        }
    }
}
