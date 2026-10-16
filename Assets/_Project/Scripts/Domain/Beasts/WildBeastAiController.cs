using System;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Beasts
{
    public enum WildBeastAiState { Idle, Patrol, Chase, WindupCast, ExecuteAttack, LeashReturn, Respawning }

    /// <summary>One persistent spawn site. Neither simulation nor respawn depends on visibility.</summary>
    public sealed class WildBeastAiController
    {
        public EntityId Id { get; }
        public WildBeastSpec Spec { get; }
        public WorldPoint HomePosition { get; }
        public EntityId? ActorId { get; internal set; }
        public EntityId? TargetId { get; internal set; }
        public WildBeastAiState State { get; internal set; }
        public int PatrolStep { get; internal set; }
        public float IdleRemaining { get; internal set; } = 1;
        public float PatrolRemaining { get; internal set; }
        public float RespawnRemaining { get; internal set; }
        public EntityId? RewardOwnerId { get; internal set; }
        public long PendingExperience { get; internal set; }
        public bool PendingLoot { get; internal set; }
        public bool HasPendingReward => PendingExperience > 0 || PendingLoot;
        internal WildBeastAiController(EntityId id, WildBeastSpec spec, WorldPoint home) { Id = id; Spec = spec; HomePosition = home; }

        internal void Tick(WorldState world, float deltaTime)
        {
            if (State == WildBeastAiState.Respawning)
            {
                TryGrantReward(world);
                RespawnRemaining = Math.Max(0, RespawnRemaining - deltaTime);
                // A full bag holds this site's loot and respawn; it cannot accumulate an unbounded queue.
                if (RespawnRemaining <= .000001f && !HasPendingReward)
                {
                    var spawn = world.TrySpawnActor(Spec.ActorDefinitionId, Spec.Faction, HomePosition);
                    if (spawn.Succeeded)
                    {
                        ActorId = spawn.Value.Id; spawn.Value.WildBeast = this;
                        State = WildBeastAiState.Idle; IdleRemaining = 1; RewardOwnerId = null;
                    }
                }
                return;
            }
            if (!ActorId.HasValue || !world.TryGetActor(ActorId.Value, out var actor)) return;
            actor.DesiredMovement = default; actor.MovementSpeedFactor = 1;
            if (!actor.CanAct) return;
            var homeDelta = HomePosition - actor.Transform.Position;
            ActorEntity target = null;
            if (TargetId.HasValue) world.TryGetActor(TargetId.Value, out target);
            bool targetEscaped = target != null && (target.Transform.Position - HomePosition).Length > Spec.LeashRadius;
            if (homeDelta.Length > Spec.LeashRadius || targetEscaped) BeginReturn(actor);
            if (State == WildBeastAiState.LeashReturn)
            {
                actor.Vitals.Restore(VitalType.Hp, actor.Vitals.Maximum(VitalType.Hp) * deltaTime * .25f);
                if (homeDelta.Length <= .2f)
                {
                    actor.StopMotion(); actor.Vitals.Restore(VitalType.Hp, actor.Vitals.Maximum(VitalType.Hp));
                    State = WildBeastAiState.Idle; IdleRemaining = 1; TargetId = null;
                }
                else { actor.DesiredMovement = homeDelta.Normalized; actor.MovementSpeedFactor = 1.4f; }
                return;
            }
            if (actor.Abilities.Cast != null) { State = WildBeastAiState.WindupCast; return; }
            if (State == WildBeastAiState.WindupCast) { State = WildBeastAiState.ExecuteAttack; return; }
            if (target == null || !target.Vitals.IsAlive || !world.Relations.IsHostile(actor.Faction, target.Faction))
                target = world.SpatialGrid.QueryActors(actor.Transform.Position, Spec.AggroRadius)
                    .Where(a => a.Vitals.IsAlive && world.Relations.IsHostile(actor.Faction, a.Faction)
                        && (a.Faction == ActorFaction.Player || a.Faction == ActorFaction.PlayerBeast)
                        && (a.Transform.Position - actor.Transform.Position).Length <= Spec.AggroRadius
                        && (a.Transform.Position - HomePosition).Length <= Spec.LeashRadius
                        && world.Geometry.ClearLine(actor.Transform.Position, a.Transform.Position))
                    .OrderBy(a => (a.Transform.Position - actor.Transform.Position).LengthSquared).ThenBy(a => a.Id).FirstOrDefault();
            if (target != null)
            {
                TargetId = target.Id; State = WildBeastAiState.Chase;
                world.Content.TryGetTechnique(Spec.AttackId, out var technique);
                var offset = target.Transform.Position - actor.Transform.Position;
                if (offset.Length <= technique.Delivery.Range && world.Geometry.ClearLine(actor.Transform.Position, target.Transform.Position))
                {
                    var result = world.Combat.TryCast(world, actor.Id, Spec.AttackId, new AimRequest(offset.Normalized, target.Id));
                    if (result.Succeeded) State = WildBeastAiState.WindupCast;
                }
                else actor.DesiredMovement = offset.Normalized;
                return;
            }
            TargetId = null;
            if (State == WildBeastAiState.Chase || State == WildBeastAiState.ExecuteAttack) { BeginReturn(actor); return; }
            if (State == WildBeastAiState.Idle)
            {
                IdleRemaining = Math.Max(0, IdleRemaining - deltaTime);
                if (IdleRemaining > 0) return;
                PatrolStep = (PatrolStep + 1) % 4; PatrolRemaining = 5; State = WildBeastAiState.Patrol;
            }
            double angle = PatrolStep * Math.PI * .5;
            var waypoint = HomePosition + new WorldPoint((float)Math.Cos(angle), (float)Math.Sin(angle)) * Spec.PatrolRadius;
            var travel = waypoint - actor.Transform.Position;
            PatrolRemaining = Math.Max(0, PatrolRemaining - deltaTime);
            if (travel.Length <= .2f || PatrolRemaining == 0)
            { State = WildBeastAiState.Idle; IdleRemaining = 1.5f; }
            else { actor.DesiredMovement = travel.Normalized; actor.MovementSpeedFactor = .4f; }
        }
        private void BeginReturn(ActorEntity actor)
        {
            State = WildBeastAiState.LeashReturn; TargetId = null;
            actor.Abilities.Cast = null; actor.StopMotion(); actor.State = ActorActionState.Idle;
        }
        internal void Deplete(WorldState world, EntityId? rewardOwner)
        {
            if (State == WildBeastAiState.Respawning) return;
            var actor = ActorId; ActorId = null; TargetId = null;
            State = WildBeastAiState.Respawning; RespawnRemaining = Spec.RespawnSeconds;
            IdleRemaining = 0; PatrolRemaining = 0;
            RewardOwnerId = rewardOwner;
            PendingExperience = rewardOwner.HasValue ? Spec.Experience : 0;
            PendingLoot = rewardOwner.HasValue && Spec.Loot.Count > 0;
            if (actor.HasValue) world.RemoveActorInternal(actor.Value);
            TryGrantReward(world);
        }
        private void TryGrantReward(WorldState world)
        {
            if (!RewardOwnerId.HasValue || !world.Players.TryGetValue(RewardOwnerId.Value, out var player)) return;
            if (PendingExperience > 0 && player.Cultivation.AddExperience(PendingExperience).Succeeded)
            {
                world.Events.Publish(new CombatRewardGrantedEvent(player.ActorId, Id, PendingExperience, false));
                PendingExperience = 0;
            }
            if (PendingLoot && player.Inventory.TryAddAll(Spec.Loot).Succeeded)
            {
                PendingLoot = false;
                world.Events.Publish(new CombatRewardGrantedEvent(player.ActorId, Id, 0, true));
            }
        }
    }
}
