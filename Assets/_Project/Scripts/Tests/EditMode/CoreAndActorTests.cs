using System;
using NUnit.Framework;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Navigation;

namespace LinhThu.Tests.EditMode
{
    public sealed class CoreAndActorTests
    {
        [Test]
        public void AllocatorsNeverWrapAndRandomContinuesAfterRestore()
        {
            var ids = new EntityIdAllocator();
            Assert.That(ids.Allocate(), Is.Not.EqualTo(ids.Allocate()));
            Assert.Throws<OverflowException>(() => new EntityIdAllocator(long.MaxValue).Allocate());
            Assert.Throws<OverflowException>(() => new SourceIdAllocator(int.MaxValue).Allocate());
            var rng = new SeededRandomSource(4);
            rng.Next01();
            var state = rng.Capture();
            var expected = rng.Next01();
            rng.Restore(state);
            Assert.That(rng.Next01(), Is.EqualTo(expected));
        }

        [Test]
        public void EventsQueueUntilCommitAndListenersCanUnsubscribe()
        {
            var bus = new DomainEventBus();
            int value = 0;
            var subscription = bus.Subscribe<int>(x => value += x);
            bus.Publish(2);
            Assert.That(value, Is.Zero);
            bus.Flush();
            Assert.That(value, Is.EqualTo(2));
            subscription.Dispose();
            bus.Publish(3);
            bus.Flush();
            Assert.That(value, Is.EqualTo(2));
            bus.Subscribe<int>(_ => throw new InvalidOperationException("listener"));
            bus.Subscribe<int>(x => value += x);
            bus.Publish(4);
            Assert.Throws<AggregateException>(() => bus.Flush());
            Assert.That(value, Is.EqualTo(6));
            bus.Flush();
            Assert.That(value, Is.EqualTo(6));
        }

        [Test]
        public void EveryFactionUsesActorEntityAndDeathEmitsExactlyOnce()
        {
            var w = Fixture.World();
            int deaths = 0;
            w.Events.Subscribe<ActorDiedEvent>(_ => deaths++);
            foreach (ActorFaction faction in Enum.GetValues(typeof(ActorFaction)))
            {
                var result = w.TrySpawnActor("target", faction, new WorldPoint(2, 0), faction == ActorFaction.PlayerBeast ? w.Player.ActorId : (EntityId? )null);
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.Value, Is.TypeOf<ActorEntity>());
                w.DealDamage(result.Value, w.Player.ActorId, 999);
                w.DealDamage(result.Value, w.Player.ActorId, 1);
                Assert.That(result.Value.CanAct, Is.False);
            }

            w.Tick(.1f);
            Assert.That(deaths, Is.EqualTo(Enum.GetValues(typeof(ActorFaction)).Length));
            w.Tick(.1f);
            Assert.That(deaths, Is.EqualTo(5));
        }
    }
}
