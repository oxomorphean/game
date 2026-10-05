using System;
using System.IO;
using System.Linq;
using LinhThu.ArtSample;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

/// <summary>Explicitly invoked via the Unity plugin; imports and builds the small art sample.</summary>
public static class ArtSampleBuilder
{
    private const string Root = "Assets/ArtSample";
    public const string ScenePath = Root + "/SpiritTurtleGarden.unity";

    public static string CreateScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (GraphicsSettings.currentRenderPipeline != null)
            throw new InvalidOperationException("This sample uses the existing Built-in renderer.");

        Directory.CreateDirectory(Root + "/Sprites");
        Directory.CreateDirectory(Root + "/Tiles");
        ImportSprite(Root + "/Sprites/turtle.png", new Vector2(0.5f, 12f / 64f));
        ImportGenerated("gate", 128, 128, 0.5f);
        ImportGenerated("tree", 96, 128, 0.5f);
        ImportGenerated("tree-puffy", 112, 128, 0.5f, "tree-puffy-v3");
        ImportGenerated("pond", 96, 64, 0.5f, "pond-v3", true);
        ImportGenerated("rock", 32, 32, 0.5f);
        Sprite[] idleFrames = ImportTurtleFrames("idle", 4);
        Sprite[] runFrames = ImportTurtleFrames("walk", 6);
        CreateFloorSprite("grass", false);
        CreateFloorSprite("stone", true);
        CreateFloorSprite("stone-moss", true, 1);
        CreateFloorSprite("stone-grass", true, 2);
        CreateShadow();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        QualitySettings.antiAliasing = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.vSyncCount = 0;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 8;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("526B65");
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 20;
        var pixels = cameraObject.AddComponent<PixelPerfectCamera>();
        pixels.assetsPPU = 16;
        pixels.refResolutionX = 384;
        pixels.refResolutionY = 256;
        pixels.upscaleRT = true;
        pixels.cropFrameX = true;
        pixels.cropFrameY = true;
        pixels.stretchFill = false;

        var gridObject = new GameObject("Small sample floor");
        gridObject.AddComponent<Grid>();
        var floorObject = new GameObject("Editable grass and stone tiles");
        floorObject.transform.SetParent(gridObject.transform, false);
        var floor = floorObject.AddComponent<Tilemap>();
        floor.tileAnchor = Vector3.zero;
        floorObject.AddComponent<TilemapRenderer>().sortingOrder = -100;
        Tile grass = LoadTile("grass");
        Tile stone = LoadTile("stone");
        Tile moss = LoadTile("stone-moss");
        Tile grassStone = LoadTile("stone-grass");
        for (int y = -7; y < 7; y++)
            for (int x = -11; x < 11; x++)
            {
                bool paving = x >= -5 && x < 5 && y >= -6 && y < 3;
                int variation = Math.Abs(x * 13 + y * 7) % 11;
                Tile pavingTile = variation < 2 ? moss : variation < 4 ? grassStone : stone;
                floor.SetTile(new Vector3Int(x, y, 0), paving ? pavingTile : grass);
            }
        var props = new GameObject("Gate – rock – flowering tree sample");
        PlaceSprite("Sect gate", "gate", new Vector2(0, 1.25f), props.transform);
        var oldTree = PlaceSprite("Pink flowering tree", "tree", new Vector2(-7, 0.75f), props.transform);
        var puffyTree = PlaceSprite("Puffy cherry blossom tree", "tree-puffy", new Vector2(7, 1.25f), props.transform);
        PlaceTreeShadow("tree", oldTree, props.transform);
        PlaceTreeShadow("tree-puffy", puffyTree, props.transform);
        var pond = PlaceSprite("Jade courtyard pond", "pond", new Vector2(7.5f, -4.5f), props.transform);
        pond.sortingOrder = -90;
        PlaceSprite("Moss rock", "rock", new Vector2(6.5f, 0), props.transform);
        PlaceSprite("Moss rock small", "rock", new Vector2(-6, -2.5f), props.transform);

