using System;
using System.Linq;
using LinhThu.Editor;
using LinhThu.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Applies the owner's fixed-path/grouped-pond decision to the newly created sample.</summary>
public static class GroupPondAndPath
{
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing the map.");
        ModularMapBuilder.PrepareAssets();
        var root = PrefabUtility.LoadPrefabContents(ModularMapBuilder.MapPath);
        try
        {
            var layout = root.GetComponent<ModularMapLayout>();
            var pond = layout.GetComponentsInChildren<ModularMapProp>().FirstOrDefault(p => p.visual.sprite.name == "pond");
            var position = pond == null ? new Vector3(7.25f, -1.35f, 0) : pond.transform.localPosition;
            foreach (var prop in layout.GetComponentsInChildren<ModularMapProp>())
                if (prop.floorLayer || prop.name.StartsWith("Đá bờ hồ", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(prop.gameObject);
            var floor = root.transform.Find("Mặt đất · hồ · đá lát") ?? root.transform.Find("Hồ nước trọn cụm");
            if (floor == null) throw new InvalidOperationException("Missing pond parent.");
            floor.name = "Hồ nước trọn cụm";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModularMapBuilder.PrefabRoot + "pond-cluster.prefab");
            if (source == null) throw new InvalidOperationException("Missing imported pond-cluster prefab.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, floor);
            instance.name = "Hồ nước trọn cụm"; instance.transform.localPosition = position;
            layout.ground.name = "Nền cỏ + đường đất cố định";
            layout.ground.sprite = ModularMapBuilder.Load("ground-road");
            layout.ground.transform.localScale = Vector3.one * (20f / layout.ground.sprite.bounds.size.x);
            layout.size = layout.ground.bounds.size;
            PrefabUtility.SaveAsPrefabAsset(root, ModularMapBuilder.MapPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        // These intermediate assets were created in this task and are absent from the final layout.
        foreach (string name in new[] { "meadow-base", "pond", "earth-patch", "stepping-stone" })
        {
            AssetDatabase.DeleteAsset(ModularMapBuilder.PrefabRoot + name + ".prefab");
            AssetDatabase.DeleteAsset(ModularMapBuilder.ArtRoot + name + ".png");
        }
        AssetDatabase.SaveAssets();
        CartoonArtBuilder.Prepare();
        return "Fixed irregular trail baked into ground; pond and shore are one PNG/prefab; other placements retained.";
    }
}
