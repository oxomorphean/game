using System;
using System.Collections.Generic;

namespace LinhThu.Domain.Navigation
{
    public readonly struct ChunkCoord : IEquatable<ChunkCoord>, IComparable<ChunkCoord>
    {
        public const float Size = 16f;
        public readonly int X, Y;
        public ChunkCoord(int x, int y) { X = x; Y = y; }
        public static ChunkCoord FromWorld(WorldPoint point)
        {
            if (!point.Finite) throw new ArgumentException(nameof(point));
            return new ChunkCoord((int)Math.Floor(point.X / Size), (int)Math.Floor(point.Y / Size));
        }
        public IEnumerable<ChunkCoord> Neighbors(int radius)
        {
            if (radius < 0 || radius > 128) throw new ArgumentOutOfRangeException(nameof(radius));
            for (int y = Y - radius; y <= Y + radius; y++)
                for (int x = X - radius; x <= X + radius; x++) yield return new ChunkCoord(x, y);
        }
        internal static IEnumerable<ChunkCoord> Cover(WorldPoint min, WorldPoint max)
        {
            var first = FromWorld(min); var last = FromWorld(max);
            for (int y = first.Y; y <= last.Y; y++)
                for (int x = first.X; x <= last.X; x++) yield return new ChunkCoord(x, y);
        }
        public bool Equals(ChunkCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is ChunkCoord other && Equals(other);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
        public int CompareTo(ChunkCoord other) => Y != other.Y ? Y.CompareTo(other.Y) : X.CompareTo(other.X);
        public override string ToString() => $"({X}, {Y})";
        public static bool operator ==(ChunkCoord a, ChunkCoord b) => a.Equals(b);
        public static bool operator !=(ChunkCoord a, ChunkCoord b) => !a.Equals(b);
    }
}
