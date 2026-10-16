using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;
using LinhThu.Domain.Combat.Effects;
using LinhThu.Domain.Combat.Targeting;

namespace LinhThu.Domain.Combat
{
    public sealed class CombatResolver
    {
        private readonly IDomainContent content;
        private readonly CombatRules rules;
        private readonly AbilityEffectRegistry effects;
        private readonly TargetSelectorRegistry targeting;
        private readonly IRandomSource random;
        private readonly DomainEventBus events;
        internal readonly List<ProjectileState> projectiles = new List<ProjectileState>();
        public IReadOnlyList<ProjectileState> Projectiles => projectiles.AsReadOnly();
        public CombatResolver(IDomainContent content, CombatRules rules, AbilityEffectRegistry effects, TargetSelectorRegistry targeting, IRandomSource random, DomainEventBus events)
        {
            this.content = content;
            this.rules = rules;
            this.effects = effects;
            this.targeting = targeting;
            this.random = random;
            this.events = events;
        }

        public DomainResult TryCast(WorldState world, EntityId casterId, string techniqueId, AimRequest aim)
        {
            if (!world.TryGetActor(casterId, out var caster))
                return DomainResult.Fail(DomainError.NotFound);
            if (!caster.CanAct || caster.Abilities.Cast != null || caster.Dash.Active)
                return DomainResult.Fail(DomainError.InvalidState);
            if (!caster.Abilities.Knows(techniqueId) || !content.TryGetTechnique(techniqueId, out var spec))
                return DomainResult.Fail(DomainError.MissingDefinition);
            if (caster.Abilities.GetCooldownRemaining(techniqueId) > .000001f)
                return DomainResult.Fail(DomainError.OnCooldown);
            if (!aim.Direction.Finite || aim.Direction.LengthSquared < .000001f)
                return DomainResult.Fail(DomainError.InvalidInput);
            if (caster.Vitals.CurrentQi < spec.QiCost)
                return DomainResult.Fail(DomainError.InsufficientResource);
            if (aim.TargetId.HasValue && spec.Delivery.Shape != DeliveryShape.Self)
            {
                if (!world.TryGetActor(aim.TargetId.Value, out var target) || !TargetRules.Matches(world, caster.Faction, caster.Id, target, spec.Delivery.TargetFilter))
                    return DomainResult.Fail(DomainError.InvalidInput);
                if ((target.Transform.Position - caster.Transform.Position).Length > spec.Delivery.Range + target.Transform.CollisionRadius)
                    return DomainResult.Fail(DomainError.OutOfRange);
                bool clear = spec.Delivery.Shape == DeliveryShape.Projectile
                    ? world.Geometry.ClearProjectileLine(caster.Transform.Position, target.Transform.Position)
                    : world.Geometry.ClearLine(caster.Transform.Position, target.Transform.Position);
                if (!clear)
                    return DomainResult.Fail(DomainError.Blocked);
            }

            // Circle windups lock their ground target at acceptance; moving away must evade them.
            if (spec.Delivery.Shape == DeliveryShape.CircleAoE)
            {
                var center = aim.TargetId.HasValue && world.TryGetActor(aim.TargetId.Value, out var aimed) ? aimed.Transform.Position : caster.Transform.Position;
                aim = new AimRequest(aim.Direction, aim.TargetId, center);
            }
            else aim = new AimRequest(aim.Direction, aim.TargetId);
            var source = EffectSourceSnapshot.Capture(caster);
            var ctx = new AbilityExecutionContext(world, caster, null, aim.Direction, source);
            var validation = ValidateEffects(spec.Effects, ctx, new HashSet<string>());
            if (!validation.Succeeded)
                return validation;
            var targets = targeting.Resolve(spec.Delivery.Shape).Select(world, caster, spec.Delivery, aim);
            foreach (var id in targets)
            {
                world.TryGetActor(id, out var target);
                var c = new AbilityExecutionContext(world, caster, target, aim.Direction, source);
                var result = ValidateEffects(spec.Effects, c, new HashSet<string>());
                if (!result.Succeeded)
                    return result;
            }

            caster.CancelLifeActivity();
            caster.Vitals.TrySpend(VitalType.Qi, spec.QiCost);
            caster.Abilities.StartCooldown(techniqueId, spec.Cooldown * (1 - Math.Max(0, Math.Min(rules.CooldownReductionCap, caster.Stats.Get(StatId.CooldownReduction)))));
            caster.Abilities.Cast = new CastState(spec, aim);
            caster.Transform.CurrentVelocity = default;
            caster.Transform.FacingDirection = aim.Direction.Normalized;
            caster.State = ActorActionState.Casting;
            events.Publish(new AbilityStartedEvent(caster.Id, techniqueId, aim.Direction));
            if (spec.CastDuration == 0)
                AdvanceCast(world, caster, 0);
            return DomainResult.Ok();
        }

