using LinhThu.Client.World;
using EntityId = LinhThu.Domain.Core.EntityId;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

namespace LinhThu.Client.UI
{
    /// <summary>The command target owns both the caption and the world-object anchor.</summary>
    [DefaultExecutionOrder(110)]
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        public RectTransform bubble;
        public TMP_Text label;
        [System.NonSerialized] public Actors.ActorView actor;
        [System.NonSerialized] public RectTransform[] fixedHud = System.Array.Empty<RectTransform>();
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

        public void RefreshPosition()
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
                    bubble.localPosition = (Vector3)(point + Vector2.up * (bubble.rect.height * bubble.pivot.y + 6));
                if (visible)
                {
                    var initial = HudScreenGeometry.Project(bubble);
                    var blockers = new List<Rect>();
                    if (actor != null) blockers.Add(HudScreenGeometry.Expand(HudScreenGeometry.Project(actor.VisualBounds, gameCamera), 8));
                    foreach (var rect in fixedHud)
                        if (rect != null && rect.gameObject.activeInHierarchy) blockers.Add(HudScreenGeometry.Expand(HudScreenGeometry.Project(rect), 4));
                    var screen = gameCamera.pixelRect;
                    var xs = new List<float> { initial.xMin };
                    var ys = new List<float> { initial.yMin };
                    foreach (var rect in blockers)
                    {
                        xs.Add(rect.xMin - initial.width - 2); xs.Add(rect.xMax + 2);
                        ys.Add(rect.yMax + 2);
                    }
                    bool found = false; float score = float.MaxValue; Rect best = initial;
                    foreach (float x in xs)
                        foreach (float y in ys)
                        {
                            var candidate = new Rect(Mathf.Clamp(x, screen.xMin + 8, screen.xMax - initial.width - 8), y, initial.width, initial.height);
                            // The object remains the anchor: never place any part below its top.
                            if (y < initial.yMin || candidate.yMax > screen.yMax - 8 || blockers.Exists(r => r.Overlaps(candidate))) continue;
                            float distance = (candidate.center - initial.center).sqrMagnitude;
                            if (distance < score) { found = true; score = distance; best = candidate; }
                        }
                    visible = found;
                    if (found)
                    {
                        var anchor = new Vector2(best.xMin + best.width * bubble.pivot.x, best.yMin + best.height * bubble.pivot.y);
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)bubble.parent, anchor, uiCamera, out var adjusted))
                            bubble.localPosition = adjusted;
                    }
                }
            }
            bubble.gameObject.SetActive(visible);
        }
    }
}
