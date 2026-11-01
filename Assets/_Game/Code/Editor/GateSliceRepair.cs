using System;
using System.Linq;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Turret.View;
using _Game.Code.Features.Presentation.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Game.Code.Editor
{
    public static class GateSliceRepair
    {
        private const string Root = "Assets/_Game/GatePrototype";

        [MenuItem("Last Shift/Repair Approved Gate Slice")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode before repairing the scene.");
            }

            ReplaceDesignerTrees();
            PlaceTurret();
            ConfigureLighting();
            RepairGateHierarchy();
            ConfigureBalance();

            var ui = GameObject.Find("UI");

            if (ui.GetComponent<StockpileHudView>() == null)
            {
                ui.AddComponent<StockpileHudView>();
            }

            if (ui.GetComponent<MetalPanelView>() == null)
            {
                ui.AddComponent<MetalPanelView>();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        private static void ReplaceDesignerTrees()
        {
            var environment = GameObject.Find("Environment").transform;
            var root = environment.Find("DesignerTrees");

            if (root == null)
            {
                root = new GameObject("DesignerTrees").transform;
                root.SetParent(environment, false);

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/CorrectedLocation.fbx");
                var foliage = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Rendering/TreeFoliage.mat");
                var bark = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Rendering/TreeBark.mat");
                var trees = source.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(renderer => renderer.name.StartsWith("Leaf Oak2.")).ToArray();

                foreach (var sourceRenderer in trees)
                {
                    var tree = new GameObject(sourceRenderer.name);
                    tree.transform.SetParent(root, false);
                    tree.transform.position = sourceRenderer.transform.position;
                    tree.transform.rotation = sourceRenderer.transform.rotation;
                    tree.transform.localScale = sourceRenderer.transform.lossyScale;
                    tree.AddComponent<MeshFilter>().sharedMesh = sourceRenderer.GetComponent<MeshFilter>().sharedMesh;

                    var renderer = tree.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = sourceRenderer.sharedMaterials
                        .Select(material => material.name.Contains("Bark") ? bark : foliage).ToArray();
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            var placeholders = environment.Find("PlaceholderTrees");

            if (placeholders != null)
            {
                Object.DestroyImmediate(placeholders.gameObject);
            }
        }

        private static void PlaceTurret()
        {
            var rig = GameObject.Find("TurretRig").transform;
            rig.position = new Vector3(0, 4.8f, -20.5f);
            var parts = rig.GetComponentsInChildren<Transform>();
            var pitch = parts.First(item => item.name == "PitchPivot");
            pitch.localPosition = new Vector3(0, -0.55f, 0.7f);
            rig.GetComponent<TurretView>().Camera.transform.localPosition = new Vector3(0, 0.55f, -0.7f);
            var barrel = parts.First(item => item.name == "Barrel");
            barrel.localPosition = new Vector3(0, 0, 0.9f);
            barrel.localScale = new Vector3(0.12f, 0.12f, 1.9f);
            barrel.GetComponent<MeshFilter>().sharedMesh = BarrelMesh();

            parts.First(item => item.name == "Muzzle").localPosition = new Vector3(0, 0, 1.85f);
            var mount = parts.First(item => item.name == "Mount");
            mount.SetParent(parts.First(item => item.name == "YawPivot"), false);
            mount.localPosition = new Vector3(0, -0.9f, 0.25f);
            mount.localScale = new Vector3(0.42f, 0.45f, 0.4f);
            rig.GetComponent<TurretView>().Camera.fieldOfView = 60;

            var support = GameObject.Find("TurretSupport");

            if (support == null)
            {
                support = new GameObject("TurretSupport");
            }

            var metal = barrel.GetComponent<Renderer>().sharedMaterial;
            var receiver = pitch.Find("Receiver");

            if (receiver == null)
            {
                receiver = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                receiver.name = "Receiver";
                receiver.SetParent(pitch, false);
                Object.DestroyImmediate(receiver.GetComponent<Collider>());
            }

            receiver.localPosition = new Vector3(0, -0.05f, -0.32f);
            receiver.localScale = new Vector3(0.34f, 0.22f, 0.65f);
            receiver.GetComponent<Renderer>().sharedMaterial = metal;
            SupportPart(support.transform, "Footing", new Vector3(0, 2.9f, -20.4f),
                new Vector3(3.6f, 0.2f, 3.3f), metal);
            SupportPart(support.transform, "Pedestal", new Vector3(0, 3.4f, -20.25f),
                new Vector3(0.35f, 0.8f, 0.35f), metal);

            for (var index = 0; index < 4; index++)
            {
                var x = index % 2 == 0 ? -1.4f : 1.4f;
                var z = index < 2 ? -21.6f : -19.2f;
                SupportPart(support.transform, "TowerLeg" + index, new Vector3(x, 1.4f, z),
                    new Vector3(0.22f, 2.8f, 0.22f), metal);
            }
        }

        private static void SupportPart(Transform root, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = root.Find(name);

            if (part == null)
            {
                part = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                part.name = name;
                part.SetParent(root, false);
            }

            part.position = position;
            part.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void RepairGateHierarchy()
        {
            var gate = GameObject.Find("Environment/GateMechanism").transform;
            var left = gate.Find("LeftHinge");
            var right = gate.Find("RightHinge");
            var parts = gate.GetComponentsInChildren<Transform>(true);

            foreach (var name in new[] { "part_0", "part_12" })
            {
                parts.First(part => part.name == name).SetParent(left, true);
            }

            foreach (var name in new[] { "part_9", "part_13" })
            {
                parts.First(part => part.name == name).SetParent(right, true);
            }
        }

        private static void ConfigureBalance()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GatePrototypeSettings>(Root + "/GatePrototypeSettings.asset");
            settings.shiftDuration = 720;
            settings.firstSpawnDelay = 25;
            settings.spawnInterval = 20;
            settings.maxLivingZombies = 24;
            settings.shotsPerSecond = 3;
            settings.attackDamage = 3;
            settings.initialPitch = 10;
            settings.minPitch = -7;
            settings.maxPitch = 40;
            settings.survivorWorkDuration = 50;
            settings.expeditionReturnDuration = 12;
            settings.firstRefugeeDelay = 80;
            settings.refugeeInterval = 180;
            settings.zombieSpeed = 1.5f;
            EditorUtility.SetDirty(settings);
        }

        private static Mesh BarrelMesh()
        {
            var path = Root + "/Rendering/GunBarrel.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh != null)
            {
                return mesh;
            }

            var temporary = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mesh = Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);
            Object.DestroyImmediate(temporary);
            var rotation = Quaternion.Euler(90, 0, 0);
            mesh.vertices = mesh.vertices.Select(vertex => Vector3.Scale(rotation * vertex, new Vector3(1, 1, 0.5f))).ToArray();
            mesh.normals = mesh.normals.Select(normal => rotation * normal).ToArray();
            mesh.RecalculateBounds();
            mesh.name = "Turret barrel";
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void ConfigureLighting()
        {
            var camera = Object.FindFirstObjectByType<TurretView>().Camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.20f, 0.23f, 0.24f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.36f, 0.37f);
            RenderSettings.ambientEquatorColor = new Color(0.20f, 0.22f, 0.21f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.13f, 0.12f);

            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .First(light => light.type == LightType.Directional);
            sun.color = new Color(0.94f, 0.85f, 0.70f);
            sun.intensity = 1.15f;
            sun.transform.rotation = Quaternion.Euler(32, -30, 0);
        }
    }
}
