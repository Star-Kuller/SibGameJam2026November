using System;
using System.Collections.Generic;
using _Game.Code.Features.GateEncounter.Models;
using _Game.Code.Features.Gates.Models;
using _Game.Code.Features.Survivors.Models;
using _Game.Code.Features.Turret.Models;
using _Game.Code.Features.Zombies.Models;
using UnityEditor;
using UnityEngine;
using SessionState = _Game.Code.Features.GateEncounter.Models.SessionState;

namespace _Game.Code.Editor
{
    // Small, repeatable checks without introducing a second test stack.
    public static class GatePrototypeChecks
    {
        [MenuItem("Last Shift/Run Model Checks")]
        public static void RunFromMenu()
        {
            var checks = Run();
            Debug.Log("[Last Shift] PASS: " + string.Join(", ", checks));
        }

        public static string[] Run()
        {
            var checks = new List<string>();
            CheckGate();
            checks.Add("gate transition, health and repeated commands");

            CheckPopulation();
            checks.Add("population accounting and capacity");

            CheckSurvivor();
            checks.Add("survivor arrival, waiting, recall and one-time death");

            CheckZombie();
            checks.Add("zombie route, attack windup and death");

            CheckTurret();
            checks.Add("shot cooldown and aim limits");

            CheckSession();
            checks.Add("menu, pause and terminal result");

            CheckSchedule();
            checks.Add("waves and spawn cutoff");

            CheckStockpile();
            checks.Add("finite ammo, cargo delivery and loss");
            return checks.ToArray();
        }

        private static void CheckGate()
        {
            using var gate = new GateModel(300);
            Require(gate.Toggle(), "Closed gate must start opening.");
            Require(!gate.Toggle(), "Repeated input must not reverse a moving gate.");

            gate.AdvanceTransition(0.5f);
            Require(!gate.CanPass && gate.Openness == 0.5f, "Half-open gate is not passable.");
            gate.AdvanceTransition(0.5f);
            Require(gate.CanPass, "Fully opened gate is passable.");

            gate.Toggle();
            Require(!gate.CanPass, "Closing blocks new entries immediately.");
            gate.AdvanceTransition(1);
            Require(gate.State == GateState.Closed && gate.Openness == 0, "Gate closes completely.");

            gate.ApplyDamage(400);
            Require(gate.Health == 0 && !gate.Toggle(), "Destroyed gate cannot be operated.");
            gate.Reset();
            Require(gate.Health == 300 && gate.State == GateState.Closed, "Gate resets cleanly.");
        }

