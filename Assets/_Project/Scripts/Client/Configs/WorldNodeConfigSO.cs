using System;
using System.Linq;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [Serializable]
    public struct ItemAmountData
    {
        public ItemConfigSO item;
        [Min(1)]
        public int count;
        public ItemStack ToStack() => new ItemStack(ConfigData.Required(item, "item reward").Id, count);
    }

    [CreateAssetMenu(menuName = "Linh Thu/World Node")]
    public sealed class WorldNodeConfigSO : ContentConfigSO
    {
        public WorldNodeKind kind;
        public float interactionRadius = 1.4f, growthSeconds = 12, respawnSeconds = 10;
        public ItemAmountData[] rewards = Array.Empty<ItemAmountData>();
        public ItemConfigSO seedItem;
        public string destinationMapId;
        public Vector2 portalDestination;
        [Min(.1f)] public float fishingBiteSeconds = 4;
        public Vector2 fishingWaterOffset;
        [Tooltip("Convex bank outline relative to the node, matching the pond's ground footprint. Empty supports a single shore point.")]
        public Vector2[] fishingShoreline = Array.Empty<Vector2>();
        public World.WorldNodeView prefab;
        public WorldNodeSpec ToSpec() => new WorldNodeSpec(Id, kind, rewards.Select(r => r.ToStack()), interactionRadius, growthSeconds, respawnSeconds, seedItem != null ? seedItem.Id : null, string.IsNullOrWhiteSpace(destinationMapId) ? null : destinationMapId, new WorldPoint(portalDestination.x, portalDestination.y), fishingBiteSeconds, new WorldPoint(fishingWaterOffset.x, fishingWaterOffset.y), fishingShoreline.Select(p => new WorldPoint(p.x, p.y)));
    }
}
