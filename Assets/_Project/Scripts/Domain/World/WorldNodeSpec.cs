using System;
using System.Collections.Generic;
using LinhThu.Domain.Core;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.World
{
    public enum WorldNodeKind
    {
        HerbNode,
        OreNode,
        SpiritEggNode,
        SpiritFieldPlot,
        TeleportPortal,
        FishingSpot
    }

    public sealed class WorldNodeSpec
    {
        public string DefinitionId { get; }

        public WorldNodeKind Kind { get; }

        public float InteractionRadius { get; }

        public float GrowthSeconds { get; }

        public float RespawnSeconds { get; }

        public IReadOnlyList<ItemStack> Rewards { get; }

        public string SeedItemId { get; }

        public string DestinationMapId { get; }

        public WorldPoint PortalDestination { get; }
        public float FishingBiteSeconds { get; }
        public WorldPoint FishingWaterOffset { get; }
        public IReadOnlyList<WorldPoint> FishingShoreline { get; }

        public WorldNodeSpec(string definitionId, WorldNodeKind kind, IEnumerable<ItemStack> rewards, float interactionRadius = 2, float growthSeconds = 1, float respawnSeconds = 1, string seedItemId = null, string destinationMapId = null, WorldPoint portalDestination = default, float fishingBiteSeconds = 4, WorldPoint fishingWaterOffset = default, IEnumerable<WorldPoint> fishingShoreline = null)
        {
            DefinitionId = Guard.Id(definitionId);
            Kind = Guard.EnumValue(kind);
            Rewards = Guard.Copy(rewards);
            InteractionRadius = Guard.Number(interactionRadius, nameof(interactionRadius), 0, 100);
            GrowthSeconds = Guard.Number(growthSeconds, nameof(growthSeconds), 0, 86400);
            RespawnSeconds = Guard.Number(respawnSeconds, nameof(respawnSeconds), 0, 86400);
            SeedItemId = seedItemId == null ? null : Guard.Id(seedItemId);
            DestinationMapId = destinationMapId == null ? null : Guard.Id(destinationMapId);
            if (!portalDestination.Finite)
                throw new ArgumentException(nameof(portalDestination));
            PortalDestination = portalDestination;
            FishingBiteSeconds = Guard.Number(fishingBiteSeconds, nameof(fishingBiteSeconds), .1f, 300);
            if (!fishingWaterOffset.Finite || fishingWaterOffset.Length > 10) throw new ArgumentException(nameof(fishingWaterOffset));
            FishingWaterOffset = fishingWaterOffset;
            FishingShoreline = Guard.Copy(fishingShoreline ?? Array.Empty<WorldPoint>());
            if (FishingShoreline.Count > 0)
            {
                var footprint = new WorldObstacle(FishingShoreline);
                if (kind != WorldNodeKind.FishingSpot || !footprint.Overlaps(fishingWaterOffset, 0))
                    throw new ArgumentException("A fishing shoreline must enclose its water target.");
            }
            if (kind == WorldNodeKind.FishingSpot && (Rewards.Count == 0 || (FishingShoreline.Count == 0 && fishingWaterOffset.LengthSquared < .01f)))
                throw new ArgumentException("Fishing requires a water target and reward.");
            if (kind == WorldNodeKind.TeleportPortal && DestinationMapId == null)
                throw new ArgumentException("Portal destination required.");
        }
    }
}
