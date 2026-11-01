using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using _Game.Code.Features.Survivors.View;
using _Game.Code.Features.Zombies.View;
using Object = UnityEngine.Object;

namespace _Game.Code.Editor
{
    public static class DesignerAssetSetup
    {
        private const string Art = "Assets/_Game/Art/";
        private const string Prefabs = "Assets/_Game/Design/Prefabs/";

        [MenuItem("Last Shift/Apply Designer Assets")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode before updating art.");
            }

            EditorSceneManager.OpenScene("Assets/_Game/Scenes/GatePrototype.unity");
            var wood = Material("PropWood", new Color(0.29f, 0.23f, 0.15f));
            var pumpkin = Material("Pumpkin", new Color(0.50f, 0.24f, 0.07f));
            var metal = AssetDatabase.LoadAssetAtPath<Material>(Art + "Materials/GateMetal.mat");

            var barrel = Prop("Barrel", "BarrelAndPumpkin", name => name != "Сфера", 1.1f, wood);
            var fruit = Prop("Pumpkin", "BarrelAndPumpkin", name => name == "Сфера", 0.4f, pumpkin);
            var crates = Prop("Crates", "Crates", _ => true, 1.35f, wood);
            var pallet = Prop("WoodPallet", "WoodPallet", name => name == "pallet_1", 0.18f, wood);
            Place(barrel, new Vector3(-8, 0, -9), 20);
            Place(fruit, new Vector3(-7.1f, 0, -8.5f), 0);
            Place(crates, new Vector3(9, 0, -7), -20);
            Place(pallet, new Vector3(9.2f, 0, -3.8f), 25);

            ConfigureTurret(metal);
            ConfigureCharacter(true);
            ConfigureCharacter(false);
            CreateArmedResidentPrefab();
            ConfigureGateMotion();

            var settings = AssetDatabase.LoadAssetAtPath<_Game.Code.Features.GateEncounter.GatePrototypeSettings>(
                "Assets/_Game/Design/Configs/GatePrototypeSettings.asset");
            settings.deathDuration = 2.8f;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static GameObject Prop(string name, string sourceName, Func<string, bool> keep, float height, Material material)
        {
            var root = new GameObject(name);
            root.layer = LayerMask.NameToLayer("World");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Models/Environment/" + sourceName + ".fbx");
            var visual = Object.Instantiate(source, root.transform);
            visual.name = "Visual";

            foreach (var camera in visual.GetComponentsInChildren<Camera>())
            {
                Object.DestroyImmediate(camera.gameObject);
            }

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                if (renderer != null && !keep(renderer.name))
                {
                    Object.DestroyImmediate(renderer.gameObject);
                }
            }

            var renderers = visual.GetComponentsInChildren<Renderer>();
            var bounds = BoundsOf(renderers);
            var scale = height / bounds.size.y;
            visual.transform.localScale *= scale;
            visual.transform.localPosition = -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;

            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
            }

            bounds = BoundsOf(renderers);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "Environment/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;

            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static void Place(GameObject prefab, Vector3 position, float yaw)
        {
            var parent = GameObject.Find("Environment").transform;
            var old = parent.Find(prefab.name);

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        }

        private static void ConfigureTurret(Material metal)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Models/Weapons/The_Turret_1.obj");
            var visual = Object.Instantiate(source);
            visual.GetComponentInChildren<Renderer>().sharedMaterial = metal;
            PrefabUtility.SaveAsPrefabAsset(visual, Prefabs + "Weapons/Turret.prefab");
            Object.DestroyImmediate(visual);

