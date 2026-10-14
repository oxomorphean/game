using System.Collections.Generic;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Combat.Targeting
{
    /// <summary>Preflight aim candidates; swept impacts are resolved by CombatResolver each simulation tick.</summary>
    public sealed class ProjectileTargetSelector : ITargetSelector
    {
        public DeliveryShape Shape => DeliveryShape.Projectile;
        public IReadOnlyList<EntityId> Select(WorldState world, ActorEntity caster, DeliverySpec d, AimRequest aim) => new LineTargetSelector().Select(world, caster, d, aim);
    }
}
