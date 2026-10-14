using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.Inventory
{
    public readonly struct EquippedItem
    {
        public string ItemId { get; }

        public int SourceId { get; }

        public EquippedItem(string itemId, int sourceId)
        {
            ItemId = Guard.Id(itemId);
            if (sourceId <= 0)
                throw new ArgumentException(nameof(sourceId));
            SourceId = sourceId;
        }
    }

    public sealed class EquipmentLoadout
    {
        private readonly IDomainContent content;
        private readonly SourceIdAllocator sources;
        private readonly Dictionary<string, EquippedItem> equipped = new Dictionary<string, EquippedItem>();
        public IReadOnlyDictionary<string, EquippedItem> Slots { get; }

        public EquipmentLoadout(IDomainContent content, SourceIdAllocator sources)
        {
            this.content = content;
            this.sources = sources;
            Slots = new ReadOnlyDictionary<string, EquippedItem>(equipped);
        }

        public DomainResult TryEquip(string slotId, string itemId, InventoryContainer inventory, ActorEntity owner)
        {
            if (owner == null || !owner.CanAct)
                return DomainResult.Fail(DomainError.InvalidState);
            if (!content.TryGetItem(itemId, out var spec))
                return DomainResult.Fail(DomainError.MissingDefinition);
            if (spec.Category != ItemCategory.Artifact || slotId != spec.EquipmentSlot)
                return DomainResult.Fail(DomainError.InvalidInput);
            var appearance = owner.Appearance;
            if (spec.OutfitId != null)
            {
                if (owner.Spec.Appearance == null)
                    return DomainResult.Fail(DomainError.InvalidInput);
                appearance = appearance.WithOutfit(spec.OutfitId);
                if (!owner.Spec.Appearance.Supports(appearance))
                    return DomainResult.Fail(DomainError.InvalidInput);
            }
            if (equipped.TryGetValue(slotId, out var old) && old.ItemId == itemId)
                return DomainResult.Fail(DomainError.NoEffect);
            var next = inventory.Clone();
            var remove = next.TryRemove(itemId, 1);
            if (!remove.Succeeded)
                return remove;
            if (old.SourceId > 0)
            {
                var add = next.TryAdd(new ItemStack(old.ItemId, 1));
                if (!add.Succeeded)
                    return add;
            }

            if (old.SourceId == 0 && sources.Next == int.MaxValue)
                return DomainResult.Fail(DomainError.CapacityExceeded);
            int source = old.SourceId > 0 ? old.SourceId : sources.Next;
            var mods = spec.Modifiers.Select(m => m.FromSource(source)).ToArray();
            try
            {
                owner.Stats.SetModifiersFromSource(source, mods);
            }
            catch (ArgumentException error)
            {
                return DomainResult.Fail(DomainError.InvalidInput, error.Message);
            }
            catch (OverflowException error)
            {
                return DomainResult.Fail(DomainError.CapacityExceeded, error.Message);
            }

            if (old.SourceId == 0)
                sources.Allocate();
            inventory.Replace(next.Slots);
            equipped[slotId] = new EquippedItem(itemId, source);
            owner.Appearance = appearance;
            owner.Vitals.ClampToLimits();
            return DomainResult.Ok();
        }

        public DomainResult TryUnequip(string slotId, InventoryContainer inventory, ActorEntity owner)
        {
            if (!owner.CanAct)
                return DomainResult.Fail(DomainError.InvalidState);
            if (slotId == null || !equipped.TryGetValue(slotId, out var item))
                return DomainResult.Fail(DomainError.NotFound);
            var next = inventory.Clone();
            var add = next.TryAdd(new ItemStack(item.ItemId, 1));
            if (!add.Succeeded)
                return add;
            try
            {
                owner.Stats.RemoveAllModifiersFromSource(item.SourceId);
            }
            catch (ArgumentException error)
            {
                return DomainResult.Fail(DomainError.InvalidInput, error.Message);
            }
            catch (OverflowException error)
            {
                return DomainResult.Fail(DomainError.CapacityExceeded, error.Message);
            }

            inventory.Replace(next.Slots);
            equipped.Remove(slotId);
            content.TryGetItem(item.ItemId, out var removed);
            if (removed.OutfitId != null)
                owner.Appearance = owner.Appearance.WithOutfit(owner.Spec.Appearance.Default.OutfitId);
            owner.Vitals.ClampToLimits();
            return DomainResult.Ok();
        }

        internal void Restore(string slot, EquippedItem item, ActorEntity owner)
        {
            content.TryGetItem(item.ItemId, out var spec);
            if (spec.OutfitId != null && (owner.Spec.Appearance == null || !owner.Spec.Appearance.Supports(owner.Appearance.WithOutfit(spec.OutfitId))))
                throw new ArgumentException("Equipped outfit is incompatible with the actor wardrobe.");
            equipped.Add(slot, item);
            owner.Stats.SetModifiersFromSource(item.SourceId, spec.Modifiers.Select(m => m.FromSource(item.SourceId)).ToArray());
        }
    }
}
