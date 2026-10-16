using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.Persistence
{
    // DTOs contain only data; no Unity objects, delegates, file paths or CLR type metadata.
    public sealed class GameSaveDto
    {
        public string SchemaId = "linhthu.foundation";
        public const int CurrentVersion = 3;
        public int SchemaVersion = CurrentVersion;
        public string MapDefinitionId;
        public double SimulationSeconds;
        public long TickIndex;
        public long NextEntityId;
        public int NextSourceId;
        public RandomStateDto Random;
        public long LocalPlayerId;
        public float ChunkSize = ChunkCoord.Size;
        public PointDto WorldCenter, WorldHalfSize;
        [JsonProperty(Required = Required.Always)]
        public List<PlayerSaveDto> Players = new List<PlayerSaveDto>();
        [JsonIgnore] public PlayerSaveDto Player => Players?.FirstOrDefault(p => p != null && p.ActorId == LocalPlayerId);
        public RulesSaveDto Rules;
        [JsonProperty(Required = Required.Always)]
        public List<WildBeastSaveDto> WildBeasts = new List<WildBeastSaveDto>();
        [JsonProperty(Required = Required.Always)]
        public List<ActorSaveDto> Actors = new List<ActorSaveDto>();
        [JsonProperty(Required = Required.Always)]
        public List<WorldNodeSaveDto> Nodes = new List<WorldNodeSaveDto>();
        [JsonProperty(Required = Required.Always)]
        public List<ProjectileSaveDto> Projectiles = new List<ProjectileSaveDto>();
    }

    public sealed class WildBeastSaveDto
    {
        public long Id;
        public string DefinitionId;
        public PointDto Home;
        public long? ActorId, TargetId, RewardOwnerId;
        public WildBeastAiState State;
        public int PatrolStep;
        public float IdleRemaining, PatrolRemaining, RespawnRemaining;
        public long PendingExperience;
        public bool PendingLoot;
    }

    public struct PointDto
    {
        public float X, Y;
        public PointDto(WorldPoint point)
        {
            X = point.X;
            Y = point.Y;
        }

        public WorldPoint ToPoint() => new WorldPoint(X, Y);
    }

    public sealed class RandomStateDto
    {
        public string AlgorithmId;
        public uint State;
    }

    public sealed class RulesSaveDto
    {
        public float DefenseScale, OffenseScale, ResistanceScale, CooldownReductionCap;
        [JsonProperty(Required = Required.Always)]
        public List<ElementSaveDto> Elements = new List<ElementSaveDto>();
        [JsonProperty(Required = Required.Always)]
        public List<FactionSaveDto> Hostilities = new List<FactionSaveDto>();
    }

    public sealed class ElementSaveDto
    {
        public ElementType Attack, Defense;
        public float Factor;
    }

    public sealed class FactionSaveDto
    {
        public ActorFaction From, To;
    }

    public sealed class PlayerSaveDto
    {
        public long ActorId;
        public int RealmSourceId;
        public string RealmId;
        public long CultivationExp;
        public int SubStage;
        public int InventoryCapacity;
        [JsonProperty(Required = Required.Always)]
        public List<ItemSaveDto> Inventory = new List<ItemSaveDto>();
        [JsonProperty(Required = Required.Always)]
        public List<EquipmentSaveDto> Equipment = new List<EquipmentSaveDto>();
        [JsonProperty(Required = Required.Always)]
        public List<BeastSaveDto> Beasts = new List<BeastSaveDto>();
    }

    public sealed class ItemSaveDto
    {
        public string DefinitionId;
        public int Count;
    }

    public sealed class EquipmentSaveDto
    {
        public string SlotId, ItemId;
        public int SourceId;
    }

    public sealed class BeastSaveDto
    {
        public long Id, OwnerId;
        public string DefinitionId, QualityId, BloodlineId;
        public float Bond;
        public BeastState State;
        public long? ActorId, AssignedFieldId;
        public int BondSourceId;
        public float? RestingHp, RestingQi;
    }

    public sealed class ActorSaveDto
    {
        public long Id;
        public string DefinitionId;
        public ActorFaction Faction;
        public long? OwnerId, LastDamagerId;
        public PointDto Position, Facing, Velocity, DesiredMovement;
        public float MovementSpeedFactor = 1;
        public DashSaveDto Dash;
        public AppearanceSaveDto Appearance;
        // Explicit idle state is retained on actors without a life activity.
        public LifeActivitySaveDto LifeActivity = new LifeActivitySaveDto();
        [JsonProperty(Required = Required.Always)]
        public Dictionary<StatId, float> BaseStats = new Dictionary<StatId, float>();
        public float Hp, Qi, Shield;
        public ActorActionState State;
        [JsonProperty(Required = Required.Always)]
        public List<ModifierSaveDto> ManualModifiers = new List<ModifierSaveDto>();
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, float> Cooldowns = new Dictionary<string, float>();
        public CastSaveDto Cast;
        [JsonProperty(Required = Required.Always)]
        public List<StatusSaveDto> Statuses = new List<StatusSaveDto>();
    }

    public sealed class DashSaveDto
    {
        public PointDto Direction;
        public float Duration, Remaining, PeakSpeed, CooldownRemaining;
    }

    public sealed class LifeActivitySaveDto
    {
        public float MeditationElapsed, FishingRemaining;
        public long? FishingNodeId;
        public PointDto FishingTarget;
    }

    public sealed class AppearanceSaveDto
    {
        public string OutfitId, HairId, EyeId;
        public ActorAppearance ToAppearance() => OutfitId == null && HairId == null && EyeId == null
            ? default : new ActorAppearance(OutfitId, HairId, EyeId);
    }

    public sealed class ModifierSaveDto
    {
        public StatId StatId;
        public ModifierType Type;
        public float Value;
        public int SourceId;
    }

    public sealed class CastSaveDto
    {
        public string TechniqueId;
        public PointDto Direction;
        public PointDto? LockedPoint;
        public long? TargetId;
        public float WindupRemaining, ChannelRemaining, UntilPulse;
        public bool Started;
    }

    public sealed class SourceSaveDto
    {
        public long? RewardOwnerId;
        public long ActorId;
        public ActorFaction Faction;
        [JsonProperty(Required = Required.Always)]
        public Dictionary<StatId, float> Stats = new Dictionary<StatId, float>();
    }

    public sealed class StatusSaveDto
    {
        public string DefinitionId;
        public int SourceId, StackCount;
        public long CasterId;
        public float RemainingDuration, UntilNextTick;
        public SourceSaveDto Source;
    }

    public sealed class WorldNodeSaveDto
    {
        public long Id;
        public string DefinitionId;
        public PointDto Position;
        public float GrowthTimer;
        public bool IsDepleted;
        public string CropDefinitionId;
        public long? AssignedBeastId;
    }

    public sealed class ProjectileSaveDto
    {
        public long Id;
        public string TechniqueId;
        public SourceSaveDto Source;
        public PointDto Position, Direction;
        public float RemainingLifetime;
        [JsonProperty(Required = Required.Always)]
        public List<long> HitTargets = new List<long>();
    }
}
