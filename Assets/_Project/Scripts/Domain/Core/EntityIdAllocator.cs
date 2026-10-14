using System;

namespace LinhThu.Domain.Core
{
    public sealed class EntityIdAllocator
    {
        public long Next { get; private set; }

        public EntityIdAllocator(long next = 1)
        {
            if (next <= 0)
                throw new ArgumentException(nameof(next));
            Next = next;
        }

        public EntityId Allocate()
        {
            if (Next == long.MaxValue)
                throw new OverflowException("Entity IDs exhausted.");
            return new EntityId(Next++);
        }
    }
}