        private static void CheckPopulation()
        {
            using var colony = new ColonyModel(8, 12);
            var resident = colony.People[0];
            var id = resident.EntityId;
            var name = resident.Name;
            var route = new[] { Vector3.zero, Vector3.right };

            Require(colony.People.Count == 8 && colony.Inside == 8, "Starting residents have persistent records.");
            Require(colony.Dispatch(id, route, true), "A selected resident can wait inside for the gate.");
            Require(!colony.Dispatch(id, route, true), "A second expedition cannot be assigned.");
            Require(colony.WaitingForExit == 1 && colony.Outside == 0 && colony.Total == 8,
                "Waiting for exit does not change the living population.");

            resident.BeginExit();
            resident.Move(1);
            resident.GatherCargo(1, 12, 30);
            resident.Recall(new[] { resident.Position, Vector3.zero });
            resident.Move(1);
            resident.Enter(new[] { resident.Position, Vector3.back });
            resident.Move(1);
            Require(colony.Admit(resident) && !colony.Admit(resident), "Arrival is registered exactly once.");
            Require(resident.UnloadCargo(out _, out _) && !resident.UnloadCargo(out _, out _),
                "Cargo is unloaded once.");
            Require(ReferenceEquals(colony.Find(id), resident) && resident.Name == name,
                "A returned resident keeps identity and name.");
            Require(colony.Dispatch(id, route, false), "The same resident can take another expedition.");
            Require(resident.Die(true) && !resident.Die(true), "A person cannot die twice.");
            Require(colony.Total == 7 && colony.Lost == 1 && colony.FriendlyFireDeaths == 1,
                "Death updates population and preserves its cause.");
            Require(!colony.Dispatch(id, route, false), "A dead resident cannot be dispatched again.");

            for (var index = 0; index < 5; index++)
            {
                var refugee = colony.CreateRefugee(route);
                refugee.Move(1);
                refugee.Enter(new[] { refugee.Position, Vector3.back });
                refugee.Move(10);
                Require(colony.Admit(refugee) && !colony.Admit(refugee), "A refugee joins exactly once.");
                Require(ReferenceEquals(colony.Find(refugee.EntityId), refugee), "Admission keeps the refugee identity.");
            }

            Require(colony.Total == 12 && !colony.HasCapacity, "Population respects the capacity.");
            colony.Reset();
            Require(colony.Total == 8 && colony.People.Count == 8 && colony.Lost == 0 && colony.Rescued == 0,
                "A new run clears old residents and loss history.");
        }
        private static void CheckSurvivor()
        {
            var route = new[] { Vector3.zero, Vector3.right, Vector3.right * 2 };
            var refugee = new SurvivorModel(1, "Иван", false, route);
            route[1] = Vector3.up * 100;
            refugee.Move(2);
            Require(refugee.State == SurvivorState.Waiting && refugee.Position == Vector3.right * 2,
                "Arrival follows a copied route and stops outside.");

            refugee.AdvanceWaiting(30);
            Require(refugee.WaitingTime == 30, "Waiting time advances by the requested amount.");
            Require(refugee.Enter(new[] { refugee.Position, Vector3.back }), "Waiting refugee can enter.");
            refugee.Move(100);
            Require(refugee.State == SurvivorState.Safe && !refugee.Die(), "Safe people are not targets.");

            var resident = new SurvivorModel(2, "Михаил", true, new[] { Vector3.zero, Vector3.forward });
            resident.Move(1);
            Require(resident.State == SurvivorState.Working, "Resident reaches the external post.");
            Require(resident.Recall(new[] { resident.Position, Vector3.zero }), "Worker can be recalled.");
            Require(!resident.Recall(new[] { resident.Position, Vector3.zero }), "Recall is idempotent.");
            Require(resident.Die() && !resident.Die(), "A person dies once.");
            resident.Move(100);
            Require(resident.Position == Vector3.forward, "Dead person does not move.");

            var impatient = new SurvivorModel(3, "Павел", false, new[] { Vector3.zero, Vector3.right });
            impatient.Move(1);
            Require(impatient.Leave() && !impatient.Leave(), "Refugee leaves only once.");
        }

        private static void CheckZombie()
        {
            var zombie = new ZombieModel(1, 1,
                new[] { Vector3.zero, Vector3.right, Vector3.right * 1.1f, Vector3.right * 2 });
            Require(zombie.Move(2), "A movement step may cross several waypoints.");

            zombie.ScheduleFirstAttack(2, 1);
            Require(!zombie.TryAttack(2.99, 1) && zombie.TryAttack(3, 1), "First gate hit waits a full interval.");

            zombie.StartHumanAttack(5, 10, 1.5f);
            zombie.StartHumanAttack(5, 11, 1.5f);
            Require(!zombie.TryHumanAttack(5, 11.49) && zombie.TryHumanAttack(5, 11.5),
                "Human attack gets a stable rescue window.");

            zombie.StartHumanAttack(5, 12, 1.5f);
            zombie.CancelHumanAttack();
            Require(!zombie.TryHumanAttack(5, 20), "Lost contact cancels the pending human attack.");

            Require(zombie.ApplyDamage(1) && !zombie.ApplyDamage(1), "Zombie death is counted once.");
            Require(!zombie.Move(100) && !zombie.TryAttack(100, 1), "Dead zombie cannot move or attack.");
        }

        private static void CheckTurret()
        {
            var turret = new TurretModel(60, -12, 45, 5);
            Require(turret.TryFire(0) && !turret.TryFire(0.19) && turret.TryFire(0.2), "Fire rate is limited.");
            Require(turret.TryFire(100) && !turret.TryFire(100), "Idle time cannot accumulate a burst.");

            turret.Aim(new Vector2(1000, 1000));
            Require(turret.Yaw == 60 && turret.Pitch == -12, "Aim is clamped.");
        }

