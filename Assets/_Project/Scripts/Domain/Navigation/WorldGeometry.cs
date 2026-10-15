using System;
using System.Collections.Generic;

namespace LinhThu.Domain.Navigation
{
    internal static class GeometryRules
    {
        public static bool Range(float value, float min, float max) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;
    }

    public readonly struct WorldPoint
    {
        public readonly float X, Y;
        public WorldPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float LengthSquared => X * X + Y * Y;
        public float Length => (float)Math.Sqrt(LengthSquared);
        public bool Finite => GeometryRules.Range(X, -100000, 100000) && GeometryRules.Range(Y, -100000, 100000);
        public WorldPoint Normalized => Length > .00001f ? this / Length : new WorldPoint(1, 0);
        public static WorldPoint operator +(WorldPoint a, WorldPoint b) => new WorldPoint(a.X + b.X, a.Y + b.Y);
        public static WorldPoint operator -(WorldPoint a, WorldPoint b) => new WorldPoint(a.X - b.X, a.Y - b.Y);
        public static WorldPoint operator *(WorldPoint a, float n) => new WorldPoint(a.X * n, a.Y * n);
        public static WorldPoint operator /(WorldPoint a, float n) => new WorldPoint(a.X / n, a.Y / n);
        public static float Dot(WorldPoint a, WorldPoint b) => a.X * b.X + a.Y * b.Y;
    }

    /// <summary>Ground footprint: legacy circles/boxes or an immutable convex polygon in world coordinates.</summary>
    public readonly struct WorldObstacle
    {
        public readonly WorldPoint Center, HalfSize;
        public readonly bool Box;
        private readonly WorldPoint[] vertices;
        public int VertexCount => vertices == null ? 0 : vertices.Length;
        public WorldPoint Vertex(int index) => vertices[index];
        public WorldObstacle(WorldPoint center, WorldPoint halfSize, bool box)
        {
            if (!center.Finite || !GeometryRules.Range(halfSize.X, .01f, 100) || !GeometryRules.Range(halfSize.Y, .01f, 100))
                throw new ArgumentException("Invalid obstacle.");
            Center = center;
            HalfSize = box ? halfSize : new WorldPoint(halfSize.X, halfSize.X);
            Box = box;
            vertices = null;
        }

        public WorldObstacle(IEnumerable<WorldPoint> polygon)
        {
            if (polygon == null)
                throw new ArgumentException("Missing footprint.");
            vertices = new List<WorldPoint>(polygon).ToArray();
            if (vertices.Length < 3 || vertices.Length > 32)
                throw new ArgumentException("A footprint needs 3–32 vertices.");
            float area = 0, minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                var a = vertices[i];
                var b = vertices[(i + 1) % vertices.Length];
                if (!a.Finite || (b - a).LengthSquared < .000001f)
                    throw new ArgumentException("Invalid footprint vertex.");
                area += Cross(a, b);
                minX = Math.Min(minX, a.X);
                minY = Math.Min(minY, a.Y);
                maxX = Math.Max(maxX, a.X);
                maxY = Math.Max(maxY, a.Y);
            }

            if (Math.Abs(area) < .0001f)
                throw new ArgumentException("Empty footprint.");
            if (area < 0)
                Array.Reverse(vertices);
            for (int i = 0; i < vertices.Length; i++)
                for (int j = 0; j < vertices.Length; j++)
                    if (Cross(vertices[(i + 1) % vertices.Length] - vertices[i], vertices[j] - vertices[i]) < -.00001f)
                        throw new ArgumentException("Footprints must be convex; use separate components for concave props.");
            Center = new WorldPoint((minX + maxX) * .5f, (minY + maxY) * .5f);
            HalfSize = new WorldPoint((maxX - minX) * .5f, (maxY - minY) * .5f);
            Box = false;
        }

        private static float Cross(WorldPoint a, WorldPoint b) => a.X * b.Y - a.Y * b.X;
        private bool Contains(WorldPoint point)
        {
            for (int i = 0; i < vertices.Length; i++)
                if (Cross(vertices[(i + 1) % vertices.Length] - vertices[i], point - vertices[i]) < 0)
                    return false;
            return true;
        }

