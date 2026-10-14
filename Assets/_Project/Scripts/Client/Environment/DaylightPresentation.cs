using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LinhThu.Client.Environment
{
    /// <summary>Visual daylight only: a short morning-to-sunset preview without a gameplay clock or save data.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "LinhThu.Prototype", "LinhThu.Prototype", "DaylightPresentation")]
    public sealed class DaylightPresentation : MonoBehaviour
    {
        [Range(6, 18)]
        public float startHour = 8;
        [Min(30)]
        public float dayDurationSeconds = 240;
        public bool advanceTime = true;
        public TMP_FontAsset clockFont;
        public bool showClock = true;
        public float Hour { get; private set; }

        public bool IsScrubbing { get; private set; }

        public bool CapturesInput => IsScrubbing || scrubbedFrame == Time.frameCount;
        public static DaylightPresentation Current { get; private set; }

        public static Vector2 ShadowDirection { get; private set; }

        public static float ShadowLength { get; private set; }

        public static float ShadowOpacity { get; private set; }

        public static float Overhead { get; private set; }

        public static Color LightTint { get; private set; }

        public static float SunHorizontal { get; private set; }

        public static float SunAltitude { get; private set; }

        public static float GoldenHour { get; private set; }

        private float started;
        private DaylightSkyGraphic sky;
        private TextMeshProUGUI clock;
        private TextMeshProUGUI playbackLabel;
        private Slider timeSlider;
        private int shownMinute = -1;
        private bool shownAdvancing;
        private int scrubbedFrame = -1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Current = null;
            ApplyLighting(12);
        }

        public static DaylightPresentation Ensure(GameObject owner)
        {
            var presentation = owner.GetComponent<DaylightPresentation>();
            return presentation != null ? presentation : throw new System.InvalidOperationException("Missing authored daylight component.");
        }

        private void OnEnable()
        {
            Current = this;
            SetHour(startHour);
        }

        private void Start()
        {
            if (!showClock)
                return;
            var canvasMap = transform as RectTransform;
            if (canvasMap != null)
            {
                foreach (var image in GetComponentsInChildren<Image>(true))
                    if (image.name == "Sprite" && image.GetComponent<DaylightCanvasEffect>() == null)
                        image.gameObject.AddComponent<DaylightCanvasEffect>();
            }

            var layer = new GameObject("Thời gian ban ngày", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            layer.transform.SetParent(transform, false);
            var canvas = layer.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1500;
            var scaler = layer.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            var node = new GameObject("Thanh thời gian", typeof(RectTransform), typeof(DaylightSkyGraphic));
            node.transform.SetParent(layer.transform, false);
            var rect = (RectTransform)node.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -22);
            rect.sizeDelta = new Vector2(300, 52);
            sky = node.GetComponent<DaylightSkyGraphic>();
            sky.raycastTarget = true;
            sky.Owner = this;
            var text = new GameObject("Giờ trong ngày", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(node.transform, false);
            var labelRect = (RectTransform)text.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 23);
            labelRect.offsetMax = new Vector2(-66, -5);
            clock = text.GetComponent<TextMeshProUGUI>();
            clock.font = clockFont;
            if (clock.font == null)
                throw new System.InvalidOperationException("Daylight clock requires the existing Vietnamese font.");
            clock.fontSize = 14;
            clock.alignment = TextAlignmentOptions.Center;
            clock.color = new Color(.94f, .97f, .96f);
            clock.raycastTarget = false;
            var track = new GameObject("Khoảng kéo 06:00–18:00", typeof(RectTransform));
            track.transform.SetParent(node.transform, false);
            var trackRect = (RectTransform)track.transform;
            trackRect.anchorMin = new Vector2(0, 0);
            trackRect.anchorMax = new Vector2(1, 0);
            trackRect.pivot = new Vector2(.5f, 0);
            trackRect.anchoredPosition = Vector2.zero;
            trackRect.sizeDelta = new Vector2(-28, 18);
            var handle = new GameObject("Nút kéo thời gian", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(track.transform, false);
            var handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = handleRect.anchorMax = new Vector2(0, .5f);
            handleRect.sizeDelta = new Vector2(8, -6);
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(.96f, .98f, .97f);
            handleImage.raycastTarget = false;
            timeSlider = node.AddComponent<Slider>();
            timeSlider.minValue = 6;
            timeSlider.maxValue = 18;
            timeSlider.wholeNumbers = false;
            timeSlider.handleRect = handleRect;
            timeSlider.targetGraphic = sky;
            timeSlider.transition = Selectable.Transition.None;
            timeSlider.navigation = new Navigation{mode = Navigation.Mode.None};
            timeSlider.onValueChanged.AddListener(SelectHour);
            var playback = new GameObject("Chạy hoặc dừng thời gian", typeof(RectTransform), typeof(Image), typeof(Button));
            playback.transform.SetParent(node.transform, false);
            var playbackRect = (RectTransform)playback.transform;
            playbackRect.anchorMin = playbackRect.anchorMax = playbackRect.pivot = Vector2.one;
            playbackRect.anchoredPosition = new Vector2(-8, -5);
            playbackRect.sizeDelta = new Vector2(48, 22);
            var playbackImage = playback.GetComponent<Image>();
            playbackImage.color = new Color(.24f, .38f, .35f);
            var button = playback.GetComponent<Button>();
            button.targetGraphic = playbackImage;
            button.navigation = new Navigation{mode = Navigation.Mode.None};
            button.onClick.AddListener(() => SetAutoAdvance(!advanceTime));
            var playbackText = new GameObject("Trạng thái thời gian", typeof(RectTransform), typeof(TextMeshProUGUI));
            playbackText.transform.SetParent(playback.transform, false);
            var playbackTextRect = (RectTransform)playbackText.transform;
            playbackTextRect.anchorMin = Vector2.zero;
            playbackTextRect.anchorMax = Vector2.one;
            playbackTextRect.offsetMin = playbackTextRect.offsetMax = Vector2.zero;
            playbackLabel = playbackText.GetComponent<TextMeshProUGUI>();
            playbackLabel.font = clock.font;
            playbackLabel.fontSize = 12;
            playbackLabel.alignment = TextAlignmentOptions.Center;
            playbackLabel.color = clock.color;
            playbackLabel.raycastTarget = false;
            UpdateClock();
        }

        public void SetHour(float hour)
        {
            startHour = Mathf.Clamp(hour, 6, 18);
            started = Time.unscaledTime;
            Hour = startHour;
            ApplyLighting(Hour);
            UpdateClock();
        }

        /// <summary>Selects and holds a daylight hour for visual inspection.</summary>
        public void SelectHour(float hour)
        {
            advanceTime = false;
            SetHour(hour);
        }

        /// <summary>Resumes from the displayed hour or holds that hour without advancing.</summary>
        public void SetAutoAdvance(bool advancing)
        {
            SetHour(Hour);
            advanceTime = advancing;
            UpdateClock();
        }

        public void SetScrubbing(bool scrubbing)
        {
            IsScrubbing = scrubbing;
            scrubbedFrame = Time.frameCount;
            if (scrubbing)
                SetAutoAdvance(false);
        }

        public bool ContainsPointer(Vector2 screenPoint) => sky != null && RectTransformUtility.RectangleContainsScreenPoint(sky.rectTransform, screenPoint);
        private void LateUpdate()
        {
            Current = this;
            Hour = advanceTime ? 6 + Mathf.Repeat(startHour - 6 + (Time.unscaledTime - started) * 12 / Mathf.Max(30, dayDurationSeconds), 12) : startHour;
            ApplyLighting(Hour);
            UpdateClock();
        }

        private void UpdateClock()
        {
            if (sky != null)
            {
                sky.Progress = (Hour - 6) / 12;
                sky.SetVerticesDirty();
            }

            if (timeSlider != null)
                timeSlider.SetValueWithoutNotify(Hour);
            int minute = Mathf.FloorToInt(Hour * 60);
            if (clock != null && (minute != shownMinute || advanceTime != shownAdvancing))
            {
                shownMinute = minute;
                shownAdvancing = advanceTime;
                clock.text = $"{minute / 60:00}:{minute % 60:00}  ·  {(advanceTime ? "Ban ngày" : "Tạm dừng")}";
            }

            if (playbackLabel != null)
                playbackLabel.text = advanceTime ? "Dừng" : "Chạy";
        }

        public static void ApplyLighting(float hour)
        {
            float progress = Mathf.Clamp01((hour - 6) / 12), altitude = Mathf.Sin(progress * Mathf.PI);
            // Sunrise is on the left (-X), sunset on the right (+X); shadows point away from the hidden sun.
            float horizontal = -Mathf.Cos(progress * Mathf.PI);
            SunHorizontal = horizontal;
            SunAltitude = altitude;
            // The horizontal component crosses noon smoothly instead of flipping a full-length shadow.
            ShadowDirection = new Vector2(-horizontal * .75f, 0);
            ShadowLength = Mathf.Lerp(.35f, .85f, 1 - altitude);
            Overhead = Mathf.Pow(altitude, 8);
            ShadowOpacity = Mathf.Lerp(.26f, .20f, altitude);
            GoldenHour = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(14, 18, hour));
            float dawn = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(6, 9, hour));
            var morning = Color.Lerp(new Color(1, 1, .97f), new Color(1.04f, .96f, .83f), dawn);
            LightTint = Color.Lerp(morning, new Color(1.08f, .84f, .66f), GoldenHour);
            Shader.SetGlobalVector("_DaylightTint", LightTint);
            Shader.SetGlobalVector("_DaylightSun", new Vector4(horizontal, altitude, GoldenHour, 0));
            Shader.SetGlobalColor("_DaylightShade", Color.Lerp(new Color(.10f, .17f, .18f), new Color(.16f, .12f, .20f), GoldenHour));
            Shader.SetGlobalVector("_SunShadow", new Vector4(ShadowDirection.x, ShadowDirection.y, ShadowLength, ShadowOpacity));
            Shader.SetGlobalFloat("_SunOverhead", Overhead);
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
                ApplyLighting(12);
            }
        }
    }
}
