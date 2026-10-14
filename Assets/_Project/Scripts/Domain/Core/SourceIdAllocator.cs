using System;

namespace LinhThu.Domain.Core
{
    public sealed class SourceIdAllocator
    {
        public int Next { get; private set; }

        public SourceIdAllocator(int next = 1)
        {
            if (next <= 0)
                throw new ArgumentException(nameof(next));
            Next = next;
        }

        public int Allocate()
        {
            if (Next == int.MaxValue)
                throw new OverflowException("Source IDs exhausted.");
            return Next++;
        }
    }
}
