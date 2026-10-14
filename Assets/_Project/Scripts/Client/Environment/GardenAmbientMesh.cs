using System;
using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Client.Environment
{
    /// <summary>Attaches bounded ambient details to authored sprites without moving props or adding gameplay objects.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "LinhThu.Prototype", "LinhThu.Prototype", "GardenAmbientMesh")]
    public sealed class GardenAmbientMesh : MonoBehaviour
    {
        private SpriteRenderer source;
        private GardenAccentKind kind;
        private Mesh mesh;
        private MeshRenderer view;
        private VertexHelper vertices;
        private Rect spriteRect;
        private int seed;
        public static void Attach(ModularMapProp prop, Material spriteMaterial)
        {
            if (prop.visual == null || !GardenAmbientArt.TryKind(prop.visual.sprite, out var kind))
                return;
            if (prop.visual.GetComponentInChildren<GardenAmbientMesh>(true) != null)
                return;
            var node = new GameObject(kind == GardenAccentKind.Cherry ? "Cánh hoa rơi · cỏ ôm rễ" : "Gợn hồ nhẹ");
            node.transform.SetParent(prop.visual.transform, false);
            node.AddComponent<GardenAmbientMesh>().Initialize(prop.visual, kind, spriteMaterial);
        }

        private void Initialize(SpriteRenderer renderer, GardenAccentKind accent, Material template)
        {
            source = renderer;
            kind = accent;
            seed = source.transform.parent.GetEntityId().GetHashCode();
            var bounds = source.sprite.bounds;
            spriteRect = new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
            if (template == null)
                throw new InvalidOperationException("Garden ambience requires the daylight sprite material.");
            mesh = new Mesh{name = gameObject.name};
            mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            view = gameObject.AddComponent<MeshRenderer>();
            view.sharedMaterial = template;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            view.SetPropertyBlock(properties);
            view.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view.receiveShadows = false;
            vertices = new VertexHelper();
            Draw();
        }

        private void LateUpdate()
        {
            if (source != null)
                Draw();
        }

        private void Draw()
        {
            view.enabled = source.enabled;
            view.sortingLayerID = source.sortingLayerID;
            view.sortingOrder = source.sortingOrder + 1;
            GardenAmbientArt.Draw(vertices, spriteRect, kind, Time.unscaledTime, seed, Color.white);
            vertices.FillMesh(mesh);
            mesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            vertices?.Dispose();
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
