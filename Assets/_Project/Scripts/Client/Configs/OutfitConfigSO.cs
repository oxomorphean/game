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
        [Tooltip("An intentional bald appearance. All three sprite references must be empty.")]
        public bool hidden;
        public Sprite front, back, ornament;
        public Color tint = Color.white;
    }

    [Serializable]
    public sealed class EyeVariantData
    {
        public string id;
        [Tooltip("An intentional blank face. The sprite reference must be empty.")]
        public bool hidden;
        public Sprite sprite;
        public Sprite closedSprite;
        public Color tint = Color.white;
    }

    [Serializable]
    public sealed class FacialExpressionData
    {
        public bool enabled;
        public Sprite callingMouth;
        [Min(.2f)] public float blinkInterval = 3.2f;
        [Range(.03f, .5f)] public float blinkDuration = .14f;
        [Range(0, 25)] public float browAngle = 12;
    }

    [CreateAssetMenu(menuName = "Linh Thu/Outfit")]
    public sealed class OutfitConfigSO : ContentConfigSO
    {
        public string appearanceId;
        [Tooltip("Exclude base underlayers from the normal robe shortcut.")]
        public bool includeInRobeCycle = true;
        public string defaultHairId = "ink", defaultEyeId = "amber";
        public HairVariantData[] hairVariants = Array.Empty<HairVariantData>();
        public EyeVariantData[] eyeVariants = Array.Empty<EyeVariantData>();
        public FacialExpressionData face = new FacialExpressionData();
        public OutfitSlotData[] slots = Array.Empty<OutfitSlotData>();
        public ActorAppearance DefaultAppearance => new ActorAppearance(appearanceId, defaultHairId, defaultEyeId);
        public void ValidateSlots()
        {
            _ = Id;
            if (slots == null || slots.Length == 0 || slots.Any(s => string.IsNullOrWhiteSpace(s.slotId)) || slots.Select(s => s.slotId).Distinct().Count() != slots.Length)
                throw new InvalidOperationException(name + " has missing or duplicate outfit slots.");
            _ = DefaultAppearance;
            if (hairVariants == null || eyeVariants == null || hairVariants.Length == 0 || eyeVariants.Length == 0
                || hairVariants.Any(h => h == null || string.IsNullOrWhiteSpace(h.id)
                    || (h.hidden ? h.front != null || h.back != null || h.ornament != null : h.front == null || h.back == null || h.ornament == null))
                || eyeVariants.Any(e => e == null || string.IsNullOrWhiteSpace(e.id)
                    || (e.hidden ? e.sprite != null || e.closedSprite != null : e.sprite == null || e.closedSprite == null))
                || hairVariants.Select(h => h.id).Distinct().Count() != hairVariants.Length
                || eyeVariants.Select(e => e.id).Distinct().Count() != eyeVariants.Length
                || !hairVariants.Any(h => h.id == defaultHairId) || !eyeVariants.Any(e => e.id == defaultEyeId))
                throw new InvalidOperationException(name + " has an incomplete appearance palette.");
            foreach (var eye in eyeVariants.Where(e => !e.hidden))
                RequireSameBinding(eye.sprite, eye.closedSprite, "closed eyes");
            if (face == null) throw new InvalidOperationException(name + " is missing facial expression settings.");
            if (face.enabled)
            {
                if (float.IsNaN(face.blinkInterval) || float.IsInfinity(face.blinkInterval) || face.blinkInterval < .2f
                    || float.IsNaN(face.blinkDuration) || face.blinkDuration < .03f || face.blinkDuration > .5f || face.blinkDuration >= face.blinkInterval
                    || float.IsNaN(face.browAngle) || face.browAngle < 0 || face.browAngle > 25 || eyeVariants.Any(e => e.hidden))
                    throw new InvalidOperationException(name + " has invalid facial expression timing or palette.");
                foreach (var id in new[] { "mouth", "eyebrow-front", "eyebrow-back" })
                    if (!slots.Any(s => s.slotId == id && s.sprite != null))
                        throw new InvalidOperationException(name + " is missing facial slot: " + id);
                RequireSameBinding(slots.Single(s => s.slotId == "mouth").sprite, face.callingMouth, "calling mouth");
            }
        }

        private void RequireSameBinding(Sprite rest, Sprite alternate, string part)
        {
            if (rest == null || alternate == null || rest.rect.size != alternate.rect.size || rest.pivot != alternate.pivot
                || !Mathf.Approximately(rest.pixelsPerUnit, alternate.pixelsPerUnit))
                throw new InvalidOperationException(name + " changes facial bind coordinates: " + part);
        }
    }
}
