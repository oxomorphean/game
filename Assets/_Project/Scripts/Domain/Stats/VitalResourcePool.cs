using System;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Stats
{
    public enum VitalType
    {
        Hp,
        Qi,
        Shield
    }

    public readonly struct DamageApplied
    {
        public float HpLost { get; }

        public float ShieldLost { get; }

        public bool DiedNow { get; }

        internal DamageApplied(float hp, float shield, bool died)
        {
            HpLost = hp;
            ShieldLost = shield;
            DiedNow = died;
        }
    }

    public sealed class VitalResourcePool
    {
        private readonly StatSheet stats;
        public float CurrentHp { get; private set; }

        public float CurrentQi { get; private set; }

        public float CurrentShield { get; private set; }

        public bool IsAlive => CurrentHp > 0;
        public VitalResourcePool(StatSheet stats, float hp, float qi, float shield)
        {
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            CurrentHp = Guard.Number(hp, nameof(hp));
            CurrentQi = Guard.Number(qi, nameof(qi));
            CurrentShield = Guard.Number(shield, nameof(shield));
            ClampToLimits();
        }

        public float Maximum(VitalType type)
        {
            Guard.EnumValue(type);
            return Math.Max(type == VitalType.Hp ? 1 : 0, stats.Get(type == VitalType.Hp ? StatId.MaxHp : type == VitalType.Qi ? StatId.MaxQi : StatId.MaxShield));
        }

        public float Current(VitalType type)
        {
            Guard.EnumValue(type);
            return type == VitalType.Hp ? CurrentHp : type == VitalType.Qi ? CurrentQi : CurrentShield;
        }

        private void Set(VitalType type, float value)
        {
            if (type == VitalType.Hp)
                CurrentHp = value;
            else if (type == VitalType.Qi)
                CurrentQi = value;
            else
                CurrentShield = value;
        }

        public bool TrySpend(VitalType vital, float amount)
        {
            Guard.Number(amount, nameof(amount));
            Guard.EnumValue(vital);
            if (!IsAlive || Current(vital) < amount)
                return false;
            Set(vital, Current(vital) - amount);
            return true;
        }

        public float Restore(VitalType vital, float amount)
        {
            Guard.Number(amount, nameof(amount));
            Guard.EnumValue(vital);
            if (!IsAlive)
                return 0;
            ClampToLimits();
            float delta = Math.Min(amount, Maximum(vital) - Current(vital));
            Set(vital, Current(vital) + delta);
            return delta;
        }

        public DamageApplied ApplyDamage(float amount)
        {
            Guard.Number(amount, nameof(amount));
            ClampToLimits();
            bool alive = IsAlive;
            if (!alive)
                return new DamageApplied(0, 0, false);
            float shield = Math.Min(CurrentShield, amount), hp = Math.Min(CurrentHp, amount - shield);
            CurrentShield -= shield;
            CurrentHp -= hp;
            return new DamageApplied(hp, shield, alive && !IsAlive);
        }

        public void ClampToLimits()
        {
            CurrentHp = Math.Min(CurrentHp, Maximum(VitalType.Hp));
            CurrentQi = Math.Min(CurrentQi, Maximum(VitalType.Qi));
            CurrentShield = Math.Min(CurrentShield, Maximum(VitalType.Shield));
        }

        public void Regenerate(float deltaTime, float qiMultiplier = 1)
        {
            Guard.Delta(deltaTime);
            Guard.Number(qiMultiplier, nameof(qiMultiplier), 1, 100);
            ClampToLimits();
            Restore(VitalType.Hp, Math.Max(0, stats.Get(StatId.HpRegen)) * deltaTime);
            Restore(VitalType.Qi, Math.Min(Maximum(VitalType.Qi), Math.Max(0, stats.Get(StatId.QiRegen)) * qiMultiplier * deltaTime));
        }
    }
}
