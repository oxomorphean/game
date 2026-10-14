using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Actors;

namespace LinhThu.Domain.World
{
    public abstract class WorldCommand
    {
        public EntityId ActorId { get; }

        protected WorldCommand(EntityId actorId)
        {
            ActorId = actorId;
        }
    }

    public sealed class MoveCommand : WorldCommand
    {
        public WorldPoint Direction { get; }

        public MoveCommand(EntityId actor, WorldPoint direction): base(actor)
        {
            Direction = direction;
        }
    }

    public sealed class CastCommand : WorldCommand
    {
        public string TechniqueId { get; }

        public AimRequest Aim { get; }

        public CastCommand(EntityId actor, string technique, AimRequest aim): base(actor)
        {
            TechniqueId = technique;
            Aim = aim;
        }
    }

    public sealed class DashCommand : WorldCommand
    {
        public WorldPoint Direction { get; }
        public DashCommand(EntityId actorId, WorldPoint direction) : base(actorId) => Direction = direction;
    }

    public sealed class ChangeAppearanceCommand : WorldCommand
    {
        public ActorAppearance Appearance { get; }
        public ChangeAppearanceCommand(EntityId actorId, ActorAppearance appearance) : base(actorId) => Appearance = appearance;
    }

    public sealed class ToggleMeditateCommand : WorldCommand
    {
        public ToggleMeditateCommand(EntityId actorId) : base(actorId) { }
    }

    public sealed class FishCommand : WorldCommand
    {
        public EntityId? NodeId { get; }
        public FishCommand(EntityId actorId, EntityId? nodeId = null) : base(actorId) => NodeId = nodeId;
    }

    public sealed class InteractCommand : WorldCommand
    {
        public string CropId { get; }

        public InteractCommand(EntityId actor, string cropId = null) : base(actor)
        {
            CropId = cropId;
        }
    }

    public sealed class HarvestCommand : WorldCommand
    {
        public EntityId NodeId { get; }

        public HarvestCommand(EntityId actor, EntityId node): base(actor)
        {
            NodeId = node;
        }
    }

    public sealed class PlantCommand : WorldCommand
    {
        public EntityId FieldId { get; }

        public string CropId { get; }

        public PlantCommand(EntityId actor, EntityId field, string crop): base(actor)
        {
            FieldId = field;
            CropId = crop;
        }
    }

    public sealed class PortalCommand : WorldCommand
    {
        public EntityId PortalId { get; }

        public PortalCommand(EntityId actor, EntityId portal): base(actor)
        {
            PortalId = portal;
        }
    }

    public sealed class SummonCommand : WorldCommand
    {
        public EntityId BeastId { get; }

        public SummonCommand(EntityId actor, EntityId beast): base(actor)
        {
            BeastId = beast;
        }
    }

    public sealed class RecallCommand : WorldCommand
    {
        public EntityId BeastId { get; }

        public RecallCommand(EntityId actor, EntityId beast): base(actor)
        {
            BeastId = beast;
        }
    }

    public sealed class HatchCommand : WorldCommand
    {
        public EntityId BeastId { get; }

        public HatchCommand(EntityId actor, EntityId beast): base(actor)
        {
            BeastId = beast;
        }
    }

    public sealed class AssignFieldCommand : WorldCommand
    {
        public EntityId BeastId { get; }

        public EntityId? FieldId { get; }

        public AssignFieldCommand(EntityId actor, EntityId beast, EntityId? field): base(actor)
        {
            BeastId = beast;
            FieldId = field;
        }
    }

    public sealed class UseItemCommand : WorldCommand
    {
        public string ItemId { get; }

        public UseItemCommand(EntityId actor, string item): base(actor)
        {
            ItemId = item;
        }
    }

    public sealed class EquipCommand : WorldCommand
    {
        public string SlotId { get; }

        public string ItemId { get; }

        public EquipCommand(EntityId actor, string slot, string item): base(actor)
        {
            SlotId = slot;
            ItemId = item;
        }
    }

    public sealed class UnequipCommand : WorldCommand
    {
        public string SlotId { get; }

        public UnequipCommand(EntityId actor, string slot): base(actor)
        {
            SlotId = slot;
        }
    }

    public sealed class BreakthroughCommand : WorldCommand
    {
        public BreakthroughCommand(EntityId actor): base(actor)
        {
        }
    }
}
