using UnityEngine;

namespace _Game.Code.Features.GateEncounter
{
    // Configuration only; mutable encounter state belongs to the session models.
    [CreateAssetMenu(menuName = "Last Shift/Gate Prototype Settings")]
    public sealed class GatePrototypeSettings : ScriptableObject
    {
        [Header("Position")]
        [Min(1)]
        public int positionHealth = 100;

        [Header("Aiming")]
        [Range(0.01f, 1)]
        public float mouseSensitivity = 0.12f;

        [Tooltip("Degrees per second for sticks and directional key bindings.")]
        [Min(1)]
        public float lookSpeed = 90;

        [Tooltip("Starting downward angle of the turret camera.")]
        public float initialPitch = 12;

        [Range(1, 85)]
        public float yawLimit = 60;

        public float minPitch = -20;
        public float maxPitch = 40;

        [Header("Weapon")]
        [Min(0.01f)]
        public float shotsPerSecond = 5;

        [Min(1)]
        public int shotDamage = 1;

        [Min(1)]
        public float range = 150;

        [Header("Zombies")]
        [Min(1)]
        public int zombieHealth = 1;

        [Min(0.1f)]
        public float zombieSpeed = 1.5f;

        [Min(1)]
        public int attackDamage = 5;

        [Min(0.1f)]
        public float attackInterval = 1;

        [Min(0.1f)]
        public float deathDuration = 0.5f;

        [Header("Spawning")]
        [Min(0)]
        public float firstSpawnDelay = 2;

        [Min(0.1f)]
        public float spawnInterval = 2.5f;

        [Min(1)]
        public int maxLivingZombies = 20;

        [Min(0.2f)]
        public float spawnClearance = 0.8f;

        [Header("Shift")]
        [Min(30)]
        public float shiftDuration = 180;

        [Header("Gate movement")]
        [Min(0.1f)]
        public float gateTransitionDuration = 1;

        public float gatePlane = -15.4f;
        public float breachPlane = -16.8f;

        [Header("People")]
        [Min(1)]
        public int startingResidents = 8;

        [Min(1)]
        public int populationCapacity = 12;

        [Min(0.1f)]
        public float survivorSpeed = 2.2f;

        [Min(1)]
        public float survivorWorkDuration = 20;

        [Min(0)]
        public float firstRefugeeDelay = 20;

        [Min(1)]
        public float refugeeInterval = 55;

        [Min(0)]
        public int refugeeCount = 3;

        [Min(1)]
        public float refugeePatience = 30;

        [Min(0.1f)]
        public float personAttackWindup = 1.5f;

        [Min(0.1f)]
        public float personAttackRange = 1.3f;

        [Min(0.1f)]
        public float personDangerRange = 5;
    }
}
