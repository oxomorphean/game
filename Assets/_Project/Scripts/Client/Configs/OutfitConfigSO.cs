using System;
using System.Linq;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [Serializable]
    public struct OutfitSlotData
    {
        public string slotId;
        [Tooltip("Null explicitly clears an optional layer, e.g. a coat or sash.")]
        public Sprite sprite;
    }

    [CreateAssetMenu(menuName = "Linh Thu/Outfit")]
    public sealed class OutfitConfigSO : ContentConfigSO
    {
        public OutfitSlotData[] slots = Array.Empty<OutfitSlotData>();
        public void ValidateSlots()
        {
            _ = Id;
            if (slots == null || slots.Length == 0 || slots.Any(s => string.IsNullOrWhiteSpace(s.slotId)) || slots.Select(s => s.slotId).Distinct().Count() != slots.Length)
                throw new InvalidOperationException(name + " has missing or duplicate outfit slots.");
        }
    }
}
