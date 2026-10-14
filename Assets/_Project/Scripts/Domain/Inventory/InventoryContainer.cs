using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Inventory
{
    public sealed class InventoryContainer
    {
        private readonly IDomainContent content;
        private readonly List<ItemStack> slots = new List<ItemStack>();
        public int Capacity { get; }

        public IReadOnlyList<ItemStack> Slots => slots.AsReadOnly();
        public InventoryContainer(int capacity, IDomainContent content)
        {
            if (capacity < 1 || capacity > 10000)
                throw new ArgumentException(nameof(capacity));
            Capacity = capacity;
            this.content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public int Count(string definitionId) => checked(slots.Where(s => s.DefinitionId == definitionId).Sum(s => s.Count));
        public DomainResult TryAdd(ItemStack stack) => TryAddAll(new[]{stack});
        public DomainResult TryAddAll(IReadOnlyList<ItemStack> stacks)
        {
            if (stacks == null)
                return DomainResult.Fail(DomainError.InvalidInput);
            var next = new List<ItemStack>(slots);
            foreach (var stack in stacks)
            {
                if (string.IsNullOrEmpty(stack.DefinitionId) || stack.Count <= 0)
                    return DomainResult.Fail(DomainError.InvalidInput);
                if (!content.TryGetItem(stack.DefinitionId, out var spec))
                    return DomainResult.Fail(DomainError.MissingDefinition);
                long total = next.Where(s => s.DefinitionId == stack.DefinitionId).Sum(s => (long)s.Count) + stack.Count;
                if (total > int.MaxValue)
                    return DomainResult.Fail(DomainError.CapacityExceeded);
                int left = stack.Count;
                for (int i = 0; i < next.Count && left > 0; i++)
                {
                    if (next[i].DefinitionId != stack.DefinitionId)
                        continue;
                    int take = Math.Min(left, spec.MaxStack - next[i].Count);
                    next[i] = new ItemStack(stack.DefinitionId, next[i].Count + take);
                    left -= take;
                }

                long required = ((long)left + spec.MaxStack - 1) / spec.MaxStack;
                if (required > Capacity - next.Count)
                    return DomainResult.Fail(DomainError.CapacityExceeded);
                while (left > 0)
                {
                    int take = Math.Min(left, spec.MaxStack);
                    next.Add(new ItemStack(stack.DefinitionId, take));
                    left -= take;
                }
            }

            Replace(next);
            return DomainResult.Ok();
        }

        public DomainResult TryRemove(string definitionId, int count)
        {
            if (string.IsNullOrEmpty(definitionId) || count <= 0)
                return DomainResult.Fail(DomainError.InvalidInput);
            if (Count(definitionId) < count)
                return DomainResult.Fail(DomainError.InsufficientResource);
            for (int i = slots.Count - 1; i >= 0 && count > 0; i--)
            {
                var s = slots[i];
                if (s.DefinitionId != definitionId)
                    continue;
                int take = Math.Min(count, s.Count);
                count -= take;
                if (take == s.Count)
                    slots.RemoveAt(i);
                else
                    slots[i] = new ItemStack(s.DefinitionId, s.Count - take);
            }

            return DomainResult.Ok();
        }

        internal InventoryContainer Clone()
        {
            var copy = new InventoryContainer(Capacity, content);
            copy.Replace(slots);
            return copy;
        }

        internal void Replace(IEnumerable<ItemStack> values)
        {
            var copy = values.ToArray();
            slots.Clear();
            slots.AddRange(copy);
        }
    }
}
