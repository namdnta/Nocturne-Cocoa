using System.IO;
using UnityEditor;
using UnityEngine;

namespace NocturneCocoa.EditorTools
{
    /// <summary>
    /// Generates tiny white placeholder sprites (tint them with SpriteRenderer.color) so exploration can be
    /// tested before real pixel art exists. Replace them with your Aseprite art whenever you're ready.
    /// </summary>
    public static class PlaceholderSprites
    {
        public const string Folder = "Assets/_Project/Art/Shared/Placeholders";
        public const int PixelsPerUnit = 16;

        /// <summary>Plain 1x1 unit square, centre pivot. For floors and walls.</summary>
        public static Sprite Fill
        {
            get { return GetOrCreate("Placeholder_Fill", 16, 16, SpriteAlignment.Center, FillPixel); }
        }

        /// <summary>1x1 unit box with an outline, bottom pivot. For furniture (Y-sorts by its base).</summary>
        public static Sprite Box
        {
            get { return GetOrCreate("Placeholder_Box", 16, 16, SpriteAlignment.BottomCenter, BoxPixel); }
        }

        /// <summary>Small character with eyes, bottom (feet) pivot.</summary>
        public static Sprite Character
        {
            get { return GetOrCreate("Placeholder_Character", 12, 20, SpriteAlignment.BottomCenter, CharacterPixel); }
        }

        /// <summary>Small diamond used as the "you can interact" prompt.</summary>
        public static Sprite Prompt
        {
            get { return GetOrCreate("Placeholder_Prompt", 9, 9, SpriteAlignment.Center, PromptPixel); }
        }

        /// <summary>32x32 face for the dialogue box. Colour is baked in so each character looks different.</summary>
        public static Sprite Portrait(string key, Color skin, bool happy)
        {
            string fileName = $"Placeholder_Portrait_{key}_{(happy ? "happy" : "neutral")}";
            return GetOrCreate(fileName, 32, 32, SpriteAlignment.Center, (x, y, w, h) => PortraitPixel(x, y, skin, happy));
        }

        /// <summary>16x32 full-body side-view figure facing right, bottom pivot, for the dialogue stage.</summary>
        public static Sprite SideView(string key, Color body)
        {
            return GetOrCreate($"Placeholder_SideView_{key}", 16, 32, SpriteAlignment.BottomCenter, (x, y, w, h) => SideViewPixel(x, y, body));
        }

        private delegate Color PixelFunction(int x, int y, int width, int height);

        private static Sprite GetOrCreate(string fileName, int width, int height, SpriteAlignment pivot, PixelFunction pixel)
        {
            string path = $"{Folder}/{fileName}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(Folder);

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, pixel(x, y, width, height));
                }
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
        private static readonly Color Outline = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Color Dark = new Color(0.15f, 0.1f, 0.15f, 1f);

        private static Color FillPixel(int x, int y, int w, int h)
        {
            return Color.white;
        }

        private static Color BoxPixel(int x, int y, int w, int h)
        {
            bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
            return edge ? Outline : Color.white;
        }

        private static Color CharacterPixel(int x, int y, int w, int h)
        {
            // Rounded body, dark outline, two eyes near the top.
            bool corner = (x == 0 || x == w - 1) && (y == 0 || y == h - 1 || y == h - 2);
            if (corner) return Clear;

            bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
            if (edge) return Dark;

            bool eye = (x == 3 || x == w - 4) && (y == h - 6 || y == h - 7);
            return eye ? Dark : Color.white;
        }

        private static Color PortraitPixel(int x, int y, Color skin, bool happy)
        {
            float dx = x - 15.5f;
            float dy = y - 15f;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);

            if (distance > 13f) return Clear;
            if (distance > 12f) return Dark;

            bool eye = (x == 11 || x == 12 || x == 19 || x == 20) && (y == 17 || y == 18);
            if (eye) return Dark;

            bool mouth = happy
                ? ((y == 10 && (x == 13 || x == 18)) || (y == 9 && x >= 14 && x <= 17))
                : (y == 10 && x >= 13 && x <= 18);
            if (mouth) return Dark;

            bool blush = happy && y == 12 && (x == 9 || x == 10 || x == 21 || x == 22);
            if (blush) return new Color(1f, 0.55f, 0.6f, 1f);

            return skin;
        }

        private static Color SideViewPixel(int x, int y, Color body)
        {
            // Head: circle centred at (8, 25), radius 6. Body: rounded block below it.
            float dx = x - 7.5f;
            float dy = y - 25f;
            float headDistance = Mathf.Sqrt(dx * dx + dy * dy);
            bool inHead = headDistance <= 6f;
            bool headEdge = inHead && headDistance > 5f;

            bool inBody = x >= 3 && x <= 12 && y >= 0 && y <= 19;
            bool bodyEdge = inBody && (x == 3 || x == 12 || y == 0 || (y == 19 && !inHead));

            if (!inHead && !inBody) return Clear;
            if (headEdge || bodyEdge) return Dark;

            bool eye = x == 10 && (y == 26 || y == 25);
            return eye ? Dark : body;
        }

        private static Color PromptPixel(int x, int y, int w, int h)
        {
            int c = w / 2;
            int distance = Mathf.Abs(x - c) + Mathf.Abs(y - c);
            if (distance > c) return Clear;
            return distance == c ? Dark : Color.white;
        }
    }
}