using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class HudResources
{
    public static string Create()
    {
        const string path = "Assets/_Project/Resources/Fonts/NotoSans-TMP.asset";
        var font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        if (font == null || TMP_Settings.instance == null) throw new InvalidOperationException("Font or TMP essentials missing.");
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset == null)
        {
            asset = TMP_FontAsset.CreateFontAsset(font, 40, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = "NotoSans-TMP";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var tex in asset.atlasTextures) AssetDatabase.AddObjectToAsset(tex, asset);
        }
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        string alphabet = "aàáạảãâầấậẩẫăằắặẳẵeèéẹẻẽêềếệểễiìíịỉĩoòóọỏõôồốộổỗơờớợởỡuùúụủũưừứựửữyỳýỵỷỹđ";
        string source = string.Concat(Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        string chars = new string((source + alphabet + alphabet.ToUpperInvariant()).Where(c => !char.IsControl(c)).Distinct().ToArray());
        if (!asset.TryAddCharacters(chars, out string missing)) throw new InvalidOperationException("Missing TMP glyphs: " + missing);
        foreach (var tex in asset.atlasTextures) if (!AssetDatabase.Contains(tex)) AssetDatabase.AddObjectToAsset(tex, asset);
        TMP_Settings.defaultFontAsset = asset;
        EditorUtility.SetDirty(TMP_Settings.instance);
        EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
        return "TMP font ready; " + chars.Length + " characters; " + asset.atlasTextureCount + " atlas";
    }
}
