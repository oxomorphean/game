using System;
using UnityEngine;

namespace LinhThu.ArtSample
{
    /// <summary>Reuses pixel-snapped blossom petals and pond ripple sprites; affects visuals only.</summary>
    [DisallowMultipleComponent]
    public sealed class LivingGarden : MonoBehaviour
    {
        [SerializeField] private Sprite petalSprite;
        [SerializeField] private Sprite[] rippleFrames;
        [SerializeField] private Vector2[] blossomOrigins = { new Vector2(-7f, 0.75f), new Vector2(7f, 1.25f) };
        [SerializeField] private Vector2[] rippleCenters = {
            new Vector2(6.5f, -4.5f), new Vector2(7.75f, -3.9375f), new Vector2(8.5f, -5f)
        };
        [SerializeField, Range(12, 16)] private int petalCount = 14;

        private struct Petal
        {
            public SpriteRenderer renderer;
            public Transform transform;
            public Vector2 origin;
            public float age, duration, endY, wind, phase;
            public uint generation;
        }

        private Petal[] petals;
        private SpriteRenderer[] ripples;
        private float rippleClock;

        public int ActivePetalCount => isActiveAndEnabled && petals != null ? petals.Length : 0;
        public int ActiveRippleCount => isActiveAndEnabled && ripples != null ? ripples.Length : 0;
        public float ElapsedVisualTime { get; private set; }

        private void Start()
        {
            if (petalSprite == null || rippleFrames == null || rippleFrames.Length != 4
                || Array.Exists(rippleFrames, frame => frame == null)
                || !ValidPoints(blossomOrigins, 2) || !ValidPoints(rippleCenters, 3)
                || petalCount < 12 || petalCount > 16)
            {
                enabled = false;
                throw new InvalidOperationException("LivingGarden requires petalSprite, four non-null rippleFrames, "
                    + "two finite blossomOrigins, three finite rippleCenters, and petalCount between 12 and 16.");
            }
            petals = new Petal[petalCount];
            ripples = new SpriteRenderer[rippleCenters.Length];
            for (int i = 0; i < petals.Length; i++)
            {
                var renderer = CreateRenderer("Blossom petal " + i, petalSprite, 1040);
                petals[i].renderer = renderer;
                petals[i].transform = renderer.transform;
                Respawn(ref petals[i], i);
                petals[i].age = petals[i].duration * (i + 0.25f) / petals.Length;
                DrawPetal(ref petals[i]);
            }
            for (int i = 0; i < ripples.Length; i++)
            {
                ripples[i] = CreateRenderer("Pond ripple " + i, rippleFrames[i], -85);
                ripples[i].transform.position = Snap(rippleCenters[i]);
            }
            SetVisible(isActiveAndEnabled);
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            ElapsedVisualTime += delta;
            rippleClock = Mathf.Repeat(rippleClock + delta * 5f, 4f);
            for (int i = 0; i < petals.Length; i++)
            {
                ref Petal petal = ref petals[i];
                petal.age += delta;
                while (petal.age >= petal.duration)
                {
                    petal.age -= petal.duration;
                    Respawn(ref petal, i);
                }
                DrawPetal(ref petal);
            }
            for (int i = 0; i < ripples.Length; i++)
            {
                int frame = Mathf.FloorToInt(Mathf.Repeat(rippleClock + i * (4f / 3f), 4f));
                if (ripples[i].sprite != rippleFrames[frame]) ripples[i].sprite = rippleFrames[frame];
            }
        }

        private void Respawn(ref Petal petal, int index)
        {
            uint seed = unchecked(0xB10550u + (uint)index * 0x9E3779B9u + petal.generation++ * 0x85EBCA6Bu);
            Vector2 tree = blossomOrigins[index % blossomOrigins.Length];
            petal.origin = tree + new Vector2(Sample(seed) * 3.5f - 1.75f, 1f + Sample(seed + 1u) * 2.5f);
            petal.duration = 9f + Sample(seed + 2u) * 4f;
            petal.endY = tree.y - 2.5f;
            petal.wind = 0.6f + Sample(seed + 3u) * 0.8f;
            petal.phase = Sample(seed + 4u) * Mathf.PI * 2f;
        }

        private static void DrawPetal(ref Petal petal)
        {
            float progress = petal.age / petal.duration;
            float x = petal.origin.x + progress * petal.wind + Mathf.Sin(petal.age * 1.2f + petal.phase) * 0.25f;
            float y = Mathf.Lerp(petal.origin.y, petal.endY, progress);
            petal.transform.position = Snap(new Vector2(x, y));
            petal.renderer.sortingOrder = 1040 - Mathf.RoundToInt(y * 16f);
        }

        private SpriteRenderer CreateRenderer(string childName, Sprite sprite, int order)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void OnEnable() => SetVisible(true);
        private void OnDisable() => SetVisible(false);

        private void SetVisible(bool visible)
        {
            if (petals != null)
                for (int i = 0; i < petals.Length; i++) petals[i].renderer.enabled = visible;
            if (ripples != null)
                for (int i = 0; i < ripples.Length; i++) ripples[i].enabled = visible;
        }

        private static Vector3 Snap(Vector2 point) => new Vector3(
            Mathf.Round(point.x * 16f) / 16f, Mathf.Round(point.y * 16f) / 16f, 0f);

        private static float Sample(uint seed)
        {
            seed ^= seed >> 16;
            seed = unchecked(seed * 0x7FEB352Du);
            seed ^= seed >> 15;
            seed = unchecked(seed * 0x846CA68Bu);
            seed ^= seed >> 16;
            return (seed & 0x00FFFFFFu) / 16777216f;
        }

        private static bool ValidPoints(Vector2[] points, int count)
        {
            if (points == null || points.Length != count) return false;
            for (int i = 0; i < points.Length; i++)
                if (float.IsNaN(points[i].x) || float.IsInfinity(points[i].x)
                    || float.IsNaN(points[i].y) || float.IsInfinity(points[i].y)) return false;
            return true;
        }
    }
}
