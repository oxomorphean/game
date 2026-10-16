using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.World
{
    /// <summary>Derived from live world state: no sticky selection or movement threshold.</summary>
    public sealed class InteractionTargetSelector
    {
        public WorldNodeEntity Select(WorldState world) => Select(world, world.LocalPlayerId);

        public WorldNodeEntity Select(WorldState world, EntityId playerId)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (!world.Players.ContainsKey(playerId)) return null;
            var actor = world.Actors[playerId];
            if (!actor.CanAct)
                return null;
            WorldNodeEntity best = null;
            float bestDistance = float.PositiveInfinity, bestFacing = float.NegativeInfinity;
            foreach (var node in world.SpatialGrid.QueryNodes(actor.Transform.Position, Math.Min(10000, Math.Max(0, actor.Stats.Get(StatId.PickupRadius)))))
            {
                if (!world.Interactions.CanFocus(world, actor.Id, node.Id))
                    continue;
                var delta = node.InteractionPosition(actor.Transform.Position) - actor.Transform.Position;
                float distance = delta.LengthSquared;
                var direction = delta.Normalized;
                float facing = direction.X * actor.Transform.FacingDirection.X + direction.Y * actor.Transform.FacingDirection.Y;
                // Distance always wins after even a tiny step. Facing only resolves equal distances.
                bool tied = Math.Abs(distance - bestDistance) <= .000001f;
                if (best == null || (!tied && distance < bestDistance) ||
                    (tied && (facing > bestFacing || (facing == bestFacing && node.Id.CompareTo(best.Id) < 0))))
                {
                    best = node;
                    bestDistance = distance;
                    bestFacing = facing;
                }
            }

            return best;
        }
    }
}
