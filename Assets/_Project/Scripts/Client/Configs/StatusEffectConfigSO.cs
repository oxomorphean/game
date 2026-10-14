using System;
using System.Linq;
using LinhThu.Domain.Combat;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [CreateAssetMenu(menuName = "Linh Thu/Status Effect")]
    public sealed class StatusEffectConfigSO : ContentConfigSO
    {
        [Min(.001f)]
        public float duration = 5;
        [Min(0)]
        public float tickInterval;
        [Min(1)]
        public int maxStacks = 1;
        public StatusStackPolicy stackPolicy;
        public bool clearOnDeath = true;
        public StatModifierData[] modifiers = Array.Empty<StatModifierData>();
        [SerializeReference]
        public AbilityEffectData[] tickEffects = Array.Empty<AbilityEffectData>();
        public StatusEffectSpec ToSpec() => new StatusEffectSpec(Id, duration, tickInterval, maxStacks, stackPolicy, modifiers.Select(x => x.ToSpec()), tickEffects.Select(e => e != null ? e.ToSpec() : throw new InvalidOperationException(name + " has a null effect.")), clearOnDeath);
    }
}
