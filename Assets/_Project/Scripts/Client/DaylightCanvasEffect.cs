using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Prototype
{
    /// <summary>Projects a Canvas silhouette or multiplies its existing vertex colors by the shared sunlight.</summary>
    public sealed class DaylightCanvasEffect : BaseMeshEffect
    {
        public bool shadow;
        public Vector4 footprint;
        private void LateUpdate() { if (graphic != null && isActiveAndEnabled) graphic.SetVerticesDirty(); }
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            var direction = DaylightPresentation.ShadowDirection;
            if (shadow)
            {
                var image = graphic as UnityEngine.UI.Image;
                if (image != null && image.sprite != null)
                {
                    var rect = image.rectTransform.rect;
                    var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(image.sprite);
                    mesh.Clear(); const int cols = 4, rows = 24;
                    for (int y = 0; y <= rows; y++)
                    for (int x = 0; x <= cols; x++)
                    {
                        float u = (float)x / cols, v = (float)y / rows;
                        mesh.AddVert(new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, u), Mathf.Lerp(rect.yMin, rect.yMax, v), 0), graphic.color,
                            new Vector2(Mathf.Lerp(uv.x, uv.z, u), Mathf.Lerp(uv.y, uv.w, v)));
                    }
                    for (int y = 0; y < rows; y++)
                    for (int x = 0; x < cols; x++)
                    {
                        int n = y * (cols + 1) + x;
                        mesh.AddTriangle(n, n + cols + 1, n + 1); mesh.AddTriangle(n + 1, n + cols + 1, n + cols + 2);
                    }
                }
            }
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                var vertex = new UIVertex(); mesh.PopulateUIVertex(ref vertex, i);
                if (shadow)
                {
                    float height = vertex.position.y;
                    float profile = Mathf.Clamp01(footprint.w);
                    float overhead = DaylightPresentation.Overhead * profile;
                    float castDepth = footprint.w > 2.5f ? .8f : Mathf.Lerp(.12f, .42f, profile);
                    float depth = Mathf.Lerp(castDepth, footprint.y, overhead);
                    var ground = new Vector3(vertex.position.x, height * depth - footprint.x * overhead * depth, 0);
                    ground += (Vector3)direction * (footprint.w >= 1.5f ? height : Mathf.Max(0, height)) * DaylightPresentation.ShadowLength;
                    float weight = footprint.w > .5f && footprint.w < 1.5f ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(height / Mathf.Max(.001f, footprint.z))) : 1;
                    vertex.position = Vector3.Lerp(vertex.position, ground, weight);
                    float canopy = footprint.w > 1.5f && footprint.w < 2.5f
                        ? Mathf.Lerp(1, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(footprint.z, footprint.z * 1.35f, height)), overhead) : 1;
                    vertex.color = new Color(0, 0, 0, DaylightPresentation.ShadowOpacity * canopy);
                }
                else vertex.color = (Color)vertex.color * DaylightPresentation.LightTint;
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
