using System;
using NUnit.Framework;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Cultivation;

namespace LinhThu.Tests.EditMode
{
    public sealed class CultivationTests
    {
        [Test]
        public void BreakthroughReplacesRealmSourceAndKeepsExcessExperience()
        {
            var w = Fixture.World();
            var p = w.Player.Cultivation;
            float attack = w.PlayerActor.Stats.Get(StatId.Attack);
            Assert.That(w.Cultivation.TryBreakthrough(w.PlayerActor, p, w.Player.RealmSourceId).ErrorCode, Is.EqualTo(DomainError.InsufficientResource));
            p.AddExperience(125);
            Assert.That(p.SubStage, Is.EqualTo(CultivationSubStage.Perfection));
            Assert.That(w.Cultivation.TryBreakthrough(w.PlayerActor, p, w.Player.RealmSourceId).Succeeded, Is.True);
            Assert.That(p.CurrentRealmId, Is.EqualTo("foundation"));
            Assert.That(p.CurrentCultivationExp, Is.EqualTo(25));
            Assert.That(w.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(attack + 5));
            Assert.That(w.Cultivation.TryBreakthrough(w.PlayerActor, p, w.Player.RealmSourceId).Succeeded, Is.False);
            Assert.That(w.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(attack + 5));
        }

        [Test]
        public void SubstagesAndExperienceOverflowAreValidated()
        {
            var w = Fixture.World();
            var p = w.Player.Cultivation;
            Assert.That(p.SubStage, Is.EqualTo(CultivationSubStage.Early));
            p.AddExperience(20);
            Assert.That(p.SubStage, Is.EqualTo(CultivationSubStage.Middle));
            p.AddExperience(30);
            Assert.That(p.SubStage, Is.EqualTo(CultivationSubStage.Late));
            p.AddExperience(50);
            Assert.That(p.SubStage, Is.EqualTo(CultivationSubStage.Perfection));
            Assert.That(p.AddExperience(-1).Succeeded, Is.False);
            Assert.That(p.AddExperience(long.MaxValue).Succeeded, Is.False);
            Assert.That(p.CurrentCultivationExp, Is.EqualTo(100));
        }

        [Test]
        public void MissingNextRealmAndDeadActorCannotBreakThrough()
        {
            var c = Fixture.Content();
            c.Add(new RealmSpec("orphan", "missing", new long[]{0, 1, 2, 3}, 3, Array.Empty<StatModifierSpec>()));
            var w = Fixture.World(content: c, realmId: "orphan");
            var p = w.Player.Cultivation;
            p.AddExperience(5);
            int events = 0;
            w.Events.Subscribe<RealmBreakthroughEvent>(_ => events++);
            float attack = w.PlayerActor.Stats.Get(StatId.Attack);
            Assert.That(w.Cultivation.TryBreakthrough(w.PlayerActor, p, w.Player.RealmSourceId).ErrorCode, Is.EqualTo(DomainError.MissingDefinition));
            w.DealDamage(w.PlayerActor, w.Player.ActorId, 999);
            w.Events.Flush();
            Assert.That(w.Cultivation.TryBreakthrough(w.PlayerActor, p, w.Player.RealmSourceId).ErrorCode, Is.EqualTo(DomainError.InvalidState));
            Assert.That(p.CurrentCultivationExp, Is.EqualTo(5));
            Assert.That(p.CurrentRealmId, Is.EqualTo("orphan"));
            Assert.That(w.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(attack));
            Assert.That(events, Is.Zero);
        }
    }
}
