using System;
using System.IO;
using System.Linq;
using LinhThu.Prototype;
using LinhThu.Prototype.Characters;
using LinhThu.Prototype.Preview;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Editor
{
    /// <summary>Creates and builds the art-only entry scene with the existing authored map and character.</summary>
    public static class ArtPreviewBuilder
    {
        public const string Scene = "Assets/_Project/Content/Scenes/ArtPreview.unity";
        public const string Hero = "Assets/_Project/Content/Characters/Hero8Directions.asset";
        public const string Map = "Assets/_Project/Content/Prefabs/Environment/MeadowMainMap.prefab";

        [MenuItem("Demo/Mỹ thuật/Tạo bản xem map và nhân vật")]
        public static void CreateMenu() => Prepare();

        public static string Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save current scene edits first.");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Content/Characters"))
                AssetDatabase.CreateFolder("Assets/_Project/Content", "Characters");
            foreach (string name in new[] { "Hero8Directions", "ModularHeroFront" })
            {
                string from = "Assets/_Project/Content/Combat/" + name + ".asset";
                string to = "Assets/_Project/Content/Characters/" + name + ".asset";
                if (File.Exists(from))
                {
                    string error = AssetDatabase.MoveAsset(from, to);
                    if (!string.IsNullOrEmpty(error)) throw new IOException(error);
                }
            }
            // Keep the existing modular art scene and all appearance data; remove only its attack control.
            EditorSceneManager.OpenScene("Assets/_Project/Content/Scenes/HeroModularTrial.unity");
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
                if (Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Any(i => button.onClick.GetPersistentMethodName(i) == "Slash"))
                    UnityEngine.Object.DestroyImmediate(button.gameObject);
            foreach (var label in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
                if (label.text.Contains("J: chém")) label.text = label.text.Replace("     J: chém", "");
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Map);
            if (prefab == null) throw new InvalidOperationException("Missing current editable meadow map.");
            var map = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<ModularMapLayout>();
            var camera = new GameObject("Camera xem mẫu").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 5.875f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = new Color(.36f, .52f, .22f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = false; camera.allowMSAA = false;
            var host = new GameObject("Art Preview").AddComponent<ArtPreviewHost>();
            host.map = map; host.viewCamera = camera;
            host.character = AssetDatabase.LoadAssetAtPath<DirectionalHeroDefinition>(Hero);
            if (host.character == null) throw new InvalidOperationException("Missing current painted character.");
            host.gameObject.AddComponent<ArtPreviewHud>();
            if (!EditorSceneManager.SaveScene(scene, Scene)) throw new IOException("Could not save art-preview scene.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            foreach (string path in new[] {
                "Assets/_Project/Content/Scenes/CultivationV1.unity", "Assets/_Project/Content/Scenes/PlayerCombatTrial.unity",
                "Assets/_Project/Content/Scenes/ThanDinhPrototype.unity", "Assets/_Project/Content/Combat/AdventureConfig.asset",
                "Assets/_Project/Content/Combat/CombatTrialConfig.asset", "Assets/_Project/Content/Combat/CrystalLootConfig.asset" })
                Retire(path);
            AssetDatabase.Refresh(); AssetDatabase.SaveAssets();
            return Scene;
        }

        private static void Retire(string path)
        {
            foreach (string file in new[] { path, path + ".meta" })
            {
                if (!File.Exists(file)) continue;
                string to = Path.Combine("Recovery/ArtOnly-20261012/retired-source", file);
                Directory.CreateDirectory(Path.GetDirectoryName(to));
                File.Move(file, to);
            }
        }

        [MenuItem("Demo/Mỹ thuật/Build Web xem mẫu")]
        public static void BuildMenu() => Build();

        public static string Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before building.");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            const string output = "WebBuildV1";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Scene }, target = BuildTarget.WebGL, locationPathName = Path.GetFullPath(output), options = BuildOptions.None });
            var result = report.summary;
            Directory.CreateDirectory("Verification/ArtOnly");
            File.WriteAllText("Verification/ArtOnly/build.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                result = result.result.ToString(), errors = result.totalErrors, warnings = result.totalWarnings,
                bytes = result.totalSize, seconds = result.totalTime.TotalSeconds,
                messages = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Error || m.type == LogType.Warning).Select(m => m.content).ToArray()
            }, Newtonsoft.Json.Formatting.Indented) + "\n");
            if (result.result != BuildResult.Succeeded) throw new InvalidOperationException("Art-preview WebGL build failed.");
            var files = Directory.GetFiles(output + "/Build");
            string html = File.ReadAllText("Web/art-preview.html"), version = Guid.NewGuid().ToString("N");
            foreach (var pair in new[] { new[] { "__LOADER__", ".loader.js" }, new[] { "__DATA__", ".data" }, new[] { "__FRAMEWORK__", ".framework.js" }, new[] { "__WASM__", ".wasm" } })
                html = html.Replace(pair[0], Path.GetFileName(files.Single(f => f.EndsWith(pair[1], StringComparison.Ordinal))) + "?v=" + version);
            File.WriteAllText(output + "/index.html", html);
            File.Copy("Assets/_Project/Resources/Fonts/OFL.txt", output + "/FONT-LICENSE.txt", true);
            return "Built art-only WebGL: " + result.totalSize + " bytes";
        }
    }
}
