using System;
using System.Collections.Generic;
using UnityEngine;

namespace LinhThu.Prototype
{
    /// <summary>Subdivides a sprite silhouette at the ground line; sprite swaps update UVs without changing the foot anchor.</summary>
    public sealed class SunShadowMesh : IDisposable
    {
        readonly Mesh mesh;
        readonly GameObject node;
        readonly SpriteRenderer source;
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        Sprite sprite;
        Vector2[] normalized;
        Rect previousRect;
        Vector2 previousPivot;
        float previousPpu;
        float previousCanopy, previousCenter, previousContact;
        public MeshRenderer Renderer { get; }

        public SunShadowMesh(SpriteRenderer silhouette)
        {
            source = silhouette;
            node = new GameObject("Bóng mặt trời · silhouette");
            node.transform.SetParent(source.transform, false);
            mesh = new Mesh { name = "Sun shadow silhouette" };
            mesh.MarkDynamic();
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            Renderer = node.AddComponent<MeshRenderer>();
            Renderer.sharedMaterial = source.sharedMaterial;
            Renderer.sortingLayerID = source.sortingLayerID;
            Renderer.sortingOrder = source.sortingOrder;
            Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Renderer.receiveShadows = false;
            source.enabled = false;
        }

        public void Refresh(SunShadowSettings settings, Vector3 foot, float referenceHeight)
        {
            var next = source.sprite;
            Renderer.enabled = next != null && source.gameObject.activeInHierarchy;
            if (next == null) return;
            bool profileChanged = previousCanopy != settings.canopyBlueRatio || previousCenter != settings.center || previousContact != settings.contactHeight;
            if (sprite != next || profileChanged)
            {
                if (normalized == null || previousRect.size != next.rect.size || previousPivot != next.pivot || previousPpu != next.pixelsPerUnit || profileChanged)
                    Rebuild(next, settings, source.transform.InverseTransformPoint(foot));
                var uv = new Vector2[normalized.Length];
                for (int i = 0; i < uv.Length; i++)
                    uv[i] = new Vector2((next.rect.x + normalized[i].x * next.rect.width) / next.texture.width,
                        (next.rect.y + normalized[i].y * next.rect.height) / next.texture.height);
                mesh.uv = uv;
                properties.SetTexture("_MainTex", next.texture);
                sprite = next;
            }
            settings.Apply(properties, foot, referenceHeight);
            properties.SetColor("_Color", source.color);
            properties.SetVector("_Flip", new Vector4(source.flipX ? -1 : 1, source.flipY ? -1 : 1, 1, 1));
            Renderer.SetPropertyBlock(properties);
            Renderer.sortingOrder = source.sortingOrder;
        }

        void Rebuild(Sprite next, SunShadowSettings settings, Vector3 foot)
        {
            const int columns = 24, rows = 32;
            var ys = new List<float>();
            for (int y = 0; y <= rows; y++) ys.Add((float)y / rows);
            float contactRow = next.pivot.y / next.rect.height;
            if (contactRow > 0 && contactRow < 1 && !ys.Exists(y => Mathf.Abs(y - contactRow) < .00001f)) ys.Add(contactRow);
            ys.Sort();
            int gridCount = (columns + 1) * ys.Count;
            bool canopy = settings.canopyBlueRatio > 0;
            var vertices = new Vector3[gridCount + (canopy ? 4 : 0)];
            normalized = new Vector2[vertices.Length];
            var colors = new Color32[vertices.Length];
            for (int y = 0; y < ys.Count; y++)
            for (int x = 0; x <= columns; x++)
            {
                int n = y * (columns + 1) + x;
                normalized[n] = new Vector2((float)x / columns, ys[y]);
                vertices[n] = (Vector2.Scale(normalized[n], next.rect.size) - next.pivot) / next.pixelsPerUnit;
                colors[n] = new Color32(255, 255, 255, 255);
            }
            var triangles = new int[columns * (ys.Count - 1) * 6 + (canopy ? 6 : 0)];
            int t = 0;
            for (int y = 0; y < ys.Count - 1; y++)
            for (int x = 0; x < columns; x++)
            {
                int a = y * (columns + 1) + x, b = a + 1, c = a + columns + 1, d = c + 1;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
            if (canopy)
            {
                float height = next.rect.height / next.pixelsPerUnit;
                float width = height * settings.contactHeight * .45f;
                vertices[gridCount] = foot + new Vector3(-width, 0);
                vertices[gridCount + 1] = foot + new Vector3(width, 0);
                vertices[gridCount + 2] = foot + new Vector3(-width * .7f, height * settings.center);
                vertices[gridCount + 3] = foot + new Vector3(width * .7f, height * settings.center);
                colors[gridCount] = colors[gridCount + 2] = new Color32(0, 0, 0, 0);
                colors[gridCount + 1] = colors[gridCount + 3] = new Color32(255, 0, 0, 0);
                triangles[t++] = gridCount; triangles[t++] = gridCount + 2; triangles[t++] = gridCount + 1;
                triangles[t++] = gridCount + 1; triangles[t++] = gridCount + 2; triangles[t++] = gridCount + 3;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.colors32 = colors; mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * next.bounds.size.magnitude * 6);
            previousRect = next.rect; previousPivot = next.pivot; previousPpu = next.pixelsPerUnit;
            previousCanopy = settings.canopyBlueRatio; previousCenter = settings.center; previousContact = settings.contactHeight;
        }

        public void Dispose()
        {
            if (Application.isPlaying) { UnityEngine.Object.Destroy(node); UnityEngine.Object.Destroy(mesh); }
            else { UnityEngine.Object.DestroyImmediate(node); UnityEngine.Object.DestroyImmediate(mesh); }
        }
    }
}
