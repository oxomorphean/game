using System;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.World
{
    /// <summary>Derived from live world state: no sticky selection or movement threshold.</summary>
    public sealed class InteractionTargetSelector
    {
        public WorldNodeEntity Select(WorldState world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            var actor = world.PlayerActor;
            if (!actor.CanAct)
                return null;
            WorldNodeEntity best = null;
            float bestDistance = float.PositiveInfinity, bestFacing = float.NegativeInfinity;
            foreach (var node in world.Nodes.Values)
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
