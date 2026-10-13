using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Imports original GardenV8 PNGs through Unity's sprite APIs; no scene or texture YAML edits.</summary>
public static class GardenV8Builder
{
    private const string Root = "Assets/_Project/Art/GardenV8/";
    public static string Import()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before importing art.");
        foreach (string name in new[] { "paving-slab", "paving-split", "paving-petal", "grass-meadow-a", "grass-meadow-b" })
            Single(name, new Vector2(.5f, .5f));
        Single("bamboo", new Vector2(.5f, 4f / 80));
        Single("spirit-herb", new Vector2(.5f, 3f / 32));
        Single("jade-ore", new Vector2(.5f, 4f / 32));
        Single("spirit-egg", new Vector2(.5f, 2f / 32));
        var importer = Configure("player-cultivator-sheet", SpriteImportMode.Multiple);
        var provider = Provider(importer);
        var previous = provider.GetSpriteRects();
        var rects = new SpriteRect[30];
        for (int i = 0; i < rects.Length; i++)
        {
            string name = "cultivator-" + i.ToString("D2");
            var old = previous.FirstOrDefault(x => x.name == name);
            rects[i] = new SpriteRect { name = name, spriteID = old == null ? GUID.Generate() : old.spriteID,
                rect = new Rect(i * 32, 0, 32, 48), alignment = SpriteAlignment.Custom, pivot = new Vector2(.5f, 4f / 48) };
        }
        provider.SetSpriteRects(rects);
        var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (ids == null) throw new InvalidOperationException("Cultivator sheet needs sprite name/file ID provider.");
        ids.SetNameFileIdPairs(rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        if (AssetDatabase.LoadAllAssetsAtPath(Root + "player-cultivator-sheet.png").OfType<Sprite>().Count() != 30)
            throw new InvalidOperationException("Cultivator import frame count must be 30.");
        AssetDatabase.SaveAssets();
        return "Imported 10 original assets; cultivator has 30 frames, all Point/PPU16/no mipmaps/no compression.";
    }
    private static TextureImporter Configure(string name, SpriteImportMode mode)
    {
        string path = Root + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("No texture importer: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = mode;
        importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048; importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true; importer.isReadable = false; importer.SaveAndReimport();
        return importer;
    }
    private static ISpriteEditorDataProvider Provider(TextureImporter importer)
    {
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) throw new InvalidOperationException("No sprite editor data provider.");
        provider.InitSpriteEditorDataProvider(); return provider;
    }
    private static void Single(string name, Vector2 pivot)
    {
        var importer = Configure(name, SpriteImportMode.Single); var provider = Provider(importer);
        var rects = provider.GetSpriteRects();
        if (rects.Length != 1) throw new InvalidOperationException("Expected one sprite: " + name);
        rects[0].alignment = SpriteAlignment.Custom; rects[0].pivot = pivot;
        provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
    }
}
