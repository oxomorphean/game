using System;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Combat.Effects;
using LinhThu.Domain.Stats;

namespace LinhThu.Client.Configs
{
    [Serializable]
    public abstract class AbilityEffectData
    {
        public abstract AbilityEffectSpec ToSpec();
    }

    [Serializable]
    public sealed class DamageEffectData : AbilityEffectData
    {
        public StatId scalingStat = StatId.Attack;
        public float coefficient = 1;
        public float flatDamage;
        public ElementType element = ElementType.Physical;
        public override AbilityEffectSpec ToSpec() => new DamageEffectSpec(scalingStat, coefficient, flatDamage, element);
    }

    [Serializable]
    public sealed class ApplyStatusEffectData : AbilityEffectData
    {
        public StatusEffectConfigSO status;
        public override AbilityEffectSpec ToSpec() => new ApplyStatusEffectSpec(ConfigData.Required(status, "status effect").Id);
    }

    [Serializable]
    public sealed class DisplacementEffectData : AbilityEffectData
    {
        public float distance = 2;
        public DisplacementTarget target = DisplacementTarget.Caster;
        public override AbilityEffectSpec ToSpec() => new DisplacementEffectSpec(distance, target);
    }

    [Serializable]
    public sealed class RestoreVitalEffectData : AbilityEffectData
    {
        public VitalType vital;
        public float amount = 20;
        public override AbilityEffectSpec ToSpec() => new RestoreVitalEffectSpec(vital, amount);
    }
}
