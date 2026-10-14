using System;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Combat.Effects
{
    public enum DisplacementTarget
    {
        Caster,
        Target
    }

    public sealed class DisplacementEffectSpec : AbilityEffectSpec
    {
        public float Distance { get; }

        public DisplacementTarget Target { get; }

        public DisplacementEffectSpec(float distance, DisplacementTarget target)
        {
            Distance = Guard.Number(distance, nameof(distance), 0, 100);
            Target = Guard.EnumValue(target);
        }
    }

    public sealed class DisplacementEffect : IAbilityEffect
    {
        public Type SpecType => typeof(DisplacementEffectSpec);
        public DomainResult Validate(AbilityEffectSpec spec, AbilityExecutionContext context) => DomainResult.Ok();
        public void Apply(AbilityEffectSpec spec, AbilityExecutionContext context)
        {
            var value = (DisplacementEffectSpec)spec;
            var actor = value.Target == DisplacementTarget.Caster ? context.Caster : context.Target;
            if (actor != null && actor.Vitals.IsAlive)
                context.World.Motor.Displace(actor.Transform, context.Direction.Normalized * (value.Distance * context.Stacks));
        }
    }
}
