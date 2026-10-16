using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Core;
using LinhThu.Domain.World;
using UnityEngine;
using EntityId = LinhThu.Domain.Core.EntityId;

namespace LinhThu.Client.Combat
{
    /// <summary>Ground warning from the committed cast; the mesh never resolves hits.</summary>
    public sealed class AttackTelegraphPresenter : MonoBehaviour
    {
        public Material material;
        private WorldState world;
        private sealed class Warning
        {
            public GameObject Root;
            public Mesh Mesh;
        }
        private readonly Dictionary<EntityId, Warning> active = new Dictionary<EntityId, Warning>();
        private readonly Stack<Warning> pool = new Stack<Warning>();
        public int VisibleCount => active.Count;
        public void Bind(WorldState value)
        {
            if (material == null) throw new InvalidOperationException("Telegraph requires an authored material.");
            ReleaseAll(); world = value;
        }
        public void Refresh()
        {
            if (world == null) return;
            var casts = world.Actors.Values.Where(a => a.Abilities.Cast != null && a.State == ActorActionState.Casting).ToArray();
            var live = new HashSet<EntityId>(casts.Select(a => a.Id));
            foreach (var id in active.Keys.Where(id => !live.Contains(id)).ToArray()) Release(id);
            foreach (var actor in casts)
            {
                var cast = actor.Abilities.Cast;
                if (!world.Content.TryGetTechnique(cast.TechniqueId, out var technique) || technique.CastDuration <= 0) continue;
                if (!active.TryGetValue(actor.Id, out var warning))
                {
                    warning = pool.Count > 0 ? pool.Pop() : Create(); active.Add(actor.Id, warning); warning.Root.SetActive(true);
                }
                var point = technique.Delivery.Shape == DeliveryShape.CircleAoE && cast.Aim.LockedPoint.HasValue
                    ? cast.Aim.LockedPoint.Value : actor.Transform.Position;
                warning.Root.transform.position = new Vector3(point.X, point.Y, 0);
                warning.Root.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(cast.Aim.Direction.Y, cast.Aim.Direction.X) * Mathf.Rad2Deg);
                bool hostile = world.Relations.IsHostile(actor.Faction, world.PlayerActor.Faction);
                Draw(warning.Mesh, technique.Delivery, Mathf.Clamp01(1 - cast.WindupRemaining / technique.CastDuration), hostile);
            }
        }
        private Warning Create()
        {
            var root = new GameObject("Attack telegraph", typeof(MeshFilter), typeof(MeshRenderer)); root.transform.SetParent(transform, false);
            var mesh = new Mesh { name = "Cast area", hideFlags = HideFlags.DontSave };
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.sortingOrder = -7900; // Above shadows, below every actor.
            return new Warning { Root = root, Mesh = mesh };
        }
        public static Vector2[] Outline(DeliverySpec delivery)
        {
            const int segments = 48;
            if (delivery.Shape == DeliveryShape.LineRay || delivery.Shape == DeliveryShape.Projectile)
            {
                var points = new List<Vector2>();
                for (int i = 0; i <= segments / 2; i++)
                {
                    float a = -Mathf.PI * .5f + Mathf.PI * i / (segments / 2);
                    points.Add(new Vector2(delivery.Range + Mathf.Cos(a) * delivery.Radius, Mathf.Sin(a) * delivery.Radius));
                }
                for (int i = 0; i <= segments / 2; i++)
                {
                    float a = Mathf.PI * .5f + Mathf.PI * i / (segments / 2);
                    points.Add(new Vector2(Mathf.Cos(a) * delivery.Radius, Mathf.Sin(a) * delivery.Radius));
                }
                return points.ToArray();
            }
            bool cone = delivery.Shape == DeliveryShape.ConeSector;
            float angle = cone ? delivery.AngleDegrees * Mathf.Deg2Rad : Mathf.PI * 2;
            float radius = cone ? delivery.Range : delivery.Radius;
            var outline = new List<Vector2>();
            if (cone && delivery.AngleDegrees < 360) outline.Add(Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float a = -angle * .5f + angle * i / segments;
                outline.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
            return outline.ToArray();
        }
        private static void Draw(Mesh mesh, DeliverySpec delivery, float progress, bool hostile)
        {
            var outline = Outline(delivery);
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
            void Layer(float scale, float alpha)
            {
                int start = vertices.Count;
                var center = delivery.Shape == DeliveryShape.LineRay || delivery.Shape == DeliveryShape.Projectile ? new Vector2(delivery.Range * .5f, 0) : Vector2.zero;
                vertices.Add(center * scale); colors.Add(hostile ? new Color(1, .23f, .12f, alpha) : new Color(.25f, .8f, .7f, alpha));
                foreach (var point in outline) { vertices.Add(point * scale); colors.Add(colors[start]); }
                for (int i = 0; i < outline.Length; i++) { triangles.Add(start); triangles.Add(start + 1 + (i + 1) % outline.Length); triangles.Add(start + 1 + i); }
            }
            Layer(1, .18f); Layer(Mathf.Max(.001f, progress), .42f);
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
        }
        private void Release(EntityId id)
        {
            var warning = active[id]; warning.Root.SetActive(false); active.Remove(id); pool.Push(warning);
        }
        private void ReleaseAll() { foreach (var id in active.Keys.ToArray()) Release(id); }
        private void LateUpdate() => Refresh();
        private void OnDisable() => ReleaseAll();
        private void OnDestroy()
        {
            ReleaseAll();
            foreach (var warning in pool)
                if (Application.isPlaying) Destroy(warning.Mesh); else DestroyImmediate(warning.Mesh);
            pool.Clear();
        }
    }
}
