using System;
using System.IO;
using System.Linq;
using LinhThu.Prototype;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class LogicBuilder
{
    private const string Scene = "Assets/_Project/Content/Scenes/ThanDinhPrototype.unity";
    public static string CreateScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        Directory.CreateDirectory("Assets/_Project/Content/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Prototype Camera").AddComponent<Camera>();
        camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -10);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.92f, .92f, .87f);
        camera.allowHDR = false; camera.allowMSAA = false;
        var host = new GameObject("ThanDinh Local Prototype").AddComponent<PrototypeHost>();
        ConfigureArt(host.GetComponent<GameplayHud>());
        EditorSceneManager.SaveScene(scene, Scene); AssetDatabase.SaveAssets();
        Selection.activeGameObject = GameObject.Find("ThanDinh Local Prototype");
        var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        view.maximized = true; view.Focus();
        return Scene;
    }
    public static string ApplyArt()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        var hud = UnityEngine.Object.FindAnyObjectByType<GameplayHud>();
        if (hud == null) throw new InvalidOperationException("Open ThanDinhPrototype scene first.");
        ConfigureArt(hud);
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        EditorSceneManager.SaveScene(hud.gameObject.scene);
        AssetDatabase.SaveAssets();
        return "Garden asset references saved on gameplay HUD; existing art scene preserved.";
    }
    private static void ConfigureArt(GameplayHud hud)
    {
        const string root = "Assets/ArtSample/Sprites/";
        var settings = new SerializedObject(hud);
        foreach (var pair in new[] { new[] { "grass", "grass-clean" }, new[] { "grassShade", "grass-shade-b" },
            new[] { "gate", "gate" }, new[] { "tree", "tree" }, new[] { "puffyTree", "tree-puffy" },
            new[] { "rock", "rock" }, new[] { "pond", "pond" }, new[] { "flower", "flower-cluster" },
            new[] { "lantern", "lantern" }, new[] { "shadow", "shadow" }, new[] { "petal", "petal" } })
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + pair[1] + ".png");
            if (sprite == null) throw new InvalidOperationException("Missing existing sprite: " + pair[1]);
            settings.FindProperty("art." + pair[0]).objectReferenceValue = sprite;
        }
        const string fresh = "Assets/_Project/Art/GardenV8/";
        foreach (var pair in new[] { new[] { "bamboo", "bamboo" }, new[] { "herb", "spirit-herb" },
            new[] { "ore", "jade-ore" }, new[] { "egg", "spirit-egg" } })
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fresh + pair[1] + ".png");
            if (sprite == null) throw new InvalidOperationException("Import GardenV8 sprites first: " + pair[1]);
            settings.FindProperty("art." + pair[0]).objectReferenceValue = sprite;
        }
        var tiles = settings.FindProperty("art.paving"); tiles.arraySize = 3;
        string[] names = { "paving-slab", "paving-split", "paving-petal" };
        for (int i = 0; i < names.Length; i++) tiles.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(fresh + names[i] + ".png");
        var meadow = settings.FindProperty("art.meadow"); meadow.arraySize = 2;
        meadow.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(fresh + "grass-meadow-a.png");
        meadow.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(fresh + "grass-meadow-b.png");
        foreach (var pair in new[] { new[] { "playerFrames", fresh + "player-cultivator-sheet.png", "30" }, new[] { "rippleFrames", root + "water-ripple.png", "4" } })
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(pair[1]).OfType<Sprite>().OrderBy(x => x.name).ToArray();
            if (sprites.Length != int.Parse(pair[2])) throw new InvalidOperationException("Incorrect animation frame count: " + pair[1]);
            var frames = settings.FindProperty("art." + pair[0]); frames.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
        foreach (string name in new[] { "idle", "walk" })
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(root + "turtle-" + name + ".png").OfType<Sprite>().OrderBy(x => x.name).ToArray();
            if (sprites.Length != (name == "idle" ? 4 : 6)) throw new InvalidOperationException("Missing turtle animation: " + name);
            var frames = settings.FindProperty("art." + name); frames.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
    public static string BuildWeb()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building.");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false; PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
        Directory.CreateDirectory("Assets/_Project/Resources/Project");
        File.Copy("Verification/LogicV1/domain-tests.json", "Assets/_Project/Resources/Project/logic-tests.json", true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string path = Path.GetFullPath("WebBuildLogic");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene }, target = BuildTarget.WebGL, locationPathName = path, options = BuildOptions.None });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
        var files = Directory.GetFiles(Path.Combine(path, "Build"));
        string html = File.ReadAllText("Web/logic.html");
        string version = Guid.NewGuid().ToString("N");
        foreach (var pair in new[] { new[] { "__LOADER__", ".loader.js" }, new[] { "__DATA__", ".data" }, new[] { "__FRAMEWORK__", ".framework.js" }, new[] { "__WASM__", ".wasm" } })
            html = html.Replace(pair[0], Path.GetFileName(files.Single(f => f.EndsWith(pair[1], StringComparison.Ordinal))) + "?v=" + version);
        File.WriteAllText(Path.Combine(path, "index.html"), html);
        File.Copy("Assets/_Project/Resources/Fonts/OFL.txt", Path.Combine(path, "FONT-LICENSE.txt"), true);
        Directory.CreateDirectory("Verification/LogicV1");
        string json = JsonUtility.ToJson(new BuildEvidence { result = report.summary.result.ToString(), seconds = report.summary.totalTime.TotalSeconds, bytes = report.summary.totalSize, errors = report.summary.totalErrors }, true);
        File.WriteAllText("Verification/LogicV1/build.json", json + "\n");
        return json;
    }
    [Serializable] private sealed class BuildEvidence { public string result; public double seconds; public ulong bytes; public int errors; }
}
