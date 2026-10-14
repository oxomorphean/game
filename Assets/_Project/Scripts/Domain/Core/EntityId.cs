using System;

namespace LinhThu.Domain.Core
{
    public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
    {
        public long Value { get; }

        public EntityId(long value)
        {
            if (value <= 0)
                throw new ArgumentException(nameof(value));
            Value = value;
        }

        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId id && Equals(id);
        public override int GetHashCode() => Value.GetHashCode();
        public int CompareTo(EntityId other) => Value.CompareTo(other.Value);
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static bool operator ==(EntityId a, EntityId b) => a.Equals(b);
        public static bool operator !=(EntityId a, EntityId b) => !a.Equals(b);
    }
}
