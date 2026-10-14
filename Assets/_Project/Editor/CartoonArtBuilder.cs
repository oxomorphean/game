using System;
using System.IO;
using System.Linq;
using LinhThu.Prototype;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LinhThu.Editor
{
    /// <summary>Imports the high-view art sample and binds presentation through Editor APIs.</summary>
    public static class CartoonArtBuilder
    {
        private const string Root = "Assets/_Project/Art/CartoonV2/";
        private const string Scene = "Assets/_Project/Content/Scenes/CartoonArtTrial.unity";

        public static string Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before preparing art.");
            foreach (string kind in new[] { "flat", "blank", "shadow", "herb", "ore", "egg", "portal", "camp" }) CreateIcon(kind);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string path in Directory.GetFiles(Root, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                bool character = path.EndsWith("hero.png", StringComparison.Ordinal) || path.EndsWith("huyen-quy.png", StringComparison.Ordinal);
                importer.filterMode = character ? FilterMode.Trilinear : FilterMode.Bilinear;
                importer.mipmapEnabled = character;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 100;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = path.EndsWith("field.png", StringComparison.Ordinal) ? 2048 : 1024;
                importer.spritePivot = new Vector2(.5f, path.EndsWith("hero.png", StringComparison.Ordinal) ? .14f
                    : path.EndsWith("huyen-quy.png", StringComparison.Ordinal) ? .19f : .5f);
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            ModularMapBuilder.PrepareAssets();
            AssetDatabase.SaveAssets(); BindTrial();
            return "Art imports updated; existing map layouts preserved.";
        }

        private static Sprite Load(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + name + ".png");
            if (sprite == null) throw new InvalidOperationException("Missing imported high-view sprite " + name);
            return sprite;
        }

        private static void CreateIcon(string kind)
        {
            int size = kind == "flat" || kind == "blank" ? 16 : 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size * 2 - 1, v = (y + .5f) / size * 2 - 1, d = 0;
                Color color = Color.clear;
                switch (kind)
                {
                    case "flat": color = new Color(.42f, .60f, .28f); break;
                    case "blank": break;
                    case "shadow": d = u * u + v * v * 2; color = new Color(.10f, .16f, .13f, Mathf.Clamp01((1 - d) * 3)); break;
                    case "herb": d = u * u * 2 + v * v; color = new Color(.26f, .59f, .33f, Mathf.Clamp01((.72f - d) * 16)); break;
                    case "ore": d = Mathf.Abs(u) * .7f + Mathf.Abs(v); color = new Color(.39f, .52f, .62f, Mathf.Clamp01((.83f - d) * 18)); break;
                    case "egg": d = u * u * 1.8f + v * v; color = new Color(.96f, .83f, .55f, Mathf.Clamp01((.7f - d) * 18)); break;
                    case "portal": d = Mathf.Abs(u * u + v * v - .45f); color = new Color(.50f, .38f, .75f, Mathf.Clamp01((.17f - d) * 20)); break;
                    case "camp": d = Mathf.Max(Mathf.Abs(u) * 1.5f, Mathf.Abs(v)); color = new Color(.90f, .61f, .23f, Mathf.Clamp01((.7f - d) * 20)); break;
                }
                texture.SetPixel(x, y, color);
            }
            texture.Apply(); File.WriteAllBytes(Root + kind + ".png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        public static string CreateTrial()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("High-view trial camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 5.625f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.42f, .60f, .28f); camera.allowHDR = false; camera.allowMSAA = false;
            var hero = Character("Hero", CartoonBodyKind.Human, Load("hero"), new Vector2(.55f, -.55f), 1.65f);
            var pet = Character("Pet", CartoonBodyKind.Turtle, Load("huyen-quy"), new Vector2(-.75f, -1.10f), 1.60f);
            var director = new GameObject("Trial").AddComponent<CartoonTrialDirector>();
            director.view = camera; director.hero = hero; director.pet = pet;
            ModularMapBuilder.AttachToTrial(director);
            var canvas = new GameObject("Trial instructions", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            var font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-TMP");
            if (font == null) throw new InvalidOperationException("Trial instructions require the Vietnamese font.");
            Text(canvas.transform, font, "NGỰ THÚ SƯ · MẪU GÓC NHÌN CAO", new Vector2(20, -16), 20);
            Text(canvas.transform, font, "WASD / bấm sân để đi · Tab đổi người/thú · R về điểm xuất phát", new Vector2(20, -44), 15);
            director.controlLabel = Text(canvas.transform, font, "Đang điều khiển: Ngự thú sư", new Vector2(20, -68), 14);
            EditorSceneManager.SaveScene(scene, Scene); AssetDatabase.SaveAssets(); Selection.activeGameObject = hero.gameObject;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            return Scene;
        }

        private static void BindTrial()
        {
            if (!File.Exists(Scene)) { CreateTrial(); return; }
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            var director = UnityEngine.Object.FindAnyObjectByType<CartoonTrialDirector>();
            if (director == null) throw new InvalidOperationException("Existing art trial is missing its director.");
            ModularMapBuilder.AttachToTrial(director);
            director.hero.source = Load("hero"); director.pet.source = Load("huyen-quy");
            EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
            EditorSceneManager.SaveScene(director.gameObject.scene);
        }

        private static CartoonCharacterRig Character(string name, CartoonBodyKind kind, Sprite sprite, Vector2 position, float size)
        {
            var rig = new GameObject(name).AddComponent<CartoonCharacterRig>();
            rig.kind = kind; rig.source = sprite; rig.displaySize = size; rig.transform.position = position;
            var shadow = new GameObject("Contact shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(rig.transform, false); shadow.sprite = Load("shadow");
            shadow.transform.localScale = new Vector3(1.0f / shadow.sprite.bounds.size.x, .3f / shadow.sprite.bounds.size.y, 1);
            shadow.color = new Color(1, 1, 1, .23f); rig.contactShadow = shadow;
            return rig;
        }

        private static TMP_Text Text(Transform parent, TMP_FontAsset font, string value, Vector2 point, float size)
        {
            var go = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = point; rect.sizeDelta = new Vector2(1100, 28);
            var text = go.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size;
            text.text = value; text.color = new Color(.08f, .14f, .15f); text.raycastTarget = false;
            return text;
        }

        public static string BuildTrialWeb()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before building.");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Scene }, target = BuildTarget.WebGL,
                locationPathName = Path.GetFullPath("WebBuild"), options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("High-view Web build failed.");
            var files = Directory.GetFiles("WebBuild/Build");
            string html = File.ReadAllText("Web/cartoon.html"), version = Guid.NewGuid().ToString("N");
            foreach (var pair in new[] { new[] { "__LOADER__", ".loader.js" }, new[] { "__DATA__", ".data" }, new[] { "__FRAMEWORK__", ".framework.js" }, new[] { "__WASM__", ".wasm" } })
                html = html.Replace(pair[0], Path.GetFileName(files.Single(file => file.EndsWith(pair[1], StringComparison.Ordinal))) + "?v=" + version);
            File.WriteAllText("WebBuild/index.html", html);
            File.Copy("Assets/_Project/Resources/Fonts/OFL.txt", "WebBuild/FONT-LICENSE.txt", true);
            string json = JsonUtility.ToJson(new BuildEvidence {
                success = true, seconds = report.summary.totalTime.TotalSeconds,
                bytes = report.summary.totalSize, errors = report.summary.totalErrors }, true);
            File.WriteAllText("Verification/CartoonTrial/high-view-build.json", json + "\n"); return json;
        }

        [Serializable] private sealed class BuildEvidence { public bool success; public double seconds; public ulong bytes; public int errors; }
    }
}
