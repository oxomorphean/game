using System;
using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Prototype
{
    /// <summary>Reuses the project's existing garden sprites for the local gameplay preview.</summary>
    [Serializable]
    public sealed class PrototypeMapArt
    {
        public Sprite grass, grassShade, gate, tree, puffyTree, rock, pond, flower, lantern, shadow;
        public Sprite[] paving, idle, walk;
        public const float ActorScale = 1.5f;

        public void Validate()
        {
            foreach (var sprite in new[] { grass, grassShade, gate, tree, puffyTree, rock, pond, flower, lantern, shadow })
                if (sprite == null) throw new InvalidOperationException("Scene logic thiếu tham chiếu asset sân vườn.");
            if (paving == null || paving.Length != 4 || idle == null || idle.Length != 4 || walk == null || walk.Length != 6 ||
                Array.Exists(paving, x => x == null) || Array.Exists(idle, x => x == null) || Array.Exists(walk, x => x == null))
                throw new InvalidOperationException("Scene logic thiếu tile hoặc frame rùa. Chạy LogicBuilder.ApplyArt.");
        }

        public void BuildGround(RectTransform world)
        {
            Picture(world, "Fresh grass", grass, new Rect(0, 0, 1196, 428));
            // Broad paving leaves plain grass at the edges and keeps resource points visible.
            for (int y = 0; y < 14; y++)
            for (int x = 0; x < 38; x++)
            {
                bool courtyard = x >= 5 && x <= 28 && y >= 6 && y <= 11;
                bool path = y >= 10 && y <= 11 || x >= 15 && x <= 17 && y >= 2 || x >= 33 && x <= 35 && y >= 6 && y <= 11;
                if (!courtyard && !path) continue;
                int hash = (x * 31 + y * 17) % 19;
                var tile = hash < 2 ? paving[2] : hash < 6 ? paving[1] : paving[0];
                Picture(world, "Courtyard tile " + x + ":" + y, tile, new Rect(x * 32, y * 32, 32, 32));
            }
            Picture(world, "Quiet grass shade", grassShade, new Rect(895, 298, 160, 80));
        }

        public void BuildGarden(RectTransform world)
        {
            Blossom(world, "Cherry tree A", tree, 75, 187);
            Blossom(world, "Cherry tree B", puffyTree, 815, 158);
            Picture(world, "Garden rock left", rock, new Rect(33, 281, 64, 64));
            Picture(world, "Garden rock right", rock, new Rect(915, 220, 48, 48));
            Picture(world, "Stone lantern left", lantern, new Rect(335, 148, 48, 72));
            Picture(world, "Stone lantern right", lantern, new Rect(963, 111, 48, 72));
            foreach (var point in new[] { new Vector2(25, 222), new Vector2(101, 356), new Vector2(729, 54), new Vector2(928, 372) })
                Picture(world, "Sparse flowers", flower, new Rect(point.x, point.y, 32, 32));
        }

        private void Blossom(Transform parent, string name, Sprite sprite, float x, float footY)
        {
            var shade = Picture(parent, name + " shadow", sprite, new Rect(x - 24, footY - 9, 150, 32));
            shade.color = new Color(.16f, .19f, .27f, .19f);
            float width = sprite.rect.width * 1.5f, height = sprite.rect.height * 1.5f;
            Picture(parent, name, sprite, new Rect(x - width * .5f, footY - height + sprite.pivot.y * 1.5f, width, height));
        }

        public Sprite Resource(string item)
        {
            switch (item)
            {
                case "stone": return rock;
                case "wood": return tree;
                case "egg": case "seed": return flower;
                default: return flower;
            }
        }

        public Sprite Frame(bool moving, float time)
        {
            var frames = moving ? walk : idle;
            return frames[Mathf.FloorToInt(time * (moving ? 8 : 4)) % frames.Length];
        }

        public static Image Picture(Transform parent, string name, Sprite sprite, Rect rect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var t = (RectTransform)go.transform;
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0, 1);
            t.anchoredPosition = new Vector2(rect.x, -rect.y); t.sizeDelta = rect.size;
            var image = go.GetComponent<Image>(); image.sprite = sprite; image.color = Color.white;
            image.raycastTarget = false; image.useSpriteMesh = false;
            return image;
        }
    }
}
