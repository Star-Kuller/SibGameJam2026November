using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.GateEncounter.Presenters;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Gates.Models;
using _Game.Code.Features.Gates.Presenters;
using _Game.Code.Features.Input.View;
using _Game.Code.Features.Survivors.Models;
using _Game.Code.Features.Survivors.Presenters;
using _Game.Code.Features.Survivors.View;
using _Game.Code.Features.Turret.Presenters;
using _Game.Code.Features.Turret.View;
using _Game.Code.Features.Zombies.Models;
using _Game.Code.Features.Zombies.Presenters;
using _Game.Code.Features.Zombies.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;
using SessionState = _Game.Code.Features.GateEncounter.Models.SessionState;

namespace _Game.Code.Editor
{
    public static class GatePlayModeChecks
    {
        private const float StepSeconds = 1f / 30;

        [MenuItem("Last Shift/Run Play Mode Checks")]
        public static void RunFromMenu()
        {
            Debug.Log("[Last Shift] " + RunAll());
        }

        public static string RunAll()
        {
            Require(Application.isPlaying, "Enter Play Mode first.");
            using var game = new Harness();
            var results = new List<object>
            {
                CheckFullShift(game),
                CheckDestroyedGate(game),
                CheckBreach(game),
                CheckLastResident(game),
                CheckPauseAndRestart(game)
            };

            return JsonConvert.SerializeObject(new { result = "PASS", checks = results });
        }

        private static object CheckFullShift(Harness game)
        {
            game.StartNewRun();
            var peakEnemies = 0;
            var dispatched = false;

            while (game.Session.State == SessionState.Running && game.Session.ElapsedTime < 250)
            {
                var people = game.VisiblePeople();
                var waiting = people.Any(person => person.Model.State == SurvivorState.Waiting);
                var crossing = people.Any(person => person.Model.State == SurvivorState.Entering
                    || person.Model.State == SurvivorState.GoingOut
                    && person.Model.Position.z < game.Settings.gatePlane);

                var dangerAtGate = game.LiveZombies().Any(zombie => zombie.Model.Position.z < -8.5f);
                var dispatchNow = !dispatched && game.Session.ElapsedTime >= 45
                    && game.Session.ElapsedTime < 60;
                var shouldOpen = (waiting || crossing || dispatchNow) && !dangerAtGate;
                var toggle = game.Gate.State == GateState.Closed && shouldOpen
                    || game.Gate.State == GateState.Open && !shouldOpen;

                game.Gates.Tick(StepSeconds, toggle);

                if (dispatchNow && game.Gate.CanPass)
                {
                    game.People.Dispatch();
                    dispatched = game.Colony.Outside > 0;
                }

                game.AdvanceActors(StepSeconds, true);
                peakEnemies = Math.Max(peakEnemies, game.Zombies.LivingCount);
                Require(peakEnemies <= game.Settings.maxLivingZombies, "Living enemy limit exceeded.");
            }

            Require(game.Session.State == SessionState.Won,
                $"Full shift failed: {game.Session.State}, time={game.Session.ElapsedTime:F1}, "
                + $"health={game.Gate.Health}, kills={game.Statistics.Kills}, shots={game.Statistics.Shots}.");
            Require(game.Colony.Rescued > 0, "The full shift must include a successful rescue.");
            Require(game.Colony.FriendlyFireDeaths == 0, "The test gunner must not shoot through a friendly.");

            return new
            {
                scenario = "complete shift through real raycasts",
                seconds = Math.Round(game.Session.ElapsedTime, 2),
                health = game.Gate.Health,
                kills = game.Statistics.Kills,
                shots = game.Statistics.Shots,
                rescued = game.Colony.Rescued,
                alive = game.Colony.Total,
                peakEnemies,
                dispatched
            };
        }

        private static object CheckDestroyedGate(Harness game)
        {
            game.StartNewRun();

            while (game.Session.State == SessionState.Running && game.Session.ElapsedTime < 220)
            {
                game.Gates.Tick(0.1f, false);
                game.AdvanceActors(0.1f, false);
            }

            Require(game.Session.State == SessionState.Lost && game.Gate.Health == 0,
                "Ignoring attackers must destroy the closed gate.");
            Require(game.Session.ResultReason.Contains("Ворота"), "Gate defeat needs a clear reason.");

            return new { scenario = "gate destruction", seconds = Math.Round(game.Session.ElapsedTime, 2) };
        }

