using LinhThu.Client.World;
using EntityId = LinhThu.Domain.Core.EntityId;
using TMPro;
using UnityEngine;

namespace LinhThu.Client.UI
{
    /// <summary>The command target owns both the caption and the world-object anchor.</summary>
    [DefaultExecutionOrder(110)]
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        public RectTransform bubble;
        public TMP_Text label;
        private WorldNodeView target;
        private UnityEngine.Camera gameCamera;
        public EntityId? FocusedNodeId => target?.Entity?.Id;
        public bool IsVisible => bubble.gameObject.activeSelf;
        public Vector3 AnchorPosition => target != null ? target.LabelPosition : Vector3.zero;

        public void SetTarget(WorldNodeView view, UnityEngine.Camera camera, string caption)
        {
            target = view;
            gameCamera = camera;
            label.text = caption;
            RefreshPosition();
        }

        private void LateUpdate() => RefreshPosition();

        private void RefreshPosition()
        {
            bool visible = target != null && target.Entity != null && gameCamera != null && target.gameObject.activeInHierarchy;
            if (visible)
            {
                var position = AnchorPosition;
                var viewport = gameCamera.WorldToViewportPoint(position);
                visible = viewport.z > 0 && viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1;
                var canvas = GetComponentInParent<Canvas>();
                var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)bubble.parent,
                    RectTransformUtility.WorldToScreenPoint(gameCamera, position), uiCamera, out var point))
                    bubble.anchoredPosition = point + Vector2.up * (bubble.rect.height * bubble.pivot.y + 6);
            }
            bubble.gameObject.SetActive(visible);
        }
    }
}
