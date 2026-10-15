using LinhThu.Client.Environment;
using LinhThu.Domain.Actors;
using UnityEngine;

namespace LinhThu.Client.Actors
{
    /// <summary>One soft ground footprint, independent of the animated or mirrored skeleton.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class GroundContactShadow : MonoBehaviour
    {
        public SpriteRenderer shadow;
        public ActorView view;
        public Transform heightSource;
        public Vector2 size = new Vector2(.6f, .24f);
        public Vector2 footOffset = new Vector2(0, .06f);
        [Range(.5f, 1)] public float dashScale = .86f;
        [Range(0, .38f)] public float opacity = .38f;
        private float restHeight;

        private void OnEnable()
        {
            restHeight = heightSource != null ? heightSource.localPosition.y : 0;
            if (shadow != null) shadow.enabled = true;
            Present();
        }

        private void LateUpdate() => Present();

        public void Present()
        {
            if (shadow == null) return;
            shadow.enabled = true;
            shadow.sortingOrder = LinhThu.Client.World.WorldRenderOrder.ContactShadow;
            shadow.color = new Color(7 / 255f, 18 / 255f, 14 / 255f,
                opacity * Mathf.Lerp(.92f, 1, DaylightPresentation.SunAltitude));
            float lift = heightSource != null ? Mathf.Max(0, heightSource.localPosition.y - restHeight) : 0;
            float scale = 1 / (1 + lift * .5f);
            if (view != null && view.Entity != null && view.Entity.State == ActorActionState.Dashing) scale *= dashScale;
            Vector2 sun = DaylightPresentation.ShadowDirection * DaylightPresentation.ShadowLength;
            Vector2 center = footOffset + sun * .06f;
            shadow.transform.localPosition = new Vector3(center.x, center.y, 0);
            shadow.transform.localRotation = Quaternion.identity;
            shadow.transform.localScale = new Vector3(size.x * scale * (1 + Mathf.Abs(sun.x) * .12f), size.y * scale, 1);
        }

        private void OnDisable()
        {
            if (shadow != null) shadow.enabled = false;
        }
    }
}
