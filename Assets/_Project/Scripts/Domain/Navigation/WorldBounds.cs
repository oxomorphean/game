using System;

namespace LinhThu.Domain.Navigation
{
    /// <summary>Shared playable extent; horizontal growth does not change the ground's unit scale.</summary>
    public readonly struct WorldBounds
    {
        public WorldPoint Center { get; }
        public WorldPoint HalfSize { get; }
        public WorldPoint Min => Center - HalfSize;
        public WorldPoint Max => Center + HalfSize;
        public WorldBounds(WorldPoint halfSize) : this(default, halfSize) { }
        public WorldBounds(WorldPoint center, WorldPoint halfSize)
        {
            if (!center.Finite || !GeometryRules.Range(halfSize.X, 1, 1000) || !GeometryRules.Range(halfSize.Y, 1, 1000))
                throw new ArgumentException("Invalid world bounds.");
            Center = center; HalfSize = halfSize;
        }
        public bool Contains(WorldPoint point, float margin)
            => point.Finite && Math.Abs(point.X - Center.X) <= HalfSize.X - margin && Math.Abs(point.Y - Center.Y) <= HalfSize.Y - margin;
        public WorldPoint Clamp(WorldPoint point, float margin)
            => new WorldPoint(Math.Max(Min.X + margin, Math.Min(Max.X - margin, point.X)), Math.Max(Min.Y + margin, Math.Min(Max.Y - margin, point.Y)));
    }
}
