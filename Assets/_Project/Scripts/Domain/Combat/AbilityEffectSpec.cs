using System;
using System.Collections.Generic;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Combat
{
    public abstract class AbilityEffectSpec
    {
    }

    public sealed class EffectSourceSnapshot
    {
        public EntityId ActorId { get; }

        public ActorFaction Faction { get; }

        public IReadOnlyDictionary<StatId, float> Stats { get; }

        public EffectSourceSnapshot(EntityId actorId, ActorFaction faction, IReadOnlyDictionary<StatId, float> stats)
        {
            if (actorId.Value <= 0 || stats == null)
                throw new ArgumentException(nameof(actorId));
            ActorId = actorId;
            Faction = Guard.EnumValue(faction);
            var copy = new Dictionary<StatId, float>();
            foreach (var stat in stats)
                copy.Add(Guard.EnumValue(stat.Key), Guard.Number(stat.Value, "source stat", -float.MaxValue, float.MaxValue));
            Stats = new System.Collections.ObjectModel.ReadOnlyDictionary<StatId, float>(copy);
        }

        public float Get(StatId stat) => Stats.TryGetValue(stat, out var value) ? value : 0;
        public static EffectSourceSnapshot Capture(ActorEntity actor)
        {
            var stats = new Dictionary<StatId, float>();
            foreach (StatId stat in Enum.GetValues(typeof(StatId)))
                stats.Add(stat, actor.Stats.Get(stat));
            return new EffectSourceSnapshot(actor.Id, actor.Faction, stats);
        }
    }

    public sealed class AbilityExecutionContext
    {
        public WorldState World { get; }

        public ActorEntity Caster { get; }

        public ActorEntity Target { get; }

        public WorldPoint Direction { get; }

        public EffectSourceSnapshot Source { get; }

        public int Stacks { get; }

        public AbilityExecutionContext(WorldState world, ActorEntity caster, ActorEntity target, WorldPoint direction, EffectSourceSnapshot source, int stacks = 1)
        {
            World = world;
            Caster = caster;
            Target = target;
            Direction = direction;
            Source = source;
            Stacks = stacks;
        }
    }

    public interface IAbilityEffect
    {
        Type SpecType { get; }

        DomainResult Validate(AbilityEffectSpec spec, AbilityExecutionContext context);
        void Apply(AbilityEffectSpec spec, AbilityExecutionContext context);
    }

    public sealed class AbilityEffectRegistry
    {
        private readonly Dictionary<Type, IAbilityEffect> handlers = new Dictionary<Type, IAbilityEffect>();
        public void Register(IAbilityEffect handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            handlers.Add(handler.SpecType, handler);
        }

        public IAbilityEffect Resolve(Type type) => handlers.TryGetValue(type, out var handler) ? handler : throw new ArgumentException("Unregistered ability effect: " + type.Name);
        public bool Supports(Type type) => handlers.ContainsKey(type);
    }

    public readonly struct DamageResult
    {
        public float Amount { get; }

        public bool Critical { get; }

        internal DamageResult(float amount, bool critical)
        {
            Amount = amount;
            Critical = critical;
        }
    }
}
