using UnityEngine;
using UnityEngine.Rendering;

namespace LinhThu.Client.Environment
{
    /// <summary>A meadow floor built from a reusable world-mapped grass material.</summary>
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "LinhThu.Prototype", "LinhThu.Prototype", "MeadowGroundSurface")]
    public sealed class MeadowGroundSurface : MonoBehaviour
    {
        public Vector2 size = new Vector2(20, 11.25f);
        [Tooltip("Local centre and radii of the softly shaded, cooler grass by the water.")]
        public Vector4 coolShorePatch = new Vector4(7.25f, -.4f, 3, 2);
        [Range(0, .4f)] public float dappledStrength = .14f;
        [Min(.1f)] public float sunlightPatchSize = 2.8f;
        [Range(0, .2f)] public float sunlightDrift = .025f;
        private Mesh ownedMesh;
        private bool dirty;
        private void OnEnable() => Rebuild();
        private void OnValidate() => dirty = true;
        private void Update()
        {
            if (dirty)
                Rebuild();
        }

        public void Rebuild()
        {
            dirty = false;
            if (ownedMesh == null)
                ownedMesh = new Mesh{name = "Editable meadow floor", hideFlags = HideFlags.DontSave};
            var half = size * .5f;
            ownedMesh.Clear();
            ownedMesh.vertices = new[]{new Vector3(-half.x, -half.y), new Vector3(half.x, -half.y), new Vector3(half.x, half.y), new Vector3(-half.x, half.y)};
            ownedMesh.triangles = new[]{0, 2, 1, 0, 3, 2};
            ownedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            var view = GetComponent<MeshRenderer>();
            view.sortingOrder = LinhThu.Client.World.WorldRenderOrder.Grass;
            view.shadowCastingMode = ShadowCastingMode.Off;
            view.receiveShadows = false;
            var properties = new MaterialPropertyBlock();
            view.GetPropertyBlock(properties);
            ApplySurfaceProperties(properties);
            view.SetPropertyBlock(properties);
        }

        public void ApplySurfaceProperties(MaterialPropertyBlock properties)
        {
            properties.SetVector("_DappledLight", new Vector4(dappledStrength, sunlightPatchSize, sunlightDrift, 0));
            var center = transform.TransformPoint(new Vector3(coolShorePatch.x, coolShorePatch.y));
            properties.SetVector("_CoolPatch", new Vector4(center.x, center.y,
                Mathf.Max(.1f, coolShorePatch.z * Mathf.Abs(transform.lossyScale.x)),
                Mathf.Max(.1f, coolShorePatch.w * Mathf.Abs(transform.lossyScale.y))));
        }

        private void OnDestroy()
        {
            if (ownedMesh == null)
                return;
            if (Application.isPlaying)
                Destroy(ownedMesh);
            else
                DestroyImmediate(ownedMesh);
        }
    }
}
