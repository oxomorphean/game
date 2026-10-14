using System;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Combat.Effects
{
    public sealed class ApplyStatusEffectSpec : AbilityEffectSpec
    {
        public string StatusDefinitionId { get; }

        public ApplyStatusEffectSpec(string statusDefinitionId)
        {
            StatusDefinitionId = Guard.Id(statusDefinitionId);
        }
    }

    public sealed class ApplyStatusEffect : IAbilityEffect
    {
        public Type SpecType => typeof(ApplyStatusEffectSpec);
        public DomainResult Validate(AbilityEffectSpec spec, AbilityExecutionContext context) => context.World.Content.TryGetStatus(((ApplyStatusEffectSpec)spec).StatusDefinitionId, out _) ? DomainResult.Ok() : DomainResult.Fail(DomainError.MissingDefinition);
        public void Apply(AbilityEffectSpec spec, AbilityExecutionContext context)
        {
            if (context.Target == null || !context.Target.Vitals.IsAlive)
                return;
            context.World.Content.TryGetStatus(((ApplyStatusEffectSpec)spec).StatusDefinitionId, out var definition);
            var result = context.Target.StatusEffects.Apply(definition, context.Source.ActorId, context.Source);
            if (!result.Succeeded)
                throw new InvalidOperationException("Status application failed: " + result.ErrorCode + " " + result.Message);
        }
    }
}
