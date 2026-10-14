using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.Combat
{
    public enum DeliveryShape
    {
        CircleAoE,
        ConeSector,
        LineRay,
        Projectile,
        Self
    }

    public enum TargetFilter
    {
        Hostile,
        Ally,
        Any
    }

    public sealed class DeliverySpec
    {
        public DeliveryShape Shape { get; }

        public float Range { get; }

        public float Radius { get; }

        public float AngleDegrees { get; }

        public float ProjectileSpeed { get; }

        public int MaxTargets { get; }

        public TargetFilter TargetFilter { get; }

        public DeliverySpec(DeliveryShape shape, float range = 3, float radius = .25f, float angleDegrees = 90, float projectileSpeed = 10, int maxTargets = 1, TargetFilter targetFilter = TargetFilter.Hostile)
        {
            Shape = Guard.EnumValue(shape);
            Range = Guard.Number(range, nameof(range), .01f, 100);
            Radius = Guard.Number(radius, nameof(radius), 0, 100);
            AngleDegrees = Guard.Number(angleDegrees, nameof(angleDegrees), 0, 360);
            ProjectileSpeed = Guard.Number(projectileSpeed, nameof(projectileSpeed), .01f, 1000);
            if (maxTargets < 1 || maxTargets > 1000)
                throw new ArgumentException(nameof(maxTargets));
            MaxTargets = maxTargets;
            TargetFilter = Guard.EnumValue(targetFilter);
        }
    }

    public readonly struct AimRequest
    {
        public WorldPoint Direction { get; }

        public EntityId? TargetId { get; }

        public AimRequest(WorldPoint direction, EntityId? targetId = null)
        {
            Direction = direction;
            TargetId = targetId;
        }
    }
}
