using System.Collections.Generic;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Combat.Targeting
{
    public sealed class CircleTargetSelector : ITargetSelector
    {
        public DeliveryShape Shape => DeliveryShape.CircleAoE;
        public IReadOnlyList<EntityId> Select(WorldState world, ActorEntity caster, DeliverySpec d, AimRequest aim)
        {
            var center = aim.LockedPoint ?? (aim.TargetId.HasValue && world.TryGetActor(aim.TargetId.Value, out var target) ? target.Transform.Position : caster.Transform.Position);
            return TargetRules.Select(world, caster, d, t => (t.Transform.Position - center).Length <= d.Radius + t.Transform.CollisionRadius && (t.Transform.Position - caster.Transform.Position).Length <= d.Range + d.Radius);
        }
    }
}