        internal DomainResult ValidateEffects(IReadOnlyList<AbilityEffectSpec> specs, AbilityExecutionContext context, HashSet<string> visiting, bool validateApplication = true)
        {
            var statuses = new List<StatusEffectSpec>();
            foreach (var spec in specs)
            {
                if (!effects.Supports(spec.GetType()))
                    return DomainResult.Fail(DomainError.MissingDefinition, "Effect handler not registered.");
                var result = effects.Resolve(spec.GetType()).Validate(spec, context);
                if (!result.Succeeded)
                    return result;
                if (spec is ApplyStatusEffectSpec status)
                {
                    if (!visiting.Add(status.StatusDefinitionId) || visiting.Count > 16)
                        return DomainResult.Fail(DomainError.InvalidInput, "Cyclic periodic status.");
                    content.TryGetStatus(status.StatusDefinitionId, out var definition);
                    var nested = ValidateEffects(definition.TickEffects, context, visiting, false);
                    visiting.Remove(status.StatusDefinitionId);
                    if (!nested.Succeeded)
                        return nested;
                    statuses.Add(definition);
                }
            }

            if (validateApplication && statuses.Count > 0 && context.Target != null && context.Target.Vitals.IsAlive)
                return context.Target.StatusEffects.ValidateApplications(statuses, context.Source.ActorId);
            return DomainResult.Ok();
        }

        public void ApplyEffects(IReadOnlyList<AbilityEffectSpec> specs, AbilityExecutionContext context)
        {
            foreach (var spec in specs)
                effects.Resolve(spec.GetType()).Apply(spec, context);
        }

        public DamageResult CalculateDamage(ActorEntity caster, ActorEntity target, DamageEffectSpec spec) => CalculateDamage(EffectSourceSnapshot.Capture(caster), target, spec);
        internal DamageResult CalculateDamage(EffectSourceSnapshot source, ActorEntity target, DamageEffectSpec spec)
        {
            double raw = Math.Max(0, spec.FlatDamage + (double)source.Get(spec.ScalingStat) * spec.Coefficient);
            double amount = Math.Max(0, raw - Math.Max(0, target.Stats.Get(StatId.Defense)) * rules.DefenseScale);
            if (spec.Element != ElementType.Physical)
            {
                var affinity = ElementInteractionTable.Affinity(spec.Element);
                amount *= rules.Elements.Get(spec.Element, target.Spec.PrimaryElement) * Math.Max(0, 1 + (double)source.Get(affinity) * rules.OffenseScale) / (1 + Math.Max(0, (double)target.Stats.Get(affinity)) * rules.ResistanceScale);
            }

            float chance = Math.Max(0, Math.Min(1, source.Get(StatId.CritRate)));
            bool crit = chance >= 1 || (chance > 0 && random.Next01() < chance);
            if (crit)
                amount *= Math.Max(1, source.Get(StatId.CritDamage));
            return new DamageResult((float)Math.Min(1e9, amount), crit);
        }

