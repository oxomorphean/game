using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D.Animation;

namespace LinhThu.Prototype.Characters
{
    /// <summary>Plays authored clips and swaps registered garment sprites without rebuilding the rig.</summary>
    public sealed class AuthoredHeroView : MonoBehaviour
    {
        public Animator animator;
        public Transform visual, shadowRoot;
        public SortingGroup sorting;
        public SpriteResolver[] garmentResolvers;
        public SpriteRenderer[] garmentShadows, allShadows;
        public string Outfit { get; private set; } = "Ivory";
        public bool Moving { get; private set; }
        public int FacingSign { get; private set; } = 1;
        MaterialPropertyBlock properties;
        static readonly int MovingParameter = Animator.StringToHash("Moving");

        void Awake()
        {
            properties = new MaterialPropertyBlock();
            if (animator == null || visual == null || shadowRoot == null || sorting == null ||
                garmentResolvers == null || garmentShadows == null || allShadows == null || garmentResolvers.Length != garmentShadows.Length)
                throw new InvalidOperationException("Authored hero requires its saved rig, animator and clothing bindings.");
            SetOutfit(Outfit);
        }

        /// <summary>Changes all garment slots while preserving the current clip and playback phase.</summary>
        public void SetOutfit(string id)
        {
            if (id != "Ivory" && id != "Navy") throw new ArgumentOutOfRangeException(nameof(id));
            for (int i = 0; i < garmentResolvers.Length; i++)
            {
                var resolver = garmentResolvers[i];
                if (!resolver.SetCategoryAndLabel(resolver.GetCategory(), id))
                    throw new InvalidOperationException("Missing authored outfit slot: " + resolver.name + "/" + id);
                resolver.ResolveSpriteToSpriteRenderer();
                garmentShadows[i].sprite = resolver.GetComponent<SpriteRenderer>().sprite;
            }
            Outfit = id;
        }

        /// <summary>Receives resolved movement; a preview controller may explicitly request a stationary run demonstration.</summary>
        public void SetMoving(bool moving)
        {
            Moving = moving;
            animator.SetBool(MovingParameter, moving);
        }

        /// <summary>Mirrors the three-quarter view without reversing the world-space sunlight direction.</summary>
        public void SetFacing(int sign)
        {
            FacingSign = sign < 0 ? -1 : 1;
            visual.localScale = shadowRoot.localScale = new Vector3(FacingSign, 1, 1);
        }

        void LateUpdate()
        {
            sorting.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
            properties.Clear();
            properties.SetVector("_GroundOrigin", new Vector4(transform.position.x, transform.position.y, 0, 1));
            foreach (var shadow in allShadows) shadow.SetPropertyBlock(properties);
        }
    }
}
