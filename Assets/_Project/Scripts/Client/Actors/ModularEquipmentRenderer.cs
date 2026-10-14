using System;
using System.Linq;
using LinhThu.Client.Configs;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace LinhThu.Client.Actors
{
    /// <summary>Shared skeletal appearance. Outfit changes preserve animator state.</summary>
    [MovedFrom(true, "LinhThu.Prototype.Actors", "LinhThu.Prototype", "ModularEquipmentRenderer")]
    public sealed class ModularEquipmentRenderer : MonoBehaviour
    {
        [Serializable]
        public sealed class Slot
        {
            public string id;
            public SpriteRenderer renderer;
            public bool optional;
        }

        public Animator animator;
        public Transform visual;
        public Slot[] slots = Array.Empty<Slot>();
        public OutfitConfigSO defaultOutfit;
        public OutfitConfigSO CurrentOutfit { get; private set; }

        void OnEnable()
        {
            if (defaultOutfit != null && CurrentOutfit == null)
                ApplyOutfit(defaultOutfit);
        }

        public void ApplyOutfit(OutfitConfigSO config)
        {
            if (config == null || animator == null || visual == null)
                throw new InvalidOperationException("Incomplete modular rig/outfit reference.");
            config.ValidateSlots();
            if (slots == null || slots.Length != config.slots.Length || slots.Any(s => s == null || s.renderer == null) || slots.Select(s => s.id).Distinct().Count() != slots.Length)
                throw new InvalidOperationException("Outfit does not match the rig's unique slots.");
            var sprites = config.slots.ToDictionary(s => s.slotId, s => s.sprite);
            foreach (var slot in slots)
            {
                if (!sprites.TryGetValue(slot.id, out var next) || (!slot.optional && next == null))
                    throw new InvalidOperationException("Missing outfit slot: " + slot.id);
                var previous = slot.renderer.sprite;
                if (previous != null && next != null && (previous.rect.size != next.rect.size || previous.pivot != next.pivot || !Mathf.Approximately(previous.pixelsPerUnit, next.pixelsPerUnit)))
                    throw new InvalidOperationException("Outfit changes bind coordinates: " + slot.id);
            }

            // Sprite references only: no rebind, Play, transform mutation or layer toggling.
            foreach (var slot in slots)
                slot.renderer.sprite = sprites[slot.id];
            CurrentOutfit = config;
        }

        public void SetFacing(float x)
        {
            if (visual == null || Mathf.Abs(x) < .001f)
                return;
            var scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * (x < 0 ? -1 : 1);
            visual.localScale = scale;
        }
    }
}
