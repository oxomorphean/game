using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Prototype
{
    /// <summary>Non-interactive daylight progress bar; the astronomical sun has no screen graphic.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DaylightSkyGraphic : MaskableGraphic
    {
        public float Progress;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var rect = rectTransform.rect;
            Quad(mesh, rect.xMin, rect.yMin, rect.xMax, rect.yMax, new Color(.10f, .16f, .17f, .90f));
            float left = rect.xMin + 14, right = rect.xMax - 14, y = rect.yMin + 8;
            Quad(mesh, left, y, right, y + 3, new Color(.72f, .83f, .81f, .24f));
            float end = Mathf.Lerp(left, right, Mathf.Clamp01(Progress));
            var tint = Color.Lerp(new Color(.48f, .85f, .72f), new Color(1, .74f, .39f), Mathf.InverseLerp(.65f, 1, Progress));
            Quad(mesh, left, y, Mathf.Max(left + 1, end), y + 3, tint);
            Quad(mesh, end - 1, y - 2, end + 1, y + 5, new Color(.96f, .98f, .97f));
        }

        private static void Quad(VertexHelper mesh, float left, float bottom, float right, float top, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, bottom), color, Vector2.zero);
            mesh.AddVert(new Vector3(left, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, bottom), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
