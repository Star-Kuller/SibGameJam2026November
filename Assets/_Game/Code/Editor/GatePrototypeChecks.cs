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
            var schedule = new SpawnScheduleModel(4, 7, 180);
            schedule.Advance(4, true);
            Require(schedule.Pending == 1, "First regular zombie is scheduled.");

            schedule.ConsumeSpawn();
            Require(schedule.Advance(60, true) && schedule.Pending == 4 && schedule.WaveNumber == 1,
                "First wave replaces the regular spawn.");

            schedule.Advance(180, true);
            Require(schedule.Pending == 0, "No new enemies after the end of the shift.");
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
