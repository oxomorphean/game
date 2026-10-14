using System;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Navigation;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [CreateAssetMenu(menuName = "Linh Thu/Actor")]
    public sealed class ActorBaseConfigSO : ContentConfigSO
    {
        public ElementType primaryElement;
        public StatValueData[] baseStats = Array.Empty<StatValueData>();
        public TechniqueConfigSO[] abilities = Array.Empty<TechniqueConfigSO>();
        [Min(.01f)]
        public float collisionRadius = .22f;
        public Actors.ActorView prefab;
        [Min(.1f)] public float acceleration = 28, deceleration = 22;
        [Min(0)] public float turnResponsiveness = 16;
        [Range(.05f, 1)] public float dashDuration = .2f;
        [Range(1, 10)] public float dashSpeedMultiplier = 4;
        [Min(.05f)] public float dashCooldown = 1;
        public OutfitConfigSO defaultOutfit;
        public OutfitConfigSO[] outfits = Array.Empty<OutfitConfigSO>();
        public bool supportsLifeActivities;
        [Min(1)] public float meditationQiMultiplier = 3;
        [Min(.1f)] public float meditationTickSeconds = 1;
        [Min(1)] public int meditationExperience = 1;
        public AppearanceSpec AppearanceSpec()
        {
            if (defaultOutfit == null && outfits.Length == 0)
                return null;
            if (defaultOutfit == null || !outfits.Contains(defaultOutfit) || outfits.Any(o => o == null)
                || outfits.Select(o => o.appearanceId).Distinct().Count() != outfits.Length)
                throw new InvalidOperationException(name + " has an incomplete wardrobe.");
            foreach (var outfit in outfits)
                outfit.ValidateSlots();
            var hair = defaultOutfit.hairVariants.Select(h => h.id).ToArray();
            var eyes = defaultOutfit.eyeVariants.Select(e => e.id).ToArray();
            if (outfits.Any(o => !hair.OrderBy(id => id).SequenceEqual(o.hairVariants.Select(h => h.id).OrderBy(id => id))
                || !eyes.OrderBy(id => id).SequenceEqual(o.eyeVariants.Select(e => e.id).OrderBy(id => id))))
                throw new InvalidOperationException(name + " has incompatible outfit palettes.");
            return new AppearanceSpec(defaultOutfit.DefaultAppearance, outfits.Select(o => o.appearanceId), hair, eyes);
        }
        public ActorSpec ToSpec() => new ActorSpec(Id, primaryElement, ConfigData.Stats(baseStats), ConfigData.Required(abilities, "actor abilities").Select(a => ConfigData.Required(a, "actor ability").Id), collisionRadius,
            new MovementSpec(acceleration, deceleration, turnResponsiveness, dashDuration, dashSpeedMultiplier, dashCooldown), AppearanceSpec(),
            supportsLifeActivities ? new LifeActivitySpec(meditationQiMultiplier, meditationTickSeconds, meditationExperience) : null);
    }
}
