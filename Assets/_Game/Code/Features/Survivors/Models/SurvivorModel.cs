using System;
using UnityEngine;

namespace _Game.Code.Features.Survivors.Models
{
    public enum SurvivorState
    {
        GoingOut,
        Working,
        Returning,
        Waiting,
        Entering,
        Safe,
        Dead,
        Left
    }

    public sealed class SurvivorModel
    {
        private Vector3[] _route;
        private int _waypoint;

        public int EntityId { get; }
        public string Name { get; }
        public bool IsResident { get; }
        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; } = Vector3.back;
        public SurvivorState State { get; private set; }
        public float WaitingTime { get; private set; }
        public float WorkingTime { get; private set; }
        public bool IsAlive => State != SurvivorState.Dead && State != SurvivorState.Left;
        public bool IsVisible => IsAlive && State != SurvivorState.Safe;
        public bool IsWalking => State == SurvivorState.GoingOut
            || State == SurvivorState.Returning
            || State == SurvivorState.Entering;

        public SurvivorModel(int entityId, string name, bool resident, Vector3[] route)
        {
            EntityId = entityId;
            Name = name;
            IsResident = resident;
            State = resident ? SurvivorState.GoingOut : SurvivorState.Returning;
            SetRoute(route);
            Position = _route[0];
        }

        public void Move(float distance)
        {
            if (!IsWalking)
            {
                return;
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
                    return;
                }

                Position = _route[_waypoint];
                distance -= remaining;
                _waypoint++;
            }

            switch (State)
            {
                case SurvivorState.GoingOut:
                    State = SurvivorState.Working;
                    break;

                case SurvivorState.Returning:
                    State = SurvivorState.Waiting;
                    WaitingTime = 0;
                    break;

                case SurvivorState.Entering:
                    State = SurvivorState.Safe;
                    break;
            }
        }

        public bool Recall(Vector3[] route)
        {
            if (State != SurvivorState.GoingOut && State != SurvivorState.Working)
            {
                return false;
            }

            SetRoute(route);
            State = SurvivorState.Returning;
            return true;
        }

        public bool Enter(Vector3[] route)
        {
            if (State != SurvivorState.Waiting)
            {
                return false;
            }

            SetRoute(route);
            State = SurvivorState.Entering;
            return true;
        }

        public void AdvanceWaiting(float seconds)
        {
            if (State == SurvivorState.Waiting)
            {
                WaitingTime += Mathf.Max(0, seconds);
            }
            else if (State == SurvivorState.Working)
            {
                WorkingTime += Mathf.Max(0, seconds);
            }
        }

        public void MoveOutsideGate(Vector3 waitingPosition)
        {
            if (State == SurvivorState.Entering)
            {
                Position = waitingPosition;
                State = SurvivorState.Waiting;
            }
        }

        public void RetreatInside(Vector3 inside)
        {
            if (State != SurvivorState.GoingOut)
            {
                return;
            }

            SetRoute(new[] { Position, inside });
            State = SurvivorState.Entering;
        }

        public bool Die()
        {
            if (!IsVisible)
            {
                return false;
            }

            State = SurvivorState.Dead;
            return true;
        }

        public bool Leave()
        {
            if (IsResident || State != SurvivorState.Waiting)
            {
                return false;
            }

            State = SurvivorState.Left;
            return true;
        }

        private void SetRoute(Vector3[] route)
        {
            if (route == null || route.Length < 2)
            {
                throw new ArgumentException("A survivor route needs at least two points.", nameof(route));
            }

            _route = (Vector3[])route.Clone();
            _waypoint = 1;
        }
    }
}
