using UnityEngine;

namespace LinhThu.Client.Environment
{
    /// <summary>Placeable environment prop; the root position is its ground contact point.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "LinhThu.Prototype", "LinhThu.Prototype", "ModularMapProp")]
    public sealed class ModularMapProp : MonoBehaviour
    {
        public SpriteRenderer visual;
        public SpriteRenderer castShadow;
        public SpriteRenderer contactShadow;
        public bool floorLayer;
        public int floorOrder = LinhThu.Client.World.WorldRenderOrder.Surface;
        public SunShadowSettings shadowSettings = SunShadowSettings.Default;
        private SunShadowMesh groundShadow;
        private MaterialPropertyBlock shadowProperties;
        private MaterialPropertyBlock surfaceProperties;
        private bool refreshProperties;
        private void OnEnable()
        {
            if (visual != null && visual.sprite != null)
            {
                if (surfaceProperties == null)
                    surfaceProperties = new MaterialPropertyBlock();
                visual.GetPropertyBlock(surfaceProperties);
                bool pond = visual.sprite.name == "pond-cluster";
                surfaceProperties.SetFloat("_SurfaceRelief", floorLayer ? 0 : .18f);
                surfaceProperties.SetFloat("_WaterSurface", pond ? 1 : 0);
                visual.SetPropertyBlock(surfaceProperties);
            }

            RefreshShadow();
            UpdateDepth();
            if (castShadow != null && castShadow.sprite != null)
                castShadow.localBounds = new Bounds(Vector3.zero, Vector3.one * castShadow.sprite.bounds.size.magnitude * 4);
        }

        private float ReferenceHeight => visual.sprite.rect.height / visual.sprite.pixelsPerUnit * Mathf.Abs(visual.transform.lossyScale.y);
        private void Start() => RefreshShadow();

        private void OnValidate() => refreshProperties = true;
        private void LateUpdate()
        {
            if (refreshProperties)
            {
                refreshProperties = false;
                OnEnable();
            }

            UpdateDepth();
            RefreshShadow();
        }

        private void RefreshShadow()
        {
            if (floorLayer || castShadow == null || visual == null || visual.sprite == null)
            {
                ReleaseShadow();
                return;
            }
            if (shadowProperties == null) shadowProperties = new MaterialPropertyBlock();
            castShadow.GetPropertyBlock(shadowProperties);
            shadowSettings.Apply(shadowProperties, transform.position, ReferenceHeight);
            castShadow.SetPropertyBlock(shadowProperties);
            if (groundShadow == null && gameObject.scene.IsValid()) groundShadow = new SunShadowMesh(castShadow);
            groundShadow?.Refresh(shadowSettings, transform.position, ReferenceHeight);
        }

        private void ReleaseShadow() { groundShadow?.Dispose(); groundShadow = null; }
        private void OnDisable() => ReleaseShadow();
        private void OnDestroy() => ReleaseShadow();
        private void UpdateDepth()
        {
            GetComponent<LinhThu.Client.World.YSortRenderer>()?.Refresh();
            // Cast shadows stay on the terrain, including when a character passes underneath a canopy.
            if (castShadow != null)
                castShadow.sortingOrder = LinhThu.Client.World.WorldRenderOrder.CastShadow;
            if (groundShadow != null)
                groundShadow.Renderer.sortingOrder = LinhThu.Client.World.WorldRenderOrder.CastShadow;
            if (contactShadow != null)
                contactShadow.sortingOrder = LinhThu.Client.World.WorldRenderOrder.ContactShadow;
        }
    }
}
