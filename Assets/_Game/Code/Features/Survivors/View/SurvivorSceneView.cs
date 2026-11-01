using System.Collections.Generic;
using UnityEngine;

namespace _Game.Code.Features.Survivors.View
{
    public sealed class SurvivorSceneView : MonoBehaviour
    {
        [SerializeField] private SurvivorView prefab;
        [SerializeField] private Transform runtime;
        [SerializeField] private Transform[] approach;
        [SerializeField] private Transform waiting;
        [SerializeField] private Transform inside;
        [SerializeField] private Transform workPoint;

        public Vector3 WaitingPosition => waiting.position;
        public Vector3 InsidePosition => inside.position;

        public Vector3[] ArrivalRoute()
        {
            var points = new Vector3[approach.Length + 1];

            for (var index = 0; index < approach.Length; index++)
            {
                points[index] = approach[index].position;
            }

            points[points.Length - 1] = WaitingPosition;
            return points;
        }

        public Vector3[] OutboundRoute()
        {
            var points = new List<Vector3>
            {
                InsidePosition,
                WaitingPosition
            };

            for (var index = approach.Length - 1; index >= 0; index--)
            {
                if (approach[index].position.z < workPoint.position.z)
                {
                    points.Add(approach[index].position);
                }
            }

            points.Add(workPoint.position);
            return points.ToArray();
        }

        public Vector3[] ReturnRoute(Vector3 from)
        {
            var points = new List<Vector3> { from };

            foreach (var point in approach)
            {
                if (point.position.z < from.z - 0.01f)
                {
                    points.Add(point.position);
                }
            }

            points.Add(WaitingPosition);
            return points.ToArray();
        }

        public Vector3[] EntranceRoute(Vector3 from)
        {
            return new[] { from, InsidePosition };
        }

        public SurvivorView Create()
        {
            return Instantiate(prefab, runtime);
        }

        public void Remove(SurvivorView view)
        {
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
    }
}
