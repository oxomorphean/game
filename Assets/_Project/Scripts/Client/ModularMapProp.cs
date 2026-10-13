using UnityEngine;

namespace LinhThu.Prototype
{
    /// <summary>Placeable environment prop; the root position is its ground contact point.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ModularMapProp : MonoBehaviour
    {
        public SpriteRenderer visual;
        public SpriteRenderer castShadow;
        public SpriteRenderer contactShadow;
        public bool floorLayer;
        public int floorOrder = -40;
        private Mesh shadowMesh;
        private MeshRenderer groundShadow;
        private MaterialPropertyBlock shadowProperties;

        /// <summary>Silhouette center/depth, contact or canopy height, and kind (1: detail, 2: tree, 3: rock).</summary>
        public static Vector4 ShadowFootprint(Sprite sprite)
        {
            bool tree = sprite.name == "cherry-tree";
            bool rock = sprite.name == "rock";
            float height = sprite.rect.height / sprite.pixelsPerUnit;
            float center = tree ? .64f : .52f;
            return new Vector4(rock ? 0 : (center - sprite.pivot.y / sprite.rect.height) * height,
                tree ? .58f : .45f, height * (tree ? .20f : rock ? 0 : .10f), tree ? 2 : rock ? 3 : 1);
        }

        private void OnEnable()
        {
            if (!floorLayer && castShadow != null && visual != null && visual.sprite != null)
            {
                var source = visual.sprite;
                if (shadowProperties == null) shadowProperties = new MaterialPropertyBlock();
                castShadow.GetPropertyBlock(shadowProperties);
                shadowProperties.SetVector("_ShadowFootprint", ShadowFootprint(source)); castShadow.SetPropertyBlock(shadowProperties);
            }
            UpdateDepth();
            if (castShadow != null && castShadow.sprite != null)
                castShadow.localBounds = new Bounds(Vector3.zero, Vector3.one * castShadow.sprite.bounds.size.magnitude * 4);
        }
        private void Start()
        {
            if (!Application.isPlaying || floorLayer || castShadow == null || visual == null || visual.sprite == null) return;
            var source = visual.sprite;
            // A regular mesh supports the canopy fade without requiring readable sprite texture data.
            const int cols = 4, rows = 24;
            var vertices = new Vector3[(cols + 1) * (rows + 1)];
            var colors = new Color32[vertices.Length]; var uv = new Vector2[vertices.Length];
            var triangles = new int[cols * rows * 6];
            for (int y = 0; y <= rows; y++)
            for (int x = 0; x <= cols; x++)
            {
                int n = y * (cols + 1) + x; float u = (float)x / cols, v = (float)y / rows;
                vertices[n] = new Vector3((source.rect.width * u - source.pivot.x) / source.pixelsPerUnit, (source.rect.height * v - source.pivot.y) / source.pixelsPerUnit);
                uv[n] = new Vector2((source.rect.x + source.rect.width * u) / source.texture.width, (source.rect.y + source.rect.height * v) / source.texture.height);
                colors[n] = new Color32(255, 255, 255, 255);
            }
            int t = 0;
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
            {
                int a = y * (cols + 1) + x, b = a + 1, c = a + cols + 1, d = c + 1;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
            shadowMesh = new Mesh { name = source.name + " · ground silhouette", vertices = vertices, uv = uv, colors32 = colors, triangles = triangles };
            var node = new GameObject("Bóng silhouette bám chân"); node.transform.SetParent(castShadow.transform, false);
            node.AddComponent<MeshFilter>().sharedMesh = shadowMesh;
            groundShadow = node.AddComponent<MeshRenderer>(); groundShadow.sharedMaterial = castShadow.sharedMaterial;
            groundShadow.sortingLayerID = castShadow.sortingLayerID; groundShadow.sortingOrder = -30;
            groundShadow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; groundShadow.receiveShadows = false;
            shadowProperties.SetTexture("_MainTex", source.texture); groundShadow.SetPropertyBlock(shadowProperties);
            groundShadow.localBounds = new Bounds(Vector3.zero, Vector3.one * source.bounds.size.magnitude * 4);
            castShadow.enabled = false;
        }
        private void OnValidate() => UpdateDepth();
        private void LateUpdate() => UpdateDepth();
        private void OnDestroy() { if (shadowMesh != null) Destroy(shadowMesh); }

        private void UpdateDepth()
        {
            if (visual != null)
                visual.sortingOrder = floorLayer ? floorOrder : 100 - Mathf.RoundToInt(transform.position.y * 10);
            // Cast shadows stay on the terrain, including when a character passes underneath a canopy.
            if (castShadow != null) castShadow.sortingOrder = -30;
            if (groundShadow != null) groundShadow.sortingOrder = -30;
            if (contactShadow != null) contactShadow.sortingOrder = floorLayer ? floorOrder - 1 : -28;
        }
    }
}
