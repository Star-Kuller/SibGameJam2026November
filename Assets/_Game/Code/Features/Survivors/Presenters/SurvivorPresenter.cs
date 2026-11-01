using System;
using System.Collections.Generic;
using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Gates.Models;
using _Game.Code.Features.Survivors.Models;
using _Game.Code.Features.Survivors.View;
using UnityEngine;
using VContainer.Unity;

namespace _Game.Code.Features.Survivors.Presenters
{
    public sealed class SurvivorPresenter : IInitializable, IDisposable
    {
        private sealed class Person
        {
            public SurvivorView View;
            public float DeathAge;
        }

        private static readonly string[] Names =
        {
            "Анна", "Михаил", "Вера", "Илья", "Нина", "Лев",
            "Ольга", "Саша", "Марк", "Ирина", "Павел", "Соня"
        };

        private readonly GatePrototypeSettings _settings;
        private readonly GateEncounterModel _session;
        private readonly ColonyModel _colony;
        private readonly GateModel _gate;
        private readonly SurvivorSceneView _scene;
        private readonly PopulationHudView _hud;
        private readonly NotificationView _notifications;
        private readonly GateEncounterView _queries;
        private readonly GateAudio _audio;
        private readonly GateAudioEvents _sounds;
        private readonly List<Person> _people = new();
        private readonly List<SurvivorView> _visiblePeople = new();

        private double _nextArrival;
        private int _arrivals;
        private int _nextId;

        public int WaitingCount { get; private set; }

        public SurvivorPresenter(
            GatePrototypeSettings settings,
            GateEncounterModel session,
            ColonyModel colony,
            GateModel gate,
            SurvivorSceneView scene,
            PopulationHudView hud,
            NotificationView notifications,
            GateEncounterView queries,
            GateAudio audio,
            GateAudioEvents sounds)
        {
            _settings = settings;
            _session = session;
            _colony = colony;
            _gate = gate;
            _scene = scene;
            _hud = hud;
            _notifications = notifications;
            _queries = queries;
            _audio = audio;
            _sounds = sounds;
        }

        public void Initialize()
        {
            _hud.Bind(_colony, _gate);
            _hud.DispatchRequested += Dispatch;
            _hud.RecallRequested += RecallAll;
        }

        public void Dispose()
        {
            _hud.DispatchRequested -= Dispatch;
            _hud.RecallRequested -= RecallAll;
        }

        public void Reset()
        {
            Clear();
            _nextArrival = _settings.firstRefugeeDelay;
            _arrivals = 0;
            _nextId = 0;
            _colony.Reset();
            RefreshHud();
        }

        public void Clear()
        {
            foreach (var person in _people)
            {
                if (person.View != null)
                {
                    _scene.Remove(person.View);
                }
            }

            _people.Clear();
            _visiblePeople.Clear();
            WaitingCount = 0;
        }

        public void Tick(float seconds, double time)
        {
            SpawnRefugeeIfDue(time);

            for (var index = _people.Count - 1; index >= 0; index--)
            {
                var person = _people[index];
                var model = person.View.Model;

                if (model.State == SurvivorState.Dead)
                {
                    person.DeathAge += seconds;
                    person.View.ShowDeath(Mathf.Clamp01(person.DeathAge / _settings.deathDuration));

                    if (person.DeathAge >= _settings.deathDuration)
                    {
                        RemoveAt(index);
                    }

                    continue;
                }

                UpdatePerson(model, seconds);
                person.View.Render(false, _settings.survivorSpeed / 1.5f);

                if (model.State == SurvivorState.Safe)
                {
                    _colony.Admit(model.IsResident);
                    _notifications.Show(model.Name + (model.IsResident
                        ? " снова в общине."
                        : " теперь в безопасности. В общине новый житель."));
                    _audio.Play(model.IsResident ? _sounds.personReturned : _sounds.personSaved, model.Position);
                    RemoveAt(index);
                }
                else if (model.State == SurvivorState.Left)
                {
                    _notifications.Show(model.Name + " больше не ждёт у ворот.");
                    RemoveAt(index);
                }
            }

            RefreshHud();
        }

        public void Dispatch()
        {
            if (_session.State != SessionState.Running)
            {
                return;
            }

            if (!_gate.CanPass)
            {
                _notifications.Show("Сначала откройте ворота для выхода жителя.");
                return;
            }

            if (!_colony.Dispatch())
            {
                return;
            }

            Create(true, _scene.OutboundRoute());
            _notifications.Show("Житель вышел к посту. Прикройте путь или отзовите его.");
            _audio.Play(_sounds.order, Vector3.zero, true);
            RefreshHud();
        }

