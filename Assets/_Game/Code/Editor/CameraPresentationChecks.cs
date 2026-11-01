using System;
using System.Linq;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.Turret.View;
using UnityEditor;
using UnityEngine;

namespace _Game.Code.Editor
{
    public static class CameraPresentationChecks
    {
        public static string Run()
        {
            var view = UnityEngine.Object.FindFirstObjectByType<TurretView>();
            var camera = view.Camera;
            var yaw = view.transform.Find("YawPivot");
            var pitch = yaw.Find("PitchPivot");
            var oldYaw = yaw.localRotation;
            var oldPitch = pitch.localRotation;
            var oldAspect = camera.aspect;
            var oldFieldOfView = camera.fieldOfView;
            var settings = AssetDatabase.LoadAssetAtPath<GatePrototypeSettings>(
                "Assets/_Game/GatePrototype/GatePrototypeSettings.asset");
            var ground = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .First(renderer => renderer.sharedMaterial != null && renderer.sharedMaterial.name == "Soil").bounds;
            var ends = GameObject.Find("Environment/GateStructure").GetComponentsInChildren<MeshRenderer>()
                .Where(renderer => renderer.name == "part_3" || renderer.name == "part_4")
                .Select(renderer => WallEnd(renderer)).ToArray();
            var poses = 0;

            try
            {
                foreach (var aspect in new[] { 4f / 3, 16f / 9, 21f / 9 })
                {
                    camera.aspect = aspect;
                    camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(65.81f, aspect);

                    for (var horizontal = -settings.yawLimit; horizontal <= settings.yawLimit; horizontal++)
                    {
                        yaw.localRotation = Quaternion.Euler(0, horizontal, 0);

                        for (var vertical = settings.minPitch; vertical <= settings.maxPitch; vertical++)
                        {
                            pitch.localRotation = Quaternion.Euler(vertical, 0, 0);
                            var planes = GeometryUtility.CalculateFrustumPlanes(camera);

                            if (ends.Any(end => GeometryUtility.TestPlanesAABB(planes, end)))
                            {
                                throw new InvalidOperationException(
                                    $"A wall end is visible: aspect={aspect}, yaw={horizontal}, pitch={vertical}.");
                            }

                            CheckGround(camera, ground);
                            poses++;
                        }
                    }
                }
            }
            finally
            {
                yaw.localRotation = oldYaw;
                pitch.localRotation = oldPitch;
                camera.aspect = oldAspect;
                camera.fieldOfView = oldFieldOfView;
            }

            return $"PASS: {poses} camera poses, 4:3 / 16:9 / 21:9; wall ends and ground edges stay outside view.";
        }

        private static Bounds WallEnd(Renderer renderer)
        {
            var bounds = renderer.bounds;
            var x = renderer.name == "part_3" ? bounds.min.x + 0.1f : bounds.max.x - 0.1f;
            bounds.center = new Vector3(x, bounds.center.y, bounds.center.z);
            bounds.size = new Vector3(0.2f, bounds.size.y, bounds.size.z);
            return bounds;
        }

        private static void CheckGround(Camera camera, Bounds ground)
        {
            foreach (var x in new[] { 0f, 0.5f, 1f })
            {
                foreach (var y in new[] { 0f, 0.5f, 1f })
                {
                    var ray = camera.ViewportPointToRay(new Vector3(x, y, 0));

                    if (ray.direction.y >= -0.0001f)
                    {
                        continue;
                    }

                    var distance = -ray.origin.y / ray.direction.y;
                    var far = (camera.farClipPlane - Vector3.Dot(ray.origin - camera.transform.position,
                        camera.transform.forward)) / Vector3.Dot(ray.direction, camera.transform.forward);

                    if (distance < 0 || distance > far)
                    {
                        continue;
                    }

                    var point = ray.GetPoint(distance);

                    if (point.x < ground.min.x || point.x > ground.max.x
                        || point.z < ground.min.z || point.z > ground.max.z)
                    {
                        throw new InvalidOperationException("A ground edge is visible before the far clipping plane.");
                    }
                }
            }
        }
    }
}
