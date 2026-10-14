using System;
using System.Collections.Generic;
using System.IO;
using LinhThu.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TreeShadowProbe
{
    public static object Run(string stage = "before")
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before the isolated tree render.");
        var tree = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Content/Prefabs/Environment/cherry-tree.prefab").GetComponent<ModularMapProp>();
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Tree shadow probe");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        var cameraNode = new GameObject("Camera"); cameraNode.transform.SetParent(root.transform, false);
        var camera = cameraNode.AddComponent<Camera>(); camera.enabled = false; camera.scene = scene;
        camera.orthographic = true; camera.orthographicSize = 4.5f; camera.transform.position = new Vector3(0, 0, -10);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white;
        var target = new RenderTexture(768, 768, 24); camera.targetTexture = target;
        var silhouette = new GameObject("Caster").AddComponent<SpriteRenderer>(); silhouette.transform.SetParent(root.transform, false);
        silhouette.sprite = tree.visual.sprite; silhouette.sharedMaterial = tree.castShadow.sharedMaterial;
        silhouette.transform.localScale = tree.visual.transform.localScale;
        var shadow = new SunShadowMesh(silhouette);
        var image = new Texture2D(768, 768, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        var samples = new List<object>();
        string folder = "Verification/TreeGrounding/" + stage + "/";
        Directory.CreateDirectory(folder);
        try
        {
            foreach (string variant in new[] { "baseline", "no-cutoff", "short-contact", "medium-contact" })
            foreach (float hour in new[] { 8f, 11.5f, 12f, 16f })
            {
                var settings = tree.shadowSettings;
                if (variant != "baseline") settings.baseCutoff = -1;
                if (variant == "short-contact") settings.contactHeight = .10f;
                if (variant == "medium-contact") settings.contactHeight = .18f;
                DaylightPresentation.ApplyLighting(hour);
                float height = tree.visual.sprite.rect.height / tree.visual.sprite.pixelsPerUnit * Mathf.Abs(tree.visual.transform.lossyScale.y);
                shadow.Refresh(settings, Vector3.zero, height);
                camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 768, 768), 0, 0); image.Apply();
                File.WriteAllBytes(folder + variant + "-" + hour.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png", image.EncodeToPNG());
                var pixels = image.GetPixels32();
                int left = 768, right = -1, area = 0, longest = 0, run = 0, lastBottom = -100;
                for (int x = 0; x < 768; x++)
                {
                    int bottom = -1;
                    for (int y = 0; y < 768; y++)
                        if (pixels[y * 768 + x].r < 245) { area++; if (bottom < 0) bottom = y; }
                    if (bottom >= 0) { left = Mathf.Min(left, x); right = x; }
                    run = bottom >= 0 && bottom == lastBottom ? run + 1 : 1;
                    longest = Mathf.Max(longest, bottom >= 0 ? run : 0); lastBottom = bottom;
                }
                samples.Add(new { variant, hour, area, width = right - left + 1, horizontalBottomRun = longest });
            }
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(samples, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(folder + "probe.json", json + "\n");
            return json;
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null; shadow.Dispose();
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(image);
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(scene); DaylightPresentation.ApplyLighting(12);
        }
    }
}
