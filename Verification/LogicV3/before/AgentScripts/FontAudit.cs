using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>Verifies the embedded dashboard font against source text and the Vietnamese alphabet.</summary>
public static class FontAudit
{
    public static string Run()
    {
        const string asset = "Assets/_Project/Resources/Fonts/NotoSans-Regular.ttf";
        AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(asset) as TrueTypeFontImporter;
        if (importer == null) throw new InvalidOperationException("Noto Sans font importer is missing.");
        if (!importer.includeFontData || importer.fontTextureCase != FontTextureCase.Dynamic)
        {
            importer.includeFontData = true;
            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.SaveAndReimport();
        }
        var font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        if (font == null) throw new InvalidOperationException("Resources cannot load the dashboard font.");
        string alphabet = "aàáạảãâầấậẩẫăằắặẳẵeèéẹẻẽêềếệểễiìíịỉĩoòóọỏõôồốộổỗơờớợởỡuùúụủũưừứựửữyỳýỵỷỹđ";
        string source = string.Concat(Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        string chars = new string((source + alphabet + alphabet.ToUpperInvariant() + "\u0300\u0301\u0303\u0309\u0323\u0302\u0306\u031b").Where(c => !char.IsControl(c)).Distinct().OrderBy(c => c).ToArray());
        var initializeResult = FontEngine.InitializeFontEngine();
        if (initializeResult != FontEngineError.Success) throw new InvalidOperationException("Cannot initialize font engine: " + initializeResult);
        var loadResult = FontEngine.LoadFontFace(Path.GetFullPath(asset), 16);
        if (loadResult != FontEngineError.Success) throw new InvalidOperationException("Cannot read embedded font face: " + loadResult);
        string missing;
        try
        {
            missing = new string(chars.Where(c => !FontEngine.TryGetGlyphIndex(c, out var glyph) || glyph == 0).ToArray());
        }
        finally { FontEngine.UnloadFontFace(); }
        var sizes = new[] { 10, 11, 13, 14, 16, 18, 22, 28 };
        int rasterized = 0;
        foreach (int size in sizes)
        foreach (var style in new[] { FontStyle.Normal, FontStyle.Bold })
        {
            font.RequestCharactersInTexture(chars, size, style);
            foreach (char c in chars)
            {
                if (!font.GetCharacterInfo(c, out var info, size, style)) throw new InvalidOperationException("Cannot rasterize " + c + " at " + size + " " + style);
                rasterized++;
            }
        }
        var tmp = Resources.Load<TMPro.TMP_FontAsset>("Fonts/NotoSans-TMP");
        if (tmp == null || tmp.sourceFontFile != font || tmp.material == null || tmp.atlasTextureCount < 1)
            throw new InvalidOperationException("HUD font resource, source, material or atlas is missing.");
        if (!tmp.HasCharacters(chars, out uint[] missingTmp, false, true))
            throw new InvalidOperationException("Missing HUD glyph codepoints: " + string.Join(",", missingTmp));
        var evidence = new Evidence
        {
            fontAsset = asset, fontName = font.name, coverageMethod = "FontEngine.TryGetGlyphIndex from embedded font file", resourceLoaded = true,
            includeFontData = importer.includeFontData, dynamic = font.dynamic,
            tmpResourceLoaded = true, tmpAtlasCount = tmp.atlasTextureCount, tmpCharacterCount = tmp.characterTable.Count,
            fallbackReferences = importer.fontReferences.Length, sourceCharacterCount = chars.Length,
            missingCharacters = missing, vietnameseAlphabetIncluded = true,
            normalAndBoldSizes = sizes, rasterizedSamples = rasterized,
            result = missing.Length == 0 ? "Passed" : "Failed"
        };
        string json = JsonUtility.ToJson(evidence, true);
        Directory.CreateDirectory("Verification/LogicV1");
        File.WriteAllText("Verification/LogicV1/font-check.json", json + "\n");
        if (missing.Length > 0) throw new InvalidOperationException("Missing font characters: " + missing);
        return json;
    }
    [Serializable] private sealed class Evidence
    {
        public string fontAsset, fontName, coverageMethod, missingCharacters, result;
        public bool tmpResourceLoaded;
        public int tmpAtlasCount, tmpCharacterCount;
        public bool resourceLoaded, includeFontData, dynamic, vietnameseAlphabetIncluded;
        public int fallbackReferences, sourceCharacterCount, rasterizedSamples;
        public int[] normalAndBoldSizes;
    }
}
