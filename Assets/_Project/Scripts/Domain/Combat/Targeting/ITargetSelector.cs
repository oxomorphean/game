using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Combat.Targeting
{
    public interface ITargetSelector
    {
        DeliveryShape Shape { get; }

        IReadOnlyList<EntityId> Select(WorldState world, ActorEntity caster, DeliverySpec delivery, AimRequest aim);
    }

    public sealed class TargetSelectorRegistry
    {
        private readonly Dictionary<DeliveryShape, ITargetSelector> selectors = new Dictionary<DeliveryShape, ITargetSelector>();
        public void Register(ITargetSelector selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            selectors.Add(selector.Shape, selector);
        }

        public ITargetSelector Resolve(DeliveryShape shape) => selectors.TryGetValue(shape, out var selector) ? selector : throw new ArgumentException("No target selector.");
    }

    internal static class TargetRules
    {
        internal static bool Matches(WorldState world, ActorFaction faction, EntityId casterId, ActorEntity target, TargetFilter filter)
        {
            if (!target.Vitals.IsAlive)
                return false;
            bool hostile = world.Relations.IsHostile(faction, target.Faction);
            return filter == TargetFilter.Any || (filter == TargetFilter.Hostile ? target.Id != casterId && hostile : !hostile);
        }

        internal static IReadOnlyList<EntityId> Select(WorldState world, ActorEntity caster, DeliverySpec d, Func<ActorEntity, bool> shape)
        {
            return world.SpatialGrid.QueryActors(caster.Transform.Position, d.Range + d.Radius)
                .Where(t => Matches(world, caster.Faction, caster.Id, t, d.TargetFilter) && shape(t)
                    && (d.Shape == DeliveryShape.Projectile
                        ? world.Geometry.ClearProjectileLine(caster.Transform.Position, t.Transform.Position)
                        : world.Geometry.ClearLine(caster.Transform.Position, t.Transform.Position)))
                .OrderBy(t => (t.Transform.Position - caster.Transform.Position).LengthSquared).ThenBy(t => t.Id).Take(d.MaxTargets).Select(t => t.Id).ToArray();
        }

        internal static float SegmentDistance(WorldPoint p, WorldPoint from, WorldPoint to)
        {
            var delta = to - from;
            float t = delta.LengthSquared < .000001f ? 0 : Math.Max(0, Math.Min(1, WorldPoint.Dot(p - from, delta) / delta.LengthSquared));
            return (p - (from + delta * t)).Length;
        }
    }
}