        private static void CheckSession()
        {
            using var session = new GateEncounterModel();
            Require(session.State == SessionState.MainMenu, "Game starts in the main menu.");

            session.Start();
            session.AdvanceTime(2);
            session.Pause();
            session.AdvanceTime(100);
            Require(session.ElapsedTime == 2, "Pause freezes the session clock.");

            session.Resume();
            session.Lose("Test reason");
            session.Win();
            session.Resume();
            Require(session.State == SessionState.Lost && session.ResultReason == "Test reason",
                "A terminal result cannot be overwritten.");

            session.Start();
            Require(session.ElapsedTime == 0 && session.State == SessionState.Running,
                "A new shift resets the clock.");
        }

        private static void CheckSchedule()
        {
            var schedule = new SpawnScheduleModel(25, 20, 720);
            schedule.Advance(24.9, true);
            Require(schedule.Pending == 0, "There are no enemies before the tutorial group.");
            schedule.Advance(25, true);
            Require(schedule.Pending == 2, "The first group has two walkers at 0:25.");
            schedule.ConsumeSpawn();
            schedule.ConsumeSpawn();
            schedule.Advance(59.9, true);
            Require(schedule.Pending == 0, "The tutorial has a pause after the first group.");
            schedule.Advance(60, true);
            Require(schedule.Pending == 3, "Regular groups start at 1:00.");
            Require(schedule.Advance(180, true) && schedule.Pending == 12 && schedule.WaveNumber == 1,
                "The first large wave replaces pending regular spawns at 3:00.");

            for (var index = 0; index < 12; index++)
            {
                schedule.ConsumeSpawn();
            }

            schedule.Advance(250, true, 1);
            Require(schedule.WaveActive && schedule.Pending == 0,
                "Regular spawns stay disabled while a wave enemy is alive.");
            schedule.Advance(251, true, 0);
            schedule.Advance(270.9, true);
            Require(schedule.Pending == 0, "There is a full pause after the wave is cleared.");
            schedule.Advance(271, true);
            Require(schedule.Pending == 4, "Normal groups resume after the pause.");

            Require(schedule.Advance(390, true) && schedule.Pending == 16, "Second wave: 6:30, 16 walkers.");
            for (var index = 0; index < 16; index++)
            {
                schedule.ConsumeSpawn();
            }

            Require(schedule.Advance(630, true) && schedule.Pending == 20, "Final wave: 10:30, 20 walkers.");
            schedule.Advance(720, true);
            Require(schedule.Pending == 0, "No new enemies after the end of the shift.");
        }

        private static void CheckStockpile()
        {
            using var stockpile = new StockpileModel(24, 1);
            Require(stockpile.TryUseRound() && !stockpile.TryUseRound(), "Ammo cannot become negative.");

            var person = new SurvivorModel(10, "Иван", true, new[] { Vector3.zero, Vector3.forward });
            person.Move(1);
            person.GatherCargo(0.5f, 12, 30);
            Require(person.CargoMetal == 6 && person.CargoRounds == 15, "Partial cargo is carried by the worker.");
            Require(!person.UnloadCargo(out _, out _) && stockpile.Metal == 24,
                "Cargo cannot reach the stockpile outside the base.");
            person.GatherCargo(1, 12, 30);
            person.Recall(new[] { person.Position, Vector3.zero });
            person.Move(1);
            person.Enter(new[] { person.Position, Vector3.back });
            person.Move(1);
            Require(person.UnloadCargo(out var metal, out var rounds), "Cargo unloads only after entry.");
            stockpile.Deliver(metal, rounds);
            Require(stockpile.Metal == 36 && stockpile.Rounds == 30 && stockpile.Deliveries == 1,
                "A full expedition delivers exactly 12 metal and 30 rounds.");
            Require(!person.UnloadCargo(out _, out _), "The same cargo cannot be unloaded twice.");

            var victim = new SurvivorModel(11, "Павел", true, new[] { Vector3.zero, Vector3.forward });
            victim.Move(1);
            victim.GatherCargo(1, 12, 30);
            victim.Die();
            Require(victim.CargoMetal == 0 && victim.CargoRounds == 0, "Death destroys the carried cargo.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

