using System;
using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Prototype
{
    /// <summary>Garden presentation assets; these sprites do not change gameplay positions or rules.</summary>
    [Serializable]
    public sealed class PrototypeMapArt
    {
        public Sprite grass, grassShade, gate, tree, puffyTree, rock, pond, flower, lantern, shadow;
        public Sprite bamboo, herb, ore, egg, petal;
        public Sprite[] paving, meadow, idle, walk, playerFrames, rippleFrames;
        public const float ActorScale = 1.5f;
        public static readonly Rect PondRect = new Rect(958, 283, 192, 128);

        public void Validate()
        {
            foreach (var sprite in new[] { grass, grassShade, gate, tree, puffyTree, rock, pond, flower, lantern, shadow,
                bamboo, herb, ore, egg, petal })
                if (sprite == null) throw new InvalidOperationException("Scene logic thiếu tham chiếu asset sân vườn.");
            RequireFrames(paving, 3); RequireFrames(meadow, 2); RequireFrames(idle, 4); RequireFrames(walk, 6);
            RequireFrames(playerFrames, 30); RequireFrames(rippleFrames, 4);
        }

        private static void RequireFrames(Sprite[] frames, int count)
        {
            if (frames == null || frames.Length != count || Array.Exists(frames, frame => frame == null))
                throw new InvalidOperationException("Scene logic thiếu sprite hoặc animation. Chạy GardenV8Builder.Import, rồi LogicBuilder.ApplyArt.");
        }

        public void BuildGround(RectTransform world)
        {
            Picture(world, "Fresh grass", grass, new Rect(0, 0, 1196, 428));
            // Broad, untiled meadow shapes and sparse blades leave the walking area clear.
            Picture(world, "Meadow beside cherry A", meadow[0], new Rect(15, 165, 192, 96));
            Picture(world, "Meadow beside cherry B", meadow[1], new Rect(775, 100, 192, 96));
            Picture(world, "Quiet grass shade", grassShade, new Rect(900, 278, 160, 80));
            const int step = 48;
            for (int y = 0; y < 9; y++)
            for (int x = 0; x < 25; x++)
            {
                int left = y == 4 || y == 7 ? 4 : 3;
                int right = y == 6 ? 18 : 17;
                bool courtyard = y >= 4 && y <= 7 && x >= left && x <= right;
                bool path = y >= 7 || x >= 9 && x <= 10 && y >= 2 || x >= 22 && x <= 23 && y >= 4;
                if (!courtyard && !path) continue;
                int hash = (x * 31 + y * 17) % 19;
                bool nearBlossom = (x < 6 && y < 7) || (x >= 16 && x <= 19 && y < 7);
                var tile = paving[nearBlossom && hash < 3 ? 2 : hash < 6 ? 1 : 0];
                Picture(world, "Courtyard tile " + x + ":" + y, tile, new Rect(x * step, y * step, step, step));
            }
        }

        public void BuildGarden(RectTransform world)
        {
            Blossom(world, "Cherry tree A", tree, 75, 187);
            Blossom(world, "Cherry tree B", puffyTree, 815, 158);
            Picture(world, "Garden rock left", rock, new Rect(33, 281, 64, 64));
            Picture(world, "Garden rock right", rock, new Rect(915, 220, 48, 48));
            Picture(world, "Stone lantern left", lantern, new Rect(335, 148, 48, 72));
            Picture(world, "Stone lantern right", lantern, new Rect(963, 111, 48, 72));
            PlaceAtFoot(world, "Bamboo at garden edge", bamboo, new Vector2(207, 172), ActorScale);
            foreach (var point in new[] { new Vector2(25, 222), new Vector2(101, 356), new Vector2(729, 54), new Vector2(928, 372) })
                Picture(world, "Sparse flowers", flower, new Rect(point.x, point.y, 32, 32));
        }

        public void BuildAmbient(RectTransform world)
        {
            var layer = new GameObject("Blossom and pond ambience", typeof(RectTransform));
            layer.transform.SetParent(world, false);
            var rect = (RectTransform)layer.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.zero;
            layer.AddComponent<PrototypeGardenAmbient>().Bind(petal, rippleFrames);
        }

        private void Blossom(Transform parent, string name, Sprite sprite, float x, float footY)
        {
            var shade = Picture(parent, name + " shadow", sprite, new Rect(x - 24, footY - 9, 150, 32));
            shade.color = new Color(.16f, .19f, .27f, .19f);
            PlaceAtFoot(parent, name, sprite, new Vector2(x, footY), ActorScale);
        }

        private static void PlaceAtFoot(Transform parent, string name, Sprite sprite, Vector2 foot, float scale)
        {
            var size = sprite.rect.size * scale;
            Picture(parent, name, sprite, new Rect(foot.x - sprite.pivot.x * scale,
                foot.y - size.y + sprite.pivot.y * scale, size.x, size.y));
        }

        public Sprite Resource(string item)
        {
            switch (item)
            {
                case "stone": return ore;
                case "wood": return bamboo;
                case "egg": return egg;
                case "herb": case "jade-herb": return herb;
                default: return flower;
            }
        }

        /// <param name="facing">Human front=0, back=1, side=2; left is a side flip in the presenter.</param>
        public Sprite Frame(bool moving, float time, bool human = false, int facing = 0)
        {
            if (human)
            {
                int count = moving ? 6 : 4;
                int frame = Mathf.FloorToInt(time * (moving ? 8 : 3)) % count;
                return playerFrames[Mathf.Clamp(facing, 0, 2) * 10 + (moving ? 4 : 0) + frame];
            }
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
