using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Client.Environment
{
    /// <summary>Uses the same petals, root grass and water arcs in the Canvas map; it never receives input.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "LinhThu.Prototype", "LinhThu.Prototype", "GardenAmbientGraphic")]
    public sealed class GardenAmbientGraphic : MaskableGraphic
    {
        private GardenAccentKind kind;
        private int seed;
        public static void Attach(Image source)
        {
            if (!GardenAmbientArt.TryKind(source.sprite, out var kind))
                return;
            var node = new GameObject(kind == GardenAccentKind.Cherry ? "Cánh hoa rơi · cỏ ôm rễ" : "Gợn hồ nhẹ", typeof(RectTransform), typeof(GardenAmbientGraphic));
            node.transform.SetParent(source.transform, false);
            var rect = (RectTransform)node.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = source.rectTransform.pivot;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var graphic = node.GetComponent<GardenAmbientGraphic>();
            graphic.kind = kind;
            graphic.seed = source.transform.parent.GetEntityId().GetHashCode();
            graphic.raycastTarget = false;
        }

        private void LateUpdate() => SetVerticesDirty();
        protected override void OnPopulateMesh(VertexHelper vertices) => GardenAmbientArt.Draw(vertices, rectTransform.rect, kind, Time.unscaledTime, seed, DaylightPresentation.LightTint);
    }
}
