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
            Require(colony.Dispatch() && !colony.Dispatch(), "Only one resident can be dispatched.");
            Require(colony.Total == 8 && colony.Inside == 7 && colony.Outside == 1,
                "Dispatch must preserve the total population.");

            Require(colony.Admit(true), "Returning resident must rejoin the colony.");
            Require(!colony.Admit(true), "A resident cannot be admitted twice.");

            for (var index = 0; index < 4; index++)
            {
                Require(colony.Admit(false), "Refugee must join while capacity remains.");
            }

            Require(!colony.Admit(false) && colony.Total == 12, "Population cannot exceed capacity.");

            colony.Dispatch();
            colony.RecordDeath(true, true);
            colony.RecordDeath(true, true);
            Require(colony.Total == 11 && colony.Lost == 1 && colony.FriendlyFireDeaths == 1,
                "A resident death is counted once.");

            colony.Reset();
            Require(colony.Total == 8 && colony.Rescued == 0 && colony.Lost == 0,
                "Population resets without carrying results.");
        }

        private static void CheckSurvivor()
        {
            var route = new[] { Vector3.zero, Vector3.right, Vector3.right * 2 };
            var refugee = new SurvivorModel(1, "Анна", false, route);
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

            var impatient = new SurvivorModel(3, "Вера", false, new[] { Vector3.zero, Vector3.right });
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

            var person = new SurvivorModel(10, "Анна", true, new[] { Vector3.zero, Vector3.forward });
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

            var victim = new SurvivorModel(11, "Вера", true, new[] { Vector3.zero, Vector3.forward });
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
