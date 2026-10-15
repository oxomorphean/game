using UnityEngine;
using UnityEngine.Rendering;

namespace LinhThu.Client.World
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class YSortRenderer : MonoBehaviour
    {
        public Transform foot;
        public SortingGroup group;
        public Renderer targetRenderer;
        public bool useRendererBottom;
        public float groundContactOffset;
        public bool floorLayer;
        public int floorOrder = WorldRenderOrder.Surface;
        public float GroundY => useRendererBottom && targetRenderer != null
            ? targetRenderer.bounds.min.y + groundContactOffset
            : foot != null ? foot.position.y : transform.position.y;
        public void Refresh()
        {
            int order = floorLayer ? Mathf.Clamp(floorOrder, WorldRenderOrder.Road, -9000) : WorldRenderOrder.AtGround(GroundY);
            if (group != null)
                group.sortingOrder = order;
            else if (targetRenderer != null)
                targetRenderer.sortingOrder = order;
        }

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
    }
}
