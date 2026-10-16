using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.Actors
{
    public sealed class DashState
    {
        public bool Active => Remaining > .000001f;
        public WorldPoint Direction { get; internal set; }
        public float Duration { get; internal set; }
        public float Remaining { get; internal set; }
        public float PeakSpeed { get; internal set; }
        public float CooldownRemaining { get; internal set; }

        internal void Start(WorldPoint direction, float peakSpeed, MovementSpec spec)
        {
            Direction = direction.Normalized;
            Duration = Remaining = spec.DashDuration;
            PeakSpeed = peakSpeed;
            CooldownRemaining = spec.DashCooldown;
        }

        internal void Cancel() => Remaining = 0;
        internal void TickCooldown(float delta) => CooldownRemaining = Math.Max(0, CooldownRemaining - delta);

        // Integrate the ease-out velocity with a running-speed floor while movement is held.
        // Splitting at the crossover keeps distance independent of the simulation step.
        internal float Advance(float delta, float minimumSpeed, out float elapsed, out float endSpeed)
        {
            minimumSpeed = Math.Min(PeakSpeed, Math.Max(0, minimumSpeed));
            elapsed = Math.Min(delta, Remaining);
            float from = Remaining / Duration;
            Remaining = Math.Max(0, Remaining - elapsed);
            if (Remaining < .000001f) Remaining = 0;
            float to = Remaining / Duration;
            float crossover = (float)Math.Sqrt(minimumSpeed / PeakSpeed);
            float curvedTo = Math.Min(from, Math.Max(to, crossover));
            endSpeed = Math.Max(minimumSpeed, PeakSpeed * to * to);
            return PeakSpeed * Duration * (from * from * from - curvedTo * curvedTo * curvedTo) / 3
                + minimumSpeed * Duration * (curvedTo - to);
        }

        internal void Validate(MovementSpec spec)
        {
            Guard.Number(CooldownRemaining, "dash cooldown", 0, spec.DashCooldown);
            Guard.Number(Remaining, "dash remaining", 0, spec.DashDuration);
            Guard.Number(Duration, "dash duration", 0, spec.DashDuration);
            Guard.Number(PeakSpeed, "dash peak speed", 0, 100);
            if (!Direction.Finite || (Active && (Math.Abs(Direction.Length - 1) > .0001f || Duration != spec.DashDuration || PeakSpeed <= 0 || CooldownRemaining + .0001f < Remaining)))
                throw new ArgumentException("Invalid active dash.");
        }
    }
}
