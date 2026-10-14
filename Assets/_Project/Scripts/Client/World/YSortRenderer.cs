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
        public bool floorLayer;
        public int floorOrder = -40, baseOrder = 100;
        public float unitsToOrder = 10;
        public void Refresh()
        {
            float y = foot != null ? foot.position.y : transform.position.y;
            int order = floorLayer ? floorOrder : baseOrder - Mathf.RoundToInt(y * unitsToOrder);
            if (group != null)
                group.sortingOrder = order;
            else if (targetRenderer != null)
                targetRenderer.sortingOrder = order;
        }

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
    }
}
