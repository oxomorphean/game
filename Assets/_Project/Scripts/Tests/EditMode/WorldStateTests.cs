using System;
using System.Linq;
using NUnit.Framework;
using LinhThu.Domain.Core;
using LinhThu.Domain.World;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Persistence;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Beasts;

namespace LinhThu.Tests.EditMode
{
    public sealed class WorldStateTests
    {
        [Test]
        public void InputMovesOncePerTickAndInvalidInputDoesNotAdvance()
        {
            var w = Fixture.World();
            var p = w.PlayerActor.Transform.Position;
            w.Execute(new MoveCommand(w.Player.ActorId, new WorldPoint(1, 0)));
            Assert.That(w.PlayerActor.Transform.Position, Is.EqualTo(p));
            w.Tick(.1f);
            Assert.That(w.PlayerActor.Transform.Position.X, Is.EqualTo(.4f).Within(.0001));
            Assert.That(w.Clock.TickIndex, Is.EqualTo(1));
            w.Tick(0);
            Assert.That(w.Clock.TickIndex, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => w.Tick(float.NaN));
            Assert.That(w.Clock.TickIndex, Is.EqualTo(1));
            Assert.That(w.Execute(new MoveCommand(new EntityId(999), new WorldPoint(1, 0))).ErrorCode, Is.EqualTo(DomainError.WrongOwner));
        }

        [TestCase("windup", .1f)]
        [TestCase("channel", .2f)]
        [TestCase("projectile", .1f)]
        public void SaveMidCombatContinuesExactly(string technique, float elapsed)
        {
            var w = Fixture.World();
            var enemy = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(4, 0)).Value;
            w.Execute(new CastCommand(w.Player.ActorId, technique, new AimRequest(new WorldPoint(1, 0), enemy.Id)));
            Fixture.Tick(w, elapsed);
            var mapper = new GameSaveMapper();
            var dto = mapper.Capture(w);
            var result = mapper.TryRestore(dto, w.Content, w.Geometry, new SeededRandomSource(1), new DomainEventBus());
            Assert.That(result.Succeeded, Is.True, result.Message);
            var restored = result.Value;
            Fixture.Tick(w, .5f);
            Fixture.Tick(restored, .5f);
            Assert.That(restored.Actors[enemy.Id].Vitals.CurrentHp, Is.EqualTo(enemy.Vitals.CurrentHp));
            Assert.That(restored.Random.Capture().State, Is.EqualTo(w.Random.Capture().State));
            Assert.That(restored.Ids.Next, Is.EqualTo(w.Ids.Next));
            Assert.That(restored.Clock.TickIndex, Is.EqualTo(w.Clock.TickIndex));
        }

        [Test]
        public void InvalidSaveRejectsWithoutChangingLiveWorld()
        {
            var w = Fixture.World();
            var mapper = new GameSaveMapper();
            var dto = mapper.Capture(w);
            dto.Actors.Add(dto.Actors[0]);
            var result = mapper.TryRestore(dto, w.Content, w.Geometry, new SeededRandomSource(2), new DomainEventBus());
            Assert.That(result.Succeeded, Is.False);
            Assert.That(w.Actors.Count, Is.EqualTo(1));
            Assert.That(w.PlayerActor.Vitals.CurrentHp, Is.EqualTo(200));
        }

        [Test]
        public void IdenticalSeedAndCommandSequenceProduceIdenticalCommittedWorlds()
        {
            string Play()
            {
                var w = Fixture.World();
                int harvestEvents = 0;
                w.Events.Subscribe<NodeHarvestedEvent>(_ =>
                {
                    Assert.That(w.Player.Inventory.Count("herb"), Is.EqualTo(2));
                    harvestEvents++;
                });
                w.PlayerActor.Stats.SetBase(StatId.CritRate, .5f);
                w.Execute(new MoveCommand(w.Player.ActorId, new WorldPoint(1, 0)));
                w.Tick(.1f);
                w.Execute(new MoveCommand(w.Player.ActorId, default));
                var enemy = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(2, 0)).Value;
                var beast = Fixture.Beast(w);
                Assert.That(w.Execute(new SummonCommand(w.Player.ActorId, beast.Id)).Succeeded, Is.True);
                Assert.That(w.Execute(new CastCommand(w.Player.ActorId, "strike", new AimRequest(new WorldPoint(1, 0), enemy.Id))).Succeeded, Is.True);
                w.Tick(.1f);
                Assert.That(enemy.Vitals.CurrentHp, Is.LessThan(175));
                Assert.That(w.Execute(new HarvestCommand(w.Player.ActorId, w.Nodes.Values.Single(n => n.DefinitionId == "herb-node").Id)).Succeeded, Is.True);
                w.Player.Cultivation.AddExperience(125);
                Assert.That(w.Execute(new BreakthroughCommand(w.Player.ActorId)).Succeeded, Is.True);
                Assert.That(harvestEvents, Is.EqualTo(1));
                return new SaveSerializer().Serialize(new GameSaveMapper().Capture(w));
            }

