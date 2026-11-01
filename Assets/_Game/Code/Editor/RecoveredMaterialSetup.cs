using System.IO;
using UnityEditor;
using UnityEngine;

namespace _Game.Code.Editor
{
    public static class RecoveredMaterialSetup
    {
        private const string Root = "Assets/_Game/GatePrototype";
        private const string Textures = Root + "/Art/Textures/";

        public static void Apply()
        {
            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(Textures + "RockNormal.exr");
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.sRGBTexture = false;
            normalImporter.SaveAndReimport();

            var roughnessImporter = (TextureImporter)AssetImporter.GetAtPath(Textures + "RockRoughness.jpg");
            roughnessImporter.sRGBTexture = false;
            roughnessImporter.isReadable = true;
            roughnessImporter.SaveAndReimport();
            var roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "RockRoughness.jpg");
            var pixels = roughness.GetPixels32();

            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = new Color32(0, 0, 0, (byte)(255 - pixels[index].r));
            }

            var packed = new Texture2D(roughness.width, roughness.height, TextureFormat.RGBA32, false, true);
            packed.SetPixels32(pixels);
            packed.Apply();
            var packedPath = Textures + "RockMetallicSmoothness.png";
            File.WriteAllBytes(packedPath, ImageConversion.EncodeToPNG(packed));
            Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(packedPath);
            var packedImporter = (TextureImporter)AssetImporter.GetAtPath(packedPath);
            packedImporter.sRGBTexture = false;
            packedImporter.SaveAndReimport();
            roughnessImporter.isReadable = false;
            roughnessImporter.SaveAndReimport();

            var stone = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Rendering/Stone.mat");
            stone.SetColor("_BaseColor", Color.white);
            stone.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "RockBaseColor.jpg"));
            stone.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "RockNormal.exr"));
            stone.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
            stone.SetFloat("_Smoothness", 1);
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(stone);
            stone.EnableKeyword("_NORMALMAP");
            stone.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(stone);

            var bark = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Rendering/TreeBark.mat");
            bark.SetColor("_BaseColor", Color.white);
            bark.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "BarkBaseColor.png"));
            EditorUtility.SetDirty(bark);
            AssetDatabase.SaveAssets();
        }
    }
}
