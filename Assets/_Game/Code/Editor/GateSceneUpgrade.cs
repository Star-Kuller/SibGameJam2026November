using System;
using System.IO;
using System.Linq;
using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.GateEncounter.View;
using _Game.Code.Features.Gates.View;
using _Game.Code.Features.Input.View;
using _Game.Code.Features.Presentation.View;
using _Game.Code.Features.Survivors.View;
using _Game.Code.Features.Turret.View;
using _Game.Code.Features.Zombies.View;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Game.Code.Editor
{
    // Repeatable scene migration through supported Editor APIs. Original source FBX files stay intact.
    public static class GateSceneUpgrade
    {
        private const string Root = "Assets/_Game/GatePrototype";
        private const string ScenePath = "Assets/_Game/Scenes/GatePrototype.unity";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Last Shift/Upgrade Gate Prototype")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode before upgrading the scene.");
            }

            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Directory.CreateDirectory(Root + "/Rendering");
            AssetDatabase.Refresh();

            EnsureLayer("Survivor");
            ConfigureInputActions();
            var survivorPrefab = CreateSurvivorPrefab();
            ConfigureZombieMaterial();

            ReplaceTrees();
            ConfigureTurret();
            ConfigureGate();
            ConfigureCharacters(survivorPrefab);
            ConfigureUi();
            ConfigureSettings();
            ConfigureAudio();
            GateSliceRepair.Apply();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[Last Shift] Gate scene upgrade saved.");
        }

        private static void ConfigureInputActions()
        {
            var original = JObject.Parse(File.ReadAllText(InputPath));
            var source = InputActionAsset.FromJson(original.ToString());
            var player = source.FindActionMap("Player", true);

            AddButton(player, "Gate", "<Keyboard>/space", "<Gamepad>/buttonNorth");
            AddButton(player, "Recall", "<Keyboard>/r", "<Gamepad>/dpad/down");
            AddButton(player, "CommandCursor", "<Keyboard>/tab", "<Gamepad>/select");

            var generated = JObject.Parse(source.ToJson());
            var originalMap = original["maps"].First(map => (string)map["name"] == "Player");
            var generatedMap = generated["maps"].First(map => (string)map["name"] == "Player");

            foreach (var action in generatedMap["actions"])
            {
                if (!originalMap["actions"].Any(item => (string)item["name"] == (string)action["name"]))
                {
                    ((JArray)originalMap["actions"]).Add(action.DeepClone());

                    foreach (var binding in generatedMap["bindings"]
                        .Where(item => (string)item["action"] == (string)action["name"]))
                    {
                        ((JArray)originalMap["bindings"]).Add(binding.DeepClone());
                    }
                }
            }

            File.WriteAllText(InputPath, original.ToString(Formatting.Indented) + Environment.NewLine);
            UnityEngine.Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);

            var input = GetOrAdd<PlayerInputView>(GameObject.Find("TurretRig"));
            SetReference(input, "lookAction", ActionReference("Look"));
            SetReference(input, "fireAction", ActionReference("Attack"));
            SetReference(input, "pauseAction", ActionReference("Pause"));
            SetReference(input, "gateAction", ActionReference("Gate"));
            SetReference(input, "recallAction", ActionReference("Recall"));
            SetReference(input, "cursorAction", ActionReference("CommandCursor"));
        }

        private static void AddButton(InputActionMap map, string name, string keyboard, string gamepad)
        {
            if (map.FindAction(name) != null)
            {
                return;
            }

            var action = map.AddAction(name, InputActionType.Button);
            action.expectedControlType = "Button";
            action.AddBinding(keyboard, groups: "Keyboard&Mouse");
            action.AddBinding(gamepad, groups: "Gamepad");
        }

        private static InputActionReference ActionReference(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var action = asset.FindAction("Player/" + name, true);

            return AssetDatabase.LoadAllAssetsAtPath(InputPath)
                .OfType<InputActionReference>()
                .First(reference => reference.action != null && reference.action.id == action.id);
        }

        private static SurvivorView CreateSurvivorPrefab()
        {
            var path = Root + "/Art/Survivor.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importCameras = false;
            importer.importLights = false;

            var clips = importer.defaultClipAnimations;

            foreach (var clip in clips)
            {
                clip.loopTime = true;
                clip.loopPose = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.lockRootRotation = true;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            var root = new GameObject("Survivor");
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);

            var renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>();
            var scale = 1.8f / renderer.bounds.size.y;
            visual.transform.localScale *= scale;
            visual.transform.position -= Vector3.up * renderer.bounds.min.y;

            renderer.sharedMaterial = Material("SurvivorClothes", new Color(0.50f, 0.40f, 0.27f));

            var animator = visual.GetComponentInChildren<Animator>();

            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = Controller(
                Root + "/Art/SurvivorWalk.controller",
                AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .First(clip => !clip.name.StartsWith("__preview__")));

            var accent = Material("CommunityTurquoise", new Color(0.12f, 0.78f, 0.66f));
            accent.EnableKeyword("_EMISSION");
            accent.SetColor("_EmissionColor", new Color(0.03f, 0.22f, 0.18f));
            accent.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

            var arm = visual.GetComponentsInChildren<Transform>()
                .First(transform => transform.name == "mixamorig:LeftArm");
            var band = GameObject.CreatePrimitive(PrimitiveType.Cube);
            band.name = "CommunityArmband";
            band.transform.position = arm.position;
            band.transform.rotation = arm.rotation;
            band.transform.localScale = new Vector3(0.20f, 0.14f, 0.20f);
            band.transform.SetParent(arm, true);
            band.GetComponent<Renderer>().sharedMaterial = accent;
            UnityEngine.Object.DestroyImmediate(band.GetComponent<Collider>());

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "FriendlyMarker";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition = new Vector3(0, 2.02f, 0);
            marker.transform.localRotation = Quaternion.Euler(0, 0, 45);
            marker.transform.localScale = new Vector3(0.11f, 0.11f, 0.06f);
            marker.GetComponent<Renderer>().sharedMaterial = accent;
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());

            var collider = root.AddComponent<CapsuleCollider>();
            collider.height = 1.8f;
            collider.radius = 0.28f;
            collider.center = Vector3.up * 0.9f;

            var view = root.AddComponent<SurvivorView>();
            SetReference(view, "visual", visual.transform);
            SetReference(view, "hitbox", collider);
            SetReference(view, "animator", animator);

            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = LayerMask.NameToLayer("Survivor");
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Survivor.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<SurvivorView>();
        }

        private static RuntimeAnimatorController Controller(string path, AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
                controller.AddMotion(clip);
            }
            else
            {
                controller.layers[0].stateMachine.states[0].state.motion = clip;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureZombieMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/ZombieBaseColor.png");

            if (texture == null)
            {
                return;
            }

            var material = Material("ZombieTextured", Color.white);
            material.SetTexture("_BaseMap", texture);
            var path = Root + "/Prefabs/Zombie.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ReplaceTrees()
        {
            var location = GameObject.Find("Environment/LocationVisual");

            if (PrefabUtility.IsPartOfPrefabInstance(location))
            {
                PrefabUtility.UnpackPrefabInstance(
                    location, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            var originals = location.GetComponentsInChildren<MeshRenderer>()
                .Where(renderer => renderer.name.StartsWith("Gleditsia")).ToArray();

            if (originals.Length == 0)
            {
                return;
            }

            var treeRoot = Child(GameObject.Find("Environment").transform, "PlaceholderTrees");
            var crown = CrownMesh();
            var bark = Material("TreeBark", new Color(0.22f, 0.24f, 0.18f));
            var foliage = Material("TreeFoliage", new Color(0.25f, 0.31f, 0.22f));

            foreach (var original in originals)
            {
                var bounds = original.bounds;
                var tree = new GameObject("Tree_" + original.name);
                tree.transform.SetParent(treeRoot, false);
                tree.transform.position = new Vector3(bounds.center.x, 0, bounds.center.z);

                var height = bounds.size.y;
                var width = Mathf.Clamp(bounds.size.x * 0.13f, 0.12f, 0.38f);
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trunk.name = "Trunk";
                trunk.transform.SetParent(tree.transform, false);
                trunk.transform.localPosition = Vector3.up * height * 0.3f;
                trunk.transform.localScale = new Vector3(width, height * 0.6f, width);
                trunk.GetComponent<MeshRenderer>().sharedMaterial = bark;
                UnityEngine.Object.DestroyImmediate(trunk.GetComponent<Collider>());

                AddCrown(tree.transform, crown, foliage,
                    height * 0.24f, new Vector3(bounds.size.x, height * 0.6f, bounds.size.z));
                AddCrown(tree.transform, crown, foliage,
                    height * 0.5f, new Vector3(bounds.size.x * 0.72f, height * 0.5f, bounds.size.z * 0.72f));

                UnityEngine.Object.DestroyImmediate(original.gameObject);
            }
        }

        private static void AddCrown(Transform parent, Mesh mesh, UnityEngine.Material material, float y, Vector3 scale)
        {
            var crown = new GameObject("Crown", typeof(MeshFilter), typeof(MeshRenderer));
            crown.transform.SetParent(parent, false);
            crown.transform.localPosition = Vector3.up * y;
            crown.transform.localScale = scale;
            crown.GetComponent<MeshFilter>().sharedMesh = mesh;
            crown.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh CrownMesh()
        {
            var path = Root + "/Rendering/TreeCrown.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh != null)
            {
                return mesh;
            }

            var vertices = new Vector3[10];
            var triangles = new int[48];
            vertices[8] = Vector3.up;
            vertices[9] = Vector3.zero;

            for (var side = 0; side < 8; side++)
            {
                var angle = side * Mathf.PI / 4;
                vertices[side] = new Vector3(Mathf.Cos(angle) * 0.5f, 0, Mathf.Sin(angle) * 0.5f);
                var next = (side + 1) % 8;
                var index = side * 6;
                triangles[index] = side;
                triangles[index + 1] = 8;
                triangles[index + 2] = next;
                triangles[index + 3] = next;
                triangles[index + 4] = 9;
                triangles[index + 5] = side;
            }

            mesh = new Mesh { name = "Eight-sided tree crown", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void ConfigureTurret()
        {
            var rig = GameObject.Find("TurretRig").transform;
            rig.position = new Vector3(0, 6.8f, -23.5f);

            var view = rig.GetComponent<TurretView>();
            var barrel = rig.GetComponentsInChildren<Transform>().First(item => item.name == "Barrel");
            barrel.localPosition = new Vector3(0, -0.65f, 1.3f);
            barrel.localScale = new Vector3(0.16f, 0.16f, 1.8f);

            var muzzle = rig.GetComponentsInChildren<Transform>().First(item => item.name == "Muzzle");
            muzzle.localPosition = new Vector3(0, -0.65f, 2.2f);

            var mount = rig.GetComponentsInChildren<Transform>().First(item => item.name == "Mount");
            mount.localPosition = new Vector3(0, -1.12f, 1.0f);
            mount.localScale = new Vector3(0.55f, 0.75f, 0.6f);
            view.Camera.fieldOfView = 60;

            foreach (var route in UnityEngine.Object.FindObjectsByType<ZombieRoute>(FindObjectsSortMode.None))
            {
                var point = route.transform.GetChild(route.transform.childCount - 1);
                point.position = new Vector3(point.position.x, 0, -14.2f);
            }
        }

        private static void ConfigureGate()
        {
            var environment = GameObject.Find("Environment").transform;
            var gate = Child(environment, "GateMechanism");
            var left = Child(gate, "LeftHinge");
            var right = Child(gate, "RightHinge");

            left.position = new Vector3(-4.8f, 0, -15.4f);
            right.position = new Vector3(4.2f, 0, -15.4f);

            var location = GameObject.Find("Environment/LocationVisual").transform;

            foreach (var name in new[] { "part_0", "part_12" })
            {
                MoveToHinge(location, name, left);
            }

            foreach (var name in new[] { "part_9", "part_13" })
            {
                MoveToHinge(location, name, right);
            }

            var view = GetOrAdd<GateWorldView>(gate.gameObject);
            var structure = Child(environment, "GateStructure");

            for (var index = 0; index < 14; index++)
            {
                var name = "part_" + index;

                if (index != 0 && index != 9 && index != 12 && index != 13)
                {
                    MoveToHinge(location, name, structure);
                }
            }

            // Match the gate to human scale so heads remain visible at the contact line.
            gate.localScale = new Vector3(1, 0.55f, 1);
            structure.localScale = new Vector3(1, 0.55f, 1);
            SetReference(view, "leftHinge", left);
            SetReference(view, "rightHinge", right);
            SetArray(view, "doorColliders", gate.GetComponentsInChildren<Collider>());
            view.Render(0, false);
        }

        private static void MoveToHinge(Transform location, string name, Transform hinge)
        {
            var mesh = location.Find(name);

            if (mesh != null)
            {
                mesh.SetParent(hinge, true);
            }

            var collider = GameObject.Find("Obstacle_" + name);

            if (collider != null)
            {
                collider.transform.SetParent(hinge, true);
            }
        }

        private static void ConfigureCharacters(SurvivorView prefab)
        {
            var composition = GameObject.Find("Composition");
            var runtime = GameObject.Find("Runtime").transform;
            var zombieScene = GetOrAdd<ZombieSceneView>(composition);
            SetReference(zombieScene, "prefab", AssetDatabase.LoadAssetAtPath<GameObject>(
                Root + "/Prefabs/Zombie.prefab").GetComponent<ZombieView>());
            SetReference(zombieScene, "runtime", runtime);
            SetArray(zombieScene, "routes", UnityEngine.Object.FindObjectsByType<ZombieRoute>(
                FindObjectsSortMode.None).OrderBy(route => route.name).ToArray());

            var markers = Child(GameObject.Find("GameplayMarkers").transform, "People");
            var arrivalRoot = Child(markers, "Arrival");

            var arrivalPoints = new[]
            {
                new Vector3(4, 0, 23),
                new Vector3(4.5f, 0, 18.5f),
                new Vector3(4, 0, 17),
                new Vector3(4.5f, 0, 2.5f),
                new Vector3(5, 0, 1.5f),
                new Vector3(4.5f, 0, 1),
                new Vector3(4.5f, 0, -7)
            };

            var points = new Transform[arrivalPoints.Length];

            for (var index = 0; index < points.Length; index++)
            {
                points[index] = Child(arrivalRoot, "Point " + index);
                points[index].position = arrivalPoints[index];
            }

            var waiting = Child(markers, "Waiting");
            var inside = Child(markers, "Inside");
            var work = Child(markers, "ExternalPost");
            waiting.position = new Vector3(1.5f, 0, -13.7f);
            inside.position = new Vector3(1.5f, 0, -18.4f);
            work.position = new Vector3(4.5f, 0, 7);

            var survivors = GetOrAdd<SurvivorSceneView>(composition);
            SetReference(survivors, "prefab", prefab);
            SetReference(survivors, "runtime", runtime);
            SetReference(survivors, "waiting", waiting);
            SetReference(survivors, "inside", inside);
            SetReference(survivors, "workPoint", work);
            SetArray(survivors, "approach", points);

            var queries = composition.GetComponent<GateEncounterView>();
            var serialized = new SerializedObject(queries);
            serialized.FindProperty("shotMask").intValue |= 1 << LayerMask.NameToLayer("Survivor");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureUi()
        {
            var ui = GameObject.Find("UI");
            GetOrAdd<GateHudView>(ui);
            GetOrAdd<SessionMenuView>(ui);
            GetOrAdd<NotificationView>(ui);
            GetOrAdd<InputSettingsView>(ui);
            GetOrAdd<CctvOverlayView>(ui);
            GetOrAdd<PopulationHudView>(ui);
            GetOrAdd<GateControlsView>(ui);
        }

        private static void ConfigureSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GatePrototypeSettings>(Root + "/GatePrototypeSettings.asset");
            settings.positionHealth = 300;
            settings.shiftDuration = 180;
            settings.firstSpawnDelay = 4;
            settings.spawnInterval = 7;
            settings.maxLivingZombies = 16;
            settings.maxPitch = 45;
            settings.minPitch = -12;
            settings.initialPitch = 12;
            settings.firstRefugeeDelay = 20;
            settings.refugeeInterval = 55;
            settings.refugeeCount = 3;
            EditorUtility.SetDirty(settings);
        }

        private static void ConfigureAudio()
        {
            var events = AssetDatabase.LoadAssetAtPath<GateAudioEvents>(Root + "/GateAudioEvents.asset");
            events.shot = Sound("event:/Sounds/P0/SFX/Turret_Shots");
            events.zombieHit = Sound("event:/Sounds/P0/SFX/Zombie_Hit");
            events.zombieDeath = Sound("event:/Sounds/P0/SFX/Zombie_Death");
            events.gateHit = Sound("event:/Sounds/P0/SFX/Zombie_Gate_Hit");
            events.destruction = Sound("event:/Sounds/P0/SFX/Gate_Destruction");
            events.lose = Sound("event:/Sounds/P0/SFX/Lose");
            events.win = Sound("event:/Sounds/P0/SFX/Win");
            events.button = Sound("event:/Sounds/P0/UI/Button_Press");
            events.gateOpen = Sound("event:/Sounds/P0/SFX/Gate_Open");
            events.gateClose = Sound("event:/Sounds/P0/SFX/Gate_Close");
            events.personDeath = Sound("event:/Sounds/P0/SFX/Person_Death");
            events.friendlyFire = Sound("event:/Sounds/P0/SFX/Friendly_Fire");
            events.personDanger = Sound("event:/Sounds/P0/UI/Worker_In_Danger");
            events.order = Sound("event:/Sounds/P0/UI/Order_Receiving");
            events.waveWarning = Sound("event:/Sounds/P0/UI/Big_Wave_Oncoming");
            events.ambience = Sound("event:/Sounds/P1/AMB/AMB_LOOP/Cam_1");
            events.personReturned = Sound("event:/Sounds/P1/SFX/Sortie_Return");
            EditorUtility.SetDirty(events);

            SetReference(GameObject.Find("Composition").GetComponent<GateEncounterScope>(), "audioEvents", events);
        }

        private static FMODUnity.EventReference Sound(string path)
        {
            var sound = FMODUnity.EventManager.EventFromPath(path);

            if (sound == null)
            {
                throw new InvalidOperationException("FMOD event is missing: " + path);
            }

            return new FMODUnity.EventReference { Guid = sound.Guid, Path = path };
        }

        private static UnityEngine.Material Material(string name, Color color)
        {
            var path = Root + "/Rendering/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);

            if (material == null)
            {
                material = new UnityEngine.Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureLayer(string name)
        {
            if (LayerMask.NameToLayer(name) >= 0)
            {
                return;
            }

            var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = manager.FindProperty("layers");

            for (var index = 8; index < layers.arraySize; index++)
            {
                var layer = layers.GetArrayElementAtIndex(index);

                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = name;
                    manager.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            throw new InvalidOperationException("No free layer for survivors.");
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent<T>(out var component) ? component : target.AddComponent<T>();
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);

            if (child != null)
            {
                return child;
            }

            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray<T>(UnityEngine.Object target, string field, T[] values) where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            var array = serialized.FindProperty(field);
            array.arraySize = values.Length;

            for (var index = 0; index < values.Length; index++)
            {
                array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
