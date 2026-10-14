using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Prototype.Preview
{
    /// <summary>Art-preview instructions and the owner's dashboard entry point.</summary>
    public sealed class ArtPreviewHud : MonoBehaviour
    {
        private void Start()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-TMP");
            if (font == null) throw new System.InvalidOperationException("Missing Vietnamese preview font.");
            var layer = new GameObject("Hướng dẫn xem mẫu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            layer.transform.SetParent(transform, false);
            layer.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = layer.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var panel = new GameObject("Thanh hướng dẫn", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(layer.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 0);
            rect.offsetMin = new Vector2(18, 18); rect.offsetMax = new Vector2(-18, 62);
            var background = panel.GetComponent<Image>();
            background.color = new Color(.045f, .085f, .10f, .84f); background.raycastTarget = false;
            var node = new GameObject("Điều khiển", typeof(RectTransform), typeof(TextMeshProUGUI));
            node.transform.SetParent(panel.transform, false);
            var textRect = (RectTransform)node.transform;
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 0); textRect.offsetMax = new Vector2(-16, 0);
            var label = node.GetComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = 15; label.color = new Color(.90f, .95f, .94f);
            label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false;
            label.text = "MẪU MỸ THUẬT     WASD / mũi tên hoặc bấm sân để đi     ·     R về điểm xuất phát";
            if (GetComponent<OwnerDashboard>() == null) gameObject.AddComponent<OwnerDashboard>();
        }
    }
}
