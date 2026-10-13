using System;
using LinhThu.Prototype;
using UnityEditor;
using UnityEngine;

namespace LinhThu.Editor
{
    /// <summary>Authors ground shade children while preserving prop placement, scale, and collision data.</summary>
    public static class ArtPresentationBuilder
    {
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
            Shade("bush", sprite, new Vector2(.95f, .32f), new Vector2(0, .055f), .28f);
            Shade("grass-tuft", sprite, new Vector2(.46f, .16f), new Vector2(0, .025f), .16f);
            Shade("white-flowers", sprite, new Vector2(.42f, .16f), new Vector2(0, .025f), .16f);
            Shade("pond-cluster", sprite, new Vector2(3.95f, 1.74f), new Vector2(.06f, -.025f), .16f);
            var map = PrefabUtility.LoadPrefabContents(ModularMapBuilder.MapPath);
            try
            {
                map.GetComponent<ModularMapLayout>().ground.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Art/DaylightSprite.mat");
                PrefabUtility.SaveAsPrefabAsset(map, ModularMapBuilder.MapPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(map); }
            AssetDatabase.SaveAssets();
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
