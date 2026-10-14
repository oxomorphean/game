using System;
using System.Linq;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Stats;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [CreateAssetMenu(menuName = "Linh Thu/Beast Species")]
    public sealed class BeastSpeciesConfigSO : ContentConfigSO
    {
        public ActorBaseConfigSO actor;
        public float followDistance = 1, leashDistance = 8, searchRadius = 5, catchUpMultiplier = 2;
        public TechniqueConfigSO[] assistTechniques = Array.Empty<TechniqueConfigSO>();
        public StatId[] bondStats = {StatId.Attack, StatId.Defense};
        [Min(0)]
        public float bondScale = 1;
        public bool restoreVitalsOnSummon = true;
        public string initialQualityId = "common", initialBloodlineId = "huyen-quy";
        [Range(0, 1)]
        public float initialBond = .5f;
        public BeastSpeciesSpec ToSpec() => new BeastSpeciesSpec(Id, ConfigData.Required(actor, "beast actor").Id, new BeastAiSpec(followDistance, leashDistance, searchRadius, catchUpMultiplier, assistTechniques.Select(t => ConfigData.Required(t, "assist technique").Id)), bondStats, bondScale, restoreVitalsOnSummon);
    }
}
