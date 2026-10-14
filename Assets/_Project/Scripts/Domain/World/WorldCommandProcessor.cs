using System;
using System.Collections.Generic;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.World
{
    public sealed class WorldCommandProcessor
    {
        private readonly WorldState world;
        private readonly Dictionary<Type, Func<WorldCommand, DomainResult>> handlers = new Dictionary<Type, Func<WorldCommand, DomainResult>>();
        public WorldCommandProcessor(WorldState world)
        {
            this.world = world;
            Register<MoveCommand>(c =>
            {
                if (!c.Direction.Finite)
                    return DomainResult.Fail(DomainError.InvalidInput);
                if (!world.PlayerActor.CanAct)
                    return DomainResult.Fail(DomainError.InvalidState);
                world.desiredMovement = c.Direction.LengthSquared > 1 ? c.Direction.Normalized : c.Direction;
                return DomainResult.Ok();
            });
            Register<CastCommand>(c => world.Combat.TryCast(world, c.ActorId, c.TechniqueId, c.Aim));
            Register<InteractCommand>(c => world.Interactions.TryInteractFocused(world, c.ActorId, c.CropId));
            Register<HarvestCommand>(c => world.Interactions.TryHarvest(world, c.ActorId, c.NodeId));
            Register<PlantCommand>(c => world.Interactions.TryPlant(world, c.ActorId, c.FieldId, c.CropId));
            Register<PortalCommand>(c => world.Interactions.TryUsePortal(world, c.ActorId, c.PortalId));
            Register<SummonCommand>(c => world.Player.Beasts.TrySummon(c.BeastId, world));
            Register<RecallCommand>(c => world.Player.Beasts.TryRecall(c.BeastId, world));
            Register<HatchCommand>(c => world.Player.Beasts.TryHatch(c.BeastId, world));
            Register<AssignFieldCommand>(c => c.FieldId.HasValue ? world.Player.Beasts.TryAssignToField(c.BeastId, c.FieldId.Value, world) : world.Player.Beasts.TryUnassignFromField(c.BeastId, world));
            Register<UseItemCommand>(c => world.Items.TryUse(world, c.ActorId, c.ItemId));
            Register<EquipCommand>(c => world.Player.Equipment.TryEquip(c.SlotId, c.ItemId, world.Player.Inventory, world.PlayerActor));
            Register<UnequipCommand>(c => world.Player.Equipment.TryUnequip(c.SlotId, world.Player.Inventory, world.PlayerActor));
            Register<BreakthroughCommand>(c => world.Cultivation.TryBreakthrough(world.PlayerActor, world.Player.Cultivation, world.Player.RealmSourceId));
        }

        public void Register<T>(Func<T, DomainResult> handler)
            where T : WorldCommand => handlers.Add(typeof(T), c => handler((T)c));
        public DomainResult Execute(WorldCommand command)
        {
            if (command == null)
                return DomainResult.Fail(DomainError.InvalidInput);
            if (command.ActorId != world.Player.ActorId)
                return DomainResult.Fail(DomainError.WrongOwner);
            return handlers.TryGetValue(command.GetType(), out var handler) ? handler(command) : DomainResult.Fail(DomainError.InvalidInput, "Unregistered command.");
        }
    }
}
