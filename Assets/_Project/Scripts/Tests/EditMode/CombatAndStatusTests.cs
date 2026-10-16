using System;
using System.Linq;
using NUnit.Framework;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Combat.Effects;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.World;

namespace LinhThu.Tests.EditMode
{
    public sealed class CombatAndStatusTests
    {
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void MidTickStatusUsesOnlyElapsedTimeAfterCastOrProjectileHit(bool projectile, bool channel)
        {
            var content = Fixture.Content();
            content.Add(new TechniqueSpec("timed-status", new DeliverySpec(projectile ? DeliveryShape.Projectile : DeliveryShape.Self,
                10, .1f, projectileSpeed: 20), new AbilityEffectSpec[] { new ApplyStatusEffectSpec("quick") },
                castDuration: .025f, channelDuration: channel ? .06f : 0, channelInterval: channel ? .02f : 0));
            var combined = Fixture.World(content: content);
            var split = Fixture.World(content: content);
            ActorEntity Prepare(WorldState world)
            {
                var target = projectile ? world.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(.7f, 0)).Value : world.PlayerActor;
                world.PlayerActor.Abilities.Learn("timed-status", content);
                Assert.That(world.Execute(new CastCommand(world.Player.ActorId, "timed-status", new AimRequest(new WorldPoint(1, 0)))).Succeeded, Is.True);
                return target;
            }
            var a = Prepare(combined); var b = Prepare(split);
            combined.Tick(.1f);
            for (int i = 0; i < 10; i++) split.Tick(.01f);
            Assert.That(a.Vitals.CurrentHp, Is.EqualTo(b.Vitals.CurrentHp).Within(.0001));
            Assert.That(a.Vitals.CurrentHp, Is.EqualTo(channel ? 197 : projectile ? 198 : 197).Within(.0001));
            Assert.That(a.StatusEffects.Active.Count, Is.EqualTo(b.StatusEffects.Active.Count));
            if (a.StatusEffects.Active.Count > 0)
            {
                Assert.That(a.StatusEffects.Active[0].RemainingDuration, Is.EqualTo(b.StatusEffects.Active[0].RemainingDuration).Within(.00001));
                Assert.That(a.StatusEffects.Active[0].UntilNextTick, Is.EqualTo(b.StatusEffects.Active[0].UntilNextTick).Within(.00001));
            }
        }

        [Test]
        public void PeriodicStatusCanApplyAnotherStatusWithoutLosingTheRemainingTick()
        {
            var content = Fixture.Content();
            content.Add(new StatusEffectSpec("delayed-status", .025f, .025f, 1, StatusStackPolicy.RefreshDuration,
                Array.Empty<StatModifierSpec>(), new AbilityEffectSpec[] { new ApplyStatusEffectSpec("quick") }));
            var a = Fixture.World(content: content); var b = Fixture.World(content: content);
            content.TryGetStatus("delayed-status", out var spec);
            foreach (var world in new[] { a, b })
                world.PlayerActor.StatusEffects.Apply(spec, world.Player.ActorId, EffectSourceSnapshot.Capture(world.PlayerActor));
            a.Tick(.1f);
            for (int i = 0; i < 10; i++) b.Tick(.01f);
            Assert.That(a.PlayerActor.Vitals.CurrentHp, Is.EqualTo(197));
            Assert.That(b.PlayerActor.Vitals.CurrentHp, Is.EqualTo(197));
            Assert.That(a.PlayerActor.StatusEffects.Active, Is.Empty);
            Assert.That(b.PlayerActor.StatusEffects.Active, Is.Empty);
        }