        public void Tick(WorldState world, float deltaTime)
        {
            Guard.Delta(deltaTime);
            // Advance to combat boundaries, then commit the effects at that instant.
            // A newly applied/refreshed status is aged only by the time after its hit.
            float remaining = deltaTime;
            while (remaining > 0)
            {
                float step = Math.Min(remaining, Math.Max(.000001f, TimeToNextEvent(world, remaining)));
                foreach (var actor in world.Actors.Values.OrderBy(a => a.Id).ToArray())
                    actor.StatusEffects.Tick(step, world);
                TickStep(world, step);
                remaining = Math.Max(0, remaining - step);
            }
        }

        private float TimeToNextEvent(WorldState world, float limit)
        {
            foreach (var actor in world.Actors.Values)
            {
                limit = actor.StatusEffects.TimeToNextEvent(limit);
                var cast = actor.Abilities.Cast;
                if (cast != null && actor.CanAct)
                    limit = Math.Min(limit, cast.Started ? Math.Min(cast.UntilPulse, cast.ChannelRemaining) : cast.WindupRemaining);
            }
            foreach (var projectile in projectiles)
            {
                content.TryGetTechnique(projectile.TechniqueId, out var spec);
                limit = Math.Min(limit, projectile.RemainingLifetime);
                var to = projectile.Position + projectile.Direction * (limit * spec.Delivery.ProjectileSpeed);
                float travelTime = limit;
                foreach (var target in world.SpatialGrid.QueryActorsAlong(projectile.Position, to, spec.Delivery.Radius))
                {
                    if (projectile.hits.Contains(target.Id) || !TargetRules.Matches(world, projectile.Source.Faction,
                        projectile.Source.ActorId, target, spec.Delivery.TargetFilter) || target.Id == projectile.Source.ActorId)
                        continue;
                    if (SweepCircle(projectile.Position, to, target.Transform.Position,
                        spec.Delivery.Radius + target.Transform.CollisionRadius, out float fraction))
                    {
                        var contact = projectile.Position + (to - projectile.Position) * fraction;
                        if (world.Geometry.ClearProjectileLine(projectile.Position, contact) && world.Geometry.ClearProjectileLine(contact, target.Transform.Position))
                            limit = Math.Min(limit, travelTime * fraction);
                    }
                }
            }
            return limit;
        }

        private void TickStep(WorldState world, float deltaTime)
        {
            if (deltaTime == 0)
                return;
            var spawnedTimes = new Dictionary<EntityId, float>();
            foreach (var actor in world.Actors.Values.OrderBy(x => x.Id).ToArray())
                AdvanceCast(world, actor, deltaTime, spawnedTimes);
            foreach (var p in projectiles.ToArray())
            {
                content.TryGetTechnique(p.TechniqueId, out var spec);
                var d = spec.Delivery;
                float available = spawnedTimes.TryGetValue(p.Id, out float remainder) ? remainder : deltaTime;
                float time = Math.Min(available, p.RemainingLifetime);
                if (time <= 0)
                    continue;
                var from = p.Position;
                var to = from + p.Direction * (time * d.ProjectileSpeed);
                var impacts = new List<(ActorEntity actor, float fraction)>();
                foreach (var target in world.SpatialGrid.QueryActorsAlong(from, to, d.Radius))
                {
                    if (p.hits.Contains(target.Id) || target.Id == p.Source.ActorId || !TargetRules.Matches(world, p.Source.Faction, p.Source.ActorId, target, d.TargetFilter))
                        continue;
                    if (SweepCircle(from, to, target.Transform.Position, d.Radius + target.Transform.CollisionRadius, out var fraction))
                        impacts.Add((target, fraction));
                }

                foreach (var hit in impacts.OrderBy(h => h.fraction).ThenBy(h => h.actor.Id))
                {
                    var point = from + (to - from) * hit.fraction;
                    if (!world.Geometry.ClearProjectileLine(from, point))
                        break;
                    // The swept radii can touch before the projectile reaches a thin wall.
                    // A contact on this side must not damage an actor on the other side.
                    if (!world.Geometry.ClearProjectileLine(point, hit.actor.Transform.Position))
                        continue;
                    world.TryGetActor(p.Source.ActorId, out var caster);
                    ApplyEffects(spec.Effects, new AbilityExecutionContext(world, caster, hit.actor, p.Direction, p.Source));
                    p.hits.Add(hit.actor.Id);
                    if (p.hits.Count >= d.MaxTargets)
                        break;
                }

                p.RemainingLifetime -= time;
                p.Position = to;
                if (p.RemainingLifetime <= .000001f || p.hits.Count >= d.MaxTargets || !world.Geometry.ClearProjectileLine(from, to) || !world.Geometry.Bounds.Contains(to, 0))
                    projectiles.Remove(p);
            }
        }

