using UnityEngine;

namespace LinhThu.Client.UI
{
    internal static class HudScreenGeometry
    {
        public static Rect Project(Bounds bounds, UnityEngine.Camera camera)
        {
            var min = camera.WorldToScreenPoint(bounds.min);
            var max = camera.WorldToScreenPoint(bounds.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static Rect Project(RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static Rect Expand(Rect rect, float padding) => Rect.MinMaxRect(
            rect.xMin - padding, rect.yMin - padding, rect.xMax + padding, rect.yMax + padding);
    }
}
