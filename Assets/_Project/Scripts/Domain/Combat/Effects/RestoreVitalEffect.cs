using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.Combat.Effects
{
    public sealed class RestoreVitalEffectSpec : AbilityEffectSpec
    {
        public VitalType Vital { get; }

        public float Amount { get; }

        public RestoreVitalEffectSpec(VitalType vital, float amount)
        {
            Vital = Guard.EnumValue(vital);
            Amount = Guard.Number(amount, nameof(amount));
        }
    }

    public sealed class RestoreVitalEffect : IAbilityEffect
    {
        public Type SpecType => typeof(RestoreVitalEffectSpec);
        public DomainResult Validate(AbilityEffectSpec spec, AbilityExecutionContext context) => DomainResult.Ok();
        public void Apply(AbilityEffectSpec spec, AbilityExecutionContext context)
        {
            var value = (RestoreVitalEffectSpec)spec;
            context.Target?.Vitals.Restore(value.Vital, value.Amount * context.Stacks);
        }
    }
}
