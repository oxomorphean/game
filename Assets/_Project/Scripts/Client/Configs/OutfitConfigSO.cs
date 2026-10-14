using System;
using System.Linq;
using LinhThu.Domain.Actors;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [Serializable]
    public struct OutfitSlotData
    {
        public string slotId;
        [Tooltip("Null explicitly clears an optional layer, e.g. a coat or sash.")]
        public Sprite sprite;
        public Color tint;
    }

    [Serializable]
    public sealed class HairVariantData
    {
        public string id;
        public Sprite front, back, ornament;
        public Color tint = Color.white;
    }

    [Serializable]
    public sealed class EyeVariantData
    {
        public string id;
        public Sprite sprite;
        public Color tint = Color.white;
    }

    [CreateAssetMenu(menuName = "Linh Thu/Outfit")]
    public sealed class OutfitConfigSO : ContentConfigSO
    {
        public string appearanceId;
        public string defaultHairId = "ink", defaultEyeId = "amber";
        public HairVariantData[] hairVariants = Array.Empty<HairVariantData>();
        public EyeVariantData[] eyeVariants = Array.Empty<EyeVariantData>();
        public OutfitSlotData[] slots = Array.Empty<OutfitSlotData>();
        public ActorAppearance DefaultAppearance => new ActorAppearance(appearanceId, defaultHairId, defaultEyeId);
        public void ValidateSlots()
        {
            _ = Id;
            if (slots == null || slots.Length == 0 || slots.Any(s => string.IsNullOrWhiteSpace(s.slotId)) || slots.Select(s => s.slotId).Distinct().Count() != slots.Length)
                throw new InvalidOperationException(name + " has missing or duplicate outfit slots.");
            _ = DefaultAppearance;
            if (hairVariants == null || eyeVariants == null || hairVariants.Length == 0 || eyeVariants.Length == 0
                || hairVariants.Any(h => h == null || string.IsNullOrWhiteSpace(h.id) || h.front == null || h.back == null || h.ornament == null)
                || eyeVariants.Any(e => e == null || string.IsNullOrWhiteSpace(e.id) || e.sprite == null)
                || hairVariants.Select(h => h.id).Distinct().Count() != hairVariants.Length
                || eyeVariants.Select(e => e.id).Distinct().Count() != eyeVariants.Length
                || !hairVariants.Any(h => h.id == defaultHairId) || !eyeVariants.Any(e => e.id == defaultEyeId))
                throw new InvalidOperationException(name + " has an incomplete appearance palette.");
        }
    }
}
