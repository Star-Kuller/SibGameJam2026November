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
            public float ReturnSpeed;
            public float GatheringDuration;
        }

        private readonly GatePrototypeSettings _settings;
        private readonly GateEncounterModel _session;
        private readonly ColonyModel _colony;
        private readonly StockpileModel _stockpile;
        private readonly GateModel _gate;
        private readonly SurvivorSceneView _scene;
        private readonly PopulationHudView _hud;
        private readonly NotificationView _notifications;
        private readonly GateEncounterView _queries;
        private readonly GateAudio _audio;
        private readonly GateAudioEvents _sounds;
        private readonly List<Person> _people = new();

        private double _nextArrival;
        private int _arrivals;

        public int WaitingCount { get; private set; }

        public SurvivorPresenter(
            GatePrototypeSettings settings,
            GateEncounterModel session,
            ColonyModel colony,
            StockpileModel stockpile,
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
            _stockpile = stockpile;
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
            _hud.RecallPersonRequested += RecallPerson;
        }

        public void Dispose()
        {
            _hud.DispatchRequested -= Dispatch;
            _hud.RecallRequested -= RecallAll;
            _hud.RecallPersonRequested -= RecallPerson;
        }

        public void Reset()
        {
            Clear();
            var approachSeconds = RouteLength(_scene.ArrivalRoute()) / _settings.survivorSpeed;
            _nextArrival = _settings.firstRefugeeDelay - approachSeconds;
            _arrivals = 0;
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

                UpdatePerson(person, seconds);
                person.View.Render(false, _settings.survivorSpeed / 1.5f);

                if (model.State == SurvivorState.Safe)
                {
                    var wasResident = model.IsResident;
                    _colony.Admit(model);

                    if (model.UnloadCargo(out var metal, out var rounds))
                    {
                        _stockpile.Deliver(metal, rounds);
                        _notifications.Show($"{model.Name}: доставлено {metal} металла и {rounds} патронов.");
                    }
                    else
                    {
                        _notifications.Show(model.Name + (wasResident
                            ? " вернулся без груза."
                            : " вошёл в общину."));
                    }

                    _audio.Play(wasResident ? _sounds.personReturned : _sounds.personSaved, model.Position);
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
            foreach (var person in _colony.People)
            {
                if (person.IsResident && person.State == SurvivorState.Safe)
                {
                    Dispatch(person.EntityId);
                    return;
                }
            }
        }

        public void Dispatch(int personId)
        {
            if (_session.State != SessionState.Running)
            {
                return;
            }

            var route = _scene.OutboundRoute();

            if (!_colony.Dispatch(personId, route, !_gate.CanPass))
            {
                return;
            }

            Create(_colony.Find(personId), route);
            _notifications.Show(_gate.CanPass
                ? "Вылазка началась. Прикройте возвращение с грузом."
                : "Житель готов к вылазке и ждёт внутри. Откройте ворота для выхода.");
            _audio.Play(_sounds.order, Vector3.zero, true);
            RefreshHud();
        }

        public void CancelSelection()
        {
            _hud.CancelSelection();
        }

        private void RecallPerson(int personId)
        {
            if (_session.State != SessionState.Running)
            {
                return;
            }

            var person = _people.Find(item => item.View.Model.EntityId == personId);

            if (person != null && Recall(person.View))
            {
                _notifications.Show(person.View.Model.Name + " возвращается к воротам.");
                _audio.Play(_sounds.order, Vector3.zero, true);
                RefreshHud();
            }
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
            if (!view.Model.Die(friendlyFire))
            {
                return false;
            }

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
            if ((view.Model.State == SurvivorState.GoingOut || view.Model.State == SurvivorState.WaitingToLeave)
                && view.Model.Position.z < _settings.gatePlane)
            {
                view.Model.RetreatInside(_scene.InsidePosition);
                return true;
            }

            var route = _scene.ReturnRoute(view.Model.Position);
            var recalled = view.Model.Recall(route);

            if (recalled)
            {
                var person = _people.Find(item => item.View == view);
                var entrance = _scene.EntranceRoute(_scene.WaitingPosition);
                var distance = RouteLength(route) + RouteLength(entrance);
                person.ReturnSpeed = distance / _settings.expeditionReturnDuration;
            }

            return recalled;
        }

        private void UpdatePerson(Person person, float seconds)
        {
            var model = person.View.Model;

            if (model.State == SurvivorState.WaitingToLeave && _gate.CanPass && model.BeginExit())
            {
                _notifications.Show(model.Name + " вышел за ресурсами.");
            }

            model.AdvanceWaiting(seconds);

            if (model.State == SurvivorState.Working)
            {
                var completion = model.ExpeditionTime >= _settings.survivorWorkDuration
                    ? 1
                    : model.WorkingTime / person.GatheringDuration;
                model.GatherCargo(completion, _settings.expeditionMetal, _settings.expeditionRounds);

                if (model.ExpeditionTime >= _settings.survivorWorkDuration)
                {
                    Recall(person.View);
                }
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

            var returning = model.State == SurvivorState.Returning || model.State == SurvivorState.Entering;
            var speed = model.IsResident && returning
                ? person.ReturnSpeed
                : _settings.survivorSpeed;
            model.Move(speed * seconds);
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
            _nextArrival += _settings.refugeeInterval;

            Create(_colony.CreateRefugee(route), route);
            _notifications.Show("К воротам идёт беженец. Бирюзовая повязка — свой.");
        }

        private void Create(SurvivorModel model, Vector3[] route)
        {
            var view = _scene.Create();

            view.name = "Survivor " + model.Name;
            view.Bind(model);
            var outwardSeconds = RouteLength(route) / _settings.survivorSpeed;
            _people.Add(new Person
            {
                View = view,
                ReturnSpeed = _settings.survivorSpeed,
                GatheringDuration = Mathf.Max(1, _settings.survivorWorkDuration - outwardSeconds)
            });
        }

        private static float RouteLength(Vector3[] route)
        {
            var length = 0f;

            for (var index = 1; index < route.Length; index++)
            {
                length += Vector3.Distance(route[index - 1], route[index]);
            }

            return length;
        }

        private void RemoveAt(int index)
        {
            _scene.Remove(_people[index].View);
            _people.RemoveAt(index);
        }

        private void RefreshHud()
        {
            WaitingCount = 0;

            foreach (var person in _people)
            {
                if (person.View.Model.State == SurvivorState.Waiting)
                {
                    WaitingCount++;
                }

            }

            _hud.ShowPeople(_settings.refugeePatience);
        }
    }
}
