using System;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Core;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Persistence;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using NUnit.Framework;

namespace LinhThu.Tests.EditMode
{
    public sealed class LifeActivityTests
    {
        private static WorldState Create(int capacity = 16)
        {
            var content = Fixture.Content();
            content.Add(new ItemSpec("fish", ItemCategory.Fish));
            content.Add(new WorldNodeSpec("pool", WorldNodeKind.FishingSpot, new[] { new ItemStack("fish", 1) },
                respawnSeconds: 2, fishingBiteSeconds: 4, fishingWaterOffset: new WorldPoint(1, 0)));
            return Fixture.World(content: content, capacity: capacity, nodes: new[] { new NodeSpawn("pool", new WorldPoint(1, 0)) });
        }

        [Test]
        public void MeditationHasExtraQiAndDiscreteExperienceAndStopsWhenToggled()
        {
            var world = Create(); var actor = world.PlayerActor;
            actor.Stats.SetBase(StatId.QiRegen, 2);
            actor.Vitals.TrySpend(VitalType.Qi, 20);
            Assert.That(world.Execute(new ToggleMeditateCommand(actor.Id)).Succeeded, Is.True);
            world.Execute(new MoveCommand(actor.Id, default));
            Fixture.Tick(world, .5f);
            Assert.That(actor.State, Is.EqualTo(ActorActionState.Meditating));
            Assert.That(actor.Vitals.CurrentQi, Is.EqualTo(3).Within(.001f));
            Assert.That(world.Player.Cultivation.CurrentCultivationExp, Is.Zero);
            Fixture.Tick(world, 1.5f);
            Assert.That(actor.Vitals.CurrentQi, Is.EqualTo(12).Within(.001f));
            Assert.That(world.Player.Cultivation.CurrentCultivationExp, Is.EqualTo(2));
            world.Execute(new ToggleMeditateCommand(actor.Id));
            Fixture.Tick(world, 1);
            Assert.That(actor.Vitals.CurrentQi, Is.EqualTo(14).Within(.001f));
            Assert.That(world.Player.Cultivation.CurrentCultivationExp, Is.EqualTo(2));
            Assert.That(actor.Stats.Get(StatId.QiRegen), Is.EqualTo(2), "No accumulated stat modifier.");
        }

        [TestCase(false, "move")][TestCase(true, "move")]
        [TestCase(false, "cast")][TestCase(true, "cast")]
        [TestCase(false, "dash")][TestCase(true, "dash")]
        [TestCase(false, "damage")][TestCase(true, "damage")]
        [TestCase(false, "stagger")][TestCase(true, "stagger")]
        public void MovementAndCombatCancelActivityAndClearPendingReward(bool fishing, string interruption)
        {
            var world = Create(); var actor = world.PlayerActor;
            Assert.That(world.Execute(fishing ? (WorldCommand)new FishCommand(actor.Id) : new ToggleMeditateCommand(actor.Id)).Succeeded, Is.True);
            Fixture.Tick(world, .2f);
            if (interruption == "move") world.Execute(new MoveCommand(actor.Id, new WorldPoint(1, 0)));
            if (interruption == "cast") Assert.That(world.Execute(new CastCommand(actor.Id, "windup", new AimRequest(new WorldPoint(1, 0)))).Succeeded, Is.True);
            if (interruption == "dash") Assert.That(world.Execute(new DashCommand(actor.Id, new WorldPoint(1, 0))).Succeeded, Is.True);
            if (interruption == "damage") world.DealDamage(actor, actor.Id, 1);
            if (interruption == "stagger") actor.SetStaggered(true);
            Assert.That(actor.HasLifeActivity, Is.False);
            Assert.That(actor.LifeActivity.FishingNodeId, Is.Null);
            Assert.That(actor.LifeActivity.MeditationElapsed, Is.Zero);
            Fixture.Tick(world, 5);
            Assert.That(world.Player.Inventory.Count("fish"), Is.Zero);
            Assert.That(world.Player.Cultivation.CurrentCultivationExp, Is.Zero);
            if (interruption == "move") Assert.That(actor.Transform.CurrentVelocity.Length, Is.GreaterThan(0));
        }

