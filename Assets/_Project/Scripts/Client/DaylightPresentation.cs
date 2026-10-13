using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LinhThu.Prototype
{
    /// <summary>Visual daylight only: a short morning-to-sunset preview without a gameplay clock or save data.</summary>
    public sealed class DaylightPresentation : MonoBehaviour
    {
        [Range(6, 18)] public float startHour = 8;
        [Min(30)] public float dayDurationSeconds = 240;
        public bool advanceTime = true;
        public float Hour { get; private set; }
        public static DaylightPresentation Current { get; private set; }
        public static Vector2 ShadowDirection { get; private set; }
        public static float ShadowLength { get; private set; }
        public static float ShadowOpacity { get; private set; }
        public static float Overhead { get; private set; }
        public static Color LightTint { get; private set; }
        private float started;
        private DaylightSkyGraphic sky;
        private TextMeshProUGUI clock;
        private int shownMinute = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() { Current = null; ApplyLighting(12); }

        public static DaylightPresentation Ensure(GameObject owner)
        {
            return Current != null ? Current : owner.AddComponent<DaylightPresentation>();
        }

        private void OnEnable() { Current = this; SetHour(startHour); }
        private void Start()
        {
            var canvasMap = transform as RectTransform;
            if (canvasMap != null)
            {
                foreach (var image in GetComponentsInChildren<Image>(true))
                    if (image.name == "Sprite" && image.GetComponent<DaylightCanvasEffect>() == null)
                        image.gameObject.AddComponent<DaylightCanvasEffect>();
            }
            var layer = new GameObject("Thời gian ban ngày", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            layer.transform.SetParent(transform, false);
            var canvas = layer.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1500;
            var scaler = layer.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var node = new GameObject("Thanh thời gian", typeof(RectTransform), typeof(DaylightSkyGraphic));
            node.transform.SetParent(layer.transform, false);
            var rect = (RectTransform)node.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -22); rect.sizeDelta = new Vector2(260, 44);
            sky = node.GetComponent<DaylightSkyGraphic>(); sky.raycastTarget = false;
            var text = new GameObject("Giờ trong ngày", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(node.transform, false);
            var labelRect = (RectTransform)text.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8, 13); labelRect.offsetMax = new Vector2(-8, -4);
            clock = text.GetComponent<TextMeshProUGUI>(); clock.font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-TMP");
            if (clock.font == null) throw new System.InvalidOperationException("Daylight clock requires the existing Vietnamese font.");
            clock.fontSize = 14; clock.alignment = TextAlignmentOptions.Center; clock.color = new Color(.94f, .97f, .96f); clock.raycastTarget = false;
        }

        public void SetHour(float hour)
        {
            startHour = Mathf.Clamp(hour, 6, 18); started = Time.unscaledTime; Hour = startHour; ApplyLighting(Hour);
        }

        private void LateUpdate()
        {
            Current = this;
            Hour = advanceTime ? 6 + Mathf.Repeat(startHour - 6 + (Time.unscaledTime - started) * 12 / Mathf.Max(30, dayDurationSeconds), 12) : startHour;
            ApplyLighting(Hour);
            if (sky != null) { sky.Progress = (Hour - 6) / 12; sky.SetVerticesDirty(); }
            int minute = Mathf.FloorToInt(Hour * 60);
            if (clock != null && minute != shownMinute)
            {
                shownMinute = minute; clock.text = $"{minute / 60:00}:{minute % 60:00}  ·  Ban ngày";
            }
        }

        public static void ApplyLighting(float hour)
        {
            float progress = Mathf.Clamp01((hour - 6) / 12), altitude = Mathf.Sin(progress * Mathf.PI);
            // Sunrise is on the left (-X), sunset on the right (+X); shadows point away from the hidden sun.
            float horizontal = -Mathf.Cos(progress * Mathf.PI);
            ShadowDirection = Mathf.Abs(horizontal) < .001f ? Vector2.zero : new Vector2(-Mathf.Sign(horizontal), -.18f).normalized;
            ShadowLength = Mathf.Min(1.25f, .5f * Mathf.Abs(horizontal) / Mathf.Max(.25f, altitude));
            Overhead = Mathf.Pow(altitude, 8);
            ShadowOpacity = Mathf.Lerp(.30f, .20f, altitude);
            float warm = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(14, 18, hour));
            LightTint = Color.Lerp(new Color(1, .99f, .93f), new Color(1, .77f, .48f), warm);
            Shader.SetGlobalVector("_DaylightTint", LightTint);
            Shader.SetGlobalVector("_SunShadow", new Vector4(ShadowDirection.x, ShadowDirection.y, ShadowLength, ShadowOpacity));
            Shader.SetGlobalFloat("_SunOverhead", Overhead);
        }

        private void OnDestroy() { if (Current == this) { Current = null; ApplyLighting(12); } }
    }
}
