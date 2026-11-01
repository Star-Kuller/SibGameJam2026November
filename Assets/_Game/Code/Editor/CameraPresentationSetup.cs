using System;
using System.Linq;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.Turret.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace _Game.Code.Editor
{
    public static class CameraPresentationSetup
    {
        private const string Root = "Assets/_Game/GatePrototype";
        private const float HorizontalFieldOfView = 65.81f;

        [MenuItem("Last Shift/Update Camera Presentation")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode before updating camera presentation.");
            }

            var view = UnityEngine.Object.FindFirstObjectByType<TurretView>();

            if (view == null)
            {
                throw new InvalidOperationException("Open GatePrototype before updating camera presentation.");
            }

            var rig = view.transform;
            var offset = new Vector3(0, rig.position.y, -19.4f) - rig.position;
            rig.position += offset;
            GameObject.Find("TurretSupport").transform.position += offset;

            var serialized = new SerializedObject(view);
            serialized.FindProperty("horizontalFieldOfView").floatValue = HorizontalFieldOfView;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.Camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(HorizontalFieldOfView, view.Camera.aspect);

            var settings = AssetDatabase.LoadAssetAtPath<GatePrototypeSettings>(Root + "/GatePrototypeSettings.asset");
            settings.yawLimit = 31;
            settings.minPitch = -3;
            settings.maxPitch = 34;
            EditorUtility.SetDirty(settings);

            ExpandGround();
            ConfigureFont();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        private static void ExpandGround()
        {
            var ground = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .First(renderer => renderer.sharedMaterial != null && renderer.sharedMaterial.name == "Soil");
            var width = Mathf.Max(ground.bounds.size.x, ground.bounds.size.z);
            ground.transform.localScale *= 4000 / width;

            var collider = GameObject.Find("Ground").GetComponent<BoxCollider>();
            collider.size = new Vector3(4000, collider.size.y, 4000);
        }

        private static void ConfigureFont()
        {
            var fallback = CreateFont("CameraCyrillic", "Assets/TextMesh Pro/Fonts/LiberationSans.ttf",
                "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя№—…·");
            var cameraFont = CreateFont("CameraOsd", Root + "/Fonts/VCR_OSD_MONO.ttf",
                new string(Enumerable.Range(32, 95).Select(value => (char)value).ToArray()));
            cameraFont.fallbackFontAssetTable = new System.Collections.Generic.List<FontAsset> { fallback };
            EditorUtility.SetDirty(cameraFont);
        }

        private static FontAsset CreateFont(string name, string sourcePath, string characters)
        {
            var path = Root + "/Fonts/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);

            if (asset != null)
            {
                return asset;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            asset = FontAsset.CreateFontAsset(font);
            asset.name = name;
            asset.TryAddCharacters(characters);
            AssetDatabase.CreateAsset(asset, path);
            asset.material.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(asset.material, path);

            foreach (var texture in asset.atlasTextures)
            {
                texture.hideFlags = HideFlags.None;
                AssetDatabase.AddObjectToAsset(texture, path);
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
