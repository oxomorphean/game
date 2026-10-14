using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Prototype
{
    /// <summary>Applies shared sunlight tint to the retained Canvas art preview.</summary>
    public sealed class DaylightCanvasEffect : BaseMeshEffect
    {
        private void LateUpdate() { if (graphic != null && isActiveAndEnabled) graphic.SetVerticesDirty(); }
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                var vertex = new UIVertex(); mesh.PopulateUIVertex(ref vertex, i);
                vertex.color = (Color)vertex.color * DaylightPresentation.LightTint;
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
