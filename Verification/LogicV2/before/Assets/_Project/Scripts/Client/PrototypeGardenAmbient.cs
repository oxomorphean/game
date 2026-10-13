using System;
using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Prototype
{
    /// <summary>Reuses garden petals and four pond glint frames in the gameplay Canvas; visuals only.</summary>
    public sealed class PrototypeGardenAmbient : MonoBehaviour
    {
        private struct Petal
        {
            public RectTransform rect;
            public Vector2 origin;
            public float age, lifetime, endY, drift, phase;
            public uint generation;
        }
        private Petal[] petals;
        private Image[] ripples;
        private Sprite[] rippleFrames;
        private float clock;
        public float VisualTime => clock;
        public int PetalCount => petals == null ? 0 : petals.Length;
        public int RippleCount => ripples == null ? 0 : ripples.Length;

        public void Bind(Sprite petal, Sprite[] frames)
        {
            if (petal == null || frames == null || frames.Length != 4 || Array.Exists(frames, x => x == null))
                throw new InvalidOperationException("Ambient garden needs petal and four pond glint sprites.");
            rippleFrames = frames;
            petals = new Petal[14]; ripples = new Image[3];
            for (int i = 0; i < petals.Length; i++)
            {
                petals[i].rect = PrototypeMapArt.Picture(transform, "Falling cherry petal " + i, petal, new Rect(0, 0, 12, 12)).rectTransform;
                Respawn(ref petals[i], i);
                petals[i].age = petals[i].lifetime * (i + .25f) / petals.Length;
                Draw(ref petals[i]);
            }
            var centers = new[] { new Vector2(1024, 336), new Vector2(1080, 325), new Vector2(1096, 361) };
            for (int i = 0; i < ripples.Length; i++)
                ripples[i] = PrototypeMapArt.Picture(transform, "Pond glint " + i, frames[i], new Rect(centers[i].x - 12, centers[i].y - 12, 24, 24));
        }

        private void Update()
        {
            if (petals == null) return;
            float delta = Time.unscaledDeltaTime;
            clock += delta;
            for (int i = 0; i < petals.Length; i++)
            {
                ref Petal petal = ref petals[i]; petal.age += delta;
                if (petal.age >= petal.lifetime) { petal.age %= petal.lifetime; Respawn(ref petal, i); }
                Draw(ref petal);
            }
            for (int i = 0; i < ripples.Length; i++)
                ripples[i].sprite = rippleFrames[(Mathf.FloorToInt(clock * 5) + i) % rippleFrames.Length];
        }

        private static void Respawn(ref Petal petal, int index)
        {
            uint seed = unchecked(0xB10550u + (uint)index * 0x9E3779B9u + petal.generation++ * 0x85EBCA6Bu);
            Vector2 tree = index % 2 == 0 ? new Vector2(75, 187) : new Vector2(815, 158);
            petal.origin = tree + new Vector2(Sample(seed) * 138 - 69, -160 + Sample(seed + 1) * 70);
            petal.lifetime = 9 + Sample(seed + 2) * 4;
            petal.endY = tree.y + 24;
            petal.drift = 18 + Sample(seed + 3) * 20;
            petal.phase = Sample(seed + 4) * Mathf.PI * 2;
        }

        private static void Draw(ref Petal petal)
        {
            float progress = petal.age / petal.lifetime;
            float x = petal.origin.x + progress * petal.drift + Mathf.Sin(petal.age * 1.2f + petal.phase) * 5;
            float y = Mathf.Lerp(petal.origin.y, petal.endY, progress);
            petal.rect.anchoredPosition = new Vector2(Mathf.Round(x / 1.5f) * 1.5f, -Mathf.Round(y / 1.5f) * 1.5f);
        }

        private static float Sample(uint seed)
        {
            seed ^= seed >> 16; seed = unchecked(seed * 0x7FEB352Du);
            seed ^= seed >> 15; seed = unchecked(seed * 0x846CA68Bu); seed ^= seed >> 16;
            return (seed & 0x00FFFFFFu) / 16777216f;
        }
    }
}
