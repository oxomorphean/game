using System;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [CreateAssetMenu(menuName = "Linh Thu/Actor")]
    public sealed class ActorBaseConfigSO : ContentConfigSO
    {
        public ElementType primaryElement;
        public StatValueData[] baseStats = Array.Empty<StatValueData>();
        public TechniqueConfigSO[] abilities = Array.Empty<TechniqueConfigSO>();
        [Min(.01f)]
        public float collisionRadius = .22f;
        public Actors.ActorView prefab;
        public ActorSpec ToSpec() => new ActorSpec(Id, primaryElement, ConfigData.Stats(baseStats), ConfigData.Required(abilities, "actor abilities").Select(a => ConfigData.Required(a, "actor ability").Id), collisionRadius);
    }
}
