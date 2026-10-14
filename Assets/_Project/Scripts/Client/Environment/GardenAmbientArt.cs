using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Client.Environment
{
    internal enum GardenAccentKind
    {
        Cherry,
        Pond
    }

    /// <summary>Small vector accents in sprite UV coordinates, shared by world and Canvas maps.</summary>
    internal static class GardenAmbientArt
    {
        public const int FallingPetals = 20;
        private static readonly Vector2[] RootContacts = {new Vector2(.452f, .059f), new Vector2(.477f, .055f), new Vector2(.548f, .066f), new Vector2(.564f, .070f), new Vector2(.414f, .124f), new Vector2(.613f, .122f)};
        private static readonly Vector2[] WaterCenters = {new Vector2(.38f, .51f), new Vector2(.63f, .38f), new Vector2(.48f, .29f)};
        private static readonly Vector2[] CanopyPoints = {new Vector2(.35f, .72f), new Vector2(.58f, .75f), new Vector2(.78f, .58f), new Vector2(.23f, .52f), new Vector2(.46f, .38f), new Vector2(.73f, .40f)};
        public static bool TryKind(Sprite sprite, out GardenAccentKind kind)
        {
            kind = GardenAccentKind.Cherry;
            if (sprite == null)
                return false;
            if (sprite.name == "cherry-tree")
                return true;
            kind = GardenAccentKind.Pond;
            return sprite.name == "pond-cluster";
        }

        public static void Draw(VertexHelper mesh, Rect rect, GardenAccentKind kind, float time, int seed, Color light)
        {
            mesh.Clear();
            if (kind == GardenAccentKind.Pond)
            {
                Pond(mesh, rect, time, seed, light);
                return;
            }

            Roots(mesh, rect, seed, light);
            for (int i = 0; i < FallingPetals; i++)
            {
                float lifetime = Mathf.Lerp(10, 16, Sample(seed, i, 0));
                float phase = time / lifetime + Sample(seed, i, 1);
                int cycle = Mathf.FloorToInt(phase);
                float progress = phase - cycle;
                var canopy = CanopyPoints[i % CanopyPoints.Length];
                float startX = canopy.x + (Sample(seed, i, cycle + 4) - .5f) * .07f;
                float startY = canopy.y + (Sample(seed, i, cycle + 5) - .5f) * .06f;
                float gust = Mathf.Sin(time * .31f + seed * .017f);
                float flutter = Mathf.Sin(time * (1.15f + Sample(seed, i, 8) * .8f) + i * 2.3f);
                float x = startX + progress * (.08f + gust * .045f) + flutter * .018f;
                float fall = progress + Mathf.Sin(progress * Mathf.PI * 4 + i) * .012f * Mathf.Sin(progress * Mathf.PI);
                float y = Mathf.Lerp(startY, .035f, fall);
                float alpha = Mathf.Clamp01(progress * 8) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.82f, 1, progress)));
                float size = rect.width * Mathf.Lerp(.007f, .011f, Sample(seed, i, 2));
                Petal(mesh, Point(rect, x, y), size * (.8f + .2f * Mathf.Abs(flutter)), time * (.5f + Sample(seed, i, 3)) + i + gust * .4f, new Color(1, .70f, .81f, alpha * .88f) * light);
            }
        }

        private static void Roots(VertexHelper mesh, Rect rect, int seed, Color light)
        {
            // These blades cross the bottom edges of the flared roots; there is no ground oval.
            for (int i = 0; i < RootContacts.Length; i++)
            {
                var foot = Point(rect, RootContacts[i].x, RootContacts[i].y);
                float width = rect.width * .006f, height = rect.height * (.012f + Sample(seed, i, 12) * .010f);
                Triangle(mesh, foot - Vector2.right * width, foot + new Vector2(-width * .9f, height), foot + Vector2.right * width, new Color(.24f, .42f, .09f, .94f) * light);
                Triangle(mesh, foot, foot + new Vector2(width * 1.7f, height * .85f), foot + Vector2.right * width * 1.4f, new Color(.40f, .59f, .12f, .94f) * light);
            }

            for (int i = 0; i < 10; i++)
            {
                float x = .50f + (Sample(seed, i, 20) - .5f) * .20f;
                float y = .022f + Sample(seed, i, 21) * .055f;
                Petal(mesh, Point(rect, x, y), rect.width * .0045f, Sample(seed, i, 22) * Mathf.PI * 2, new Color(.97f, .68f, .77f, .68f) * light);
            }
        }

        private static void Pond(VertexHelper mesh, Rect rect, float time, int seed, Color light)
        {
            Color reflection = Color.Lerp(new Color(.75f, .98f, .94f), new Color(1, .86f, .52f), DaylightPresentation.GoldenHour);
            // All three centers and their largest rings lie inside water, away from rocks and lilies.
            for (int i = 0; i < WaterCenters.Length; i++)
            {
                float progress = Mathf.Repeat(time / (4.2f + i * .7f) + i * .31f + Sample(seed, i, 0), 1);
                float radius = rect.width * Mathf.Lerp(.008f, .060f, progress);
                var tint = reflection * light;
                tint.a = Mathf.Sin(progress * Mathf.PI) * .34f;
                var center = Point(rect, WaterCenters[i].x, WaterCenters[i].y);
                Arc(mesh, center, radius, rect.width * .0028f, .08f, .93f, tint);
                tint.a *= .68f;
                Arc(mesh, center, radius * .78f, rect.width * .0020f, 1.14f, 1.86f, tint);
            }

            // Sparse glints follow the sun across the inner water, clear of the shoreline and lilies.
            for (int i = 0; i < 7; i++)
            {
                float phase = Mathf.Repeat(time / (2.8f + i * .19f) + Sample(seed, i, 40), 1);
                float pulse = Mathf.Pow(Mathf.Sin(phase * Mathf.PI), 12);
                float x = .49f + DaylightPresentation.SunHorizontal * .12f + (Sample(seed, i, 41) - .5f) * .22f;
                float y = .28f + Sample(seed, i, 42) * .23f;
                var center = Point(rect, x, y);
                var tint = reflection * light;
                tint.a = pulse * .80f;
                float size = rect.width * (.004f + pulse * .004f);
                var right = Vector2.right * size;
                var up = Vector2.up * size * .65f;
                Triangle(mesh, center - right, center + up * .25f, center + right, tint);
                Triangle(mesh, center - right, center - up * .25f, center + right, tint);
                Triangle(mesh, center - up, center + right * .20f, center + up, tint);
                Triangle(mesh, center - up, center - right * .20f, center + up, tint);
            }
        }

        private static void Arc(VertexHelper mesh, Vector2 center, float radius, float thickness, float from, float to, Color color)
        {
            const int segments = 20;
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.Lerp(from, to, (float)i / segments) * Mathf.PI;
                float b = Mathf.Lerp(from, to, (float)(i + 1) / segments) * Mathf.PI;
                var directionA = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * .34f);
                var directionB = new Vector2(Mathf.Cos(b), Mathf.Sin(b) * .34f);
                int start = mesh.currentVertCount;
                mesh.AddVert(center + directionA * (radius - thickness), color, Vector2.zero);
                mesh.AddVert(center + directionA * (radius + thickness), color, Vector2.zero);
                mesh.AddVert(center + directionB * (radius + thickness), color, Vector2.zero);
                mesh.AddVert(center + directionB * (radius - thickness), color, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
            }
        }

        private static void Petal(VertexHelper mesh, Vector2 center, float size, float angle, Color color)
        {
            float flutter = .45f + Mathf.Abs(Mathf.Sin(angle * 1.7f)) * .55f;
            var side = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size * flutter;
            var up = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * size * .70f;
            int start = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            mesh.AddVert(center - up, color, Vector2.zero);
            mesh.AddVert(center - side - up * .12f, color, Vector2.zero);
            mesh.AddVert(center - side * .55f + up, color, Vector2.zero);
            mesh.AddVert(center + up * .66f, color, Vector2.zero);
            mesh.AddVert(center + side * .55f + up, color, Vector2.zero);
            mesh.AddVert(center + side - up * .12f, color, Vector2.zero);
            for (int i = 1; i < 6; i++)
                mesh.AddTriangle(start, start + i, start + i + 1);
            mesh.AddTriangle(start, start + 6, start + 1);
        }

        private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero);
            mesh.AddVert(b, color, Vector2.zero);
            mesh.AddVert(c, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }

        private static Vector2 Point(Rect rect, float x, float y) => new Vector2(rect.xMin + rect.width * x, rect.yMin + rect.height * y);
        private static float Sample(int seed, int index, int cycle)
        {
            uint value = unchecked((uint)seed + (uint)index * 0x9E3779B9u + (uint)cycle * 0x85EBCA6Bu);
            value ^= value >> 16;
            value = unchecked(value * 0x7FEB352Du);
            value ^= value >> 15;
            value = unchecked(value * 0x846CA68Bu);
            value ^= value >> 16;
            return (value & 0x00FFFFFFu) / 16777216f;
        }
    }
}
