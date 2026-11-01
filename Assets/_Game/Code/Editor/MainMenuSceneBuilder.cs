using _Game.Code.Core.Scenes;
using _Game.Code.Features.Audio;
using _Game.Code.Features.GateEncounter;
using _Game.Code.Features.Input.View;
using _Game.Code.Features.MainMenu;
using _Game.Code.Features.MainMenu.View;
using _Game.Code.Features.Presentation.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace _Game.Code.Editor
{
    public static class MainMenuSceneBuilder
    {
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                throw new System.InvalidOperationException("Stop Play Mode before creating the main menu scene.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneNavigation.MainMenuPath) != null)
            {
                return;
            }

            var original = SceneManager.GetActiveScene();
            var sourceInput = Object.FindFirstObjectByType<PlayerInputView>();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var cameraObject = new GameObject("MainMenuCamera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.065f);
            cameraObject.AddComponent<FMODUnity.StudioListener>();
            new GameObject("MenuLight").AddComponent<Light>().type = LightType.Directional;

            var ui = new GameObject("MainMenuUI");
            var document = ui.AddComponent<UIDocument>();
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
                "Assets/_Game/Design/UI/GateEncounter/GatePanel.asset");
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Game/Design/UI/MainMenu/MainMenu.uxml");
            ui.AddComponent<MainMenuView>();
            ui.AddComponent<InputSettingsView>();
            ui.AddComponent<MetalPanelView>();

            var input = new GameObject("MenuInput").AddComponent<PlayerInputView>();
            EditorUtility.CopySerialized(sourceInput, input);
            var scope = new GameObject("MainMenuScope").AddComponent<MainMenuScope>();
            var serialized = new SerializedObject(scope);
            serialized.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GatePrototypeSettings>(
                "Assets/_Game/Design/Configs/GatePrototypeSettings.asset");
            serialized.FindProperty("audioEvents").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GateAudioEvents>(
                "Assets/_Game/Audio/GateAudioEvents.asset");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, SceneNavigation.MainMenuPath);
            EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(original);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Game/Scenes/Bootstrap.unity", true),
                new EditorBuildSettingsScene(SceneNavigation.MainMenuPath, true),
                new EditorBuildSettingsScene(SceneNavigation.GatePath, true)
            };
            AssetDatabase.SaveAssets();
        }
    }
}
