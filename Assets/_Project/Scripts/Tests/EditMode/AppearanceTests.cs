using System;
using NUnit.Framework;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Persistence;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;

namespace LinhThu.Tests.EditMode
{
    public sealed class AppearanceTests
    {
        [TestCase("classic")]
        [TestCase("jade")]
        [TestCase("basic")]
        public void EveryCombinationChangesAndRoundTripsWithoutChangingStats(string outfit)
        {
            var world = Fixture.World();
            float attack = world.PlayerActor.Stats.Get(StatId.Attack);
            var mapper = new GameSaveMapper();
            var serializer = new SaveSerializer();
            foreach (string hair in new[] { "ink", "silver" })
            foreach (string eyes in new[] { "amber", "jade" })
            {
                var appearance = new ActorAppearance(outfit, hair, eyes);
                Assert.That(world.Execute(new ChangeAppearanceCommand(world.Player.ActorId, appearance)).Succeeded, Is.True);
                Assert.That(world.PlayerActor.Appearance, Is.EqualTo(appearance));
                Assert.That(world.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(attack));
                var dto = serializer.TryDeserialize(serializer.Serialize(mapper.Capture(world)));
                Assert.That(dto.Succeeded, Is.True, dto.Message);
                var restored = mapper.TryRestore(dto.Value, world.Content, world.Geometry, new SeededRandomSource(2), new DomainEventBus());
                Assert.That(restored.Succeeded, Is.True, restored.Message);
                Assert.That(restored.Value.PlayerActor.Appearance, Is.EqualTo(appearance));
            }
        }

        [Test]
        public void UnknownAppearanceAndWrongOwnerAreAtomic()
        {
            var world = Fixture.World();
            var original = world.PlayerActor.Appearance;
            foreach (var invalid in new[] { new ActorAppearance("missing", "ink", "amber"), new ActorAppearance("jade", "missing", "jade"), new ActorAppearance("basic", "silver", "missing"), default })
            {
                Assert.That(world.Execute(new ChangeAppearanceCommand(world.Player.ActorId, invalid)).Succeeded, Is.False);
                Assert.That(world.PlayerActor.Appearance, Is.EqualTo(original));
            }
            Assert.That(world.Execute(new ChangeAppearanceCommand(new EntityId(999), new ActorAppearance("jade", "silver", "jade"))).ErrorCode, Is.EqualTo(DomainError.WrongOwner));
            Assert.That(world.PlayerActor.Appearance, Is.EqualTo(original));
        }

        [Test]
        public void RobeEquipPreservesHairAndEyesAndFailureDoesNotChangeAppearance()
        {
            var world = Fixture.World();
            var appearance = new ActorAppearance("basic", "silver", "jade");
            world.Execute(new ChangeAppearanceCommand(world.Player.ActorId, appearance));
            Assert.That(world.Execute(new EquipCommand(world.Player.ActorId, "body", "robe")).Succeeded, Is.False);
            Assert.That(world.PlayerActor.Appearance, Is.EqualTo(appearance));
            world.Player.Inventory.TryAdd(new ItemStack("robe", 1));
            Assert.That(world.Execute(new EquipCommand(world.Player.ActorId, "body", "robe")).Succeeded, Is.True);
            Assert.That(world.PlayerActor.Appearance, Is.EqualTo(new ActorAppearance("jade", "silver", "jade")));
            Assert.That(world.Execute(new UnequipCommand(world.Player.ActorId, "body")).Succeeded, Is.True);
            Assert.That(world.PlayerActor.Appearance, Is.EqualTo(new ActorAppearance("classic", "silver", "jade")));
        }

        [Test]
        public void InvalidSavedAppearanceRejectsTheWholeWorld()
        {
            var world = Fixture.World();
            var mapper = new GameSaveMapper();
            var dto = mapper.Capture(world);
            dto.Actors[0].Appearance.HairId = "unknown";
            var result = mapper.TryRestore(dto, world.Content, world.Geometry, new SeededRandomSource(2), new DomainEventBus());
            Assert.That(result.Succeeded, Is.False);
            Assert.That(world.PlayerActor.Appearance, Is.EqualTo(new ActorAppearance("classic", "ink", "amber")));
            dto.Actors[0].Appearance = null;
            Assert.That(mapper.TryRestore(dto, world.Content, world.Geometry, new SeededRandomSource(2), new DomainEventBus()).Succeeded, Is.False);
        }
    }
}
