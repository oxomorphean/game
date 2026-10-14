using System;
using System.IO;
using System.Linq;
using LinhThu.Prototype;
using LinhThu.Prototype.Preview;
using LinhThu.Prototype.Characters;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainMapMethod2
{
    const string Terrain = "Assets/_Project/Art/CartoonV2/Terrain/";
    const string Map = "Assets/_Project/Content/Prefabs/Environment/MeadowMainMap.prefab";
    const string Scene = "Assets/_Project/Content/Scenes/ArtPreview.unity";
    const string Evidence = "ArtDirection/CartoonV2/MainMapMethod2/";

    public static object Create()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before map authoring.");
        ImportTexture("meadow-grass.png"); ImportTexture("meadow-earth.png");
        var stones = ImportStones();
        var grass = Material("MeadowGrass", false);
        var earth = Material("MeadowEarthPath", true);
        EditorSceneManager.OpenScene(Scene);
        var host = UnityEngine.Object.FindAnyObjectByType<ArtPreviewHost>();
        if (host == null) throw new InvalidOperationException("Main V1 host missing.");
        var data = new SerializedObject(host);
        var before = (ModularMapLayout)data.FindProperty("map").objectReferenceValue;
        if (before == null) throw new InvalidOperationException("Main map missing.");
        var originalProps = before.GetComponentsInChildren<ModularMapProp>();
        var originalObstacles = before.GetComponentsInChildren<ObstacleAuthoring>();
        var root = UnityEngine.Object.Instantiate(before.gameObject, before.transform.parent);
        root.name = "Đồng cỏ chính · terrain chỉnh sửa";
        root.transform.SetPositionAndRotation(before.transform.position,before.transform.rotation);
        root.transform.localScale = before.transform.localScale;
        if (PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var layout = root.GetComponent<ModularMapLayout>();
        foreach (var old in root.GetComponentsInChildren<MeadowGroundSurface>()) UnityEngine.Object.DestroyImmediate(old.gameObject);
        foreach (var old in root.GetComponentsInChildren<MeadowTerrainPath>()) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var oldStones = root.transform.Find("Đá lát riêng");
        if (oldStones != null) UnityEngine.Object.DestroyImmediate(oldStones.gameObject);
        layout.ground.enabled = false;
        layout.ground.name = "Nền PNG cũ · tham chiếu đã tắt";
        var floorNode = new GameObject("Nền cỏ · texture lặp"); floorNode.transform.SetParent(root.transform,false);
        var floor = floorNode.AddComponent<MeadowGroundSurface>(); floor.size = layout.size;
        floor.GetComponent<MeshRenderer>().sharedMaterial = grass; floor.Rebuild();
        var pathNode = new GameObject("Đường đất · kéo các điểm trong Scene"); pathNode.transform.SetParent(root.transform,false);
        var path = pathNode.AddComponent<MeadowTerrainPath>(); path.mapSize = layout.size;
        path.knots = new[]
        {
            new MeadowTerrainPath.Knot(-10.7f,-5.65f,.72f), new MeadowTerrainPath.Knot(-8.3f,-3.65f,.72f),
            new MeadowTerrainPath.Knot(-5.6f,-2.8f,.67f), new MeadowTerrainPath.Knot(-2.85f,-1.65f,.63f),
            new MeadowTerrainPath.Knot(-2.2f,-.6f,.59f), new MeadowTerrainPath.Knot(-.4f,.1f,.74f),
            new MeadowTerrainPath.Knot(1.35f,.85f,.58f), new MeadowTerrainPath.Knot(2.2f,2.0f,.61f),
            new MeadowTerrainPath.Knot(3.5f,3.0f,.67f), new MeadowTerrainPath.Knot(4.9f,3.85f,.65f),
            new MeadowTerrainPath.Knot(5.7f,5.25f,.54f), new MeadowTerrainPath.Knot(6.5f,6.3f,.58f)
        };
        path.GetComponent<MeshRenderer>().sharedMaterial = earth; path.Rebuild();
        var paving = new GameObject("Đá lát riêng"); paving.transform.SetParent(root.transform,false);
        var locations = new[] {
            new Vector3(-9.3f,-5.0f,1.55f), new Vector3(-8.48f,-4.08f,1.12f), new Vector3(-5.25f,-2.67f,.91f),
            new Vector3(-4.50f,-2.20f,1.18f), new Vector3(-2.92f,-1.61f,.71f), new Vector3(-2.56f,-1.20f,.55f),
            new Vector3(-.62f,0,1.26f), new Vector3(1.61f,1.29f,1.03f), new Vector3(2.19f,2.12f,.84f),
            new Vector3(3.40f,3.02f,1.03f), new Vector3(4.96f,4.13f,.81f), new Vector3(5.60f,4.40f,.52f),
            new Vector3(-7.45f,-4.1f,.35f), new Vector3(-4.0f,-2.5f,.3f), new Vector3(.2f,.25f,.38f),
            new Vector3(2.95f,2.6f,.36f), new Vector3(5.20f,4.70f,.32f)
        };
        var daylight = UnityEngine.Resources.Load<UnityEngine.Material>("Art/DaylightSprite");
        if (daylight == null) throw new InvalidOperationException("Existing daylight material missing.");
        for (int i=0;i<locations.Length;i++)
        {
            var spot = locations[i]; var node = new GameObject("Đá lát " + (i+1).ToString("00")); node.transform.SetParent(paving.transform,false);
            node.transform.localPosition = new Vector3(spot.x,spot.y,0);
            node.transform.localRotation = Quaternion.Euler(0,0,(i%3-1)*7);
            var view = node.AddComponent<SpriteRenderer>(); view.sprite = stones[i%stones.Length];
            view.sharedMaterial = daylight; view.sortingOrder = -52;
            node.transform.localScale = Vector3.one * (spot.z / view.sprite.bounds.size.x);
        }
        var props = layout.GetComponentsInChildren<ModularMapProp>();
        if (props.Length != originalProps.Length || layout.size != before.size) throw new InvalidOperationException("Map footprint or props changed.");
        for (int i=0;i<props.Length;i++)
            if (props[i].transform.localPosition != originalProps[i].transform.localPosition || props[i].transform.localScale != originalProps[i].transform.localScale)
                throw new InvalidOperationException("Existing prop placement changed.");
        var obstacles = layout.GetComponentsInChildren<ObstacleAuthoring>();
        if (obstacles.Length != originalObstacles.Length) throw new InvalidOperationException("Obstacle authoring changed.");
        for (int i=0;i<obstacles.Length;i++)
            if (EditorJsonUtility.ToJson(obstacles[i]) != EditorJsonUtility.ToJson(originalObstacles[i])) throw new InvalidOperationException("Obstacle data changed.");
        var newMesh = path.GetComponent<MeshFilter>().sharedMesh;
        var initial = newMesh.vertices;
        var knot = path.knots[5]; var changed = knot; changed.position += Vector2.up*.7f; path.knots[5] = changed; path.Rebuild();
        bool editable = !initial.SequenceEqual(newMesh.vertices);
        path.knots[5] = knot; path.Rebuild();
        if (!editable) throw new InvalidOperationException("Editing a path knot did not change the path mesh.");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root,Map,InteractionMode.AutomatedAction);
        data.FindProperty("map").objectReferenceValue = layout; data.ApplyModifiedPropertiesWithoutUndo();
        UnityEngine.Object.DestroyImmediate(before.gameObject);
        if (!EditorSceneManager.SaveScene(root.scene,Scene)) throw new IOException("Could not save main map scene.");
        AssetDatabase.SaveAssets();
        var result = new { mainMap = Map, size = new { x = layout.size.x, y = layout.size.y }, props = props.Length, obstacles = obstacles.Length, stones = locations.Length, pathKnots = path.knots.Length, pathTriangles = newMesh.triangles.Length/3, pathEditable = editable, oldGroundVisible = layout.ground.enabled, colliderDataPreserved = true };
        Write("authoring.json",result); Selection.activeGameObject = pathNode;
        return result;
    }

    static void ImportTexture(string name)
    {
        string file = Terrain+name;
        AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(file);
        importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
        importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Mirror;
        importer.filterMode = FilterMode.Trilinear; importer.mipmapEnabled = true; importer.isReadable = false;
        importer.SaveAndReimport();
    }

    static Sprite[] ImportStones()
    {
        string file = Terrain+"stepping-stones.png";
        AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(file);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 200; importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
        importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear; importer.mipmapEnabled = false; importer.isReadable = false;
        importer.SaveAndReimport();
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var old = provider.GetSpriteRects();
        var rects = Enumerable.Range(0,6).Select(i => new SpriteRect {
            name = "SteppingStone"+(i+1), rect = new Rect((i%3)*512,(1-i/3)*512,512,512),
            alignment = SpriteAlignment.Center, pivot = new Vector2(.5f,.5f),
            spriteID = old.FirstOrDefault(r=>r.name=="SteppingStone"+(i+1))?.spriteID ?? GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(file).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        if (sprites.Length != 6) throw new InvalidOperationException("Stone sheet needs six independently placeable sprites.");
        return sprites;
    }

    static UnityEngine.Material Material(string name, bool path)
    {
        string file = Terrain+name+".mat";
        var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(file);
        var shader = Shader.Find("LinhThu/Meadow Terrain");
        if (shader == null) throw new InvalidOperationException("Meadow terrain shader missing.");
        if (material == null) { material = new UnityEngine.Material(shader); AssetDatabase.CreateAsset(material,file); }
        material.SetTexture("_GrassTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Terrain+"meadow-grass.png"));
        material.SetTexture("_EarthTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Terrain+"meadow-earth.png"));
        material.SetFloat("_Path",path?1:0); EditorUtility.SetDirty(material);
        return material;
    }

    public static object Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before building.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] {Scene}, target = BuildTarget.WebGL, locationPathName = Path.GetFullPath("WebBuildV1"), options = BuildOptions.None });
        var s = report.summary;
        var result = new { result = s.result.ToString(), errors = s.totalErrors, warnings = s.totalWarnings, seconds = s.totalTime.TotalSeconds, bytes = s.totalSize,
            messages = report.steps.SelectMany(step=>step.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray() };
        Write("build.json",result);
        if (s.result != BuildResult.Succeeded) throw new InvalidOperationException("Main map WebGL build failed.");
        string html = File.ReadAllText("Web/art-preview.html"), version = Guid.NewGuid().ToString("N");
        var files = Directory.GetFiles("WebBuildV1/Build");
        foreach (var pair in new[] { new[] {"__LOADER__",".loader.js"},new[] {"__DATA__",".data"},new[] {"__FRAMEWORK__",".framework.js"},new[] {"__WASM__",".wasm"} })
            html = html.Replace(pair[0],Path.GetFileName(files.Single(p=>p.EndsWith(pair[1],StringComparison.Ordinal)))+"?v="+version);
        File.WriteAllText("WebBuildV1/index.html",html);
        File.Copy("Assets/_Project/Resources/Fonts/OFL.txt","WebBuildV1/FONT-LICENSE.txt",true);
        return result;
    }

    public static object VerifySavedMap()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before checking saved content.");
        EditorSceneManager.OpenScene(Scene);
        var host = UnityEngine.Object.FindAnyObjectByType<ArtPreviewHost>();
        var layout = (ModularMapLayout)new SerializedObject(host).FindProperty("map").objectReferenceValue;
        var path = layout.GetComponentInChildren<MeadowTerrainPath>();
        var floor = layout.GetComponentInChildren<MeadowGroundSurface>();
        if (path == null || floor == null || layout.ground.enabled || path.GetComponent<MeshFilter>().sharedMesh.vertexCount == 0 || floor.GetComponent<MeshFilter>().sharedMesh.vertexCount != 4)
            throw new InvalidOperationException("Saved scene did not restore its editable terrain.");
        foreach (var renderer in new[] {path.GetComponent<MeshRenderer>(),floor.GetComponent<MeshRenderer>()})
            if (ShaderUtil.GetShaderMessages(renderer.sharedMaterial.shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
                throw new InvalidOperationException("Terrain shader has compiler errors.");
        if (!File.ReadAllBytes("Recovery/MainMapMethod2-20261008/MeadowMap.prefab").SequenceEqual(File.ReadAllBytes("Assets/_Project/Content/Prefabs/Environment/MeadowMap.prefab")))
            throw new InvalidOperationException("Original shared map was modified.");
        Selection.activeGameObject = path.gameObject;
        var result = new { savedSceneRestored = true, originalSharedMapUnchanged = true, terrainShaderErrors = 0, props = layout.GetComponentsInChildren<ModularMapProp>().Length, obstacles = layout.GetComponentsInChildren<ObstacleAuthoring>().Length };
        Write("saved-map-check.json",result);
        return result;
    }

    static void Write(string name, object value)
    {
        Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence+name,Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented)+"\n");
    }
}
