using System;
using System.IO;
using System.Collections.Generic;
using LinhThu.Prototype;
using UnityEditor;
using UnityEngine;

namespace LinhThu.Editor
{
    /// <summary>Imports loose sprites and creates reusable prefabs without overwriting authored layouts.</summary>
    public static class ModularMapBuilder
    {
        public const string ArtRoot = "Assets/_Project/Art/CartoonV2/Environment/";
        public const string PrefabRoot = "Assets/_Project/Content/Prefabs/Environment/";
        public const string MapPath = PrefabRoot + "MeadowMap.prefab";
        private static readonly string[] Names = {
            "ground-road", "cherry-tree", "rock", "grass-tuft", "white-flowers",
            "bush", "pond-cluster" };
        private static readonly Dictionary<string, Rect> VisiblePixels = new Dictionary<string, Rect>();

        public static void PrepareAssets()
        {
            Directory.CreateDirectory(PrefabRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string name in Names)
            {
                string path = ArtRoot + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing modular environment PNG: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 100;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                // Import metadata uses the visible silhouette; generated PNG padding is kept intact.
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!texture.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("Invalid PNG: " + path);
                    var pixels = texture.GetPixels32(); int left = texture.width, right = -1, bottom = texture.height, top = -1;
                    for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                    {
                        if (pixels[y * texture.width + x].a < 128) continue;
                        left = Math.Min(left, x); right = Math.Max(right, x);
                        bottom = Math.Min(bottom, y); top = Math.Max(top, y);
                    }
                    if (right < left) throw new InvalidOperationException("Empty PNG: " + path);
                    var visible = new Rect(left, bottom, right - left + 1, top - bottom + 1);
                    VisiblePixels[name] = new Rect(visible.x / texture.width, visible.y / texture.height,
                        visible.width / texture.width, visible.height / texture.height);
                    settings.spritePivot = new Vector2(visible.center.x / texture.width,
                        (IsFloor(name) ? visible.center.y : visible.yMin) / texture.height);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            Prop("cherry-tree", "Cây anh đào", 4.1f, true, new Vector2(2.5f, 1.05f));
            Prop("rock", "Đá", 1.2f, false, new Vector2(.9f, .3f));
            Prop("grass-tuft", "Bụi cỏ", .6f, false, Vector2.zero);
            Prop("white-flowers", "Hoa trắng", .58f, false, Vector2.zero);
            Prop("bush", "Bụi cây", 1.1f, false, new Vector2(.8f, .3f));
            Prop("pond-cluster", "Hồ nước · đá · cỏ bờ hồ", 3.8f, false, Vector2.zero);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!File.Exists(MapPath)) CreateMap();
            ArtPresentationBuilder.Apply();
            AssetDatabase.SaveAssets();
        }

        public static Sprite Load(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + name + ".png");
            if (sprite == null) throw new InvalidOperationException("Missing modular sprite: " + name);
            return sprite;
        }

