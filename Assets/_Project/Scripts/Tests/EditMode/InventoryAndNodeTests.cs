using System;
using System.Linq;
using NUnit.Framework;
using LinhThu.Domain.Core;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Persistence;
using LinhThu.Domain.World;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Beasts;

namespace LinhThu.Tests.EditMode
{
    public sealed class InventoryAndNodeTests
    {
        [Test]
        public void HarvestedNodeRemovesItsFootprintAndSaveRestoreKeepsWorldsIsolated()
        {
            var world = Fixture.World(nodes: new[] { new NodeSpawn("egg-node", new WorldPoint(.7f, 0)) });
            var egg = world.Nodes.Values.Single();
            var footprint = new WorldObstacle(new WorldPoint(.7f, .18f), new WorldPoint(.16f, .16f), false);
            world.Geometry.RegisterNodeObstacles(egg.Id, new[] { footprint });
            Assert.That(world.Geometry.CanStand(footprint.Center, .2f), Is.False);
            var mapper = new GameSaveMapper();
            var ready = mapper.Capture(world);
            Assert.That(world.Execute(new HarvestCommand(world.Player.ActorId, egg.Id)).Succeeded, Is.True);
            Assert.That(world.Geometry.CanStand(footprint.Center, .2f), Is.True);
            world.Motor.Displace(world.PlayerActor.Transform, footprint.Center - world.PlayerActor.Transform.Position);
            world.Events.Flush();
            var harvested = mapper.Capture(world);
            var restored = mapper.TryRestore(harvested, world.Content, world.Geometry, new SeededRandomSource(8), new DomainEventBus());
            Assert.That(restored.Succeeded, Is.True, restored.Message);
            Assert.That(restored.Value.Geometry.CanStand(footprint.Center, .2f), Is.True);
            Assert.That((restored.Value.PlayerActor.Transform.Position - footprint.Center).Length, Is.LessThan(.00001f));
            var restoredReady = mapper.TryRestore(ready, world.Content, world.Geometry, new SeededRandomSource(8), new DomainEventBus());
            Assert.That(restoredReady.Succeeded, Is.True, restoredReady.Message);
            Assert.That(restoredReady.Value.Geometry.CanStand(footprint.Center, .2f), Is.False);
            Assert.That(world.Geometry.CanStand(footprint.Center, .2f), Is.True, "Loading must not change live collision state.");
            ready.Nodes[0].GrowthTimer = -1;
            Assert.That(mapper.TryRestore(ready, world.Content, world.Geometry, new SeededRandomSource(8), new DomainEventBus()).Succeeded, Is.False);
            Assert.That(world.Geometry.CanStand(footprint.Center, .2f), Is.True);
        }

        [TestCase("Inventory")]
        [TestCase("Equipment")]
        [TestCase("Beasts")]
        public void SaveMustDistinguishMissingCollectionsFromExplicitlyEmptyOnes(string field)
        {
            var world = Fixture.World();
            world.Player.Inventory.TryAdd(new ItemStack("herb", 3));
            world.Player.Inventory.TryAdd(new ItemStack("robe", 1));
            world.Player.Equipment.TryEquip("body", "robe", world.Player.Inventory, world.PlayerActor);
            world.Events.Flush();
            var serializer = new SaveSerializer();
            var json = Newtonsoft.Json.Linq.JObject.Parse(serializer.Serialize(new GameSaveMapper().Capture(world)));
            var player = (Newtonsoft.Json.Linq.JObject)json["Players"][0];
            player.Remove(field);
            Assert.That(serializer.TryDeserialize(json.ToString()).ErrorCode, Is.EqualTo(DomainError.InvalidSave));
            player[field] = Newtonsoft.Json.Linq.JValue.CreateNull();
            Assert.That(serializer.TryDeserialize(json.ToString()).ErrorCode, Is.EqualTo(DomainError.InvalidSave));
            Assert.That(world.Player.Inventory.Count("herb"), Is.EqualTo(3));
            Assert.That(world.Player.Equipment.Slots["body"].ItemId, Is.EqualTo("robe"));

            var empty = Fixture.World();
            var emptyJson = Newtonsoft.Json.Linq.JObject.Parse(serializer.Serialize(new GameSaveMapper().Capture(empty)));
            ((Newtonsoft.Json.Linq.JObject)emptyJson["Actors"][0]).Remove("LifeActivity");
            var parsed = serializer.TryDeserialize(emptyJson.ToString());
            Assert.That(parsed.Succeeded, Is.True, parsed.Message);
            Assert.That(new GameSaveMapper().TryRestore(parsed.Value, empty.Content, empty.Geometry,
                new SeededRandomSource(1), new DomainEventBus()).Succeeded, Is.True);
        }

