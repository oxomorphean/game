using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Prototype
{
    /// <summary>Small Canvas deformation for the gameplay prototype; art-preview actors use SpriteSkin.</summary>
    public sealed class CartoonUiRig : BaseMeshEffect
    {
        public bool Moving, Human;
        private float motionBlend, stridePhase;
        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            motionBlend = Mathf.MoveTowards(motionBlend, Moving ? 1 : 0, dt * 8);
            stridePhase += dt * 10 * motionBlend;
            if (graphic != null && isActiveAndEnabled) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            var image = graphic as Image;
            if (image == null || image.sprite == null) return;
            var rect = graphic.rectTransform.rect;
            var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(image.sprite); mesh.Clear();
            const int cols = 18, rows = 12;
            float amount = Moving ? 1 : .1f, phase = Time.unscaledTime * (Moving ? 10 : 3);
            for (int y = 0; y <= rows; y++)
            for (int x = 0; x <= cols; x++)
            {
                float u = (float)x / cols, v = (float)y / rows, dx = 0, dy = 0;
                float threshold = Human ? .30f : .37f;
                if (Human)
                {
                    float stride = Mathf.Sin(stridePhase), foot = u < .5f ? stride : -stride;
                    float weight = Mathf.Clamp01((.34f - v) / .17f);
                    if (v < .34f && u > .30f && u < .66f)
                    {
                        dx = foot * .018f * motionBlend * weight;
                        dy = Mathf.Max(0, foot) * .033f * motionBlend * weight;
                    }
                    else if (v > .34f)
                        dy = (1 - Mathf.Cos(stridePhase * 2)) * .003f * motionBlend + Mathf.Sin(Time.unscaledTime * 2.8f) * .0018f * (1 - motionBlend);
                }
                else if (v < threshold)
                {
                    float weight = Mathf.Clamp01((threshold + .02f - v) / .17f);
                    float direction = u < (Human ? .50f : .40f) ? -1 : 1;
                    dx = Mathf.Sin(phase) * .008f * amount * weight * direction;
                    dy = Mathf.Abs(Mathf.Sin(phase)) * .008f * amount * weight;
                }
                else if (Human ? v > .52f : u > .70f) dy = Mathf.Sin(phase) * .002f * amount;
                mesh.AddVert(new Vector3(rect.xMin + (u + dx) * rect.width, rect.yMin + (v + dy) * rect.height, 0),
                    graphic.color, new Vector2(Mathf.Lerp(uv.x, uv.z, u), Mathf.Lerp(uv.y, uv.w, v)));
            }
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
            {
                int n = y * (cols + 1) + x;
                mesh.AddTriangle(n, n + cols + 1, n + 1); mesh.AddTriangle(n + 1, n + cols + 1, n + cols + 2);
            }
        }
    }
}
