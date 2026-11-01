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
using UnityEngine.Video;

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
                ConfigureExistingScene();
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
            ConfigureVideo(camera);
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

        public static void ConfigureExistingScene()
        {
            if (EditorApplication.isPlaying)
            {
                throw new System.InvalidOperationException("Stop Play Mode before updating the main menu scene.");
            }

            var original = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(SceneNavigation.MainMenuPath);
            var opened = !scene.isLoaded;

            if (opened)
            {
                scene = EditorSceneManager.OpenScene(SceneNavigation.MainMenuPath, OpenSceneMode.Additive);
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var camera = root.GetComponentInChildren<Camera>();

                if (camera != null)
                {
                    ConfigureVideo(camera);
                    break;
                }
            }

            EditorSceneManager.SaveScene(scene);

            if (opened)
            {
                EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(original);
            }
        }

        private static void ConfigureVideo(Camera camera)
        {
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(
                "Assets/_Game/Art/Videos/MainMenuBackground.mp4");

            if (clip == null)
            {
                throw new System.InvalidOperationException("Main-menu background video has not been imported.");
            }

            var player = camera.GetComponent<VideoPlayer>();

            if (player == null)
            {
                player = camera.gameObject.AddComponent<VideoPlayer>();
            }

            player.source = VideoSource.VideoClip;
            player.clip = clip;
            player.renderMode = VideoRenderMode.CameraFarPlane;
            player.targetCamera = camera;
            player.targetCameraAlpha = 1;
            player.aspectRatio = VideoAspectRatio.FitOutside;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.playOnAwake = true;
            player.waitForFirstFrame = true;
            player.isLooping = true;
            player.skipOnDrop = true;
            EditorUtility.SetDirty(player);
        }
    }
}
