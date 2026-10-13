using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BunnyHeaven.UI.Editor
{
    /// <summary>Sprite import settings, TMP font assets and the theme asset for the Bunny Heaven UI kit.</summary>
    public static class KitAssets
    {
        public const string Root = "Assets/UI/BunnyHeaven";
        public const string SpritesDir = Root + "/Sprites";
        public const string FontsDir = Root + "/Fonts";
        public const string ThemePath = Root + "/Theme/BunnyHeavenTheme.asset";

        // Frame PNGs are rasterised at 3x Figma points, icons at 96px, the doodle pattern at 2x.
        private const float FramePpu = 300f;
        private const float IconPpu = 400f;
        private const float PatternPpu = 200f;

        // 9-slice borders in Figma points: left, bottom, right, top.
        private static readonly Dictionary<string, Vector4> Borders = new()
        {
            { "Card", new Vector4(40, 34, 40, 30) },
            { "ModalCard", new Vector4(46, 46, 46, 46) },
            { "Button", new Vector4(46, 40, 46, 36) },
            { "ButtonSmall", new Vector4(36, 30, 36, 28) },
            { "Pill", new Vector4(34, 32, 34, 28) },
            { "Ribbon", new Vector4(20, 20, 20, 20) },
            { "Banner", new Vector4(40, 40, 40, 40) },
            { "SegmentTrack", new Vector4(26, 20, 26, 20) },
            { "SegmentOn", new Vector4(22, 18, 22, 18) },
            { "Toggle_Track", new Vector4(20, 18, 20, 18) },
            { "Slider_Track", new Vector4(10, 0, 10, 0) },
        };

        // Everything the UI is likely to print. Static atlases keep the font assets stable in git.
        private const string Charset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "·•…–—‘’“”×÷°©®™✓←→↑↓";

        [MenuItem("Bunny Heaven/UI Kit/Configure Sprites")]
        public static void ConfigureSprites()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var name = Path.GetFileNameWithoutExtension(path);
                var isIcon = path.Contains("/Icons/");
                var isPattern = path.Contains("/Patterns/");

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.spritePixelsPerUnit = isIcon ? IconPpu : isPattern ? PatternPpu : FramePpu;
                importer.maxTextureSize = 2048;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);

                var key = name.Replace("Frame_", "").Replace("_Shadow", "");
                importer.spriteBorder = Borders.TryGetValue(key, out var border) ? border * (FramePpu / 100f) : Vector4.zero;
                importer.SaveAndReimport();
            }
        }

        public static Sprite Sprite(string relativePath) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/{relativePath}.png");

        public static Sprite Icon(string name) => Sprite($"Icons/Icon_{name}");

        [MenuItem("Bunny Heaven/UI Kit/Create Font Assets")]
        public static void CreateFonts()
        {
            CreateFont("LondrinaSolid-Black", 96);
            CreateFont("ShantellSans-Regular", 72);
            CreateFont("ShantellSans-Bold", 72);
            CreateHeadingShadowMaterial();
        }

        public static TMP_FontAsset Font(string name) =>
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/{name} SDF.asset");

        public static string HeadingShadowMaterialPath => $"{FontsDir}/LondrinaSolid-Black SDF - Ink Shadow.mat";

        private static void CreateFont(string name, int samplingSize)
        {
            // Existing font assets are kept so their GUIDs (and every reference to them) stay stable.
            // Delete one by hand to regenerate it.
            var assetPath = $"{FontsDir}/{name} SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
                return;

            var source = AssetDatabase.LoadAssetAtPath<Font>($"{FontsDir}/{name}.ttf");
            var asset = TMP_FontAsset.CreateFontAsset(source, samplingSize, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, false);
            asset.name = $"{name} SDF";
            if (!asset.TryAddCharacters(Charset, out var missing) && !string.IsNullOrEmpty(missing))
                Debug.Log($"[UI Kit] {name} has no glyphs for: {missing}");

            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            asset.atlasTexture.name = $"{name} Atlas";
            asset.material.name = $"{name} SDF Material";

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static void CreateHeadingShadowMaterial()
        {
            var font = Font("LondrinaSolid-Black");
            var material = AssetDatabase.LoadAssetAtPath<Material>(HeadingShadowMaterialPath);
            if (material == null)
            {
                material = new Material(font.material) { name = "LondrinaSolid-Black SDF - Ink Shadow" };
                AssetDatabase.CreateAsset(material, HeadingShadowMaterialPath);
            }

            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", UiTheme.Hex(0x3B2A20));
            material.SetFloat("_UnderlayOffsetX", 0f);
            material.SetFloat("_UnderlayOffsetY", -0.75f);
            material.SetFloat("_UnderlayDilate", 0.6f);
            material.SetFloat("_UnderlaySoftness", 0f);
            ShaderUtilities.UpdateShaderRatios(material);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        [MenuItem("Bunny Heaven/UI Kit/Create Theme")]
        public static UiTheme CreateOrUpdateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            theme.headingFont = Font("LondrinaSolid-Black");
            theme.bodyFont = Font("ShantellSans-Regular");
            theme.bodyBoldFont = Font("ShantellSans-Bold");
            theme.headingShadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(HeadingShadowMaterialPath);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        public static UiTheme Theme => AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
    }
}