        var turtle = new GameObject("Pet");
        turtle.transform.position = new Vector3(0, -3, 0);
        var shadow = PlaceSprite("Contact shadow", "shadow", Vector2.zero, turtle.transform);
        var visual = PlaceSprite("Huyen Giap Quy", "turtle", Vector2.zero, turtle.transform);
        var controller = turtle.AddComponent<SpiritTurtleController>();
        var settings = new SerializedObject(controller);
        settings.FindProperty("visual").objectReferenceValue = visual;
        settings.FindProperty("shadow").objectReferenceValue = shadow;
        settings.FindProperty("viewCamera").objectReferenceValue = camera;
        var idleProperty = settings.FindProperty("idleFrames");
        idleProperty.arraySize = idleFrames.Length;
        for (int i = 0; i < idleFrames.Length; i++) idleProperty.GetArrayElementAtIndex(i).objectReferenceValue = idleFrames[i];
        var framesProperty = settings.FindProperty("runFrames");
        framesProperty.arraySize = runFrames.Length;
        for (int i = 0; i < runFrames.Length; i++) framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = runFrames[i];
        settings.ApplyModifiedPropertiesWithoutUndo();
        visual.sortingOrder = 1048;
        shadow.sortingOrder = 1047;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = turtle;
        var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        view.maximized = true;
        view.Focus();
        Debug.Log("[ArtSample] Native sprites at PPU 16, separate props and editable floor saved.");
        return ScenePath;
    }

    private static void ImportGenerated(string name, int width, int height, float pivotX, string sourceName = null, bool centerPivot = false)
    {
        // Unity's source-to-runtime asset normalization keeps originals outside Assets.
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!source.LoadImage(File.ReadAllBytes("ArtDirection/Sources/" + (sourceName ?? name + "-v2") + ".png")))
            throw new InvalidOperationException("Cannot decode source: " + name);
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var output = new Color32[width * height];
        var input = source.GetPixels32();
        float ratio = Mathf.Min((float)width / source.width, (float)height / source.height);
        int fittedWidth = Mathf.RoundToInt(source.width * ratio);
        int fittedHeight = Mathf.RoundToInt(source.height * ratio);
        int left = (width - fittedWidth) / 2;
        int bottom = (height - fittedHeight) / 2;
        for (int y = 0; y < fittedHeight; y++)
            for (int x = 0; x < fittedWidth; x++)
            {
                int sx = Mathf.Min(source.width - 1, Mathf.FloorToInt((x + 0.5f) / ratio));
                int sy = Mathf.Min(source.height - 1, Mathf.FloorToInt((y + 0.5f) / ratio));
                output[(y + bottom) * width + x + left] = input[sy * source.width + sx];
            }
        texture.SetPixels32(output);
        texture.Apply();
        string path = Root + "/Sprites/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        int foot = height;
        for (int y = 0; y < height; y++)
            if (Enumerable.Range(0, width).Any(x => output[y * width + x].a > 128)) { foot = y; break; }
        if (foot == height) throw new InvalidOperationException("Empty sprite: " + name);
        UnityEngine.Object.DestroyImmediate(source);
        UnityEngine.Object.DestroyImmediate(texture);
        ImportSprite(path, new Vector2(Mathf.Round(pivotX * width) / width, centerPivot ? 0.5f : (float)foot / height));
    }

    private static Sprite[] ImportTurtleFrames(string animation, int count)
    {
        string path = Root + "/Sprites/turtle-" + animation + ".png";
        var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!image.LoadImage(File.ReadAllBytes(path)) || image.width != count * 64 || image.height != 64)
            throw new InvalidOperationException("Expected " + count + " turtle frames of 64x64: " + path);
        UnityEngine.Object.DestroyImmediate(image);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 512;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) throw new InvalidOperationException("No turtle-sheet sprite provider.");
        provider.InitSpriteEditorDataProvider();
        var edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
        if (edit == null) throw new InvalidOperationException("No turtle-sheet edit capability.");
        foreach (var capability in new[] { EEditCapability.CreateAndDeleteSprite, EEditCapability.EditSpriteRect,
            EEditCapability.EditPivot, EEditCapability.EditSpriteName })
            if (!edit.GetEditCapability().HasCapability(capability))
                throw new InvalidOperationException("Turtle-sheet capability unsupported: " + capability);
        var existing = provider.GetSpriteRects();
        var rects = new SpriteRect[count];
        for (int i = 0; i < count; i++)
        {
            string name = "turtle-" + animation + "-" + i;
            var previous = existing.FirstOrDefault(rect => rect.name == name);
            rects[i] = new SpriteRect { name = name, spriteID = previous == null ? GUID.Generate() : previous.spriteID,
                rect = new Rect(i * 64, 0, 64, 64), alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 12f / 64f) };
        }
        var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (ids == null) throw new InvalidOperationException("Turtle-sheet name/file ID provider unavailable.");
        provider.SetSpriteRects(rects);
        ids.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
        var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(frame => frame.name).ToArray();
        if (frames.Length != count) throw new InvalidOperationException("Incorrect imported turtle frame count.");
        return frames;
    }

    private static void PlaceTreeShadow(string name, SpriteRenderer tree, Transform parent)
    {
        Directory.CreateDirectory(Root + "/Shadows");
        string meshPath = Root + "/Shadows/" + name + ".asset";
        string materialPath = Root + "/Shadows/" + name + ".mat";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, meshPath); }
        mesh.Clear();
        var sprite = tree.sprite;
        // Project the real tree silhouette onto the ground, toward the lower right.
        mesh.vertices = sprite.vertices.Select(point => new Vector3(point.x + point.y * 0.42f, -point.y * 0.27f, 0)).ToArray();
        mesh.uv = sprite.uv;
        mesh.triangles = sprite.triangles.Select(index => (int)index).ToArray();
        mesh.colors = Enumerable.Repeat(Color.white, mesh.vertexCount).ToArray();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Sprite shadow shader missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.mainTexture = sprite.texture;
        material.color = new Color(0.255f, 0.251f, 0.365f, 0.3f);
        EditorUtility.SetDirty(material);
        var shadow = new GameObject(tree.name + " cast shadow");
        shadow.transform.SetParent(parent, false);
        shadow.transform.localPosition = tree.transform.localPosition + new Vector3(0.125f, -0.0625f, 0);
        shadow.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = shadow.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = -80;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void ImportSprite(string path, Vector2 pivot)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("No texture importer: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 256;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.isReadable = false;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) throw new InvalidOperationException("No sprite data provider: " + path);
        provider.InitSpriteEditorDataProvider();
        var edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
        if (edit == null || !edit.GetEditCapability().HasCapability(EEditCapability.EditPivot))
            throw new InvalidOperationException("Sprite pivot editing is unsupported: " + path);
        var rects = provider.GetSpriteRects();
        if (rects.Length != 1) throw new InvalidOperationException("Expected one sprite: " + path);
        rects[0].alignment = SpriteAlignment.Custom;
        rects[0].pivot = pivot;
        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static void CreateFloorSprite(string name, bool stone, int variation = 0)
    {
        var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color baseColor = stone ? Hex("E8DDB6") : Hex("83B865");
        Color low = stone ? Hex("B1AD87") : Hex("7DB160");
        Color high = stone ? Hex("F0E6C4") : Hex("8BBF6C");
        var pixels = Enumerable.Repeat(baseColor, 256).ToArray();
        if (stone)
        {
            for (int x = 0; x < 16; x++) pixels[x] = low;
            for (int y = 0; y < 16; y++) { pixels[y * 16] = low; pixels[y * 16 + 15] = high; }
            pixels[17] = Hex("83B865");
            pixels[18] = Hex("83B865");
            if (variation > 0)
            {
                Color green = Hex("83B865");
                Color darkGreen = Hex("4C8063");
                Color lightGreen = Hex("A0C877");
                // Cluster moss into a corner; retain broad clear stone faces for walking.
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 7 - y; x++) pixels[y * 16 + x] = y < 2 ? darkGreen : green;
                if (variation == 2)
                {
                    for (int y = 1; y < 7; y++) pixels[y * 16 + 13] = darkGreen;
                    for (int y = 2; y < 6; y++) pixels[y * 16 + 12] = green;
                    pixels[5 * 16 + 11] = lightGreen;
                    pixels[6 * 16 + 13] = lightGreen;
                    pixels[3 * 16 + 14] = green;
                    pixels[4 * 16 + 15] = lightGreen;
                }
            }
        }
        else
        {
            pixels[52] = low; pixels[53] = low; pixels[173] = high; pixels[174] = high;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        string path = Root + "/Sprites/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        ImportSprite(path, Vector2.zero);
    }

    private static void CreateShadow()
    {
        var texture = new Texture2D(32, 16, TextureFormat.RGBA32, false);
        var pixels = new Color32[512];
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 32; x++)
                if (Mathf.Pow((x - 15.5f) / 14, 2) + Mathf.Pow((y - 7.5f) / 3.5f, 2) < 1)
                    pixels[y * 32 + x] = new Color32(65, 64, 93, 70);
        texture.SetPixels32(pixels);
        texture.Apply();
        string path = Root + "/Sprites/shadow.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        ImportSprite(path, new Vector2(0.5f, 0.5f));
    }

    private static Tile LoadTile(string name)
    {
        string path = Root + "/Tiles/" + name + ".asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Sprites/" + name + ".png");
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static SpriteRenderer PlaceSprite(string label, string name, Vector2 position, Transform parent)
    {
        var item = new GameObject(label);
        item.transform.SetParent(parent, false);
        item.transform.localPosition = new Vector3(position.x, position.y, 0);
        var renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Sprites/" + name + ".png");
        if (renderer.sprite == null) throw new InvalidOperationException("Missing sprite: " + name);
        renderer.sortingOrder = 1000 - Mathf.RoundToInt(position.y * 16);
        return renderer;
    }

    private static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var color)
        ? color : throw new InvalidOperationException("Invalid color: " + hex);

    public static void BuildWeb()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
        string output = Path.GetFullPath("WebBuild");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.WebGL, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Web build failed: " + report.summary.result);
        var files = Directory.GetFiles(Path.Combine(output, "Build"));
        string html = File.ReadAllText("Web/index.html");
        html = html.Replace("__LOADER__", FindFile(files, ".loader.js"))
            .Replace("__DATA__", FindFile(files, ".data"))
            .Replace("__FRAMEWORK__", FindFile(files, ".framework.js"))
            .Replace("__WASM__", FindFile(files, ".wasm"));
        File.WriteAllText(Path.Combine(output, "index.html"), html);
        File.WriteAllText("ArtDirection/ArtSample/build-result-v6.json", JsonUtility.ToJson(new Evidence {
            result = report.summary.result.ToString(), bytes = report.summary.totalSize,
            seconds = report.summary.totalTime.TotalSeconds, errors = report.summary.totalErrors }, true));
        Debug.Log("[ArtSample] Web build ready: " + output);
    }

    private static string FindFile(string[] files, string suffix) => Path.GetFileName(files.Single(p => p.EndsWith(suffix, StringComparison.Ordinal)));
    [Serializable] private class Evidence { public string result; public ulong bytes; public double seconds; public int errors; }
}
