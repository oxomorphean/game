using System;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Combat;

namespace LinhThu.Domain.Beasts
{
    public enum BeastAiState
    {
        FollowOwner,
        CombatAssist
    }

    public sealed class BeastAiController
    {
        private readonly EntityId beastId, ownerId;
        private readonly BeastAiSpec spec;
        public BeastAiState State { get; private set; }

        public BeastAiController(EntityId beastActorId, EntityId ownerActorId, BeastAiSpec spec)
        {
            beastId = beastActorId;
            ownerId = ownerActorId;
            this.spec = spec;
        }

        public void Tick(WorldState world, float deltaTime)
        {
            Guard.Delta(deltaTime);
            if (deltaTime == 0 || !world.TryGetActor(beastId, out var beast) || !world.TryGetActor(ownerId, out var owner) || !beast.CanAct || !owner.Vitals.IsAlive)
                return;
            beast.DesiredMovement = default;
            beast.MovementSpeedFactor = 1;
            if (beast.Dash.Active)
                return;
            float distance = (owner.Transform.Position - beast.Transform.Position).Length;
            if (distance > spec.LeashDistance && world.Geometry.TryFindStandableNear(owner.Transform.Position + new WorldPoint(-spec.FollowDistance, 0), beast.Transform.CollisionRadius, spec.FollowDistance, out var point))
            {
                beast.Transform.Position = point;
                beast.Transform.CurrentVelocity = default;
                distance = (owner.Transform.Position - point).Length;
            }

            var target = world.Actors.Values.Where(a => a.Id != beast.Id && a.Vitals.IsAlive && world.Relations.IsHostile(beast.Faction, a.Faction) && (a.Transform.Position - owner.Transform.Position).Length <= spec.SearchRadius && (a.Transform.Position - beast.Transform.Position).Length <= spec.LeashDistance && world.Geometry.ClearLine(beast.Transform.Position, a.Transform.Position)).OrderBy(a => owner.LastDamagerId.HasValue && a.Id == owner.LastDamagerId.Value ? 0 : 1).ThenBy(a => (a.Transform.Position - owner.Transform.Position).LengthSquared).ThenBy(a => a.Id).FirstOrDefault();
            if (target != null)
            {
                State = BeastAiState.CombatAssist;
                var offset = target.Transform.Position - beast.Transform.Position;
                bool inRange = false;
                foreach (var id in spec.AssistTechniqueIds)
                {
                    if (!world.Content.TryGetTechnique(id, out var technique))
                        continue;
                    if (offset.Length <= technique.Delivery.Range)
                    {
                        inRange = true;
                        var result = world.Combat.TryCast(world, beast.Id, id, new AimRequest(offset.LengthSquared < .000001f ? beast.Transform.FacingDirection : offset.Normalized, target.Id));
                        if (result.Succeeded)
                            break;
                    }
                }

                if (!inRange && beast.Abilities.Cast == null)
                    beast.DesiredMovement = offset.Normalized;
                return;
            }

            State = BeastAiState.FollowOwner;
            if (beast.Abilities.Cast != null)
                return;
            if (distance > spec.FollowDistance + .05f)
            {
                var offset = owner.Transform.Position - beast.Transform.Position;
                float speed = Math.Min(100, Math.Max(0, beast.Stats.Get(StatId.MoveSpeed)) * (distance > spec.LeashDistance ? spec.CatchUpMultiplier : 1));
                speed = Math.Min(speed, (distance - spec.FollowDistance) / deltaTime);
                beast.DesiredMovement = offset.Normalized;
                beast.MovementSpeedFactor = speed / Math.Max(.0001f, beast.Stats.Get(StatId.MoveSpeed));
            }
        }
    }
}
