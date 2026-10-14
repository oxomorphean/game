using System;
using System.Collections.Generic;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Combat.Targeting
{
    public sealed class ConeTargetSelector : ITargetSelector
    {
        public DeliveryShape Shape => DeliveryShape.ConeSector;
        public IReadOnlyList<EntityId> Select(WorldState world, ActorEntity caster, DeliverySpec d, AimRequest aim)
        {
            return TargetRules.Select(world, caster, d, t =>
            {
                var offset = t.Transform.Position - caster.Transform.Position;
                return offset.Length <= d.Range + t.Transform.CollisionRadius && (offset.LengthSquared < .000001f || WorldPoint.Dot(offset.Normalized, aim.Direction.Normalized) >= Math.Cos(d.AngleDegrees * Math.PI / 360));
            });
        }
    }
}
