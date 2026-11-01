using System;
using System.Collections.Generic;
using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Gates.Models;
using _Game.Code.Features.Survivors.Presenters;
using _Game.Code.Features.Zombies.Models;
using _Game.Code.Features.Zombies.View;
using UnityEngine;

namespace _Game.Code.Features.Zombies.Presenters
{
    public sealed class ZombiePresenter
    {
        private sealed class Zombie
        {
            public ZombieModel Model;
            public ZombieView View;
            public float DeathAge;
            public bool FromWave;
        }

        private readonly GatePrototypeSettings _settings;
        private readonly GateEncounterModel _session;
        private readonly GateModel _gate;
        private readonly CombatStatisticsModel _statistics;
        private readonly SpawnScheduleModel _schedule;
        private readonly ZombieSceneView _scene;
        private readonly GateEncounterView _queries;
        private readonly SurvivorPresenter _survivors;
        private readonly NotificationView _notifications;
        private readonly GateAudio _audio;
        private readonly GateAudioEvents _sounds;
        private readonly List<Zombie> _zombies = new();

        private int _nextId;
        private int _nextRoute;
        private int _warnedWave;

        public int LivingCount
        {
            get
            {
                var count = 0;

                foreach (var zombie in _zombies)
                {
                    if (zombie.Model.State != ZombieState.Dead)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public ZombiePresenter(
            GatePrototypeSettings settings,
            GateEncounterModel session,
            GateModel gate,
            CombatStatisticsModel statistics,
            SpawnScheduleModel schedule,
            ZombieSceneView scene,
            GateEncounterView queries,
            SurvivorPresenter survivors,
            NotificationView notifications,
            GateAudio audio,
            GateAudioEvents sounds)
        {
            _settings = settings;
            _session = session;
            _gate = gate;
            _statistics = statistics;
            _schedule = schedule;
            _scene = scene;
            _queries = queries;
            _survivors = survivors;
            _notifications = notifications;
            _audio = audio;
            _sounds = sounds;
        }

        public void Reset()
        {
            Clear();
            _nextId = 0;
            _nextRoute = 0;
            _warnedWave = 0;
            _schedule.Reset();

            if (_scene.Routes.Length == 0)
            {
                throw new InvalidOperationException("Assign zombie routes.");
            }

            foreach (var route in _scene.Routes)
            {
                route.ReadPoints();
            }
        }

        public void Clear()
        {
            foreach (var zombie in _zombies)
            {
                if (zombie.View != null)
                {
                    _scene.Remove(zombie.View);
                }
            }

            _zombies.Clear();
        }

        public void Move(float seconds)
        {
            UpdateSpawning();
            var distance = _settings.zombieSpeed * seconds;

            foreach (var zombie in _zombies)
            {
                if (zombie.Model.State == ZombieState.Dead)
                {
                    continue;
                }

                MoveZombie(zombie.Model, distance);
                zombie.View.RenderPose(_settings.zombieSpeed / 1.5f);

                if (_session.State != SessionState.Running)
                {
                    SetPaused(true);
                    return;
                }
            }
        }

        public void ResolveAttacks()
        {
            foreach (var zombie in _zombies)
            {
                var model = zombie.Model;

                if (model.State == ZombieState.Dead)
                {
                    continue;
                }

                var person = _survivors.FindNearest(model.Position, _settings.personAttackRange);

                if (person != null)
                {
                    model.StartHumanAttack(
                        person.Model.EntityId,
                        _session.ElapsedTime,
                        _settings.personAttackWindup);

                    if (model.TryHumanAttack(person.Model.EntityId, _session.ElapsedTime))
                    {
                        _survivors.Kill(person, false);
                    }

                    continue;
                }

                model.CancelHumanAttack();

                var atGate = (model.Position - model.GatePosition).sqrMagnitude < 0.5f;

                if (!atGate || _gate.CanPass
                    || !model.TryAttack(_session.ElapsedTime, _settings.attackInterval))
                {
                    continue;
                }

                _gate.ApplyDamage(_settings.attackDamage);
                _audio.Play(_sounds.gateHit, model.Position);
            }
        }

        public bool ApplyShot(ZombieView view, Vector3 hitPoint)
        {
            if (view.Model.State == ZombieState.Dead)
            {
                return false;
            }

            var killed = view.Model.ApplyDamage(_settings.shotDamage);
            view.RenderPose(_settings.zombieSpeed / 1.5f);

            if (killed)
            {
                _statistics.RecordKill();
                _audio.Play(_sounds.zombieDeath, hitPoint);
            }
            else
            {
                _audio.Play(_sounds.zombieHit, hitPoint);
            }

            return true;
        }

        public void Cleanup(float seconds)
        {
            for (var index = _zombies.Count - 1; index >= 0; index--)
            {
                var zombie = _zombies[index];

                if (zombie.Model.State != ZombieState.Dead)
                {
                    continue;
                }

                zombie.DeathAge += seconds;
                zombie.View.ShowDeath(Mathf.Clamp01(zombie.DeathAge / _settings.deathDuration));

                if (zombie.DeathAge >= _settings.deathDuration)
                {
                    _scene.Remove(zombie.View);
                    _zombies.RemoveAt(index);
                }
            }
        }

        public void SetPaused(bool paused)
        {
            foreach (var zombie in _zombies)
            {
                zombie.View.SetAnimationPaused(paused, _settings.zombieSpeed / 1.5f);
            }
        }

        private void MoveZombie(ZombieModel model, float distance)
        {
            var person = _survivors.FindNearest(model.Position, _settings.personDangerRange);

            if (person != null)
            {
                _survivors.Flee(person);

                if ((person.Model.Position - model.Position).sqrMagnitude
                    > _settings.personAttackRange * _settings.personAttackRange)
                {
                    model.MoveToward(person.Model.Position, distance);
                }

                return;
            }

            if (model.Move(distance))
            {
                model.ScheduleFirstAttack(_session.ElapsedTime, _settings.attackInterval);
            }

            if (model.State == ZombieState.Attacking && _gate.CanPass)
            {
                var inside = new Vector3(model.Position.x, model.Position.y, _settings.breachPlane);
                model.MoveToward(inside, distance);

                if (model.Position.z <= _settings.breachPlane + 0.01f)
                {
                    _session.Lose("Зомби прорвался через открытые ворота.");
                }
            }
            else if (model.State == ZombieState.Attacking)
            {
                model.MoveToward(model.GatePosition, distance);
            }
        }

        private void UpdateSpawning()
        {
            var time = _session.ElapsedTime;

            if (_schedule.NextWaveTime - time <= 20 && _warnedWave == _schedule.WaveNumber)
            {
                _warnedWave++;
                _notifications.Show("Волна приближается. Отзовите людей и закройте ворота.", true);
                _audio.Play(_sounds.waveWarning, Vector3.zero, true);
            }

            var livingWaveZombies = 0;

            foreach (var zombie in _zombies)
            {
                if (zombie.FromWave && zombie.Model.State != ZombieState.Dead)
                {
                    livingWaveZombies++;
                }
            }

            if (_schedule.Advance(time, LivingCount < _settings.maxLivingZombies, livingWaveZombies))
            {
                var label = _schedule.WaveNumber == 3 ? "Финальная волна" : "Волна " + _schedule.WaveNumber;
                _notifications.ShowWave(label.ToUpperInvariant() + " НАЧАЛАСЬ");
                _notifications.Show(label + ". Удерживайте периметр!", true);

                if (_schedule.WaveNumber == 3)
                {
                    _survivors.RecallAll();
                }
            }

            if (_schedule.Pending == 0 || LivingCount >= _settings.maxLivingZombies)
            {
                return;
            }

            for (var attempt = 0; attempt < _scene.Routes.Length; attempt++)
            {
                var route = _scene.Routes[_nextRoute % _scene.Routes.Length];
                _nextRoute++;

                if (!_queries.IsSpawnClear(route.SpawnPosition, _settings.spawnClearance))
                {
                    continue;
                }

                _nextId++;
                var model = new ZombieModel(_nextId, _settings.zombieHealth, route.ReadPoints());
                var view = _scene.Create();
                view.name = "Zombie " + model.EntityId;
                view.Bind(model, _settings.zombieSpeed / 1.5f);

                _zombies.Add(new Zombie
                {
                    Model = model,
                    View = view,
                    FromWave = _schedule.PendingIsWave
                });
                _schedule.ConsumeSpawn();
                break;
            }
        }
    }
}
