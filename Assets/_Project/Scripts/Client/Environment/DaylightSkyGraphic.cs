using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LinhThu.Client.Environment
{
    /// <summary>Daylight slider artwork and drag capture; the astronomical sun has no screen graphic.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "LinhThu.Prototype", "LinhThu.Prototype", "DaylightSkyGraphic")]
    public sealed class DaylightSkyGraphic : MaskableGraphic, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
    {
        public float Progress;
        public DaylightPresentation Owner;
        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
                Owner.SetScrubbing(true);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
                Owner.SetScrubbing(false);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
                Owner.SetScrubbing(true);
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
                Owner.SetScrubbing(false);
        }

        protected override void OnDisable()
        {
            if (Owner != null)
                Owner.SetScrubbing(false);
            base.OnDisable();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            float left = rect.xMin + 14, right = rect.xMax - 14, y = rect.yMin + 8;
            Quad(mesh, left, y, right, y + 3, new Color(.72f, .83f, .81f, .24f));
            float end = Mathf.Lerp(left, right, Mathf.Clamp01(Progress));
            var tint = Color.Lerp(new Color(.48f, .85f, .72f), new Color(1, .74f, .39f), Mathf.InverseLerp(.65f, 1, Progress));
            Quad(mesh, left, y, Mathf.Max(left + 1, end), y + 3, tint);
        }

        private static void Quad(VertexHelper mesh, float left, float bottom, float right, float top, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, bottom), color, Vector2.zero);
            mesh.AddVert(new Vector3(left, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, bottom), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