        [Test]
        public void InventoryAddAllAndRemoveAreAtomic()
        {
            var bag = new InventoryContainer(1, Fixture.Content());
            Assert.That(bag.TryAddAll(new[]{new ItemStack("herb", 1), new ItemStack("ore", 1)}).ErrorCode, Is.EqualTo(DomainError.CapacityExceeded));
            Assert.That(bag.Slots, Is.Empty);
            bag.TryAdd(new ItemStack("herb", 3));
            Assert.That(bag.TryRemove("herb", 4).Succeeded, Is.False);
            Assert.That(bag.Count("herb"), Is.EqualTo(3));
            Assert.That(bag.TryRemove("herb", 3).Succeeded, Is.True);
            Assert.That(bag.Slots, Is.Empty);
            Assert.Throws<ArgumentException>(() => new ItemStack("herb", 0));
        }

        [Test]
        public void StackMergeAndIntegerOverflowAreAtomic()
        {
            var c = Fixture.Content();
            var small = new InventoryContainer(2, c);
            small.TryAdd(new ItemStack("herb", 98));
            small.TryAdd(new ItemStack("herb", 3));
            Assert.That(small.Slots.Select(s => s.Count), Is.EqualTo(new[]{99, 2}));
            Assert.That(small.TryRemove("herb", -1).ErrorCode, Is.EqualTo(DomainError.InvalidInput));
            Assert.That(small.Count("herb"), Is.EqualTo(101));
            c.Add(new ItemSpec("stone", ItemCategory.SpiritStone, 1000000));
            var large = new InventoryContainer(3000, c);
            Assert.That(large.TryAdd(new ItemStack("stone", int.MaxValue)).Succeeded, Is.True);
            int slots = large.Slots.Count;
            Assert.That(large.TryAdd(new ItemStack("stone", 1)).ErrorCode, Is.EqualTo(DomainError.CapacityExceeded));
            Assert.That(large.Count("stone"), Is.EqualTo(int.MaxValue));
            Assert.That(large.Slots.Count, Is.EqualTo(slots));
        }

        [Test]
        public void HarvestHerbAndOreOnlyOnceThenRegrow()
        {
            var w = Fixture.World();
            var herb = w.Nodes.Values.Single(n => n.DefinitionId == "herb-node");
            var ore = w.Nodes.Values.Single(n => n.DefinitionId == "ore-node");
            Assert.That(w.Interactions.TryHarvest(w, w.Player.ActorId, herb.Id).Succeeded, Is.True);
            Assert.That(w.Interactions.TryHarvest(w, w.Player.ActorId, herb.Id).Succeeded, Is.False);
            Assert.That(w.Interactions.TryHarvest(w, w.Player.ActorId, ore.Id).Succeeded, Is.True);
            Assert.That(w.Player.Inventory.Count("herb"), Is.EqualTo(2));
            Assert.That(w.Player.Inventory.Count("ore"), Is.EqualTo(1));
            Fixture.Tick(w, 1);
            Assert.That(herb.IsReady, Is.True);
        }

        [Test]
        public void GameSaveJsonRoundTripPreservesRuntimeAndDoesNotDoubleDerivedStats()
        {
            var w = Fixture.World();
            w.Player.Inventory.TryAdd(new ItemStack("robe", 1));
            w.Player.Equipment.TryEquip("body", "robe", w.Player.Inventory, w.PlayerActor);
            var b = Fixture.Beast(w);
            w.Player.Beasts.TrySummon(b.Id, w);
            w.Player.Cultivation.AddExperience(25);
            w.Events.Flush();
            var serializer = new SaveSerializer();
            var mapper = new GameSaveMapper();
            float attack = w.PlayerActor.Stats.Get(LinhThu.Domain.Stats.StatId.Attack);
            for (int i = 0; i < 3; i++)
            {
                var parsed = serializer.TryDeserialize(serializer.Serialize(mapper.Capture(w)));
                Assert.That(parsed.Succeeded, Is.True, parsed.Message);
                var restored = mapper.TryRestore(parsed.Value, w.Content, w.Geometry, new SeededRandomSource(9), new DomainEventBus());
                Assert.That(restored.Succeeded, Is.True, restored.Message);
                w = restored.Value;
                Assert.That(w.PlayerActor.Stats.Get(LinhThu.Domain.Stats.StatId.Attack), Is.EqualTo(attack));
                Assert.That(w.Player.Cultivation.CurrentCultivationExp, Is.EqualTo(25));
                Assert.That(w.Player.Beasts.All.Single().ActorId.HasValue, Is.True);
            }

            Assert.That(serializer.TryDeserialize("broken").Succeeded, Is.False);
        }

