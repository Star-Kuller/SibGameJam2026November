using System;
using R3;

namespace _Game.Code.Features.GateEncounter.Models
{
    public enum SessionState
    {
        MainMenu,
        Running,
        Paused,
        Won,
        Lost
    }

    // Session lifecycle and clock only. Gate health and population have their own models.
    public sealed class GateEncounterModel : IDisposable
    {
        private readonly ReactiveProperty<SessionState> _state = new(SessionState.MainMenu);

        public SessionState State => _state.Value;
        public Observable<SessionState> StateChanged => _state;
        public double ElapsedTime { get; private set; }
        public string ResultReason { get; private set; } = string.Empty;

        public void Start()
        {
            ElapsedTime = 0;
            ResultReason = string.Empty;
            _state.Value = SessionState.Running;
        }

        public void ReturnToMenu()
        {
            _state.Value = SessionState.MainMenu;
        }

        public void AdvanceTime(float seconds)
        {
            if (State == SessionState.Running)
            {
                ElapsedTime += Math.Max(0, seconds);
            }
        }

        public void Pause()
        {
            if (State == SessionState.Running)
            {
                _state.Value = SessionState.Paused;
            }
        }

        public void Resume()
        {
            if (State == SessionState.Paused)
            {
                _state.Value = SessionState.Running;
            }
        }

        public void Win()
        {
            Finish(SessionState.Won, "Смена окончена. Периметр удержан.");
        }

        public void Lose(string reason)
        {
            Finish(SessionState.Lost, reason);
        }

        public void Dispose()
        {
            _state.Dispose();
        }

        private void Finish(SessionState result, string reason)
        {
            if (State != SessionState.Running)
            {
                return;
            }

            ResultReason = reason;
            _state.Value = result;
        }
    }
}
