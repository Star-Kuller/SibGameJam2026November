using System;
using R3;

namespace _Game.Code.Features.Gates.Models
{
    public enum GateState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    public sealed class GateModel : IDisposable
    {
        private readonly ReactiveProperty<int> _health;
        private readonly ReactiveProperty<GateState> _state = new(GateState.Closed);

        public int MaximumHealth { get; }
        public int Health => _health.Value;
        public bool IsCritical => Health > 0 && Health < MaximumHealth * 0.3f;
        public GateState State => _state.Value;
        public float Openness { get; private set; }
        public bool CanPass => State == GateState.Open;
        public bool IsMoving => State == GateState.Opening || State == GateState.Closing;
        public Observable<int> HealthChanged => _health;
        public Observable<GateState> StateChanged => _state;

        public GateModel(int maximumHealth)
        {
            if (maximumHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            }

            MaximumHealth = maximumHealth;
            _health = new ReactiveProperty<int>(maximumHealth);
        }

        public void Reset()
        {
            _health.Value = MaximumHealth;
            Openness = 0;
            _state.Value = GateState.Closed;
        }

        public bool Toggle()
        {
            if (IsMoving || Health == 0)
            {
                return false;
            }

            _state.Value = CanPass ? GateState.Closing : GateState.Opening;
            return true;
        }

        public void AdvanceTransition(float fraction)
        {
            fraction = Math.Max(0, fraction);

            if (State == GateState.Opening)
            {
                Openness = Math.Min(1, Openness + fraction);

                if (Openness >= 1)
                {
                    _state.Value = GateState.Open;
                }
            }
            else if (State == GateState.Closing)
            {
                Openness = Math.Max(0, Openness - fraction);

                if (Openness <= 0)
                {
                    _state.Value = GateState.Closed;
                }
            }
        }

        public void ApplyDamage(int damage)
        {
            if (damage > 0)
            {
                _health.Value = Math.Max(0, Health - damage);
            }
        }

        public void Dispose()
        {
            _health.Dispose();
            _state.Dispose();
        }
    }
}