        private void AdvanceCast(WorldState world, ActorEntity actor, float delta, Dictionary<EntityId, float> spawnedTimes = null)
        {
            var cast = actor.Abilities.Cast;
            if (cast == null)
                return;
            if (!actor.CanAct)
            {
                actor.Abilities.Cast = null;
                return;
            }

            content.TryGetTechnique(cast.TechniqueId, out var spec);
            if (!cast.Started)
            {
                float spent = Math.Min(delta, cast.WindupRemaining);
                cast.WindupRemaining -= spent;
                delta -= spent;
                if (cast.WindupRemaining > .000001f)
                    return;
                cast.Started = true;
                ExecutePulse(world, actor, spec, cast.Aim, delta, spawnedTimes);
                events.Publish(new AbilityExecutedEvent(actor.Id, spec.DefinitionId));
                if (!actor.CanAct)
                {
                    actor.Abilities.Cast = null;
                    return;
                }

                if (spec.ChannelDuration == 0)
                {
                    actor.Abilities.Cast = null;
                    if (actor.CanAct)
                        actor.State = ActorActionState.Idle;
                    return;
                }

                actor.State = ActorActionState.Channeling;
            }

            float elapsed = Math.Min(delta, cast.ChannelRemaining);
            cast.UntilPulse -= elapsed;
            cast.ChannelRemaining -= elapsed;
            while (cast.UntilPulse <= .000001f && actor.CanAct)
            {
                ExecutePulse(world, actor, spec, cast.Aim, Math.Max(0, delta - elapsed - cast.UntilPulse), spawnedTimes);
                cast.UntilPulse += spec.ChannelInterval;
            }

            if (cast.ChannelRemaining <= .000001f || !actor.CanAct)
            {
                actor.Abilities.Cast = null;
                if (actor.CanAct)
                    actor.State = ActorActionState.Idle;
            }
        }

        private void ExecutePulse(WorldState world, ActorEntity caster, TechniqueSpec spec, AimRequest aim, float remainingTick, Dictionary<EntityId, float> spawnedTimes)
        {
            var source = EffectSourceSnapshot.Capture(caster);
            if (spec.Delivery.Shape == DeliveryShape.Projectile)
            {
                var projectile = new ProjectileState(world.Ids.Allocate(), spec, source, caster.Transform.Position, aim.Direction);
                projectiles.Add(projectile);
                if (spawnedTimes != null)
                    spawnedTimes.Add(projectile.Id, remainingTick);
                return;
            }

            foreach (var id in targeting.Resolve(spec.Delivery.Shape).Select(world, caster, spec.Delivery, aim))
                if (world.TryGetActor(id, out var target))
                    ApplyEffects(spec.Effects, new AbilityExecutionContext(world, caster, target, aim.Direction, source));
        }

        private static bool SweepCircle(WorldPoint from, WorldPoint to, WorldPoint center, float radius, out float fraction)
        {
            var offset = from - center;
            var delta = to - from;
            float c = offset.LengthSquared - radius * radius;
            if (c <= 0)
            {
                fraction = 0;
                return true;
            }

            float a = delta.LengthSquared, b = WorldPoint.Dot(offset, delta), disc = b * b - a * c;
            if (a < .00000001f || disc < 0)
            {
                fraction = 0;
                return false;
            }

            fraction = (-b - (float)Math.Sqrt(disc)) / a;
            return fraction >= 0 && fraction <= 1;
        }
    }
}
