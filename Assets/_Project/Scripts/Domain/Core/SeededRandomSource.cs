using System;

namespace LinhThu.Domain.Core
{
    public sealed class SeededRandomSource : IRandomSource
    {
        public const string Algorithm = "xorshift32";
        private uint state;
        public SeededRandomSource(uint seed)
        {
            if (seed == 0)
                throw new ArgumentException(nameof(seed));
            state = seed;
        }

        public float Next01()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state >> 8) * (1f / 16777216f);
        }

        public RandomState Capture() => new RandomState(Algorithm, state);
        public void Restore(RandomState saved)
        {
            if (saved.AlgorithmId != Algorithm || saved.State == 0)
                throw new ArgumentException("Unsupported RNG state.");
            state = saved.State;
        }
    }
}
