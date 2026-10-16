using System.Collections.Generic;
using System.Linq;
using LinhThu.Client.World;
using LinhThu.Domain.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace LinhThu.Client.Actors
{
    /// <summary>Reads committed casts/dash state. Poses and frozen silhouettes never change the actor's simulation.</summary>
    [DefaultExecutionOrder(-20), DisallowMultipleComponent]
    public sealed class ActorActionFeedback : MonoBehaviour
    {
        public ActorView view;
        public Transform chest, upperArm, forearm, spiritSword;
        public Material afterimageMaterial;
        public float CastWeight => Mathf.Clamp01(castRemaining / .32f);
        public int ActiveAfterimages => ghosts.Count(g => g.remaining > 0);
        private float castRemaining, dashElapsed;
        private int emitted;
        private bool wasDashing;
        private Vector3 swordRest;
        private bool captured;
        private readonly Dictionary<Transform, Quaternion> animated = new Dictionary<Transform, Quaternion>();
        private readonly List<Ghost> ghosts = new List<Ghost>();
        private readonly List<Vector3> ghostVertices = new List<Vector3>();
        private sealed class Ghost
        {
            public GameObject root;
            public Mesh[] meshes;
            public MeshRenderer[] parts;
            public float remaining;
        }
        private void Update() => Restore();
        private void LateUpdate() => Present(Time.deltaTime, Time.time);
        public void OnCast(WorldPoint direction) { castRemaining = .32f; }
        public void ObserveState()
        {
            bool dashing = view != null && view.Entity != null && view.Entity.Dash.Active;
            if (dashing && !wasDashing) { dashElapsed = 0; emitted = 0; }
            wasDashing = dashing;
        }
        public void Present(float delta, float time)
        {
            if (view == null || view.Entity == null) return;
            ObserveState();
            if (!captured && spiritSword != null) { swordRest = spiritSword.localPosition; captured = true; }
            if (spiritSword != null)
            {
                spiritSword.gameObject.SetActive(view.Entity.Vitals.IsAlive);
                spiritSword.localPosition = swordRest + Vector3.up * (Mathf.Sin(time * 2.2f) * .065f);
                spiritSword.localRotation = Quaternion.Euler(0, 0, -22 + Mathf.Sin(time * 1.6f) * 5);
            }
            if (castRemaining > 0 && !view.Entity.HasLifeActivity)
            {
                float weight = Mathf.Sin(CastWeight * Mathf.PI * .5f);
                Pose(chest, -12 * weight); Pose(upperArm, -42 * weight); Pose(forearm, 28 * weight);
            }
            castRemaining = Mathf.Max(0, castRemaining - Mathf.Max(0, delta));
            foreach (var ghost in ghosts)
            {
                if (ghost.remaining <= 0) continue;
                ghost.remaining = Mathf.Max(0, ghost.remaining - Mathf.Max(0, delta));
                ghost.root.SetActive(ghost.remaining > 0);
                foreach (var part in ghost.parts)
                {
                    var block = new MaterialPropertyBlock(); part.GetPropertyBlock(block);
                    block.SetColor("_Color", new Color(.52f, .94f, .82f, .28f * ghost.remaining / .28f)); part.SetPropertyBlock(block);
                }
            }
            if (wasDashing)
            {
                dashElapsed += Mathf.Max(0, delta);
                if (emitted < 3 && dashElapsed >= emitted * .055f) { CaptureGhost(); emitted++; }
            }
        }
        private void Pose(Transform bone, float angle)
        {
            if (bone == null) return;
            if (!animated.ContainsKey(bone)) animated.Add(bone, bone.localRotation);
            bone.localRotation = animated[bone] * Quaternion.Euler(0, 0, angle);
        }
        private void Restore()
        {
            foreach (var pair in animated) if (pair.Key != null) pair.Key.localRotation = pair.Value;
            animated.Clear();
        }
        private void CaptureGhost()
        {
            if (view.equipment == null || afterimageMaterial == null) return;
            var slots = view.equipment.slots;
            Ghost ghost = ghosts.FirstOrDefault(g => g.remaining <= 0);
            if (ghost == null)
            {
                if (ghosts.Count == 3) ghost = ghosts.OrderBy(g => g.remaining).First();
                else
                {
                    ghost = new Ghost { root = new GameObject("DashAfterimage"), meshes = new Mesh[slots.Length], parts = new MeshRenderer[slots.Length] };
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ghost.root, gameObject.scene);
                    ghost.root.AddComponent<SortingGroup>();
                    for (int i = 0; i < slots.Length; i++)
                    {
                        var node = new GameObject(slots[i].id); node.transform.SetParent(ghost.root.transform, false);
                        ghost.meshes[i] = new Mesh { name = "Frozen dash pose" }; ghost.meshes[i].MarkDynamic();
                        node.AddComponent<MeshFilter>().sharedMesh = ghost.meshes[i];
                        ghost.parts[i] = node.AddComponent<MeshRenderer>(); ghost.parts[i].sharedMaterial = afterimageMaterial;
                    }
                    ghosts.Add(ghost);
                }
            }
            ghost.root.GetComponent<SortingGroup>().sortingOrder = WorldRenderOrder.AtGround(view.transform.position.y) - 1;
            ghost.remaining = .28f; ghost.root.SetActive(true);
            for (int i = 0; i < slots.Length; i++)
            {
                var source = slots[i].renderer; var sprite = source.sprite; var part = ghost.parts[i];
                part.enabled = source.enabled && sprite != null;
                if (!part.enabled) continue;
                var mesh = ghost.meshes[i]; mesh.Clear();
                slots[i].Geometry.CopyWorldVertices(ghostVertices);
                mesh.SetVertices(ghostVertices);
                mesh.uv = sprite.uv; mesh.triangles = sprite.triangles.Select(t => (int)t).ToArray(); mesh.RecalculateBounds();
                part.sortingOrder = source.sortingOrder;
                var block = new MaterialPropertyBlock(); block.SetTexture("_MainTex", sprite.texture);
                block.SetColor("_Color", new Color(.52f, .94f, .82f, .28f)); part.SetPropertyBlock(block);
            }
        }
        public void ResetPresentation()
        {
            Restore(); castRemaining = 0; wasDashing = false; emitted = 0;
            foreach (var ghost in ghosts) { ghost.remaining = 0; if (ghost.root != null) ghost.root.SetActive(false); }
        }
        private void OnDisable() => ResetPresentation();
        private void OnDestroy()
        {
            foreach (var ghost in ghosts)
            {
                foreach (var mesh in ghost.meshes) Release(mesh);
                Release(ghost.root);
            }
        }
        private static void Release(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