            Assert.That(Play(), Is.EqualTo(Play()));
        }

        [Test]
        public void SavedProjectileHitHistoryPreventsDuplicateDamageAfterLoad()
        {
            var w = Fixture.World();
            var first = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(.4f, 0)).Value;
            var second = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(1.5f, 0)).Value;
            w.Execute(new CastCommand(w.Player.ActorId, "projectile", new AimRequest(new WorldPoint(1, 0))));
            w.Tick(.02f);
            float hp = first.Vitals.CurrentHp;
            Assert.That(hp, Is.EqualTo(175));
            Assert.That(w.Combat.Projectiles.Single().HitTargets, Does.Contain(first.Id));
            var mapper = new GameSaveMapper();
            var result = mapper.TryRestore(mapper.Capture(w), w.Content, w.Geometry, new SeededRandomSource(1), new DomainEventBus());
            Assert.That(result.Succeeded, Is.True, result.Message);
            w.Tick(.1f);
            result.Value.Tick(.1f);
            Assert.That(result.Value.Actors[first.Id].Vitals.CurrentHp, Is.EqualTo(hp));
            Assert.That(result.Value.Actors[second.Id].Vitals.CurrentHp, Is.EqualTo(second.Vitals.CurrentHp));
        }

        [Test]
        public void RepeatedJsonLoadPreservesStatusStacksAndFieldAssignment()
        {
            var w = Fixture.World();
            var actor = w.PlayerActor;
            var field = w.Nodes.Values.Single(n => n.DefinitionId == "field");
            var beast = Fixture.Beast(w);
            w.Player.Inventory.TryAdd(new ItemStack("seed", 1));
            w.Interactions.TryPlant(w, actor.Id, field.Id, "herb-node");
            w.Player.Beasts.TryAssignToField(beast.Id, field.Id, w);
            w.Content.TryGetStatus("burn", out var burn);
            var source = EffectSourceSnapshot.Capture(actor);
            actor.StatusEffects.Apply(burn, actor.Id, source);
            actor.StatusEffects.Apply(burn, actor.Id, source);
            w.DealDamage(actor, actor.Id, 10);
            w.Motor.Displace(actor.Transform, new WorldPoint(1, 1));
            w.Tick(.2f / 2);
            var mapper = new GameSaveMapper();
            var serializer = new SaveSerializer();
            for (int i = 0; i < 3; i++)
            {
                var parsed = serializer.TryDeserialize(serializer.Serialize(mapper.Capture(w)));
                Assert.That(parsed.Succeeded, Is.True);
                var restored = mapper.TryRestore(parsed.Value, w.Content, w.Geometry, new SeededRandomSource(1), new DomainEventBus());
                Assert.That(restored.Succeeded, Is.True, restored.Message);
                w = restored.Value;
                Assert.That(w.PlayerActor.Stats.Get(StatId.MoveSpeed), Is.EqualTo(2));
                Assert.That(w.PlayerActor.StatusEffects.Active.Single().StackCount, Is.EqualTo(2));
                Assert.That(w.Player.Beasts.All.Single().AssignedFieldId, Is.EqualTo(field.Id));
                Assert.That(w.Nodes[field.Id].AssignedBeastId, Is.EqualTo(beast.Id));
                Assert.That(w.PlayerActor.Transform.Position, Is.EqualTo(new WorldPoint(1, 1)));
                Assert.That(w.PlayerActor.Vitals.CurrentHp, Is.EqualTo(190));
            }

            Fixture.Tick(w, .9f);
            Assert.That(w.Nodes[field.Id].IsReady, Is.True);
            Assert.That(w.PlayerActor.Vitals.CurrentHp, Is.EqualTo(186));
        }

        [TestCase("future-schema")]
        [TestCase("retired-schema")]
        [TestCase("missing-definition")]
        [TestCase("duplicate-id")]
        [TestCase("negative-count")]
        [TestCase("blocked-position")]
        [TestCase("nan-hp")]
        [TestCase("infinite-stat")]
        [TestCase("next-source")]
        [TestCase("rng")]
        public void CorruptSaveDoesNotReplaceWorldOrConsumeInjectedRandom(string corruption)
        {
            var w = Fixture.World();
            w.Player.Inventory.TryAdd(new ItemStack("herb", 1));
            var mapper = new GameSaveMapper();
            var serializer = new SaveSerializer();
            string before = serializer.Serialize(mapper.Capture(w));
            var dto = mapper.Capture(w);
            switch (corruption)
            {
                case "future-schema":
                    dto.SchemaVersion = 2;
                    break;
                case "retired-schema":
                    dto.SchemaId = "retired";
                    break;
                case "missing-definition":
                    dto.Actors[0].DefinitionId = "missing";
                    break;
                case "duplicate-id":
                    dto.Actors.Add(dto.Actors[0]);
                    break;
                case "negative-count":
                    dto.Player.Inventory[0].Count = -1;
                    break;
                case "blocked-position":
                    dto.Actors[0].Position = new PointDto(new WorldPoint(100, 100));
                    break;
                case "nan-hp":
                    dto.Actors[0].Hp = float.NaN;
                    break;
                case "infinite-stat":
                    dto.Actors[0].BaseStats[StatId.Attack] = float.PositiveInfinity;
                    break;
                case "next-source":
                    dto.NextSourceId = dto.Player.RealmSourceId;
                    break;
                case "rng":
                    dto.Random.AlgorithmId = "unknown";
                    break;
            }

            var random = new SeededRandomSource(99);
            uint state = random.Capture().State;
            var result = mapper.TryRestore(dto, w.Content, w.Geometry, random, new DomainEventBus());
            Assert.That(result.Succeeded, Is.False, corruption);
            Assert.That(random.Capture().State, Is.EqualTo(state));
            Assert.That(serializer.Serialize(mapper.Capture(w)), Is.EqualTo(before));
        }

        [Test]
        public void JsonLimitsSchemaAndOptionalFieldsAreExplicit()
        {
            var w = Fixture.World();
            var serializer = new SaveSerializer();
            string json = serializer.Serialize(new GameSaveMapper().Capture(w));
            Assert.That(serializer.TryDeserialize("").ErrorCode, Is.EqualTo(DomainError.InvalidSave));
            Assert.That(serializer.TryDeserialize(new string (' ', SaveValidation.MaxJsonCharacters + 1)).ErrorCode, Is.EqualTo(DomainError.InvalidSave));
            Assert.That(serializer.TryDeserialize("{\"SchemaId\":\"a\",\"SchemaId\":\"b\"}").Succeeded, Is.False);
            Assert.That(serializer.TryDeserialize("{}").ErrorCode, Is.EqualTo(DomainError.UnsupportedSchema));
            Assert.That(serializer.TryDeserialize(json + "{}").Succeeded, Is.False);
            string tooDeep = json.Insert(1, "\"Deep\":" + new string('[', 66) + "0" + new string(']', 66) + ",");
            Assert.That(serializer.TryDeserialize(tooDeep).ErrorCode, Is.EqualTo(DomainError.InvalidSave));
            var withOptional = serializer.TryDeserialize(json.Insert(1, "\"FutureOptionalField\":17,"));
            Assert.That(withOptional.Succeeded, Is.True, withOptional.Message);
            ((DomainContent)w.Content).Add(new ItemSpec("future-item", ItemCategory.SpiritStone));
            Assert.That(new GameSaveMapper().TryRestore(withOptional.Value, w.Content, w.Geometry, new SeededRandomSource(1), new DomainEventBus()).Succeeded, Is.True);
        }
    }
}