        [Test]
        public void MultiRewardNodeCommitsAllOrNothingAndEmitsOnlyOnce()
        {
            var content = Fixture.Content();
            content.Add(new WorldNodeSpec("mixed", WorldNodeKind.HerbNode, new[]{new ItemStack("herb", 1), new ItemStack("ore", 1)}));
            var blocked = Fixture.World(content: content, capacity: 1, nodes: new[]{new NodeSpawn("mixed", default)});
            var node = blocked.Nodes.Values.Single();
            int events = 0;
            blocked.Events.Subscribe<NodeHarvestedEvent>(_ => events++);
            Assert.That(blocked.Execute(new HarvestCommand(blocked.Player.ActorId, node.Id)).ErrorCode, Is.EqualTo(DomainError.CapacityExceeded));
            Assert.That(blocked.Player.Inventory.Slots, Is.Empty);
            Assert.That(node.IsReady, Is.True);
            Assert.That(events, Is.Zero);
            var w = Fixture.World(content: content, capacity: 2, nodes: new[]{new NodeSpawn("mixed", default)});
            node = w.Nodes.Values.Single();
            w.Events.Subscribe<NodeHarvestedEvent>(_ => events++);
            Assert.That(w.Execute(new HarvestCommand(w.Player.ActorId, node.Id)).Succeeded, Is.True);
            Assert.That(w.Execute(new HarvestCommand(w.Player.ActorId, node.Id)).Succeeded, Is.False);
            Assert.That(w.Player.Inventory.Count("herb"), Is.EqualTo(1));
            Assert.That(w.Player.Inventory.Count("ore"), Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void FieldPlantGrowthAssignmentAndHarvestUseRuntimeState()
        {
            var w = Fixture.World();
            var field = w.Nodes.Values.Single(n => n.DefinitionId == "field");
            var beast = Fixture.Beast(w);
            w.Player.Inventory.TryAdd(new ItemStack("seed", 1));
            Assert.That(w.Interactions.TryPlant(w, w.Player.ActorId, field.Id, "herb-node").Succeeded, Is.True);
            Assert.That(w.Player.Inventory.Count("seed"), Is.Zero);
            Assert.That(w.Interactions.TryHarvest(w, w.Player.ActorId, field.Id).Succeeded, Is.False);
            Assert.That(w.Player.Beasts.TryAssignToField(beast.Id, field.Id, w).Succeeded, Is.True);
            Assert.That(beast.ActorId, Is.Null);
            Fixture.Tick(w, .9f);
            Assert.That(field.IsReady, Is.False);
            w.Tick(.1f);
            Assert.That(field.IsReady, Is.True);
            Assert.That(w.Interactions.TryHarvest(w, w.Player.ActorId, field.Id).Succeeded, Is.True);
            Assert.That(w.Player.Inventory.Count("herb"), Is.EqualTo(2));
            Assert.That(field.CropDefinitionId, Is.Null);
            Assert.That(field.IsReady, Is.False);
            Assert.That(w.Player.Beasts.TryUnassignFromField(beast.Id, w).Succeeded, Is.True);
        }

        [Test]
        public void EggNodeDepletesAndEggItemCreatesHatchableRosterEntry()
        {
            var w = Fixture.World(nodes: new[]{new NodeSpawn("egg-node", default)});
            var node = w.Nodes.Values.Single();
            Assert.That(w.Interactions.TryHarvest(w, w.Player.ActorId, node.Id).Succeeded, Is.True);
            Fixture.Tick(w, 2);
            Assert.That(node.IsReady, Is.False);
            Assert.That(w.Items.TryUse(w, w.Player.ActorId, "egg").Succeeded, Is.True);
            Assert.That(w.Player.Inventory.Count("egg"), Is.Zero);
            var beast = w.Player.Beasts.All.Single();
            Assert.That(beast.State, Is.EqualTo(BeastState.InEgg));
            Assert.That(w.Player.Beasts.TryHatch(beast.Id, w).Succeeded, Is.True);
        }

        [Test]
        public void FullVitalsAndKnownScrollDoNotConsumeItems()
        {
            var w = Fixture.World();
            w.Player.Inventory.TryAdd(new ItemStack("pill", 2));
            w.Player.Inventory.TryAdd(new ItemStack("scroll", 1));
            Assert.That(w.Items.TryUse(w, w.Player.ActorId, "pill").ErrorCode, Is.EqualTo(DomainError.NoEffect));
            Assert.That(w.Items.TryUse(w, w.Player.ActorId, "scroll").ErrorCode, Is.EqualTo(DomainError.NoEffect));
            Assert.That(w.Player.Inventory.Count("pill"), Is.EqualTo(2));
            Assert.That(w.Player.Inventory.Count("scroll"), Is.EqualTo(1));
            w.DealDamage(w.PlayerActor, w.Player.ActorId, 10);
            Assert.That(w.Items.TryUse(w, w.Player.ActorId, "pill").Succeeded, Is.True);
            Assert.That(w.PlayerActor.Vitals.CurrentHp, Is.EqualTo(200));
            Assert.That(w.Player.Inventory.Count("pill"), Is.EqualTo(1));
        }

        [Test]
        public void EquipmentSwapAndUnequipRejectWhenOldItemCannotFit()
        {
            var c = Fixture.Content();
            c.Add(new ItemSpec("stacked-robe", ItemCategory.Artifact, 2, "body", new[]{new StatModifierSpec(StatId.Attack, ModifierType.FlatAdd, 7)}));
            var w = Fixture.World(content: c, capacity: 1);
            var bag = w.Player.Inventory;
            var equipment = w.Player.Equipment;
            bag.TryAdd(new ItemStack("robe", 1));
            Assert.That(equipment.TryEquip("body", "robe", bag, w.PlayerActor).Succeeded, Is.True);
            bag.TryAdd(new ItemStack("stacked-robe", 2));
            int source = equipment.Slots["body"].SourceId;
            Assert.That(equipment.TryEquip("body", "stacked-robe", bag, w.PlayerActor).ErrorCode, Is.EqualTo(DomainError.CapacityExceeded));
            Assert.That(equipment.TryUnequip("body", bag, w.PlayerActor).ErrorCode, Is.EqualTo(DomainError.CapacityExceeded));
            Assert.That(bag.Count("stacked-robe"), Is.EqualTo(2));
            Assert.That(equipment.Slots["body"].ItemId, Is.EqualTo("robe"));
            Assert.That(w.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(28));
            bag.TryRemove("stacked-robe", 1);
            Assert.That(equipment.TryEquip("body", "stacked-robe", bag, w.PlayerActor).Succeeded, Is.True);
            Assert.That(equipment.Slots["body"].SourceId, Is.EqualTo(source));
            Assert.That(bag.Count("robe"), Is.EqualTo(1));
            Assert.That(w.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(32));
        }

        [Test]
        public void InteractionRejectsWrongOwnerDistanceWallAndBlockedPortalDestination()
        {
            var w = Fixture.World();
            var herb = w.Nodes.Values.Single(n => n.DefinitionId == "herb-node");
            Assert.That(w.Interactions.TryHarvest(w, new EntityId(999), herb.Id).ErrorCode, Is.EqualTo(DomainError.WrongOwner));
            w.Motor.Displace(w.PlayerActor.Transform, new WorldPoint(-5, 0));
            Assert.That(w.Interactions.TryHarvest(w, w.Player.ActorId, herb.Id).ErrorCode, Is.EqualTo(DomainError.OutOfRange));
            Assert.That(herb.IsReady, Is.True);
            var geo = new WorldGeometry(new WorldPoint(20, 20), new[]{new WorldObstacle(new WorldPoint(.5f, 0), new WorldPoint(.01f, 2), true), new WorldObstacle(new WorldPoint(5, 0), new WorldPoint(1, 1), true)});
            w = Fixture.World(geo);
            herb = w.Nodes.Values.Single(n => n.DefinitionId == "herb-node");
            var portal = w.Nodes.Values.Single(n => n.DefinitionId == "portal");
            Assert.That(w.Interactions.TryHarvest(w, w.Player.ActorId, herb.Id).ErrorCode, Is.EqualTo(DomainError.Blocked));
            var position = w.PlayerActor.Transform.Position;
            Assert.That(w.Interactions.TryUsePortal(w, w.Player.ActorId, portal.Id).ErrorCode, Is.EqualTo(DomainError.Blocked));
            Assert.That(w.PlayerActor.Transform.Position, Is.EqualTo(position));
            var clear = Fixture.World();
            portal = clear.Nodes.Values.Single(n => n.DefinitionId == "portal");
            Assert.That(clear.Interactions.TryUsePortal(clear, clear.Player.ActorId, portal.Id).Succeeded, Is.True);
            Assert.That(clear.PlayerActor.Transform.Position, Is.EqualTo(new WorldPoint(5, 0)));
        }
    }
}
