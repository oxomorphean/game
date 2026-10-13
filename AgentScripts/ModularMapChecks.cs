using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinhThu.Editor;
using LinhThu.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Artifact checks for independent placement, persistence, and shared prefab presentation.</summary>
public static class ModularMapChecks
{
    private const string Trial = "Assets/_Project/Content/Scenes/CartoonArtTrial.unity";
    [Serializable] private sealed class Check { public string name; public bool passed; }
    [Serializable] private sealed class Evidence { public int passed, failed, placedProps; public Check[] checks; }

    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before artifact checks.");
        EditorSceneManager.OpenScene(Trial);
        var checks = new List<Check>();
        Action<string, bool> check = (name, passed) => checks.Add(new Check { name = name, passed = passed });
        var layout = UnityEngine.Object.FindAnyObjectByType<ModularMapLayout>();
        var props = layout.GetComponentsInChildren<ModularMapProp>();
        check("Fixed irregular path belongs to terrain sprite", layout.ground.sprite.name == "ground-road");
        check("21 separately selectable prop roots", props.Length == 21 && props.All(p => p.visual != null));
        var sources = props.Select(p => AssetDatabase.GetAssetPath(p.visual.sprite)).Distinct().ToArray();
        check("Six independent environment PNG sprites", sources.Length == 6 && sources.All(p => p.StartsWith(ModularMapBuilder.ArtRoot, StringComparison.Ordinal)));
        check("Six reusable object prefabs", Directory.GetFiles(ModularMapBuilder.PrefabRoot, "*.prefab").Length == 7);
        check("Trial is an instance of the shared authored map", PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(layout.gameObject) == ModularMapBuilder.MapPath);
        var ui = new GameObject("Temporary modular HUD check", typeof(RectTransform));
        try
        {
            var presentation = new PrototypeMapArt { smoothStyle = true, environmentPrefab = ModularMapBuilder.Layout() };
            presentation.BuildGarden((RectTransform)ui.transform);
            check("HUD builds enabled props from persistent prefab asset", presentation.Foreground.Count == 20 && ui.GetComponentsInChildren<UnityEngine.UI.Image>().Length == 28);
        }
        finally { UnityEngine.Object.DestroyImmediate(ui); }
        bool alpha = true;
        foreach (string path in sources)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("Invalid PNG: " + path);
                alpha &= texture.GetPixel(0, 0).a == 0 && texture.GetPixel(texture.width - 1, texture.height - 1).a == 0;
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        check("Standalone PNGs have transparent corners", alpha);
        check("Pond and its shore move as one composite prop", props.Count(p => p.visual.sprite.name == "pond-cluster") == 1 && !props.Any(p => p.name.StartsWith("Đá bờ hồ", StringComparison.Ordinal)) && !props.Any(p => p.visual.sprite.name == "earth-patch" || p.visual.sprite.name == "stepping-stone"));
        var tree = props.Single(p => p.name == "Anh đào trái");
        var rock = props.Single(p => p.name == "Đá trái");
        var beforeTree = tree.transform.position; var beforeRock = rock.transform.position;
        var target = beforeTree + new Vector3(.65f, -.25f, 0);
        try
        {
            tree.transform.position = target;
            PrefabUtility.RecordPrefabInstancePropertyModifications(tree.transform);
            EditorSceneManager.MarkSceneDirty(tree.gameObject.scene);
            if (!EditorSceneManager.SaveScene(tree.gameObject.scene)) throw new InvalidOperationException("Could not save moved tree.");
            EditorSceneManager.OpenScene(Trial);
            layout = UnityEngine.Object.FindAnyObjectByType<ModularMapLayout>();
            props = layout.GetComponentsInChildren<ModularMapProp>();
            tree = props.Single(p => p.name == "Anh đào trái"); rock = props.Single(p => p.name == "Đá trái");
            check("Moved tree persists after scene save and reopen", Vector3.Distance(tree.transform.position, target) < .0001f);
            check("Moving tree leaves rock and substrate unchanged", rock.transform.position == beforeRock && layout.ground.transform.localPosition == Vector3.zero);
            check("Separate tree shadow follows its root", tree.castShadow != null && tree.castShadow.transform.IsChildOf(tree.transform));
            CartoonArtBuilder.Prepare();
            layout = UnityEngine.Object.FindAnyObjectByType<ModularMapLayout>();
            props = layout.GetComponentsInChildren<ModularMapProp>(); tree = props.Single(p => p.name == "Anh đào trái");
            check("Reimport preserves authored scene placement", Vector3.Distance(tree.transform.position, target) < .0001f && props.Length == 21);
        }
        finally
        {
            EditorSceneManager.OpenScene(Trial);
            layout = UnityEngine.Object.FindAnyObjectByType<ModularMapLayout>();
            tree = layout.GetComponentsInChildren<ModularMapProp>().Single(p => p.name == "Anh đào trái");
            tree.transform.position = beforeTree;
            PrefabUtility.RecordPrefabInstancePropertyModifications(tree.transform);
            EditorSceneManager.MarkSceneDirty(tree.gameObject.scene);
            if (!EditorSceneManager.SaveScene(tree.gameObject.scene)) throw new InvalidOperationException("Could not restore sample placement.");
        }
        var evidence = new Evidence { passed = checks.Count(c => c.passed), failed = checks.Count(c => !c.passed), placedProps = props.Length, checks = checks.ToArray() };
        var json = "{\n  \"passed\": " + evidence.passed + ",\n  \"failed\": " + evidence.failed + ",\n  \"placedProps\": " + evidence.placedProps + ",\n  \"checks\": [\n" +
            string.Join(",\n", checks.Select(c => "    " + JsonUtility.ToJson(c))) + "\n  ]\n}";
        File.WriteAllText("Verification/CartoonTrial/modular-artifacts.json", json + "\n");
        if (evidence.failed > 0) throw new InvalidOperationException(json);
        return json;
    }
}
