using System;
using System.Linq;
using LinhThu.Domain.Cultivation;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [CreateAssetMenu(menuName = "Linh Thu/Realm")]
    public sealed class RealmConfigSO : ContentConfigSO
    {
        public RealmConfigSO nextRealm;
        public long[] subStageThresholds = {0, 20, 50, 100};
        public long breakthroughCost = 100;
        public StatModifierData[] modifiers = Array.Empty<StatModifierData>();
        public RealmSpec ToSpec() => new RealmSpec(Id, nextRealm != null ? nextRealm.Id : null, subStageThresholds, breakthroughCost, modifiers.Select(x => x.ToSpec()));
    }
}
