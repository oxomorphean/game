using UnityEngine;
using UnityEngine.Rendering;

namespace LinhThu.Prototype
{
    /// <summary>A meadow floor built from a reusable world-mapped grass material.</summary>
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class MeadowGroundSurface : MonoBehaviour
    {
        public Vector2 size = new Vector2(20, 11.25f);
        private Mesh ownedMesh;
        private bool dirty;

        private void OnEnable() => Rebuild();
        private void OnValidate() => dirty = true;
        private void Update() { if (dirty) Rebuild(); }

        public void Rebuild()
        {
            dirty = false;
            if (ownedMesh == null) ownedMesh = new Mesh { name = "Editable meadow floor", hideFlags = HideFlags.DontSave };
            var half = size * .5f;
            ownedMesh.Clear();
            ownedMesh.vertices = new[] { new Vector3(-half.x, -half.y), new Vector3(half.x, -half.y), new Vector3(half.x, half.y), new Vector3(-half.x, half.y) };
            ownedMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            ownedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            var view = GetComponent<MeshRenderer>();
            view.sortingOrder = -60;
            view.shadowCastingMode = ShadowCastingMode.Off;
            view.receiveShadows = false;
        }

        private void OnDestroy()
        {
            if (ownedMesh == null) return;
            if (Application.isPlaying) Destroy(ownedMesh); else DestroyImmediate(ownedMesh);
        }
    }
}