        [Test]
        public void OnlyAcceptedCastTurnsTheActorTowardItsAim()
        {
            var world = Fixture.World();
            var actor = world.PlayerActor;
            world.Execute(new MoveCommand(actor.Id, new WorldPoint(-1, 0)));
            world.Tick(.1f);
            world.Execute(new MoveCommand(actor.Id, default));
            Assert.That(actor.Transform.FacingDirection.X, Is.LessThan(0));
            Assert.That(world.Execute(new CastCommand(actor.Id, "unknown", new AimRequest(new WorldPoint(1, 0)))).Succeeded, Is.False);
            Assert.That(actor.Transform.FacingDirection.X, Is.LessThan(0));
            Assert.That(world.Execute(new CastCommand(actor.Id, "strike", new AimRequest(new WorldPoint(3, 4)))).Succeeded, Is.True);
            Assert.That(actor.Transform.FacingDirection.X, Is.EqualTo(.6f).Within(.00001));
            Assert.That(actor.Transform.FacingDirection.Y, Is.EqualTo(.8f).Within(.00001));
        }

        [Test]
        public void ProjectileContactRadiusCannotReachThroughAThinWall()
        {
            var geometry = new WorldGeometry(new WorldPoint(20, 20), new[] {
                new WorldObstacle(new WorldPoint(1, 0), new WorldPoint(.01f, 4), true) });
            var world = Fixture.World(geometry);
            var near = world.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(.6f, 0)).Value;
            var behind = world.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(1.22f, 0)).Value;
            Assert.That(behind, Is.Not.Null);
            Assert.That(world.Execute(new CastCommand(world.Player.ActorId, "projectile", new AimRequest(new WorldPoint(1, 0)))).Succeeded, Is.True);
            world.Tick(.1f);
            Assert.That(near.Vitals.CurrentHp, Is.LessThan(200), "A target before the wall still receives its hit.");
            Assert.That(behind.Vitals.CurrentHp, Is.EqualTo(200), "The expanded hit circle must not bypass the wall.");
            Assert.That(world.Combat.Projectiles, Is.Empty);
        }

        [Test]
        public void CastFeedbackEventIsEmittedOnceOnlyAfterACommandIsCommitted()
        {
            var world = Fixture.World(); var actor = world.PlayerActor;
            var events = new System.Collections.Generic.List<AbilityStartedEvent>();
            using (world.Events.Subscribe<AbilityStartedEvent>(e => events.Add(e)))
            {
                Assert.That(world.Combat.TryCast(world, actor.Id, "unknown", new AimRequest(new WorldPoint(1, 0))).Succeeded, Is.False);
                world.Events.Flush(); Assert.That(events, Is.Empty);
                float qi = actor.Vitals.CurrentQi;
                Assert.That(world.Combat.TryCast(world, actor.Id, "strike", new AimRequest(new WorldPoint(1, 0))).Succeeded, Is.True);
                world.Events.Flush(); Assert.That(events.Count, Is.EqualTo(1));
                Assert.That(events[0].CasterId, Is.EqualTo(actor.Id)); Assert.That(events[0].TechniqueId, Is.EqualTo("strike"));
                Assert.That(actor.Vitals.CurrentQi, Is.LessThan(qi));
                Assert.That(world.Combat.TryCast(world, actor.Id, "strike", new AimRequest(new WorldPoint(1, 0))).Succeeded, Is.False);
                world.Events.Flush(); Assert.That(events.Count, Is.EqualTo(1));
            }
        }
        [Test]
        public void FiveAffinitiesDriveOffenseAndResistanceAndPhysicalBypassesThem()
        {
            var w = Fixture.World();
            var a = w.PlayerActor;
            var b = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(2, 0)).Value;
            // The fixture realm supplies +5 Attack; the formula uses final Attack = 40.
            a.Stats.SetBase(StatId.Attack, 35);
            Assert.That(a.Stats.Get(StatId.Attack), Is.EqualTo(40));
            a.Stats.SetBase(StatId.FireAffinity, .1f);
            b.Stats.SetBase(StatId.Defense, 10);
            b.Stats.SetBase(StatId.FireAffinity, .2f);
            Assert.That(w.Combat.CalculateDamage(a, b, new DamageEffectSpec(StatId.Attack, 1.5f, 0, ElementType.Fire)).Amount, Is.EqualTo(55).Within(.001));
            Assert.That(w.Combat.CalculateDamage(a, b, new DamageEffectSpec(StatId.Attack, 1.5f, 0, ElementType.Physical)).Amount, Is.EqualTo(50));
            a.Stats.SetBase(StatId.CritRate, 1);
            a.Stats.SetBase(StatId.CritDamage, 2);
            Assert.That(w.Combat.CalculateDamage(a, b, new DamageEffectSpec(StatId.Attack, 1.5f, 0, ElementType.Physical)).Amount, Is.EqualTo(100));
            Assert.That(w.Rules.Elements.Get(ElementType.Wood, ElementType.Fire), Is.EqualTo(1));
        }

        [TestCase(ElementType.Metal, StatId.MetalAffinity, 1f)]
        [TestCase(ElementType.Wood, StatId.WoodAffinity, 1f)]
        [TestCase(ElementType.Water, StatId.WaterAffinity, 1f)]
        [TestCase(ElementType.Fire, StatId.FireAffinity, 1.2f)]
        [TestCase(ElementType.Earth, StatId.EarthAffinity, 1f)]
        public void EachElementUsesItsSameAffinityForAttackAndResistance(ElementType element, StatId affinity, float interaction)
        {
            var w = Fixture.World();
            var actor = w.PlayerActor;
            var target = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(2, 0)).Value;
            actor.Stats.SetBase(affinity, .1f);
            target.Stats.SetBase(affinity, .2f);
            var damage = w.Combat.CalculateDamage(actor, target, new DamageEffectSpec(StatId.Attack, 0, 60, element));
            Assert.That(damage.Amount, Is.EqualTo(60 * interaction * 1.1f / 1.2f).Within(.001));
        }

        [Test]
        public void RejectedCastDoesNotSpendAndCooldownTicksOnce()
        {
            var w = Fixture.World();
            var b = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(2, 0)).Value;
            var a = w.PlayerActor;
            var aim = new AimRequest(new WorldPoint(1, 0), b.Id);
            float qi = a.Vitals.CurrentQi;
            Assert.That(w.Combat.TryCast(w, a.Id, "unknown", aim).Succeeded, Is.False);
            Assert.That(a.Vitals.CurrentQi, Is.EqualTo(qi));
            a.Stats.SetBase(StatId.CooldownReduction, .2f);
            Assert.That(w.Combat.TryCast(w, a.Id, "strike", aim).Succeeded, Is.True);
            Assert.That(a.Vitals.CurrentQi, Is.EqualTo(qi - 2));
            Assert.That(w.Combat.TryCast(w, a.Id, "strike", aim).ErrorCode, Is.EqualTo(DomainError.OnCooldown));
            Fixture.Tick(w, .79f);
            Assert.That(a.Abilities.GetCooldownRemaining("strike"), Is.GreaterThan(0));
            w.Tick(.01f);
            Assert.That(a.Abilities.GetCooldownRemaining("strike"), Is.Zero.Within(.00001));
        }

        [Test]
        public void StatusStacksHaveSeparateCastersAndExpireWithFinalPeriodicTick()
        {
            var w = Fixture.World();
            var a = w.PlayerActor;
            var other = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(2, 0)).Value;
            w.Content.TryGetStatus("burn", out var spec);
            float hp = a.Vitals.CurrentHp;
            for (int i = 0; i < 3; i++)
                a.StatusEffects.Apply(spec, other.Id, EffectSourceSnapshot.Capture(other));
            Assert.That(a.StatusEffects.Active.Single().StackCount, Is.EqualTo(2));
            Assert.That(a.Stats.Get(StatId.MoveSpeed), Is.EqualTo(2));
            Fixture.Tick(w, 3);
            Assert.That(a.Vitals.CurrentHp, Is.EqualTo(hp - 12).Within(.001));
            Assert.That(a.StatusEffects.Active, Is.Empty);
            Assert.That(a.Stats.Get(StatId.MoveSpeed), Is.EqualTo(4));
        }

        [Test]
        public void PeriodicSplitAndCombinedDeltaMatchEvenIfCasterWasRemoved()
        {
            var a = Fixture.World();
            var b = Fixture.World();
            foreach (var w in new[]{a, b})
            {
                var caster = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(2, 0)).Value;
                w.Content.TryGetStatus("quick", out var spec);
                w.PlayerActor.StatusEffects.Apply(spec, caster.Id, EffectSourceSnapshot.Capture(caster));
                w.RemoveActor(caster.Id);
            }

            a.Tick(.06f);
            b.Tick(.02f);
            b.Tick(.02f);
            b.Tick(.02f);
            Assert.That(a.PlayerActor.Vitals.CurrentHp, Is.EqualTo(b.PlayerActor.Vitals.CurrentHp).Within(.001));
            Assert.That(a.PlayerActor.Vitals.CurrentHp, Is.EqualTo(197));
        }

        [TestCase(DeliveryShape.Self)]
        [TestCase(DeliveryShape.CircleAoE)]
        [TestCase(DeliveryShape.ConeSector)]
        [TestCase(DeliveryShape.LineRay)]
        [TestCase(DeliveryShape.Projectile)]
        public void EveryDeliveryExecutesThroughRegisteredHandlers(DeliveryShape shape)
        {
            var w = Fixture.World();
            var target = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(1, 0)).Value;
            string id = "shape-" + shape;
            w.PlayerActor.Abilities.Learn(id, w.Content);
            float hp = target.Vitals.CurrentHp;
            Assert.That(w.Combat.TryCast(w, w.Player.ActorId, id, new AimRequest(new WorldPoint(1, 0), shape == DeliveryShape.Self ? (EntityId? )null : target.Id)).Succeeded, Is.True);
            Fixture.Tick(w, .2f);
            if (shape != DeliveryShape.Self)
                Assert.That(target.Vitals.CurrentHp, Is.LessThan(hp));
            else
                Assert.That(w.PlayerActor.Transform.Position.X, Is.GreaterThan(0));
        }

        [TestCase(StatusStackPolicy.RefreshDuration, 1, 1)]
        [TestCase(StatusStackPolicy.AddStacksRefreshDuration, 1, 2)]
        [TestCase(StatusStackPolicy.IndependentInstances, 3, 3)]
        public void EachStackPolicyRefreshesAndKeepsCasterSourcesSeparate(StatusStackPolicy policy, int instances, int multipliers)
        {
            var w = Fixture.World();
            var actor = w.PlayerActor;
            var other = w.TrySpawnActor("target", ActorFaction.Neutral, new WorldPoint(2, 0)).Value;
            var spec = new StatusEffectSpec("haste", 1, 0, 2, policy, new[]{new StatModifierSpec(StatId.MoveSpeed, ModifierType.PercentMult, .1f)}, Array.Empty<AbilityEffectSpec>());
            var source = EffectSourceSnapshot.Capture(actor);
            actor.StatusEffects.Apply(spec, actor.Id, source);
            w.Tick(.1f);
            actor.StatusEffects.Apply(spec, actor.Id, source);
            actor.StatusEffects.Apply(spec, actor.Id, source);
            Assert.That(actor.StatusEffects.Active.Count, Is.EqualTo(instances));
            Assert.That(actor.Stats.Get(StatId.MoveSpeed), Is.EqualTo(4 * Math.Pow(1.1, multipliers)).Within(.0001));
            actor.StatusEffects.Apply(spec, other.Id, EffectSourceSnapshot.Capture(other));
            Assert.That(actor.StatusEffects.Active.Count, Is.EqualTo(instances + 1));
            int otherSource = actor.StatusEffects.Active.Single(s => s.CasterId == other.Id).SourceId;
            Assert.That(actor.StatusEffects.Remove(otherSource), Is.True);
            Assert.That(actor.Stats.Get(StatId.MoveSpeed), Is.EqualTo(4 * Math.Pow(1.1, multipliers)).Within(.0001));
            Fixture.Tick(w, 1);
            Assert.That(actor.StatusEffects.Active, Is.Empty);
            Assert.That(actor.Stats.Get(StatId.MoveSpeed), Is.EqualTo(4));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeathOrStaggerCancelsChannelWithoutRefundOrExtraPulse(bool stagger)
        {
            var w = Fixture.World();
            var actor = w.PlayerActor;
            var enemy = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(2, 0)).Value;
            Assert.That(w.Execute(new CastCommand(actor.Id, "channel", new AimRequest(new WorldPoint(1, 0), enemy.Id))).Succeeded, Is.True);
            w.Tick(.1f);
            float hp = enemy.Vitals.CurrentHp, qi = actor.Vitals.CurrentQi;
            if (stagger)
                actor.SetStaggered(true);
            else
                w.DealDamage(actor, enemy.Id, 999);
            Fixture.Tick(w, .5f);
            Assert.That(actor.Abilities.Cast, Is.Null);
            Assert.That(enemy.Vitals.CurrentHp, Is.EqualTo(hp));
            Assert.That(actor.Vitals.CurrentQi, Is.EqualTo(qi));
            Assert.That(actor.State, Is.EqualTo(stagger ? ActorActionState.Staggered : ActorActionState.Dead));
        }

        [Test]
        public void InsufficientQiAndInvalidCastDoNotConsumeRandomOrPublishExecuted()
        {
            var w = Fixture.World();
            var actor = w.PlayerActor;
            int executed = 0;
            w.Events.Subscribe<AbilityExecutedEvent>(_ => executed++);
            actor.Vitals.TrySpend(VitalType.Qi, 19);
            var rng = w.Random.Capture();
            Assert.That(w.Execute(new CastCommand(actor.Id, "strike", new AimRequest(new WorldPoint(1, 0)))).ErrorCode, Is.EqualTo(DomainError.InsufficientResource));
            actor.SetStaggered(true);
            Assert.That(w.Execute(new CastCommand(actor.Id, "projectile", new AimRequest(new WorldPoint(1, 0)))).ErrorCode, Is.EqualTo(DomainError.InvalidState));
            actor.SetStaggered(false);
            Assert.That(w.Execute(new CastCommand(actor.Id, "projectile", new AimRequest(new WorldPoint(float.NaN, 0)))).ErrorCode, Is.EqualTo(DomainError.InvalidInput));
            Assert.That(actor.Vitals.CurrentQi, Is.EqualTo(1));
            Assert.That(actor.Abilities.GetCooldownRemaining("strike"), Is.Zero);
            Assert.That(w.Random.Capture().State, Is.EqualTo(rng.State));
            Assert.That(executed, Is.Zero);
        }

        [Test]
        public void ProjectileReceivesOnlyTimeAfterWindupAndHitsEachActorOnce()
        {
            var content = Fixture.Content();
            content.Add(new TechniqueSpec("delayed", new DeliverySpec(DeliveryShape.Projectile, 10, .1f, projectileSpeed: 20, maxTargets: 3), new AbilityEffectSpec[]{new DamageEffectSpec(StatId.Attack, 1, 0, ElementType.Physical)}, castDuration: .075f));
            var a = Fixture.World(content: content);
            var b = Fixture.World(content: content);
            foreach (var w in new[]{a, b})
            {
                w.PlayerActor.Abilities.Learn("delayed", w.Content);
                w.Execute(new CastCommand(w.Player.ActorId, "delayed", new AimRequest(new WorldPoint(1, 0))));
            }

            a.Tick(.1f);
            b.Tick(.05f);
            b.Tick(.05f);
            Assert.That(a.Combat.Projectiles.Single().Position.X, Is.EqualTo(.5f).Within(.0001));
            Assert.That(b.Combat.Projectiles.Single().Position.X, Is.EqualTo(.5f).Within(.0001));
            var target = a.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(.8f, 0)).Value;
            a.Tick(.01f);
            float hp = target.Vitals.CurrentHp;
            Assert.That(hp, Is.LessThan(200));
            a.Tick(.01f);
            Assert.That(target.Vitals.CurrentHp, Is.EqualTo(hp));
        }

        [Test]
        public void ThinWallBlocksTargetedCastAndSweptProjectile()
        {
            var geo = new WorldGeometry(new WorldPoint(20, 20), new[]{new WorldObstacle(new WorldPoint(1, 0), new WorldPoint(.01f, 4), true)});
            var w = Fixture.World(geo);
            var enemy = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(2, 0)).Value;
            float qi = w.PlayerActor.Vitals.CurrentQi;
            Assert.That(w.Execute(new CastCommand(w.Player.ActorId, "strike", new AimRequest(new WorldPoint(1, 0), enemy.Id))).ErrorCode, Is.EqualTo(DomainError.Blocked));
            Assert.That(w.PlayerActor.Vitals.CurrentQi, Is.EqualTo(qi));
            Assert.That(w.Execute(new CastCommand(w.Player.ActorId, "projectile", new AimRequest(new WorldPoint(1, 0)))).Succeeded, Is.True);
            w.Tick(.1f);
            Assert.That(enemy.Vitals.CurrentHp, Is.EqualTo(200));
            Assert.That(w.Combat.Projectiles, Is.Empty);
        }

        [TestCase(0, false)] [TestCase(1, false)] [TestCase(2, false)]
        [TestCase(0, true)] [TestCase(1, true)] [TestCase(2, true)]
        public void ProjectileCollisionPolicyIsIndependentOfFeetAndSurvivesRestore(int shape, bool blocks)
        {
            var center = new WorldPoint(1, 0);
            var obstacle = shape == 2
                ? new WorldObstacle(new[] { new WorldPoint(.9f,-1), new WorldPoint(1.1f,-1), new WorldPoint(1.1f,1), new WorldPoint(.9f,1) }, blocks)
                : new WorldObstacle(center, new WorldPoint(.1f,1), shape == 0, blocks);
            var geometry = new WorldGeometry(new WorldPoint(20,20), new[] { obstacle });
            Assert.That(geometry.CanStand(center,.2f), Is.False);
            Assert.That(geometry.ClearLine(default,new WorldPoint(2,0)), Is.False);
            Assert.That(geometry.ClearProjectileLine(default,new WorldPoint(2,0)), Is.EqualTo(!blocks));
            var world = Fixture.World(geometry);
            var target = world.TrySpawnActor("target",ActorFaction.HostileMonster,new WorldPoint(2,0)).Value;
            var result = world.Execute(new CastCommand(world.Player.ActorId,"projectile",new AimRequest(new WorldPoint(1,0),target.Id)));
            Assert.That(result.Succeeded, Is.EqualTo(!blocks));
            if (blocks)
            {
                Assert.That(result.ErrorCode, Is.EqualTo(DomainError.Blocked));
                world.Execute(new CastCommand(world.Player.ActorId,"projectile",new AimRequest(new WorldPoint(1,0))));
            }
            var mapper = new LinhThu.Domain.Persistence.GameSaveMapper();
            var restored = mapper.TryRestore(mapper.Capture(world),world.Content,geometry,new SeededRandomSource(2),new DomainEventBus());
            Assert.That(restored.Succeeded, Is.True,restored.Message);
            foreach (var w in new[] { world,restored.Value })
            {
                w.Tick(.1f);
                if (blocks) Assert.That(w.Actors[target.Id].Vitals.CurrentHp, Is.EqualTo(200));
                else Assert.That(w.Actors[target.Id].Vitals.CurrentHp, Is.LessThan(200));
                Assert.That(w.Geometry.CanStand(center,.2f), Is.False);
            }
        }

        [Test]
        public void ComposedShieldRestoreAndDashRespectLimitsAndGeometry()
        {
            var c = Fixture.Content();
            c.Add(new StatusEffectSpec("shield", 1, 0, 1, StatusStackPolicy.RefreshDuration, new[]{new StatModifierSpec(StatId.MaxShield, ModifierType.FlatAdd, 50)}, Array.Empty<AbilityEffectSpec>()));
            c.Add(new TechniqueSpec("guard", new DeliverySpec(DeliveryShape.Self, 3), new AbilityEffectSpec[]{new ApplyStatusEffectSpec("shield"), new RestoreVitalEffectSpec(VitalType.Shield, 60), new DisplacementEffectSpec(3, DisplacementTarget.Caster)}));
            var w = Fixture.World(new WorldGeometry(new WorldPoint(20, 20), new[]{new WorldObstacle(new WorldPoint(1, 0), new WorldPoint(.01f, 4), true)}), c);
            w.PlayerActor.Abilities.Learn("guard", c);
            Assert.That(w.Execute(new CastCommand(w.Player.ActorId, "guard", new AimRequest(new WorldPoint(1, 0)))).Succeeded, Is.True);
            Assert.That(w.PlayerActor.Vitals.CurrentShield, Is.EqualTo(50));
            Assert.That(w.PlayerActor.Transform.Position.X, Is.LessThan(1));
            Fixture.Tick(w, 1);
            Assert.That(w.PlayerActor.Vitals.CurrentShield, Is.Zero);
        }

        [Test]
        public void InvalidCombinedBuffsRejectCastAndPillBeforeAnyMutation()
        {
            var c = Fixture.Content();
            var effects = new System.Collections.Generic.List<AbilityEffectSpec>{new RestoreVitalEffectSpec(VitalType.Hp, 20)};
            for (int i = 0; i < 5; i++)
            {
                c.Add(new StatusEffectSpec("overflow-" + i, 1, 0, 1, StatusStackPolicy.RefreshDuration, new[]{new StatModifierSpec(StatId.Attack, ModifierType.PercentMult, 1e9f)}, Array.Empty<AbilityEffectSpec>()));
                effects.Add(new ApplyStatusEffectSpec("overflow-" + i));
            }

            c.Add(new TechniqueSpec("overflow", new DeliverySpec(DeliveryShape.Self, 1), effects, 2, 1));
            c.Add(new ItemSpec("bad-pill", ItemCategory.Pill, useEffects: effects));
            var w = Fixture.World(content: c);
            var actor = w.PlayerActor;
            actor.Abilities.Learn("overflow", c);
            w.Player.Inventory.TryAdd(new ItemStack("bad-pill", 1));
            w.DealDamage(actor, actor.Id, 30);
            w.Events.Flush();
            int source = w.Sources.Next;
            float qi = actor.Vitals.CurrentQi;
            Assert.That(w.Execute(new CastCommand(actor.Id, "overflow", new AimRequest(new WorldPoint(1, 0)))).ErrorCode, Is.EqualTo(DomainError.InvalidInput));
            Assert.That(w.Items.TryUse(w, actor.Id, "bad-pill").ErrorCode, Is.EqualTo(DomainError.InvalidInput));
            Assert.That(actor.Vitals.CurrentHp, Is.EqualTo(170));
            Assert.That(actor.Vitals.CurrentQi, Is.EqualTo(qi));
            Assert.That(w.Sources.Next, Is.EqualTo(source));
            Assert.That(actor.StatusEffects.Active, Is.Empty);
            Assert.That(w.Player.Inventory.Count("bad-pill"), Is.EqualTo(1));
            Assert.That(actor.Abilities.GetCooldownRemaining("overflow"), Is.Zero);
        }
    }
}
