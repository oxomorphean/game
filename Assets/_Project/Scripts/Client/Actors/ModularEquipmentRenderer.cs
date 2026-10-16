using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Client.Configs;
using LinhThu.Domain.Actors;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace LinhThu.Client.Actors
{
    /// <summary>Shared skeletal appearance. Changes never rebind or reset animation.</summary>
    [MovedFrom(true, "LinhThu.Prototype.Actors", "LinhThu.Prototype", "ModularEquipmentRenderer")]
    public sealed class ModularEquipmentRenderer : MonoBehaviour
    {
        [Serializable]
        public sealed class Slot
        {
            public string id;
            public SpriteRenderer renderer;
            public bool optional;
            [NonSerialized] private SpritePoseGeometry geometry;
            public SpritePoseGeometry Geometry => geometry ?? (geometry = new SpritePoseGeometry(renderer));
        }

        public Animator animator;
        public Transform visual;
        public Slot[] slots = Array.Empty<Slot>();
        public OutfitConfigSO defaultOutfit;
        public OutfitConfigSO[] outfits = Array.Empty<OutfitConfigSO>();
        public Color materialTint = Color.white;
        [Range(0, 1)] public float monochromeTint;
        [Min(.01f)] public float hitFlashDuration = .13f;
        public Color hitFlashColor = new Color(1, .95f, .8f, 1);
        public OutfitConfigSO CurrentOutfit { get; private set; }
        public ActorAppearance CurrentAppearance { get; private set; }
        public float FlashAmount => Mathf.Clamp01(flashRemaining / Mathf.Max(.01f, hitFlashDuration));
        private readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();
        private MaterialPropertyBlock properties;
        private float flashRemaining;
        private static readonly int Tint = Shader.PropertyToID("_AppearanceTint"), Flash = Shader.PropertyToID("_HitFlash"), FlashColor = Shader.PropertyToID("_HitFlashColor");

        void OnEnable()
        {
            if (defaultOutfit != null && CurrentOutfit == null)
                ApplyOutfit(defaultOutfit);
        }

        public void ApplyAppearance(ActorAppearance appearance)
        {
            if (CurrentOutfit != null && appearance == CurrentAppearance)
                return;
            var config = outfits.FirstOrDefault(o => o != null && o.appearanceId == appearance.OutfitId);
            if (config == null)
                throw new InvalidOperationException("No client outfit for appearance: " + appearance.OutfitId);
            Apply(config, appearance);
        }

        public void ApplyOutfit(OutfitConfigSO config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Apply(config, config.DefaultAppearance);
        }

        private void Apply(OutfitConfigSO config, ActorAppearance appearance)
        {
            if (animator == null || visual == null)
                throw new InvalidOperationException("Incomplete modular rig reference.");
            config.ValidateSlots();
            if (slots == null || slots.Length != config.slots.Length || slots.Any(s => s == null || s.renderer == null) || slots.Select(s => s.id).Distinct().Count() != slots.Length)
                throw new InvalidOperationException("Outfit does not match the rig's unique slots.");
            var hair = config.hairVariants.FirstOrDefault(h => h.id == appearance.HairId);
            var eyes = config.eyeVariants.FirstOrDefault(e => e.id == appearance.EyeId);
            if (hair == null || eyes == null)
                throw new InvalidOperationException("No client hair/eyes for appearance.");
            var sprites = config.slots.ToDictionary(s => s.slotId, s => s.sprite);
            var tints = config.slots.ToDictionary(s => s.slotId, s => s.tint);
            sprites["hair-front"] = hair.front; sprites["hair-back"] = hair.back; sprites["hair-ornament"] = hair.ornament;
            sprites["eyes"] = eyes.sprite;
            tints["hair-front"] = tints["hair-back"] = tints["hair-ornament"] = hair.tint;
            tints["eyes"] = eyes.tint;
            foreach (var slot in slots)
            {
                if (!sprites.TryGetValue(slot.id, out var next) || (!slot.optional && next == null))
                    throw new InvalidOperationException("Missing outfit slot: " + slot.id);
                var previous = slot.renderer.sprite;
                if (previous != null && next != null && (previous.rect.size != next.rect.size || previous.pivot != next.pivot || !Mathf.Approximately(previous.pixelsPerUnit, next.pixelsPerUnit)))
                    throw new InvalidOperationException("Outfit changes bind coordinates: " + slot.id);
                slot.Geometry.ValidateSwap(next, slot.id);
            }

            // Validate the complete snapshot before touching any renderer. Shared material stays intact.
            colors.Clear();
            foreach (var slot in slots)
            {
                slot.renderer.sprite = sprites[slot.id];
                colors.Add(slot.id, tints[slot.id]);
            }
            CurrentOutfit = config;
            CurrentAppearance = appearance;
            ApplyProperties();
        }

        public void SetTint(string slotId, Color tint)
        {
            if (!colors.ContainsKey(slotId))
                throw new ArgumentException("Unknown appearance slot: " + slotId);
            colors[slotId] = tint;
            ApplyProperties();
        }

        public void HitFlash()
        {
            flashRemaining = hitFlashDuration;
            ApplyProperties();
        }

        private void Update() => AdvancePresentation(Time.deltaTime);
        public void AdvancePresentation(float deltaTime)
        {
            if (flashRemaining <= 0) return;
            flashRemaining = Mathf.Max(0, flashRemaining - Mathf.Max(0, deltaTime));
            ApplyProperties();
        }

        private void ApplyProperties()
        {
            if (properties == null) properties = new MaterialPropertyBlock();
            foreach (var slot in slots)
            {
                if (slot?.renderer == null) continue;
                properties.Clear();
                slot.renderer.GetPropertyBlock(properties);
                properties.SetColor(Tint, colors.TryGetValue(slot.id, out var tint) ? tint : Color.white);
                properties.SetColor("_MaterialTint", materialTint);
                properties.SetFloat("_MonochromeTint", monochromeTint);
                properties.SetColor(FlashColor, hitFlashColor);
                properties.SetFloat(Flash, FlashAmount);
                slot.renderer.SetPropertyBlock(properties);
            }
        }

        private void OnDisable()
        {
            flashRemaining = 0;
            ApplyProperties();
        }

        public void SetFacing(float x)
        {
            if (visual == null || Mathf.Abs(x) < .001f) return;
            var scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * (x < 0 ? -1 : 1);
            visual.localScale = scale;
        }
    }
}
