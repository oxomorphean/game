using System;

namespace LinhThu.Domain.Core
{
    public sealed class SimulationClock
    {
        public double ElapsedSeconds { get; private set; }

        public long TickIndex { get; private set; }

        public SimulationClock(double elapsed = 0, long tick = 0)
        {
            if (double.IsNaN(elapsed) || double.IsInfinity(elapsed) || elapsed < 0 || tick < 0)
                throw new ArgumentException("Invalid clock.");
            ElapsedSeconds = elapsed;
            TickIndex = tick;
        }

        public void Advance(float deltaTime)
        {
            Guard.Delta(deltaTime);
            if (deltaTime == 0)
                return;
            var next = checked(TickIndex + 1);
            ElapsedSeconds += deltaTime;
            TickIndex = next;
        }
    }
}