        private static object CheckBreach(Harness game)
        {
            game.StartNewRun();
            game.Session.AdvanceTime(game.Settings.firstSpawnDelay + 0.1f);
            game.Zombies.Move(0);

            var zombie = game.LiveZombies().First();
            zombie.Model.Move(100);
            zombie.Model.ScheduleFirstAttack(game.Session.ElapsedTime, game.Settings.attackInterval);
            game.Gates.Tick(game.Settings.gateTransitionDuration, true);
            game.Zombies.Move(3);

            Require(game.Session.State == SessionState.Lost
                && game.Session.ResultReason.Contains("прорвался"),
                "Crossing the open gate must immediately end the shift.");

            return new { scenario = "open gate breach", reason = game.Session.ResultReason };
        }

        private static object CheckLastResident(Harness game)
        {
            game.StartNewRun();
            game.Gates.Tick(game.Settings.gateTransitionDuration, true);

            for (var index = 0; index < game.Settings.startingResidents; index++)
            {
                game.People.Dispatch();
                var person = game.VisiblePeople().Single();
                Require(game.People.Kill(person, true), "Each dispatched resident must die once.");
            }

            game.Outcome.Evaluate();
            Require(game.Colony.Total == 0 && game.Session.State == SessionState.Lost,
                "Losing the last resident must end the shift.");
            Require(game.Colony.FriendlyFireDeaths == game.Settings.startingResidents,
                "Friendly-fire losses must match the actual people.");

            return new { scenario = "last resident lost", reason = game.Session.ResultReason };
        }

        private static object CheckPauseAndRestart(Harness game)
        {
            game.StartNewRun();
            game.Gates.Tick(0.25f, true);
            game.Session.AdvanceTime(1);
            game.Session.Pause();

            var elapsed = game.Session.ElapsedTime;
            var openness = game.Gate.Openness;

            for (var index = 0; index < 20; index++)
            {
                game.Loop.Tick();
            }

            Require(game.Session.ElapsedTime == elapsed && game.Gate.Openness == openness,
                "Pause must freeze both time and gate movement.");

            game.Session.Resume();
            game.Turret.Tick(0.1f, InputFrame(true), true);
            Require(game.Statistics.Shots == 0, "Held fire must not resume automatically.");

            game.Turret.Tick(0.1f, InputFrame(false), true);
            game.Session.AdvanceTime(0.21f);
            game.Turret.Tick(0.1f, InputFrame(true), true);
            Require(game.Statistics.Shots == 1, "A released and pressed fire action must fire again.");

            var input = Object.FindFirstObjectByType<PlayerInputView>();
            typeof(PlayerInputView).GetMethod("OnApplicationFocus", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(input, new object[] { false });
            Require(game.Session.State == SessionState.Paused, "Focus loss must pause the game.");

            for (var index = 0; index < 2; index++)
            {
                game.StartNewRun();
                Require(game.Gate.Health == game.Gate.MaximumHealth
                    && game.Colony.Total == game.Settings.startingResidents
                    && game.Statistics.Kills == 0 && game.Zombies.LivingCount == 0
                    && game.VisiblePeople().Length == 0,
                    "Restart must not retain entities or results.");
            }

            Require(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Count(camera => camera.isActiveAndEnabled) == 1, "There must be one gameplay camera.");
            Require(Object.FindObjectsByType<FMODUnity.StudioListener>(FindObjectsSortMode.None)
                .Count(listener => listener.isActiveAndEnabled) == 1, "There must be one FMOD listener.");

            return new { scenario = "pause, focus, held fire and two restarts" };
        }

