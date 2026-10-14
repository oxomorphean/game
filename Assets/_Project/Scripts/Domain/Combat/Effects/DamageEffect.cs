using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.Combat.Effects
{
    public sealed class DamageEffectSpec : AbilityEffectSpec
    {
        public StatId ScalingStat { get; }

        public float Coefficient { get; }

        public float FlatDamage { get; }

        public ElementType Element { get; }

        public DamageEffectSpec(StatId scalingStat, float coefficient, float flatDamage, ElementType element)
        {
            ScalingStat = Guard.EnumValue(scalingStat);
            Coefficient = Guard.Number(coefficient, nameof(coefficient), 0, 1000);
            FlatDamage = Guard.Number(flatDamage, nameof(flatDamage));
            Element = Guard.EnumValue(element);
        }
    }

    public sealed class DamageEffect : IAbilityEffect
    {
        public Type SpecType => typeof(DamageEffectSpec);
        public DomainResult Validate(AbilityEffectSpec spec, AbilityExecutionContext context) => DomainResult.Ok();
        public void Apply(AbilityEffectSpec spec, AbilityExecutionContext context)
        {
            if (context.Target == null || !context.Target.Vitals.IsAlive)
                return;
            var result = context.World.Combat.CalculateDamage(context.Source, context.Target, (DamageEffectSpec)spec);
            context.World.DealDamage(context.Target, context.Source.ActorId, result.Amount * context.Stacks);
        }
    }
}