        public static ModularMapLayout Layout()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapPath);
            if (prefab == null || prefab.GetComponent<ModularMapLayout>() == null)
                throw new InvalidOperationException("Missing shared modular map prefab.");
            return prefab.GetComponent<ModularMapLayout>();
        }

        private static bool IsFloor(string name) => name == "ground-road" || name == "pond-cluster";

        private static void Prop(string id, string label, float size, bool height, Vector2 shadowSize)
        {
            string path = PrefabRoot + id + ".prefab";
            if (File.Exists(path)) return;
            var root = new GameObject(label);
            try
            {
                var prop = root.AddComponent<ModularMapProp>(); prop.floorLayer = IsFloor(id);
                prop.floorOrder = -44;
                var body = new GameObject("Visual").AddComponent<SpriteRenderer>();
                body.transform.SetParent(root.transform, false); body.sprite = Load(id);
                var visible = VisiblePixels[id];
                body.transform.localScale = Vector3.one * (size / (height ? visible.height * body.sprite.bounds.size.y : visible.width * body.sprite.bounds.size.x));
                prop.visual = body;
                body.sortingOrder = prop.floorLayer ? prop.floorOrder : 100;
                if (shadowSize != Vector2.zero)
                {
                    var shade = new GameObject("Bóng đổ").AddComponent<SpriteRenderer>();
                    shade.transform.SetParent(root.transform, false);
                    shade.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/CartoonV2/shadow.png");
                    if (shade.sprite == null) throw new InvalidOperationException("Missing separate shadow sprite.");
                    shade.transform.localScale = new Vector3(shadowSize.x / shade.sprite.bounds.size.x, shadowSize.y / shade.sprite.bounds.size.y, 1);
                    shade.transform.localPosition = new Vector3(shadowSize.x * .22f, -shadowSize.y * .22f, 0);
                    shade.color = new Color(1, 1, 1, id == "cherry-tree" ? .2f : .14f);
                    shade.sortingOrder = -30;
                    prop.castShadow = shade;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CreateMap()
        {
            var root = new GameObject("MeadowMap — bố cục có thể chỉnh");
            try
            {
                var layout = root.AddComponent<ModularMapLayout>();
                var ground = new GameObject("Nền cỏ + đường đất cố định").AddComponent<SpriteRenderer>();
                ground.transform.SetParent(root.transform, false); ground.sprite = Load("ground-road");
                ground.sortingOrder = -50; ground.transform.localScale = Vector3.one * (20f / ground.sprite.bounds.size.x);
                layout.ground = ground; layout.size = ground.bounds.size;
                var floor = new GameObject("Hồ nước trọn cụm").transform; floor.SetParent(root.transform, false);
                var props = new GameObject("Cây · đá · cỏ · hoa").transform; props.SetParent(root.transform, false);
                Place(props, "cherry-tree", "Anh đào trái", new Vector2(-7.0f, 1.30f));
                Place(props, "cherry-tree", "Anh đào phải", new Vector2(7.0f, 1.55f), .94f);
                Place(props, "rock", "Đá trái", new Vector2(-7.8f, -.7f));
                Place(props, "rock", "Đá phía trên", new Vector2(-3.9f, 2.95f), .8f);
                Place(props, "rock", "Đá góc dưới", new Vector2(6.7f, -3.55f), 1.05f);
                Place(props, "bush", "Bụi cây trái", new Vector2(-7.3f, -3.2f));
                Place(props, "bush", "Bụi cây phải", new Vector2(7.0f, .35f), .9f);
                var center = new Vector2(7.25f, -1.35f);
                Place(floor, "pond-cluster", "Hồ nước trọn cụm", center);
                var grass = new[] { new Vector2(-8.5f, -2.5f), new Vector2(-6.4f, .4f), new Vector2(-2.8f, 3.1f), new Vector2(2.3f, -3.2f), new Vector2(4.9f, 1.1f), new Vector2(8.7f, -2.1f), new Vector2(.6f, 2.3f), new Vector2(-3.6f, -1.6f) };
                for (int i = 0; i < grass.Length; i++) Place(props, "grass-tuft", "Bụi cỏ " + (i + 1), grass[i], i % 2 == 0 ? 1 : .8f);
                var flowers = new[] { new Vector2(-5, -3), new Vector2(4.5f, -2.8f), new Vector2(6.15f, 1.9f), new Vector2(-5.1f, 1.8f), new Vector2(2, 2.75f) };
                for (int i = 0; i < flowers.Length; i++) Place(props, "white-flowers", "Hoa trắng " + (i + 1), flowers[i]);
                PrefabUtility.SaveAsPrefabAsset(root, MapPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void Place(Transform parent, string id, string label, Vector2 position, float scale = 1, float angle = 0)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + id + ".prefab");
            if (source == null) throw new InvalidOperationException("Missing placeable prefab: " + id);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            instance.name = label; instance.transform.localPosition = position;
            instance.transform.localScale = Vector3.one * scale;
            instance.transform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        public static void AttachToTrial(CartoonTrialDirector director)
        {
            var layout = UnityEngine.Object.FindAnyObjectByType<ModularMapLayout>();
            if (layout == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapPath);
                if (prefab == null) throw new InvalidOperationException("Missing editable meadow map.");
                layout = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<ModularMapLayout>();
            }
            if (director.ground != null && director.ground != layout.ground)
                UnityEngine.Object.DestroyImmediate(director.ground.gameObject);
            director.ground = layout.ground;
        }

        [MenuItem("Demo/Môi trường/Mở bố cục map dùng chung")]
        public static void OpenMap()
        {
            AssetDatabase.OpenAsset(Layout().gameObject);
        }

        [MenuItem("Demo/Môi trường/Lưu bố cục cảnh mẫu vào map dùng chung")]
        public static void ApplyTrialLayout()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before saving a layout.");
            var layout = UnityEngine.Object.FindAnyObjectByType<ModularMapLayout>();
            if (layout == null || !PrefabUtility.IsPartOfPrefabInstance(layout.gameObject))
                throw new InvalidOperationException("Open the art trial scene and select its modular map instance.");
            PrefabUtility.ApplyPrefabInstance(layout.gameObject, InteractionMode.UserAction);
            AssetDatabase.SaveAssets();
        }
    }
}
