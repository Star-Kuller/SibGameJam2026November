using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.Gates.Models;
using _Game.Code.Features.Survivors.Models;
using _Game.Code.Features.Zombies.Presenters;
using _Game.Code.Features.Zombies.Models;

namespace _Game.Code.Features.GateEncounter.Presenters
{
    public sealed class OutcomePresenter
    {
        private readonly GatePrototypeSettings _settings;
        private readonly GateEncounterModel _session;
        private readonly GateModel _gate;
        private readonly ColonyModel _colony;
        private readonly ZombiePresenter _zombies;
        private readonly SpawnScheduleModel _schedule;

        public OutcomePresenter(
            GatePrototypeSettings settings,
            GateEncounterModel session,
            GateModel gate,
            ColonyModel colony,
            ZombiePresenter zombies,
            SpawnScheduleModel schedule)
        {
            _settings = settings;
            _session = session;
            _gate = gate;
            _colony = colony;
            _zombies = zombies;
            _schedule = schedule;
        }

        public void Evaluate()
        {
            if (_gate.Health == 0)
            {
                _session.Lose("Ворота разрушены. Периметр больше не удержать.");
            }
            else if (_colony.Total == 0)
            {
                _session.Lose("В общине не осталось живых жителей.");
            }
            else if (_session.ElapsedTime >= _settings.shiftDuration
                && _schedule.AllWavesSpawned && _zombies.LivingCount == 0)
            {
                _session.Win();
            }
        }
    }
}
