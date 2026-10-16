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
                if (!world.Actors[c.ActorId].CanAct)
                    return DomainResult.Fail(DomainError.InvalidState);
                if (c.Direction.LengthSquared > 0) world.Actors[c.ActorId].CancelLifeActivity();
                world.Actors[c.ActorId].DesiredMovement = c.Direction.LengthSquared > 1 ? c.Direction.Normalized : c.Direction;
                return DomainResult.Ok();
            });
            Register<TryTameBeastCommand>(c => world.Taming.TryTame(world, c.ActorId, c.TargetId));
            Register<DashCommand>(c => world.TryDash(c.ActorId, c.Direction));
            Register<ToggleMeditateCommand>(c => world.LifeActivities.ToggleMeditate(c.ActorId));
            Register<FishCommand>(c => world.LifeActivities.Fish(c.ActorId, c.NodeId));
            Register<ChangeAppearanceCommand>(c =>
            {
                var actor = world.Actors[c.ActorId];
                if (!actor.CanAct)
                    return DomainResult.Fail(DomainError.InvalidState);
                if (actor.Spec.Appearance == null || !actor.Spec.Appearance.Supports(c.Appearance))
                    return DomainResult.Fail(DomainError.InvalidInput);
                actor.Appearance = c.Appearance;
                return DomainResult.Ok();
            });
            Register<CastCommand>(c => world.Combat.TryCast(world, c.ActorId, c.TechniqueId, c.Aim));
            Register<InteractCommand>(c => world.Interactions.TryInteractFocused(world, c.ActorId, c.CropId));
            Register<HarvestCommand>(c => world.Interactions.TryHarvest(world, c.ActorId, c.NodeId));
            Register<PlantCommand>(c => world.Interactions.TryPlant(world, c.ActorId, c.FieldId, c.CropId));
            Register<PortalCommand>(c => world.Interactions.TryUsePortal(world, c.ActorId, c.PortalId));
            Register<SummonCommand>(c => world.Players[c.ActorId].Beasts.TrySummon(c.BeastId, world));
            Register<RecallCommand>(c => world.Players[c.ActorId].Beasts.TryRecall(c.BeastId, world));
            Register<HatchCommand>(c => world.Players[c.ActorId].Beasts.TryHatch(c.BeastId, world));
            Register<AssignFieldCommand>(c => c.FieldId.HasValue ? world.Players[c.ActorId].Beasts.TryAssignToField(c.BeastId, c.FieldId.Value, world) : world.Players[c.ActorId].Beasts.TryUnassignFromField(c.BeastId, world));
            Register<UseItemCommand>(c => world.Items.TryUse(world, c.ActorId, c.ItemId));
            Register<EquipCommand>(c => world.Players[c.ActorId].Equipment.TryEquip(c.SlotId, c.ItemId, world.Players[c.ActorId].Inventory, world.Actors[c.ActorId]));
            Register<UnequipCommand>(c => world.Players[c.ActorId].Equipment.TryUnequip(c.SlotId, world.Players[c.ActorId].Inventory, world.Actors[c.ActorId]));
            Register<BreakthroughCommand>(c => world.Cultivation.TryBreakthrough(world.Actors[c.ActorId], world.Players[c.ActorId].Cultivation, world.Players[c.ActorId].RealmSourceId));
        }

        public void Register<T>(Func<T, DomainResult> handler)
            where T : WorldCommand => handlers.Add(typeof(T), c => handler((T)c));
        public DomainResult Execute(WorldCommand command)
        {
            if (command == null)
                return DomainResult.Fail(DomainError.InvalidInput);
            if (!world.Players.ContainsKey(command.ActorId))
                return DomainResult.Fail(DomainError.WrongOwner);
            return handlers.TryGetValue(command.GetType(), out var handler) ? handler(command) : DomainResult.Fail(DomainError.InvalidInput, "Unregistered command.");
        }
    }
}