        public void RecallAll()
        {
            if (_session.State != SessionState.Running)
            {
                return;
            }

            var recalled = false;

            foreach (var person in _people)
            {
                recalled |= Recall(person.View);
            }

            if (recalled)
            {
                _notifications.Show("Приказ принят. Свои возвращаются к воротам.");
                _audio.Play(_sounds.order, Vector3.zero, true);
            }

            RefreshHud();
        }

        public void Flee(SurvivorView person)
        {
            if (Recall(person))
            {
                _notifications.Show(person.Model.Name + " в опасности! Прикройте возвращение.", true);
                _audio.Play(_sounds.personDanger, person.Model.Position);
            }
        }

        public SurvivorView FindNearest(Vector3 position, float radius)
        {
            SurvivorView closest = null;
            var closestDistance = radius * radius;

            foreach (var person in _people)
            {
                var model = person.View.Model;

                if (!model.IsVisible || model.Position.z < _settings.gatePlane)
                {
                    continue;
                }

                var distance = (model.Position - position).sqrMagnitude;

                if (distance < closestDistance)
                {
                    closest = person.View;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        public bool Kill(SurvivorView view, bool friendlyFire)
        {
            if (!view.Model.Die())
            {
                return false;
            }

            _colony.RecordDeath(view.Model.IsResident, friendlyFire);
            view.Render(false, 0);

            var reason = friendlyFire ? " Причина — ваш выстрел." : " Причина — атака зомби.";
            _notifications.Show("Потеря: " + view.Model.Name + "." + reason, true);
            _audio.Play(_sounds.personDeath, view.Model.Position);

            if (friendlyFire)
            {
                _audio.Play(_sounds.friendlyFire, Vector3.zero, true);
            }

            RefreshHud();
            return true;
        }

        public void SetPaused(bool paused)
        {
            foreach (var person in _people)
            {
                person.View.Render(paused, _settings.survivorSpeed / 1.5f);
            }
        }

        private bool Recall(SurvivorView view)
        {
            if (view.Model.State == SurvivorState.GoingOut
                && view.Model.Position.z < _settings.gatePlane)
            {
                view.Model.RetreatInside(_scene.InsidePosition);
                return true;
            }

            return view.Model.Recall(_scene.ReturnRoute(view.Model.Position));
        }

        private void UpdatePerson(SurvivorModel model, float seconds)
        {
            model.AdvanceWaiting(seconds);

            if (model.State == SurvivorState.Working
                && model.WorkingTime >= _settings.survivorWorkDuration)
            {
                model.Recall(_scene.ReturnRoute(model.Position));
            }

            // A closing door returns a person to the nearest side; it never kills them.
            if (!_gate.CanPass && model.State == SurvivorState.Entering
                && model.Position.z >= _settings.gatePlane)
            {
                model.MoveOutsideGate(_scene.WaitingPosition);
            }

            if (!_gate.CanPass && model.State == SurvivorState.GoingOut
                && model.Position.z < _settings.gatePlane)
            {
                model.RetreatInside(_scene.InsidePosition);
            }

            if (model.State == SurvivorState.Waiting)
            {
                if (_gate.CanPass && (model.IsResident || _colony.HasCapacity))
                {
                    model.Enter(_scene.EntranceRoute(model.Position));
                }
                else if (!model.IsResident && model.WaitingTime >= _settings.refugeePatience)
                {
                    model.Leave();
                }
            }

            model.Move(_settings.survivorSpeed * seconds);
        }

        private void SpawnRefugeeIfDue(double time)
        {
            if (_arrivals >= _settings.refugeeCount || time < _nextArrival)
            {
                return;
            }

            var route = _scene.ArrivalRoute();

            if (!_queries.IsSpawnClear(route[0], _settings.spawnClearance))
            {
                return;
            }

            _arrivals++;
            _nextArrival = time + _settings.refugeeInterval;

            Create(false, route);
            _notifications.Show("К воротам идёт беженец. Бирюзовая повязка — свой.");
        }

        private void Create(bool resident, Vector3[] route)
        {
            _nextId++;
            var name = Names[(_nextId - 1) % Names.Length];
            var model = new SurvivorModel(_nextId, name, resident, route);
            var view = _scene.Create();

            view.name = "Survivor " + model.Name;
            view.Bind(model);
            _people.Add(new Person { View = view });
        }

        private void RemoveAt(int index)
        {
            _scene.Remove(_people[index].View);
            _people.RemoveAt(index);
        }

        private void RefreshHud()
        {
            WaitingCount = 0;
            _visiblePeople.Clear();

            foreach (var person in _people)
            {
                if (person.View.Model.State == SurvivorState.Waiting)
                {
                    WaitingCount++;
                }

                if (person.View.Model.IsVisible)
                {
                    _visiblePeople.Add(person.View);
                }
            }

            _hud.ShowPeople(_visiblePeople, _settings.refugeePatience);
        }
    }
}