        private WorldPoint Closest(WorldPoint point)
        {
            if (vertices == null)
                return new WorldPoint(Math.Max(Center.X - HalfSize.X, Math.Min(Center.X + HalfSize.X, point.X)), Math.Max(Center.Y - HalfSize.Y, Math.Min(Center.Y + HalfSize.Y, point.Y)));
            float best = float.MaxValue;
            WorldPoint result = default;
            for (int i = 0; i < vertices.Length; i++)
            {
                var a = vertices[i];
                var edge = vertices[(i + 1) % vertices.Length] - a;
                float t = Math.Max(0, Math.Min(1, WorldPoint.Dot(point - a, edge) / edge.LengthSquared));
                var candidate = a + edge * t;
                float distance = (point - candidate).LengthSquared;
                if (distance < best)
                {
                    best = distance;
                    result = candidate;
                }
            }

            return result;
        }

        public WorldPoint ContactNormal(WorldPoint exteriorPoint) => (exteriorPoint - (vertices == null && !Box ? Center : Closest(exteriorPoint))).Normalized;
        public WorldPoint PushOut(WorldPoint point, float radius, float clearance)
        {
            if (!Overlaps(point, radius)) return default;
            if (vertices == null && !Box)
            {
                var radial = point - Center;
                return radial.Normalized * (HalfSize.X + radius + clearance - radial.Length);
            }

            var closest = Closest(point);
            bool inside = vertices != null ? Contains(point)
                : Math.Abs(point.X - Center.X) <= HalfSize.X && Math.Abs(point.Y - Center.Y) <= HalfSize.Y;
            if (inside && vertices == null)
            {
                // Clamping an interior point gives itself, so select an actual box face.
                var relative = point - Center;
                float x = HalfSize.X - Math.Abs(relative.X), y = HalfSize.Y - Math.Abs(relative.Y);
                return x <= y ? new WorldPoint((relative.X < 0 ? -1 : 1) * (x + radius + clearance), 0)
                    : new WorldPoint(0, (relative.Y < 0 ? -1 : 1) * (y + radius + clearance));
            }
            var difference = inside ? closest - point : point - closest;
            var normal = difference.Normalized;
            if (difference.LengthSquared < .00000001f && vertices != null)
            {
                // A point exactly on a polygon edge still needs its outward normal.
                float best = float.MaxValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var edge = vertices[(i + 1) % vertices.Length] - vertices[i];
                    float distance = Math.Abs(Cross(edge, point - vertices[i])) / edge.Length;
                    if (distance < best) { best = distance; normal = new WorldPoint(edge.Y, -edge.X).Normalized; }
                }
            }
            return normal * (radius + clearance + (inside ? difference.Length : -difference.Length));
        }
        public bool Overlaps(WorldPoint point, float radius)
        {
            if (Math.Abs(point.X - Center.X) > HalfSize.X + radius || Math.Abs(point.Y - Center.Y) > HalfSize.Y + radius)
                return false;
            if (vertices != null)
                return Contains(point) || (point - Closest(point)).LengthSquared < radius * radius;
            if (!Box)
                return (point - Center).LengthSquared < (HalfSize.X + radius) * (HalfSize.X + radius);
            float dx = Math.Max(0, Math.Abs(point.X - Center.X) - HalfSize.X);
            float dy = Math.Max(0, Math.Abs(point.Y - Center.Y) - HalfSize.Y);
            return dx * dx + dy * dy < radius * radius;
        }

        public bool Crosses(WorldPoint start, WorldPoint end)
        {
            float segMinX = Math.Min(start.X, end.X);
            float segMaxX = Math.Max(start.X, end.X);
            float segMinY = Math.Min(start.Y, end.Y);
            float segMaxY = Math.Max(start.Y, end.Y);
            if (segMaxX < Center.X - HalfSize.X || segMinX > Center.X + HalfSize.X || segMaxY < Center.Y - HalfSize.Y || segMinY > Center.Y + HalfSize.Y)
                return false;
            var delta = end - start;
            if (vertices != null)
            {
                float first = 0, last = 1;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var edge = vertices[(i + 1) % vertices.Length] - vertices[i];
                    float distance = Cross(edge, start - vertices[i]), velocity = Cross(edge, delta);
                    if (Math.Abs(velocity) < .000001f)
                    {
                        if (distance < 0)
                            return false;
                        continue;
                    }

                    float t = -distance / velocity;
                    if (velocity > 0)
                        first = Math.Max(first, t);
                    else
                        last = Math.Min(last, t);
                    if (first > last)
                        return false;
                }

                return true;
            }

            if (!Box)
            {
                float t = delta.LengthSquared < .000001f ? 0 : Math.Max(0, Math.Min(1, WorldPoint.Dot(Center - start, delta) / delta.LengthSquared));
                return (start + delta * t - Center).LengthSquared <= HalfSize.X * HalfSize.X;
            }

            float enter = 0, exit = 1;
            return Slab(start.X, delta.X, Center.X - HalfSize.X, Center.X + HalfSize.X, ref enter, ref exit) && Slab(start.Y, delta.Y, Center.Y - HalfSize.Y, Center.Y + HalfSize.Y, ref enter, ref exit);
        }

        private static bool Slab(float start, float delta, float min, float max, ref float enter, ref float exit)
        {
            if (Math.Abs(delta) < .000001f)
                return start >= min && start <= max;
            float a = (min - start) / delta, b = (max - start) / delta;
            enter = Math.Max(enter, Math.Min(a, b));
            exit = Math.Min(exit, Math.Max(a, b));
            return enter <= exit;
        }
    }

    /// <summary>Circle movement with bounded substeps and wall sliding, and contour footprints.</summary>
    public sealed class WorldGeometry
    {
        public WorldBounds Bounds { get; }
        public WorldPoint HalfSize => Bounds.HalfSize;

        private readonly WorldObstacle[] obstacles;
        public WorldGeometry(WorldPoint halfSize, IEnumerable<WorldObstacle> obstacles)
            : this(new WorldBounds(halfSize), obstacles) { }
        public WorldGeometry(WorldBounds bounds, IEnumerable<WorldObstacle> obstacles)
        {
            if (bounds.HalfSize.X < 1 || bounds.HalfSize.Y < 1 || obstacles == null)
                throw new ArgumentException("Invalid arena.");
            Bounds = bounds;
            this.obstacles = new List<WorldObstacle>(obstacles).ToArray();
        }

        public bool CanStand(WorldPoint p, float radius)
        {
            if (!GeometryRules.Range(radius, .01f, 10) || !Bounds.Contains(p, radius))
                return false;
            foreach (var obstacle in obstacles)
                if (obstacle.Overlaps(p, radius))
                    return false;
            return true;
        }

        public bool ClearLine(WorldPoint a, WorldPoint b)
        {
            if (!a.Finite || !b.Finite)
                throw new ArgumentException("Invalid line.");
            foreach (var obstacle in obstacles)
                if (obstacle.Crosses(a, b))
                    return false;
            return true;
        }

        /// <summary>Optional steering assists walking around a blocked surface.</summary>
        public WorldPoint Move(WorldPoint from, WorldPoint delta, float radius, bool slideAround = false)
        {
            if (!delta.Finite || delta.Length > 2000)
                throw new ArgumentException("Invalid movement.");
            from = ResolvePenetration(from, radius);
            int steps = Math.Max(1, (int)Math.Ceiling(delta.Length / Math.Min(.12f, radius * .5f)));
            var increment = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                if (CanStand(from + increment, radius))
                {
                    from += increment;
                    continue;
                }

                if (slideAround)
                {
                    from = Slide(from, increment, radius);
                    continue;
                }

                var x = from + new WorldPoint(increment.X, 0);
                if (CanStand(x, radius))
                    from = x;
                var y = from + new WorldPoint(0, increment.Y);
                if (CanStand(y, radius))
                    from = y;
            }

            return from;
        }

        /// <summary>Recovers overlapping origins, including idle actors, without turning correction into velocity.</summary>
        public WorldPoint ResolvePenetration(WorldPoint point, float radius)
        {
            if (!point.Finite || !GeometryRules.Range(radius, .01f, 10) || radius >= Math.Min(HalfSize.X, HalfSize.Y))
                throw new ArgumentException("Invalid collision footprint.");
            if (CanStand(point, radius)) return point;
            var origin = point;
            for (int pass = 0; pass < 32; pass++)
            {
                point = Bounds.Clamp(point, radius);
                foreach (var obstacle in obstacles)
                    point += obstacle.PushOut(point, radius, .02f);
                if (CanStand(point, radius)) return point;
            }
            // Intersecting footprints can alternate their corrections. Search outwards from the
            // original contact, deterministically, rather than oscillating in a concave corner.
            float distance = .02f;
            float limit = HalfSize.Length * 2;
            while (distance <= limit)
            {
                for (int step = 0; step < 64; step++)
                {
                    double angle = step * Math.PI * 2 / 64;
                    var candidate = origin + new WorldPoint((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
                    if (CanStand(candidate, radius)) return candidate;
                }
                distance = distance * 1.15f + .02f;
            }
            throw new InvalidOperationException("The arena has no reachable free footprint for this actor.");
        }

        public bool TryFindStandableNear(WorldPoint center, float radius, float searchRadius, out WorldPoint point)
        {
            if (!center.Finite || !GeometryRules.Range(radius, .01f, 10) || !GeometryRules.Range(searchRadius, 0, 100))
                throw new ArgumentException("Invalid spawn search.");
            if (CanStand(center, radius))
            {
                point = center;
                return true;
            }

            for (int ring = 1; ring <= 16; ring++)
                for (int step = 0; step < 32; step++)
                {
                    double angle = step * Math.PI * 2 / 32;
                    float distance = searchRadius * ring / 16;
                    var candidate = center + new WorldPoint((float)Math.Cos(angle) * distance, (float)Math.Sin(angle) * distance);
                    if (CanStand(candidate, radius))
                    {
                        point = candidate;
                        return true;
                    }
                }

            point = default;
            return false;
        }

        private WorldPoint Slide(WorldPoint from, WorldPoint increment, float radius)
        {
            float fraction = FreeFraction(from, increment, radius);
            var contact = from + increment * fraction;
            var remaining = increment * (1 - fraction);
            if (remaining.LengthSquared < .00000001f)
                return contact;
            // Only a physical prop receives steering assistance; the map boundary remains a hard edge.
            var probe = contact + increment.Normalized * .002f;
            foreach (var obstacle in obstacles)
            {
                if (!obstacle.Overlaps(probe, radius))
                    continue;
                var normal = obstacle.ContactNormal(contact);
                var tangent = new WorldPoint(normal.Y, -normal.X);
                float along = WorldPoint.Dot(remaining, tangent);
                if (Math.Abs(along) < remaining.Length * .05f)
                {
                    // A head-on press chooses the nearer side; a centered tie consistently goes clockwise.
                    if (WorldPoint.Dot(contact - obstacle.Center, tangent) < -.001f)
                        tangent *= -1;
                }
                else if (along < 0)
                    tangent *= -1;
                float distance = Math.Max(Math.Abs(along), remaining.Length * .45f);
                var slide = tangent * distance;
                float available = FreeFraction(contact, slide, radius);
                if (available < .01f && Math.Abs(along) < remaining.Length * .05f)
                {
                    var alternate = slide * -1;
                    float other = FreeFraction(contact, alternate, radius);
                    if (other > available)
                    {
                        slide = alternate;
                        available = other;
                    }
                }

                return contact + slide * available;
            }

            var x = contact + new WorldPoint(remaining.X, 0);
            if (CanStand(x, radius))
                contact = x;
            var y = contact + new WorldPoint(0, remaining.Y);
            if (CanStand(y, radius))
                contact = y;
            return contact;
        }

        private float FreeFraction(WorldPoint from, WorldPoint delta, float radius)
        {
            if (CanStand(from + delta, radius))
                return 1;
            float low = 0, high = 1;
            for (int i = 0; i < 12; i++)
            {
                float middle = (low + high) * .5f;
                if (CanStand(from + delta * middle, radius))
                    low = middle;
                else
                    high = middle;
            }

            return low;
        }
    }
}
