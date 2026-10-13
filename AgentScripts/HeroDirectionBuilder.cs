using System;
using System.IO;
using System.Linq;
using LinhThu.Prototype.Characters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Newtonsoft.Json.Linq;

/// <summary>Imports authored hero sprites; art refresh keeps existing scene bindings intact.</summary>
public static class HeroDirectionBuilder
{
    const string Definition = "Assets/_Project/Content/Characters/Hero8Directions.asset";

    public static string Run() => Import("Assets/_Project/Art/Hero8Directions/", true);

    public static string Painted() => Import("Assets/_Project/Art/HeroPainted2D/", false);

    public static string Modular() => LinhThu.Editor.ModularHeroBuilder.Create();
    public static string OpenModular() => LinhThu.Editor.ModularHeroBuilder.Open();
    public static string BuildModular() => LinhThu.Editor.ModularHeroBuilder.Build();

    static string Import(string art, bool wireScenes)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before importing hero.");
        var manifest = JObject.Parse(File.ReadAllText(art + "manifest.json"));
        int size = (int)manifest["frameSize"];
        var pivot = new Vector2((float)manifest["pivot"][0], (float)manifest["pivot"][1]);
        var clips = manifest["clips"].Select(c => {
            string id = (string)c["id"], file = art + id + ".png";
            int count = (int)c["frames"];
            AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(file);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = (float)manifest["pixelsPerUnit"]; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(size * Mathf.Max(count, 8)));
            importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear; importer.mipmapEnabled = true; importer.isReadable = false;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects();
            var rects = Enumerable.Range(0,8*count).Select(i => {
                string name = id + "_" + (i/count).ToString("D2") + "_" + (i%count).ToString("D2");
                return new SpriteRect { name = name, rect = new Rect(i%count*size,(7-i/count)*size,size,size),
                    alignment = SpriteAlignment.Custom, pivot = pivot,
                    spriteID = old.FirstOrDefault(r => r.name == name)?.spriteID ?? GUID.Generate() };
            }).ToArray();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            return new DirectionalHeroClip { motion = (HeroMotion)Enum.Parse(typeof(HeroMotion),id,true),
                framesPerDirection = count, framesPerSecond = (float)c["fps"], loop = (bool)c["loop"], impactPhase = (float)c["impactPhase"],
                frames = AssetDatabase.LoadAllAssetsAtPath(file).OfType<Sprite>().OrderBy(s => s.name,StringComparer.Ordinal).ToArray() };
        }).ToArray();
        var definition = AssetDatabase.LoadAssetAtPath<DirectionalHeroDefinition>(Definition);
        if (definition == null) { definition = ScriptableObject.CreateInstance<DirectionalHeroDefinition>(); AssetDatabase.CreateAsset(definition,Definition); }
        definition.clips = clips; definition.scale = 1; definition.Validate(); EditorUtility.SetDirty(definition); AssetDatabase.SaveAssets();
        return "Imported " + clips.Sum(c=>c.frames.Length) + " hero frames from " + art + "; existing art bindings preserved.";
    }
}
