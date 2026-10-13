using System;
using UnityEngine;

namespace LinhThu.Prototype.Characters
{
    public enum HeroPartGroup { Body, Hair, Coat, Trousers, Boots, Weapon, BasicClothes }

    [Serializable]
    public sealed class ModularHeroPart
    {
        public string id;
        public HeroPartGroup group;
        public Sprite first, second;
        public Sprite[] extraVariants;
        public Vector2 anchor;
        public float height;
        public int bone, tipBone, drawOrder;
        public float tipBlend;

        public int VariantCount => (first != null ? 1 : 0) + (second != null ? 1 : 0) + (extraVariants != null ? extraVariants.Length : 0);

        public Sprite GetVariant(int index)
        {
            if (index <= 0) return first;
            if (index == 1) return second != null ? second : first;
            int extraIndex = index - (second != null ? 2 : 1);
            if (extraVariants != null && extraIndex >= 0 && extraIndex < extraVariants.Length && extraVariants[extraIndex] != null)
                return extraVariants[extraIndex];
            return first;
        }
    }

    /// <summary>Front-view artwork and attachment points; clothing choices do not own gameplay or saves.</summary>
    [CreateAssetMenu(menuName = "LinhThu/Art/Modular hero")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom("LinhThu.Prototype.Combat")]
    public sealed class ModularHeroDefinition : ScriptableObject
    {
        public ModularHeroPart[] parts;
        public Material bodyMaterial, shadowMaterial;

        public void Validate()
        {
            if (bodyMaterial == null || shadowMaterial == null || parts == null || parts.Length == 0)
                throw new InvalidOperationException("Modular hero requires materials and body parts.");
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var part in parts)
            {
                if (part == null || string.IsNullOrWhiteSpace(part.id) || !ids.Add(part.id) || part.first == null ||
                    part.height <= 0 || part.bone < 0 || part.bone >= ModularHeroRig.BoneCount ||
                    part.tipBone < 0 || part.tipBone >= ModularHeroRig.BoneCount)
                    throw new InvalidOperationException("Invalid modular body part or attachment.");
                if ((part.group == HeroPartGroup.Hair || part.group == HeroPartGroup.Coat || part.group == HeroPartGroup.Weapon) && part.second == null)
                    throw new InvalidOperationException("Hair, coat and weapon parts require trial variants.");
                if (part.extraVariants != null)
                {
                    foreach (var extra in part.extraVariants)
                        if (extra == null) throw new InvalidOperationException("Null extra variant in modular part: " + part.id);
                }
            }
        }
    }
}
