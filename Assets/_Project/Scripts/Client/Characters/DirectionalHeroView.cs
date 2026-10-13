using System;
using LinhThu.Domain.Navigation;
using UnityEngine;

namespace LinhThu.Prototype.Characters
{
    /// <summary>Sprite animation reads resolved displacement, without a combat actor or character stats.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom("LinhThu.Prototype.Combat")]
    public sealed class DirectionalHeroView : MonoBehaviour
    {
        public int Direction { get; private set; } = 6;
        public HeroMotion Motion { get; private set; }
        public Sprite CurrentSprite => body == null ? null : body.sprite;
        public SpriteRenderer Body => body;
        public SpriteRenderer Shadow => shadow;
        private DirectionalHeroDefinition definition;
        private SpriteRenderer body, shadow;
        private float began;

        public void Initialize(DirectionalHeroDefinition value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            value.Validate(); definition = value;
            body = CreateRenderer("Nhân vật minh họa", "Art/DaylightSprite");
            shadow = CreateRenderer("Bóng nhân vật", "Art/DaylightShadow");
            shadow.sortingOrder = -29;
            shadow.localBounds = new Bounds(Vector3.zero, Vector3.one * 8);
            ResetPose(0);
        }

        private SpriteRenderer CreateRenderer(string name, string material)
        {
            var node = new GameObject(name); node.transform.SetParent(transform, false);
            node.transform.localScale = Vector3.one * definition.scale;
            var renderer = node.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = Resources.Load<Material>(material);
            if (renderer.sharedMaterial == null) throw new InvalidOperationException("Missing material " + material);
            return renderer;
        }

        public static int Facing(WorldPoint movement, int previous)
        {
            if (movement.LengthSquared < .000001f) return previous;
            return ((int)Math.Floor(Math.Atan2(movement.Y, movement.X) * 4 / Math.PI + .5) + 8) % 8;
        }

        public void ResetPose(float time)
        {
            Direction = 6; Motion = HeroMotion.Idle; began = time;
            Refresh(default, time);
        }

        public void Refresh(WorldPoint displacement, float time)
        {
            if (definition == null) throw new InvalidOperationException("Initialize the character view first.");
            Direction = Facing(displacement, Direction);
            var motion = displacement.LengthSquared > .000001f ? HeroMotion.Run : HeroMotion.Idle;
            if (motion != Motion) { Motion = motion; began = time; }
            body.sprite = shadow.sprite = definition.Clip(Motion).Sample(Direction, time - began);
            body.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
        }
    }
}
