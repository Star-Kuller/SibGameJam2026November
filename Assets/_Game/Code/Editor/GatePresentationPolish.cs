using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using _Game.Code.Features.Audio;

namespace _Game.Code.Editor
{
    public static class GatePresentationPolish
    {
        public static void Apply()
        {

            var texture = SoilTexture();

            foreach (var name in new[] { "Soil", "Ground" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/" + name + ".mat");
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", new Vector2(650, 650));
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", 0.05f);
                EditorUtility.SetDirty(material);
            }

            var light = Object.FindFirstObjectByType<Light>();
            light.transform.rotation = Quaternion.Euler(32, -35, 0);
            light.color = new Color(1, 0.87f, 0.69f);
            light.intensity = 1.35f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.24f, 0.27f, 0.27f);
            RenderSettings.fogStartDistance = 25;
            RenderSettings.fogEndDistance = 115;
            RenderSettings.ambientSkyColor = new Color(0.40f, 0.43f, 0.45f);
            RenderSettings.ambientEquatorColor = new Color(0.29f, 0.29f, 0.26f);

            var sounds = AssetDatabase.LoadAssetAtPath<GateAudioEvents>("Assets/_Game/Audio/GateAudioEvents.asset");
            sounds.gateCritical = FMODUnity.EventReference.Find("event:/Sounds/P0/UI/Gate_Danger");
            EditorUtility.SetDirty(sounds);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static Texture2D SoilTexture()
        {
            const int size = 256;
            const string path = "Assets/_Game/Art/Textures/SoilTile.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture != null)
            {
                return texture;
            }

            texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "SoilTile",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var broad = SeamlessNoise(x, y, size, 0.025f);
                    var grain = SeamlessNoise(x, y, size, 0.65f);
                    var colour = Color.Lerp(new Color(0.19f, 0.18f, 0.15f), new Color(0.32f, 0.30f, 0.25f), broad);
                    pixels[y * size + x] = colour * Mathf.Lerp(0.88f, 1.12f, grain);
                    pixels[y * size + x].a = 1;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static float SeamlessNoise(int x, int y, int size, float frequency)
        {
            var u = x / (float)size;
            var v = y / (float)size;
            var first = Mathf.Lerp(Mathf.PerlinNoise(x * frequency, y * frequency),
                Mathf.PerlinNoise((x - size) * frequency, y * frequency), u);
            var second = Mathf.Lerp(Mathf.PerlinNoise(x * frequency, (y - size) * frequency),
                Mathf.PerlinNoise((x - size) * frequency, (y - size) * frequency), u);
            return Mathf.Lerp(first, second, v);
        }
    }
}

