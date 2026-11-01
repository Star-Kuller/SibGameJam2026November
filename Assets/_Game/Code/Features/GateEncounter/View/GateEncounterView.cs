using _Game.Code.Features.Turret.View;
using _Game.Code.Features.Zombies.View;
using _Game.Code.Features.Survivors.View;
using UnityEngine;

namespace _Game.Code.Features.GateEncounter.View
{
    public readonly struct ShotTarget
    {
        public readonly Vector3 Point;
        public readonly ZombieView Zombie;
        public readonly SurvivorView Survivor;

        public ShotTarget(Vector3 point, ZombieView zombie, SurvivorView survivor)
        {
            Point = point;
            Zombie = zombie;
            Survivor = survivor;
        }
    }

    // Scene physics queries only. Entity creation belongs to the corresponding scene views.
    public sealed class GateEncounterView : MonoBehaviour
    {
        [SerializeField] private TurretView turret;
        [SerializeField] private LayerMask shotMask;

        public bool IsSpawnClear(Vector3 position, float clearance)
        {
            var lowerHeight = clearance + 0.05f;
            var upperHeight = Mathf.Max(lowerHeight, 1.8f - clearance);

            return !Physics.CheckCapsule(
                position + Vector3.up * lowerHeight,
                position + Vector3.up * upperHeight,
                clearance,
                shotMask,
                QueryTriggerInteraction.Ignore);
        }

        public ShotTarget QueryShot(float range)
        {
            var ray = turret.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(ray, out var hit, range, shotMask, QueryTriggerInteraction.Ignore))
            {
                return new ShotTarget(
                    hit.point,
                    hit.collider.GetComponentInParent<ZombieView>(),
                    hit.collider.GetComponentInParent<SurvivorView>());
            }

            return new ShotTarget(ray.GetPoint(range), null, null);
        }
    }
}