        private static PlayerInputFrame InputFrame(bool fire)
        {
            return new PlayerInputFrame(Vector2.zero, false, fire, false, false, false, false);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class Harness : IDisposable
        {
            private readonly GateAudio _audio;
            private readonly GateAudioEvents _sounds;
            private readonly FieldInfo[] _soundFields;
            private readonly object[] _soundValues;
            private readonly bool _wasEditorPaused;
            private readonly GateEncounterView _queries;
            private readonly TurretView _turretView;

            public GatePrototypeSettings Settings { get; }
            public GateEncounterModel Session { get; }
            public GateEncounterPresenter Loop { get; }
            public GateModel Gate { get; }
            public GatePresenter Gates { get; }
            public ColonyModel Colony { get; }
            public SurvivorPresenter People { get; }
            public ZombiePresenter Zombies { get; }
            public TurretPresenter Turret { get; }
            public CombatStatisticsModel Statistics { get; }
            public OutcomePresenter Outcome { get; }

            public Harness()
            {
                var scope = Object.FindFirstObjectByType<GateEncounterScope>();
                var container = scope.Container;

                Settings = container.Resolve<GatePrototypeSettings>();
                Session = container.Resolve<GateEncounterModel>();
                Loop = container.Resolve<GateEncounterPresenter>();
                Gate = container.Resolve<GateModel>();
                Gates = container.Resolve<GatePresenter>();
                Colony = container.Resolve<ColonyModel>();
                People = container.Resolve<SurvivorPresenter>();
                Zombies = container.Resolve<ZombiePresenter>();
                Turret = container.Resolve<TurretPresenter>();
                Statistics = container.Resolve<CombatStatisticsModel>();
                Outcome = container.Resolve<OutcomePresenter>();
                _audio = container.Resolve<GateAudio>();
                _sounds = container.Resolve<GateAudioEvents>();
                _queries = Object.FindFirstObjectByType<GateEncounterView>();
                _turretView = Object.FindFirstObjectByType<TurretView>();

                _wasEditorPaused = EditorApplication.isPaused;
                EditorApplication.isPaused = true;

                // The accelerated simulation must not enqueue minutes of audio in a few seconds.
                _audio.Dispose();
                _soundFields = typeof(GateAudioEvents).GetFields()
                    .Where(field => field.FieldType == typeof(FMODUnity.EventReference)).ToArray();
                _soundValues = _soundFields.Select(field => field.GetValue(_sounds)).ToArray();

                foreach (var field in _soundFields)
                {
                    field.SetValue(_sounds, default(FMODUnity.EventReference));
                }
            }

            public void StartNewRun()
            {
                if (Session.State == SessionState.Running)
                {
                    Session.Pause();
                }

                var buttonName = Session.State == SessionState.MainMenu ? "start" : "restart";
                var root = Object.FindFirstObjectByType<UnityEngine.UIElements.UIDocument>().rootVisualElement;
                var button = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root, buttonName);

                using (var evt = UnityEngine.UIElements.NavigationSubmitEvent.GetPooled())
                {
                    evt.target = button;
                    button.SendEvent(evt);
                }

                Loop.Tick();
                Session.Resume();
                Turret.Tick(0, InputFrame(false), true);
            }

            public SurvivorView[] VisiblePeople()
            {
                return Object.FindObjectsByType<SurvivorView>(FindObjectsSortMode.None)
                    .Where(person => person.Model.IsVisible).ToArray();
            }

            public ZombieView[] LiveZombies()
            {
                return Object.FindObjectsByType<ZombieView>(FindObjectsSortMode.None)
                    .Where(zombie => zombie.Model.State != ZombieState.Dead)
                    .OrderBy(zombie => zombie.Model.Position.z).ToArray();
            }

            public void AdvanceActors(float seconds, bool autoAim)
            {
                Session.AdvanceTime(seconds);
                People.Tick(seconds, Session.ElapsedTime);
                Zombies.Move(seconds);

                if (Session.State != SessionState.Running)
                {
                    return;
                }

                var fire = autoAim && AimAtVisibleZombie();
                Turret.Tick(seconds, InputFrame(fire), true);
                Zombies.ResolveAttacks();
                Zombies.Cleanup(seconds);
                Outcome.Evaluate();
            }

            public void Dispose()
            {
                _audio.Dispose();
                People.Clear();
                Zombies.Clear();
                Gates.Reset();
                Colony.Reset();
                Statistics.Reset();
                Turret.Reset();
                Session.ReturnToMenu();

                for (var index = 0; index < _soundFields.Length; index++)
                {
                    _soundFields[index].SetValue(_sounds, _soundValues[index]);
                }

                EditorApplication.isPaused = _wasEditorPaused;
            }

            private bool AimAtVisibleZombie()
            {
                var camera = _turretView.Camera;

                foreach (var zombie in LiveZombies())
                {
                    var offset = zombie.Model.Position + Vector3.up * 1.65f - camera.transform.position;
                    var yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                    var pitch = -Mathf.Atan2(offset.y, new Vector2(offset.x, offset.z).magnitude) * Mathf.Rad2Deg;

                    Turret.Model.Aim(new Vector2(yaw - Turret.Model.Yaw, Turret.Model.Pitch - pitch));
                    _turretView.RenderAim(Turret.Model);
                    Physics.SyncTransforms();

                    if (_queries.QueryShot(Settings.range).Zombie == zombie)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
