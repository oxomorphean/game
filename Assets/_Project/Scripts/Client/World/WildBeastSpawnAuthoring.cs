using LinhThu.Client.Configs;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Navigation;
using UnityEngine;

namespace LinhThu.Client.World
{
    public sealed class WildBeastSpawnAuthoring : MonoBehaviour
    {
        public WildBeastConfigSO config;
        public WildBeastSeed ToSeed()
        {
            if (config == null) throw new System.InvalidOperationException("Wild spawn requires a config.");
            return new WildBeastSeed(config.Id, new WorldPoint(transform.position.x, transform.position.y));
        }
    }
}
