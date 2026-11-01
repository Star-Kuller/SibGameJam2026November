using System;
using UnityEngine;

namespace _Game.Code.Features.Zombies.Models
{
    public enum ZombieState
    {
        Moving,
        Attacking,
        Dead
    }

    public sealed class ZombieModel
    {
        private readonly Vector3[] _route;

        private int _waypoint = 1;
        private double _nextAttackAt = double.PositiveInfinity;
        private int _humanTarget;
        private double _humanAttackAt = double.PositiveInfinity;

        public int EntityId { get; }
        public int Health { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; } = Vector3.back;
        public ZombieState State { get; private set; }
        public Vector3 GatePosition => _route[_route.Length - 1];

        public ZombieModel(int entityId, int health, Vector3[] route)
        {
            if (health <= 0 || route == null || route.Length < 2)
            {
                throw new ArgumentException("Zombie needs positive health and at least two route points.");
            }

            EntityId = entityId;
            Health = health;

            _route = (Vector3[])route.Clone();
            Position = _route[0];
        }

        // Movement is a calculation over model data. The presenter decides when to request it.
        public bool Move(float distance)
        {
            if (State != ZombieState.Moving)
            {
                return false;
            }

            distance = Mathf.Max(0, distance);

            while (_waypoint < _route.Length)
            {
                var offset = _route[_waypoint] - Position;
                var remaining = offset.magnitude;

                if (remaining > 0.0001f)
                {
                    Forward = offset / remaining;
                }

                if (remaining > distance)
                {
                    Position += Forward * distance;
                    return false;
                }

                Position = _route[_waypoint];
                _waypoint++;

                distance -= remaining;
            }

            State = ZombieState.Attacking;
            return true;
        }

        public void ScheduleFirstAttack(double sessionTime, float interval)
        {
            if (State == ZombieState.Attacking)
            {
                _nextAttackAt = sessionTime + Math.Max(0.01, interval);
            }
        }

        public bool TryAttack(double sessionTime, float interval)
        {
            if (State != ZombieState.Attacking || sessionTime < _nextAttackAt)
            {
                return false;
            }

            _nextAttackAt = sessionTime + Math.Max(0.01, interval);
            return true;
        }

        public void MoveToward(Vector3 target, float distance)
        {
            if (State == ZombieState.Dead)
            {
                return;
            }

            var offset = target - Position;

            if (offset.sqrMagnitude > 0.0001f)
            {
                Forward = offset.normalized;
            }

            Position = Vector3.MoveTowards(Position, target, Mathf.Max(0, distance));
        }

        public void StartHumanAttack(int targetId, double time, float windup)
        {
            if (State == ZombieState.Dead || _humanTarget == targetId)
            {
                return;
            }

            _humanTarget = targetId;
            _humanAttackAt = time + Math.Max(0.01, windup);
        }

        public bool TryHumanAttack(int targetId, double time)
        {
            if (State == ZombieState.Dead || _humanTarget != targetId || time < _humanAttackAt)
            {
                return false;
            }

            CancelHumanAttack();
            return true;
        }

        public void CancelHumanAttack()
        {
            _humanTarget = 0;
            _humanAttackAt = double.PositiveInfinity;
        }

        public bool ApplyDamage(int damage)
        {
            if (State == ZombieState.Dead || damage <= 0)
            {
                return false;
            }

            Health = Math.Max(0, Health - damage);

            if (Health > 0)
            {
                return false;
            }

            State = ZombieState.Dead;
            _nextAttackAt = double.PositiveInfinity;

            return true;
        }
    }
}
