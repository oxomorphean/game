using LinhThu.Client.Actors;
using LinhThu.Client.World;
using UnityEngine;

namespace LinhThu.Client.Environment
{
    /// <summary>Presentation only. Transparency never moves the prop, its anchor or its footprint.</summary>
    [DefaultExecutionOrder(80), DisallowMultipleComponent]
    public sealed class CanopyOcclusionFade : MonoBehaviour
    {
        public Renderer visual;
        public YSortRenderer groundSort;
        [Range(0, 1)] public float hiddenAlpha = .42f;
        [Min(.1f)] public float fadeSpeed = 3;
        public float Alpha { get; private set; } = 1;
        private ActorView player;
        private MaterialPropertyBlock properties;

        public void Bind(ActorView value) { player = value; SetAlpha(1); }
        private void LateUpdate() => Present(Time.deltaTime);
        public void Present(float delta)
        {
            if (visual == null || groundSort == null || player == null) return;
            bool behind = player.gameObject.activeInHierarchy && player.transform.position.y > groundSort.GroundY
                && player.VisualBounds.Intersects(visual.bounds);
            SetAlpha(Mathf.MoveTowards(Alpha, behind ? hiddenAlpha : 1, Mathf.Max(0, delta) * fadeSpeed));
        }
        private void SetAlpha(float value)
        {
            Alpha = value;
            if (visual is SpriteRenderer sprite)
            {
                var color = sprite.color; color.a = value; sprite.color = color;
            }
            else if (visual != null)
            {
                if (properties == null) properties = new MaterialPropertyBlock();
                visual.GetPropertyBlock(properties);
                properties.SetColor("_Color", new Color(1, 1, 1, value));
                visual.SetPropertyBlock(properties);
            }
        }
        private void OnDisable() => SetAlpha(1);
    }
}