        [Test]
        public void FishingWaitsForBiteManualReelAwardsOnceAndEarlyPullCancels()
        {
            var world = Create(); var actor = world.PlayerActor; var spot = world.Nodes.Values.Single();
            Assert.That(world.Execute(new HarvestCommand(actor.Id, spot.Id)).ErrorCode, Is.EqualTo(DomainError.InvalidState));
            Assert.That(world.Execute(new InteractCommand(actor.Id)).Succeeded, Is.True);
            Assert.That(actor.Transform.FacingDirection, Is.EqualTo(new WorldPoint(1, 0)));
            Fixture.Tick(world, 1);
            world.Execute(new FishCommand(actor.Id));
            Assert.That(actor.State, Is.EqualTo(ActorActionState.Idle));
            Assert.That(world.Player.Inventory.Count("fish"), Is.Zero);
            world.Execute(new FishCommand(actor.Id));
            Fixture.Tick(world, 4);
            Assert.That(actor.LifeActivity.FishBiting, Is.True);
            Assert.That(world.Player.Inventory.Count("fish"), Is.Zero, "Bite is not an automatic inventory grant.");
            Assert.That(world.Execute(new InteractCommand(actor.Id)).Succeeded, Is.True);
            Assert.That(world.Player.Inventory.Count("fish"), Is.EqualTo(1));
            Assert.That(actor.State, Is.EqualTo(ActorActionState.Idle));
            Assert.That(world.Execute(new FishCommand(actor.Id, spot.Id)).Succeeded, Is.False);
            Assert.That(world.Player.Inventory.Count("fish"), Is.EqualTo(1));
        }

        [Test]
        public void FullBagKeepsBiteAndSpotUntilAtomicHarvestSucceeds()
        {
            var world = Create(1); var actor = world.PlayerActor;
            world.Player.Inventory.TryAddAll(new[] { new ItemStack("herb", 99) });
            world.Execute(new FishCommand(actor.Id)); Fixture.Tick(world, 4);
            Assert.That(world.Execute(new FishCommand(actor.Id)).ErrorCode, Is.EqualTo(DomainError.CapacityExceeded));
            Assert.That(actor.LifeActivity.FishBiting, Is.True);
            Assert.That(world.Nodes.Values.Single().IsReady, Is.True);
            world.Player.Inventory.TryRemove("herb", 99);
            Assert.That(world.Execute(new FishCommand(actor.Id)).Succeeded, Is.True);
            Assert.That(world.Player.Inventory.Count("fish"), Is.EqualTo(1));
        }

        [Test]
        public void InvalidCommandsAndWrongOwnerPreserveActivityAndFishingRequiresShore()
        {
            var world = Create(); var actor = world.PlayerActor;
            world.Execute(new ToggleMeditateCommand(actor.Id));
            Assert.That(world.Execute(new CastCommand(actor.Id, "missing", new AimRequest(new WorldPoint(1, 0)))).Succeeded, Is.False);
            Assert.That(world.Execute(new MoveCommand(actor.Id, new WorldPoint(float.NaN, 0))).Succeeded, Is.False);
            Assert.That(world.Execute(new FishCommand(new EntityId(999))).ErrorCode, Is.EqualTo(DomainError.WrongOwner));
            Assert.That(actor.State, Is.EqualTo(ActorActionState.Meditating));
            world.Execute(new MoveCommand(actor.Id, new WorldPoint(-1, 0))); Fixture.Tick(world, 1);
            Assert.That(world.Execute(new FishCommand(actor.Id)).ErrorCode, Is.EqualTo(DomainError.OutOfRange));
        }