            var rig = GameObject.Find("TurretRig");
            var barrel = rig.GetComponentsInChildren<Transform>().First(part => part.name == "Barrel");
            barrel.GetComponent<MeshFilter>().sharedMesh = source.GetComponentInChildren<MeshFilter>().sharedMesh;
            barrel.localScale = Vector3.one * 0.9f;
            barrel.localPosition = new Vector3(0, -0.48f, 0.45f);
            var receiver = rig.GetComponentsInChildren<Transform>().First(part => part.name == "Receiver");
            receiver.GetComponent<Renderer>().enabled = false;
        }

        private static void ConfigureCharacter(bool resident)
        {
            var name = resident ? "Survivor" : "Zombie";
            var path = Prefabs + "Characters/" + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var animator = root.GetComponentInChildren<Animator>();
                var modelPath = Art + "Models/Characters/" + name + ".fbx";
                var walk = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                    .First(clip => clip.name == "Armature|mixamo.com");
                var idle = SaveClip(Freeze(walk, name + "Idle"));
                var work = Freeze(walk, name + "Work");
                var carry = Object.Instantiate(walk);
                carry.name = name + "Carry";
                var visual = animator.gameObject;
                AddArmMotion(work, visual.transform, true);
                AddArmMotion(carry, visual.transform, false);
                work = SaveClip(work);
                carry = SaveClip(carry);

                var source = resident ? "Armature.003|mixamo.com.003" : "Armature|mixamo.com";
                var death = RetargetDeath(source, visual, name + "Death");
                var controllerPath = Art + "Animations/" + name + "States.controller";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

                if (controller == null)
                {
                    controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                }

                var stateMachine = controller.layers[0].stateMachine;

                foreach (var state in stateMachine.states)
                {
                    stateMachine.RemoveState(state.state);
                }

                AddState(stateMachine, "Walk", walk);
                AddState(stateMachine, "Idle", idle);
                AddState(stateMachine, "Work", work);
                AddState(stateMachine, "Carry", carry);
                AddState(stateMachine, "Death", death);
                animator.runtimeAnimatorController = controller;

                if (resident)
                {
                    ConfigureResidentProps(root, visual);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static AnimationClip Freeze(AnimationClip source, string name)
        {
            var clip = Object.Instantiate(source);
            clip.name = name;

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var value = AnimationUtility.GetEditorCurve(clip, binding).Evaluate(source.length * 0.25f);
                AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0, 1.2f, value));
            }

            return clip;
        }

        private static AnimationClip SaveClip(AnimationClip clip)
        {
            var path = Art + "Animations/" + clip.name + ".anim";
            var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (saved == null)
            {
                AssetDatabase.CreateAsset(clip, path);
                return clip;
            }

            EditorUtility.CopySerialized(clip, saved);
            Object.DestroyImmediate(clip);
            return saved;
        }

        private static void AddState(AnimatorStateMachine machine, string name, AnimationClip clip)
        {
            var state = machine.AddState(name);
            state.motion = clip;
        }

        private static void AddArmMotion(AnimationClip clip, Transform root, bool working)
        {
            foreach (var boneName in new[] { "mixamorig:RightArm", "mixamorig:LeftArm" })
            {
                var bone = root.GetComponentsInChildren<Transform>().First(item => item.name == boneName);
                var path = AnimationUtility.CalculateTransformPath(bone, root);
                var sign = boneName.Contains("Right") ? 1 : -1;
                var lower = bone.localRotation * Quaternion.Euler(0, 0, sign * 35);
                var upper = bone.localRotation * Quaternion.Euler(working ? -35 : -15, 0, sign * 60);
                var values = new[] { lower, working ? upper : lower, lower };
                var properties = new[] { "x", "y", "z", "w" };

                for (var component = 0; component < 4; component++)
                {
                    var curve = new AnimationCurve();

                    for (var index = 0; index < values.Length; index++)
                    {
                        curve.AddKey(index * 0.6f, values[index][component]);
                    }

                    clip.SetCurve(path, typeof(Transform), "m_LocalRotation." + properties[component], curve);
                }
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static AnimationClip RetargetDeath(string sourceName, GameObject target, string name)
        {
            var path = Art + "Animations/CharacterDeaths.fbx";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var original = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(clip => clip.name == sourceName);
            var sourceRoot = sourceName.Split('|')[0];
            var clip = new AnimationClip { name = name, frameRate = original.frameRate };

            foreach (var binding in AnimationUtility.GetCurveBindings(original))
            {
                if (!binding.path.StartsWith(sourceRoot + "/"))
                {
                    continue;
                }

                var targetPath = "Armature/" + binding.path.Substring(sourceRoot.Length + 1);
                var targetBone = target.transform.Find(targetPath);
                var sourceBone = source.transform.Find(binding.path);

                if (targetBone == null || sourceBone == null)
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(original, binding);

                if (binding.propertyName.StartsWith("m_LocalPosition."))
                {
                    var component = "xyz".IndexOf(binding.propertyName.Last());
                    var ratio = sourceBone.localPosition.magnitude > 0.0001f
                        ? targetBone.localPosition.magnitude / sourceBone.localPosition.magnitude
                        : 1;
                    var offset = targetBone.localPosition[component] - sourceBone.localPosition[component] * ratio;
                    var keys = curve.keys;

                    for (var index = 0; index < keys.Length; index++)
                    {
                        keys[index].value = keys[index].value * ratio + offset;
                        keys[index].inTangent *= ratio;
                        keys[index].outTangent *= ratio;
                    }

                    curve.keys = keys;
                }

                var converted = binding;
                converted.path = targetPath;
                AnimationUtility.SetEditorCurve(clip, converted, curve);
            }

            return SaveClip(clip);
        }

        private static void ConfigureResidentProps(GameObject root, GameObject visual)
        {
            foreach (var old in root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == "CarriedCrates" || item.name == "GatheringTool").ToArray())
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var view = root.GetComponent<SurvivorView>();
            var renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>();
            var colours = new[] { new Color(0.42f, 0.35f, 0.24f), new Color(0.31f, 0.37f, 0.31f), new Color(0.37f, 0.37f, 0.40f) };
            var materials = colours.Select((colour, index) => Material("ResidentClothes" + index, colour)).ToArray();
            var data = new SerializedObject(view);
            data.FindProperty("clothesRenderer").objectReferenceValue = renderer;
            var variants = data.FindProperty("clothesVariants");
            variants.arraySize = materials.Length;

            for (var index = 0; index < materials.Length; index++)
            {
                variants.GetArrayElementAtIndex(index).objectReferenceValue = materials[index];
            }

            var spine = visual.GetComponentsInChildren<Transform>().First(bone => bone.name == "mixamorig:Spine2");
            var cargo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Environment/Crates.prefab"));
            cargo.name = "CarriedCrates";
            Object.DestroyImmediate(cargo.GetComponent<Collider>());
            cargo.transform.localScale = Vector3.one * 0.23f;
            cargo.transform.position = root.transform.position + new Vector3(0, 1.05f, 0.30f);
            cargo.transform.SetParent(spine, true);
            cargo.SetActive(false);
            data.FindProperty("cargoVisual").objectReferenceValue = cargo;

            var hand = visual.GetComponentsInChildren<Transform>().First(bone => bone.name == "mixamorig:RightHand");
            var tool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tool.name = "GatheringTool";
            Object.DestroyImmediate(tool.GetComponent<Collider>());
            tool.transform.position = hand.position - Vector3.up * 0.15f;
            tool.transform.localScale = new Vector3(0.04f, 0.4f, 0.04f);
            tool.GetComponent<Renderer>().sharedMaterial = Material("PropWood", colours[0]);
            tool.transform.SetParent(hand, true);
            tool.SetActive(false);
            data.FindProperty("toolVisual").objectReferenceValue = tool;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateArmedResidentPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Animations/ResidentShooting.fbx");
            var root = Object.Instantiate(source);
            root.name = "ResidentShooting";
            var controllerPath = Art + "Animations/ResidentShooting.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            foreach (var state in controller.layers[0].stateMachine.states)
            {
                controller.layers[0].stateMachine.RemoveState(state.state);
            }
            var clip = AssetDatabase.LoadAllAssetsAtPath(Art + "Animations/ResidentShooting.fbx").OfType<AnimationClip>()
                .Single(item => item.name == "Armature|mixamo.com");
            AddState(controller.layers[0].stateMachine, "Shoot", clip);
            var animator = root.GetComponent<Animator>();

            if (animator == null)
            {
                animator = root.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = Material("ResidentClothes0", new Color(0.42f, 0.35f, 0.24f));
            }

            PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "Characters/ResidentShooting.prefab");
            Object.DestroyImmediate(root);
        }

        private static void ConfigureGateMotion()
        {
            var source = AssetDatabase.LoadAllAssetsAtPath(Art + "Animations/GateOpen.fbx").OfType<AnimationClip>()
                .Single(clip => clip.name == "part_0|part_0Действие");
            var binding = AnimationUtility.GetCurveBindings(source)
                .Single(item => item.path == "part_0" && item.propertyName == "m_LocalPosition.x");
            var original = AnimationUtility.GetEditorCurve(source, binding);
            var peak = original.keys.OrderByDescending(key => key.value).First();
            var curve = new AnimationCurve();

            foreach (var key in original.keys.Where(key => key.time <= peak.time))
            {
                curve.AddKey(key.time / peak.time, key.value / peak.value);
            }

            var view = Object.FindFirstObjectByType<_Game.Code.Features.Gates.View.GateWorldView>();
            var data = new SerializedObject(view);
            data.FindProperty("openingMotion").animationCurveValue = curve;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material Material(string name, Color colour)
        {
            var path = Art + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                material.SetColor("_BaseColor", colour);
                material.SetFloat("_Smoothness", 0.15f);
                AssetDatabase.CreateAsset(material, path);
            }

            return material;
        }
    }
}
