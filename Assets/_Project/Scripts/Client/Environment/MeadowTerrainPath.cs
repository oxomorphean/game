using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace LinhThu.Client.Environment
{
    /// <summary>Editable curved earth path; its knots affect visual terrain only.</summary>
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "LinhThu.Prototype", "LinhThu.Prototype", "MeadowTerrainPath")]
    public sealed class MeadowTerrainPath : MonoBehaviour
    {
        [Serializable]
        public struct Knot
        {
            public Vector2 position;
            [Min(.1f)]
            public float halfWidth;
            public Knot(float x, float y, float width)
            {
                position = new Vector2(x, y);
                halfWidth = width;
            }
        }

        public Knot[] knots = Array.Empty<Knot>();
        [Range(8, 64)]
        public int samplesPerSegment = 32;
        public Vector2 mapSize = new Vector2(20, 11.25f);
        private Mesh ownedMesh;
        private bool dirty;
        private void OnEnable() => Rebuild();
        private void OnValidate() => dirty = true;
        private void Update()
        {
            if (dirty)
                Rebuild();
        }

        public Vector2 Evaluate(int segment, float t)
        {
            var p0 = knots[Mathf.Max(0, segment - 1)].position;
            var p1 = knots[segment].position;
            var p2 = knots[segment + 1].position;
            var p3 = knots[Mathf.Min(knots.Length - 1, segment + 2)].position;
            return .5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t);
        }

        public void Rebuild()
        {
            dirty = false;
            if (ownedMesh == null)
                ownedMesh = new Mesh{name = "Editable earth spline", hideFlags = HideFlags.DontSave};
            ownedMesh.Clear();
            GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            var view = GetComponent<MeshRenderer>();
            view.sortingOrder = -55;
            view.shadowCastingMode = ShadowCastingMode.Off;
            view.receiveShadows = false;
            if (knots.Length < 2)
                return;
            int steps = Mathf.Max(8, samplesPerSegment), rows = (knots.Length - 1) * steps + 1;
            var vertices = new Vector3[rows * 3];
            var widths = new Vector2[vertices.Length];
            var triangles = new int[(rows - 1) * 12];
            for (int i = 0; i < rows; i++)
            {
                int segment = Mathf.Min(i / steps, knots.Length - 2);
                float t = (float)(i - segment * steps) / steps;
                var centre = Evaluate(segment, t);
                var tangent = (Evaluate(segment, Mathf.Min(1, t + .002f)) - Evaluate(segment, Mathf.Max(0, t - .002f))).normalized;
                var normal = new Vector2(-tangent.y, tangent.x);
                float width = Mathf.Max(.1f, Mathf.Lerp(knots[segment].halfWidth, knots[segment + 1].halfWidth, Mathf.SmoothStep(0, 1, t)));
                for (int side = 0; side < 3; side++)
                {
                    float across = (side - 1) * (1 + .28f / width);
                    var point = centre + normal * (across * width);
                    vertices[i * 3 + side] = new Vector3(point.x, point.y);
                    widths[i * 3 + side] = new Vector2(across, width);
                }

                if (i == rows - 1)
                    continue;
                for (int side = 0; side < 2; side++)
                {
                    int a = i * 3 + side, j = i * 12 + side * 6;
                    triangles[j] = a;
                    triangles[j + 1] = a + 3;
                    triangles[j + 2] = a + 1;
                    triangles[j + 3] = a + 1;
                    triangles[j + 4] = a + 3;
                    triangles[j + 5] = a + 4;
                }
            }

            ownedMesh.vertices = vertices;
            ownedMesh.uv2 = widths;
            ownedMesh.triangles = triangles;
            ownedMesh.RecalculateBounds();
            var block = new MaterialPropertyBlock();
            block.SetVector("_MapHalfSize", new Vector4(mapSize.x * .5f, mapSize.y * .5f, 0, 0));
            view.SetPropertyBlock(block);
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
