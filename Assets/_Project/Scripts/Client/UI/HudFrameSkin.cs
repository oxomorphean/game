using System;
using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Client.UI
{
    /// <summary>One antialiased, six-pixel-radius nine-slice shared by the HUD's panels.</summary>
    public sealed class HudFrameSkin : IDisposable
    {
        public Sprite Frame { get; }
        public Sprite Solid { get; }
        private readonly Texture2D texture;
        public HudFrameSkin()
        {
            const int size = 32;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Ink and jade HUD frame", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            Color ink = new Color32(14, 26, 28, 224), bronze = new Color32(200, 178, 115, 128), jade = new Color32(94, 158, 139, 144);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var q = new Vector2(Mathf.Abs(x + .5f - size / 2f), Mathf.Abs(y + .5f - size / 2f)) - Vector2.one * (size / 2f - 6);
                    float distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - 6;
                    var color = distance > -1.5f ? bronze : distance > -3.5f && distance < -2.5f ? jade : ink;
                    color.a *= Mathf.Clamp01(.5f - distance);
                    pixels[y * size + x] = color;
                }
            texture.SetPixels(pixels); texture.Apply(false, true);
            Frame = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * 8);
            Frame.name = "Ink and jade nine-slice";
            Solid = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 100);
        }
        public void Apply(Image image) { image.sprite = Frame; image.type = Image.Type.Sliced; image.color = Color.white; }
        public void Dispose() { Destroy(Frame); Destroy(Solid); Destroy(texture); }
        private static void Destroy(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