        [TestCase(false)][TestCase(true)]
        public void SaveRestoresActivityProgressAndFullAppearanceWithoutDuplicateRewards(bool fishing)
        {
            var world = Create(); var actor = world.PlayerActor;
            var appearance = new ActorAppearance("jade", "silver", "jade");
            world.Execute(new ChangeAppearanceCommand(actor.Id, appearance));
            world.Execute(fishing ? (WorldCommand)new FishCommand(actor.Id) : new ToggleMeditateCommand(actor.Id));
            Fixture.Tick(world, .35f);
            var mapper = new GameSaveMapper(); var serializer = new SaveSerializer();
            var dto = serializer.TryDeserialize(serializer.Serialize(mapper.Capture(world))).Value;
            var load = mapper.TryRestore(dto, world.Content, world.Geometry, new SeededRandomSource(9), new DomainEventBus());
            Assert.That(load.Succeeded, Is.True, load.Message);
            Assert.That(load.Value.PlayerActor.Appearance, Is.EqualTo(appearance));
            Fixture.Tick(world, 3.65f); Fixture.Tick(load.Value, 3.65f);
            Assert.That(load.Value.Player.Cultivation.CurrentCultivationExp, Is.EqualTo(world.Player.Cultivation.CurrentCultivationExp));
            Assert.That(load.Value.PlayerActor.LifeActivity.FishBiting, Is.EqualTo(actor.LifeActivity.FishBiting));
            if (fishing)
            {
                Assert.That(load.Value.Execute(new FishCommand(actor.Id)).Succeeded, Is.True);
                Assert.That(load.Value.Player.Inventory.Count("fish"), Is.EqualTo(1));
            }
            dto.Actors.Single(a => a.Id == actor.Id.Value).Velocity.X = 1;
            Assert.That(mapper.TryRestore(dto, world.Content, world.Geometry, new SeededRandomSource(9), new DomainEventBus()).Succeeded, Is.False);
        }

        [TestCase(false)][TestCase(true)]
        public void PondPerimeterChecksReachAndObstructionsAndRestoresFishing(bool blocked)
        {
            var content = Fixture.Content();
            content.Add(new ItemSpec("fish", ItemCategory.Fish));
            var shore = new[] { new WorldPoint(-2, -1), new WorldPoint(2, -1), new WorldPoint(2, 1), new WorldPoint(-2, 1) };
            var center = new WorldPoint(5, 0);
            content.Add(new WorldNodeSpec("pool", WorldNodeKind.FishingSpot, new[] { new ItemStack("fish", 1) },
                interactionRadius: .75f, fishingShoreline: shore));
            var obstacles = new System.Collections.Generic.List<WorldObstacle> { new WorldObstacle(shore.Select(p => p + center)) };
            if (blocked) obstacles.Add(new WorldObstacle(new WorldPoint(2.9f, 0), new WorldPoint(.04f, .5f), true));
            var world = Fixture.World(content: content, geometry: new WorldGeometry(new WorldPoint(20, 20), obstacles),
                nodes: new[] { new NodeSpawn("pool", center) });
            var actor = world.PlayerActor; var spot = world.Nodes.Values.Single();
            Assert.That(world.Execute(new FishCommand(actor.Id, spot.Id)).ErrorCode, Is.EqualTo(DomainError.OutOfRange));
            world.Motor.Displace(actor.Transform, new WorldPoint(2.4f, 0));
            var result = world.Execute(new FishCommand(actor.Id, spot.Id));
            if (blocked)
            {
                Assert.That(result.ErrorCode, Is.EqualTo(DomainError.Blocked));
                Assert.That(world.FocusedNode, Is.Null);
                Assert.That(actor.HasLifeActivity, Is.False);
            }
            else
            {
                Assert.That(result.Succeeded, Is.True);
                var mapper = new GameSaveMapper();
                var restored = mapper.TryRestore(mapper.Capture(world), content, world.Geometry, new SeededRandomSource(9), new DomainEventBus());
                Assert.That(restored.Succeeded, Is.True, restored.Message);
                Fixture.Tick(restored.Value, 4);
                Assert.That(restored.Value.Execute(new FishCommand(actor.Id)).Succeeded, Is.True);
                Assert.That(restored.Value.Player.Inventory.Count("fish"), Is.EqualTo(1));
                Assert.That(restored.Value.PlayerActor.Transform.Position, Is.EqualTo(actor.Transform.Position));
            }
        }

        [Test]
        public void InvalidShorelineCannotPutWaterOutsidePond()
        {
            var rewards = new[] { new ItemStack("fish", 1) };
            var shore = new[] { new WorldPoint(-2, -1), new WorldPoint(2, -1), new WorldPoint(2, 1), new WorldPoint(-2, 1) };
            Assert.Throws<ArgumentException>(() => new WorldNodeSpec("pool", WorldNodeKind.FishingSpot, rewards,
                fishingWaterOffset: new WorldPoint(4, 0), fishingShoreline: shore));
            Assert.Throws<ArgumentException>(() => new WorldNodeSpec("pool", WorldNodeKind.FishingSpot, rewards, fishingShoreline: shore.Take(2)));
        }
    }
}
