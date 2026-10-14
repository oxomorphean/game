using System;
using System.Linq;
using LinhThu.Prototype;
using LinhThu.Prototype.Preview;
using UnityEditor;
using UnityEngine;

namespace LinhThu.Editor
{
    /// <summary>Authors ground shade children while preserving prop placement, scale, and collision data.</summary>
    public static class ArtPresentationBuilder
    {
        [MenuItem("Demo/Mỹ thuật/Thống nhất bóng mặt trời")]
        public static void ConfigureSunShadows()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring shadow geometry.");
            PrepareMaterials();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Art/DaylightShadow.mat");
            // Initial asset measurements are saved on prefabs; runtime never selects a rule by sprite name.
            var authored = new System.Collections.Generic.Dictionary<string, SunShadowSettings> {
                ["cherry-tree"] = new SunShadowSettings { heightScale=1, width=1.3f, depth=.64f, center=.49f, contactHeight=.10f, baseCutoff=-1, canopyBlueRatio=.65f },
                ["rock"] = new SunShadowSettings { heightScale=1, width=1.4f, depth=.65f, center=.28f, contactHeight=.10f, baseCutoff=-1 },
                ["bush"] = new SunShadowSettings { heightScale=1, width=1.4f, depth=.5f, center=.40f, contactHeight=.10f, baseCutoff=-1 },
                ["grass-tuft"] = SunShadowSettings.Default,
                ["white-flowers"] = SunShadowSettings.Default
            };
            var paths = authored.Keys.Select(id => ModularMapBuilder.PrefabRoot + id + ".prefab")
                .Concat(new[] { ModularMapBuilder.MapPath, ArtPreviewBuilder.Map });
            foreach (string path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var prop in root.GetComponentsInChildren<ModularMapProp>(true))
                    {
                        if (prop.floorLayer) continue;
                        prop.shadowSettings = authored[prop.visual.sprite.name];
                        prop.castShadow.sharedMaterial = material;
                        if (prop.contactShadow != null) UnityEngine.Object.DestroyImmediate(prop.contactShadow.gameObject);
                        prop.contactShadow = null;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var hero = AssetDatabase.LoadAssetAtPath<LinhThu.Prototype.Characters.DirectionalHeroDefinition>(ArtPreviewBuilder.Hero);
            hero.shadowSettings = SunShadowSettings.Default; hero.shadowReferenceHeight = 1.35f;
            EditorUtility.SetDirty(hero);
            var modular = AssetDatabase.LoadAssetAtPath<LinhThu.Prototype.Characters.ModularHeroDefinition>("Assets/_Project/Content/Characters/ModularHeroFront.asset");
            modular.shadowSettings = SunShadowSettings.Default; modular.shadowReferenceHeight = 1.2f;
            modular.shadowMaterial = material; EditorUtility.SetDirty(modular);
            var production = PrefabUtility.LoadPrefabContents(HeroProductionBuilder.Prefab);
            try
            {
                var view = production.GetComponent<LinhThu.Prototype.Characters.AuthoredHeroView>();
                view.shadowSettings = SunShadowSettings.Default; view.shadowReferenceHeight = 1.3f;
                foreach (var shadow in view.allShadows) shadow.sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(production, HeroProductionBuilder.Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(production); }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Demo/Mỹ thuật/Chỉnh footprint sát bờ hồ")]
        public static void ApplyPondShore()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring pond collision.");
            // The low shoreline is solid; raised rock tops and decorative grass are not ground footprints.
            var shoreline = new[] { new Vector2(.09f, .48f), new Vector2(.14f, .61f), new Vector2(.29f, .70f),
                new Vector2(.46f, .72f), new Vector2(.64f, .69f), new Vector2(.77f, .60f),
                new Vector2(.85f, .44f), new Vector2(.81f, .27f), new Vector2(.66f, .15f),
                new Vector2(.46f, .14f), new Vector2(.29f, .22f), new Vector2(.15f, .35f) };
            foreach (string path in new[] { ModularMapBuilder.PrefabRoot + "pond-cluster.prefab", ArtPreviewBuilder.Map })
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var pond = root.GetComponentsInChildren<ModularMapProp>(true).Single(p => p.visual.sprite.name == "pond-cluster");
                    var obstacle = pond.GetComponent<ObstacleAuthoring>();
                    if (obstacle == null) throw new InvalidOperationException("Pond footprint is missing in " + path);
                    var serialized = new SerializedObject(obstacle);
                    var polygon = serialized.FindProperty("polygon"); polygon.arraySize = shoreline.Length;
                    var sprite = pond.visual.sprite;
                    for (int i = 0; i < shoreline.Length; i++)
                    {
                        Vector2 point = (Vector2.Scale(shoreline[i], sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
                        polygon.GetArrayElementAtIndex(i).vector2Value = pond.transform.InverseTransformPoint(pond.visual.transform.TransformPoint(point));
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    obstacle.Definition(); // Reject a non-convex contour before saving it.
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Demo/Mỹ thuật/Chỉnh footprint gốc anh đào")]
        public static void ApplyTreeGrounding()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring tree collision.");
            // The contour covers the trunk and spreading roots; the canopy remains walkable underneath.
            var roots = new[] { new Vector2(.41f, .125f), new Vector2(.43f, .175f), new Vector2(.50f, .205f),
                new Vector2(.57f, .175f), new Vector2(.625f, .125f), new Vector2(.58f, .055f),
                new Vector2(.51f, .040f), new Vector2(.455f, .065f) };
            foreach (string path in new[] { ModularMapBuilder.PrefabRoot + "cherry-tree.prefab", ArtPreviewBuilder.Map })
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var trees = root.GetComponentsInChildren<ModularMapProp>(true).Where(p => p.visual.sprite.name == "cherry-tree").ToArray();
                    if (trees.Length == 0) throw new InvalidOperationException("Tree is missing in " + path);
                    foreach (var tree in trees)
                    {
                        tree.shadowSettings.contactHeight = .10f;
                        tree.shadowSettings.baseCutoff = -1;
                        tree.shadowSettings.depth = .64f;
                        tree.shadowSettings.center = .49f;
                        tree.shadowSettings.canopyBlueRatio = .65f;
                        var obstacle = tree.GetComponent<ObstacleAuthoring>();
                        if (obstacle == null) throw new InvalidOperationException("Tree footprint is missing in " + path);
                        var serialized = new SerializedObject(obstacle);
                        var polygon = serialized.FindProperty("polygon"); polygon.arraySize = roots.Length;
                        var sprite = tree.visual.sprite;
                        for (int i = 0; i < roots.Length; i++)
                        {
                            Vector2 point = (Vector2.Scale(roots[i], sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
                            polygon.GetArrayElementAtIndex(i).vector2Value = tree.transform.InverseTransformPoint(tree.visual.transform.TransformPoint(point));
                        }
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        obstacle.Definition();
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Demo/Mỹ thuật/Gắn asset vào mặt đất")]
        public static void ApplyFromMenu() => Apply();

        public static string Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before preparing ground contact.");
            PrepareMaterials();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/CartoonV2/shadow.png");
            if (sprite == null) throw new InvalidOperationException("Missing existing ground shadow sprite.");
            Shade("cherry-tree", sprite, Vector2.zero, Vector2.zero, 0);
            Shade("rock", sprite, Vector2.zero, Vector2.zero, 0);
            Shade("bush", sprite, Vector2.zero, Vector2.zero, 0);
            Shade("grass-tuft", sprite, Vector2.zero, Vector2.zero, 0);
            Shade("white-flowers", sprite, Vector2.zero, Vector2.zero, 0);
            Shade("pond-cluster", sprite, new Vector2(3.95f, 1.74f), new Vector2(.06f, -.025f), .16f);
            var map = PrefabUtility.LoadPrefabContents(ModularMapBuilder.MapPath);
            try
            {
                map.GetComponent<ModularMapLayout>().ground.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Art/DaylightSprite.mat");
                PrefabUtility.SaveAsPrefabAsset(map, ModularMapBuilder.MapPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(map); }
            AssetDatabase.SaveAssets();
            ConfigureSunShadows();
            return "Prop silhouettes stay grounded; tree/rock ellipse shades removed, placements and obstacle components preserved.";
        }

        private static void Shade(string id, Sprite sprite, Vector2 size, Vector2 position, float opacity)
        {
            string path = ModularMapBuilder.PrefabRoot + id + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var prop = root.GetComponent<ModularMapProp>();
                if (prop == null || prop.visual == null) throw new InvalidOperationException("Missing prop visual: " + path);
                var node = root.transform.Find("Bóng tiếp xúc");
                if (opacity == 0)
                {
                    if (node != null) UnityEngine.Object.DestroyImmediate(node.gameObject);
                    prop.contactShadow = null;
                }
                else
                {
                    if (node == null) { node = new GameObject("Bóng tiếp xúc").transform; node.SetParent(root.transform, false); }
                    var contact = node.GetComponent<SpriteRenderer>();
                    if (contact == null) contact = node.gameObject.AddComponent<SpriteRenderer>();
                    contact.sprite = sprite; contact.color = new Color(1, 1, 1, opacity);
                    contact.transform.localPosition = position;
                    contact.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
                    contact.sortingOrder = prop.floorLayer ? prop.floorOrder - 1 : -28;
                    prop.contactShadow = contact;
                }
                prop.visual.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Art/DaylightSprite.mat");
                if (!prop.floorLayer)
                {
                    if (prop.castShadow == null)
                    {
                        prop.castShadow = new GameObject("Bóng đổ").AddComponent<SpriteRenderer>();
                        prop.castShadow.transform.SetParent(root.transform, false);
                    }
                    var cast = prop.castShadow; cast.sprite = prop.visual.sprite;
                    cast.transform.localPosition = prop.visual.transform.localPosition;
                    cast.transform.localRotation = prop.visual.transform.localRotation;
                    cast.transform.localScale = prop.visual.transform.localScale;
                    cast.color = Color.white; cast.sortingOrder = -30;
                    cast.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Art/DaylightShadow.mat");
                    cast.localBounds = new Bounds(Vector3.zero, Vector3.one * cast.sprite.bounds.size.magnitude * 4);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [InitializeOnLoadMethod]
        private static void DefaultLight() => DaylightPresentation.ApplyLighting(12);

        private static void PrepareMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Art")) AssetDatabase.CreateFolder("Assets/_Project/Resources", "Art");
            foreach (var item in new[] { new[] { "DaylightSprite", "LinhThu/Daylight Sprite" }, new[] { "DaylightShadow", "LinhThu/Daylight Shadow" } })
            {
                var shader = Shader.Find(item[1]);
                if (shader == null) throw new InvalidOperationException("Missing daylight shader: " + item[1]);
                string path = "Assets/_Project/Resources/Art/" + item[0] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
                else { material.shader = shader; EditorUtility.SetDirty(material); }
            }
            DefaultLight();
        }
    }
}
