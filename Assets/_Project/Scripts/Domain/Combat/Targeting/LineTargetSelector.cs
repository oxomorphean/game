using System.Collections.Generic;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Combat.Targeting
{
    public sealed class LineTargetSelector : ITargetSelector
    {
        public DeliveryShape Shape => DeliveryShape.LineRay;
        public IReadOnlyList<EntityId> Select(WorldState world, ActorEntity caster, DeliverySpec d, AimRequest aim)
        {
            var start = caster.Transform.Position;
            var end = start + aim.Direction.Normalized * d.Range;
            return TargetRules.Select(world, caster, d, t => TargetRules.SegmentDistance(t.Transform.Position, start, end) <= d.Radius + t.Transform.CollisionRadius);
        }
    }
}
