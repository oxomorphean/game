using System;
using System.Collections.Generic;
using NUnit.Framework;
using LinhThu.Domain.Stats;

namespace LinhThu.Tests.EditMode
{
    public sealed class StatSheetTests
    {
        [Test]
        public void FormulaAndSourceRemovalRespectOperatorOrder()
        {
            var s = new StatSheet(new Dictionary<StatId, float>{{StatId.Attack, 100}});
            s.AddModifier(new StatModifier(StatId.Attack, ModifierType.FlatAdd, 20, 1));
            s.AddModifier(new StatModifier(StatId.Attack, ModifierType.PercentAdd, .1f, 2));
            s.AddModifier(new StatModifier(StatId.Attack, ModifierType.PercentAdd, .2f, 2));
            s.AddModifier(new StatModifier(StatId.Attack, ModifierType.PercentMult, .1f, 3));
            s.AddModifier(new StatModifier(StatId.Attack, ModifierType.PercentMult, -.1f, 4));
            Assert.That(s.Get(StatId.Attack), Is.EqualTo(154.44f).Within(.001));
            Assert.That(s.RemoveAllModifiersFromSource(1), Is.EqualTo(1));
            Assert.That(s.Get(StatId.Attack), Is.EqualTo(128.7f).Within(.001));
            Assert.That(s.RemoveAllModifiersFromSource(1), Is.Zero);
            s.SetBase(StatId.Attack, 200);
            Assert.That(s.Get(StatId.Attack), Is.EqualTo(257.4f).Within(.001));
        }

        [Test]
        public void ReplaceIsAtomicAndDoesNotAccumulate()
        {
            var s = new StatSheet(new Dictionary<StatId, float>{{StatId.Attack, 10}});
            var mods = new[]{new StatModifier(StatId.Attack, ModifierType.FlatAdd, -3, 7)};
            s.SetModifiersFromSource(7, mods);
            s.SetModifiersFromSource(7, mods);
            Assert.That(s.Get(StatId.Attack), Is.EqualTo(7));
            Assert.Throws<ArgumentException>(() => s.SetModifiersFromSource(7, new[]{new StatModifier(StatId.Attack, ModifierType.FlatAdd, 8, 8)}));
            Assert.Throws<ArgumentException>(() => s.SetBase(StatId.Attack, float.NaN));
            Assert.Throws<ArgumentException>(() => new StatModifier(StatId.Attack, ModifierType.FlatAdd, float.PositiveInfinity, 7));
            Assert.That(s.Get(StatId.Attack), Is.EqualTo(7));
            s.RemoveAllModifiersFromSource(7);
            Assert.That(s.Get(StatId.Attack), Is.EqualTo(10));
            Assert.That(Enum.GetValues(typeof(StatId)).Length, Is.EqualTo(22));
        }

        [Test]
        public void VitalsAbsorbShieldClampAndNeverRegenerateDeadActors()
        {
            var s = new StatSheet(new Dictionary<StatId, float>{{StatId.MaxHp, 100}, {StatId.MaxQi, 20}, {StatId.MaxShield, 30}, {StatId.HpRegen, 10}});
            var v = new VitalResourcePool(s, 100, 20, 30);
            var hit = v.ApplyDamage(50);
            Assert.That(hit.ShieldLost, Is.EqualTo(30));
            Assert.That(hit.HpLost, Is.EqualTo(20));
            s.SetBase(StatId.MaxHp, 50);
            v.ClampToLimits();
            Assert.That(v.CurrentHp, Is.EqualTo(50));
            s.SetBase(StatId.MaxHp, 200);
            v.ClampToLimits();
            Assert.That(v.CurrentHp, Is.EqualTo(50));
            Assert.That(v.ApplyDamage(60).DiedNow, Is.True);
            Assert.That(v.ApplyDamage(60).DiedNow, Is.False);
            v.Regenerate(.1f);
            v.Restore(VitalType.Hp, 100);
            Assert.That(v.CurrentHp, Is.Zero);
        }

        [Test]
        public void NegativeModifiersAndNegativeFinalStatsUseTheSameFormula()
        {
            var sheet = new StatSheet(new Dictionary<StatId, float> { { StatId.Attack, -10 } });
            sheet.AddModifier(new StatModifier(StatId.Attack, ModifierType.FlatAdd, -5, 1));
            sheet.AddModifier(new StatModifier(StatId.Attack, ModifierType.PercentAdd, -.2f, 2));
            sheet.AddModifier(new StatModifier(StatId.Attack, ModifierType.PercentMult, -.5f, 3));
            Assert.That(sheet.Get(StatId.Attack), Is.EqualTo(-6).Within(.0001));
            sheet.RemoveAllModifiersFromSource(2);
            Assert.That(sheet.Get(StatId.Attack), Is.EqualTo(-7.5f).Within(.0001));
        }
    }
}
