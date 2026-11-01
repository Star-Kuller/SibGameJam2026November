using System;
using UnityEngine;

namespace _Game.Code.Features.Zombies.View
{
    public sealed class ZombieRoute : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints = Array.Empty<Transform>();

        public Vector3 SpawnPosition => waypoints[0].position;

        public Vector3[] ReadPoints()
        {
            if (waypoints.Length < 2)
            {
                throw new InvalidOperationException($"Route {name} needs at least two waypoints.");
            }

            var points = new Vector3[waypoints.Length];

            for (var index = 0; index < points.Length; index++)
            {
                var waypoint = waypoints[index];

                if (waypoint == null)
                {
                    throw new InvalidOperationException($"Missing waypoint {index} on {name}.");
                }

                points[index] = waypoint.position;
            }

            return points;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.65f, 0.2f);

            for (var index = 0; index < waypoints.Length; index++)
            {
                var waypoint = waypoints[index];

                if (waypoint == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(waypoint.position, 0.4f);

                if (index == 0 || waypoints[index - 1] == null)
                {
                    continue;
                }

                var previousWaypoint = waypoints[index - 1];
                Gizmos.DrawLine(previousWaypoint.position, waypoint.position);
            }
        }
    }
}
