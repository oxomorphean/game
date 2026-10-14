using System;
using System.Linq;
using LinhThu.Domain.Combat;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [CreateAssetMenu(menuName = "Linh Thu/Technique")]
    public sealed class TechniqueConfigSO : ContentConfigSO
    {
        public DeliveryShape shape;
        public TargetFilter targetFilter = TargetFilter.Hostile;
        [Min(.01f)]
        public float range = 4;
        [Min(0)]
        public float radius = .2f;
        [Range(0, 360)]
        public float angleDegrees = 90;
        [Min(.01f)]
        public float projectileSpeed = 8;
        [Min(1)]
        public int maxTargets = 1;
        [Min(0)]
        public float qiCost = 5, cooldown = 1, castDuration, channelDuration, channelInterval;
        [SerializeReference]
        public AbilityEffectData[] effects = Array.Empty<AbilityEffectData>();
        public Color effectColor = new Color(.7f, .9f, 1);
        public TechniqueSpec ToSpec() => new TechniqueSpec(Id, new DeliverySpec(shape, range, radius, angleDegrees, projectileSpeed, maxTargets, targetFilter), effects.Select(e => e != null ? e.ToSpec() : throw new InvalidOperationException(name + " has a null effect.")), qiCost, cooldown, castDuration, channelDuration, channelInterval);
    }
}
