using System.Collections.Generic;
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
        public int SchemaVersion = 1;
        public string MapDefinitionId;
        public double SimulationSeconds;
        public long TickIndex;
        public long NextEntityId;
        public int NextSourceId;
        public RandomStateDto Random;
        public PlayerSaveDto Player;
        public RulesSaveDto Rules;
        public List<ActorSaveDto> Actors = new List<ActorSaveDto>();
        public List<WorldNodeSaveDto> Nodes = new List<WorldNodeSaveDto>();
        public List<ProjectileSaveDto> Projectiles = new List<ProjectileSaveDto>();
        public PointDto DesiredMovement;
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
        public List<ElementSaveDto> Elements = new List<ElementSaveDto>();
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
        public List<ItemSaveDto> Inventory = new List<ItemSaveDto>();
        public List<EquipmentSaveDto> Equipment = new List<EquipmentSaveDto>();
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
        public PointDto Position, Facing;
        public Dictionary<StatId, float> BaseStats = new Dictionary<StatId, float>();
        public float Hp, Qi, Shield;
        public ActorActionState State;
        public List<ModifierSaveDto> ManualModifiers = new List<ModifierSaveDto>();
        public Dictionary<string, float> Cooldowns = new Dictionary<string, float>();
        public CastSaveDto Cast;
        public List<StatusSaveDto> Statuses = new List<StatusSaveDto>();
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
        public long? TargetId;
        public float WindupRemaining, ChannelRemaining, UntilPulse;
        public bool Started;
    }

    public sealed class SourceSaveDto
    {
        public long ActorId;
        public ActorFaction Faction;
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
        public List<long> HitTargets = new List<long>();
    }
}
