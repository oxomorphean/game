using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Domain.Navigation;
using UnityEngine;

namespace LinhThu.Client.World
{
    /// <summary>Hibernates authored presentation roots. Domain entities and footprints stay resident.</summary>
    [DefaultExecutionOrder(102)]
    public sealed class WorldChunkStreamer : MonoBehaviour
    {
        [Range(0, 4)] public int activeChunkRadius = 1;
        private readonly Dictionary<ChunkCoord, List<GameObject>> chunks = new Dictionary<ChunkCoord, List<GameObject>>();
        private readonly HashSet<GameObject> activeRoots = new HashSet<GameObject>();
        private UnityEngine.Camera viewCamera;
        private ActorView player;
        private ChunkCoord lastMin, lastMax;
        private bool initialized, hasWindow;
        public int RegisteredChunkCount => chunks.Count;
        public int ActiveRootCount => activeRoots.Count;
        public IEnumerable<ChunkCoord> RegisteredChunks => chunks.Keys;

        public void Bind(IEnumerable<GameObject> roots, ActorView localPlayer, UnityEngine.Camera camera)
        {
            RestoreAll(); chunks.Clear(); activeRoots.Clear(); hasWindow = false;
            player = localPlayer; viewCamera = camera;
            if (player == null || camera == null) throw new ArgumentException("Streaming requires player and camera.");
            foreach (var root in roots.Distinct())
            {
                if (root == null) throw new ArgumentException("Missing streaming root.");
                // Authored-disabled props are not streamable; never revive intentionally hidden content.
                if (!root.activeInHierarchy) continue;
                var bounds = new Bounds(root.transform.position, Vector3.zero);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);
                // Include the silhouette and cast shadow beyond the prop's foot cell.
                bounds.Expand(new Vector3(2, 2, 0));
                var min = ChunkCoord.FromWorld(new WorldPoint(bounds.min.x, bounds.min.y));
                var max = ChunkCoord.FromWorld(new WorldPoint(bounds.max.x, bounds.max.y));
                for (int y = min.Y; y <= max.Y; y++) for (int x = min.X; x <= max.X; x++)
                {
                    var key = new ChunkCoord(x, y);
                    if (!chunks.TryGetValue(key, out var list)) chunks.Add(key, list = new List<GameObject>());
                    list.Add(root);
                }
                activeRoots.Add(root);
            }
            initialized = true;
            Refresh();
        }
        public void Refresh()
        {
            if (!initialized || player == null || viewCamera == null) return;
            float halfY = viewCamera.orthographicSize;
            float halfX = halfY * viewCamera.aspect;
            var camera = viewCamera.transform.position; var position = player.transform.position;
            var first = ChunkCoord.FromWorld(new WorldPoint(Mathf.Min(camera.x - halfX, position.x), Mathf.Min(camera.y - halfY, position.y)));
            var last = ChunkCoord.FromWorld(new WorldPoint(Mathf.Max(camera.x + halfX, position.x), Mathf.Max(camera.y + halfY, position.y)));
            int radius = Mathf.Clamp(activeChunkRadius, 0, 4);
            var min = new ChunkCoord(first.X - radius, first.Y - radius);
            var max = new ChunkCoord(last.X + radius, last.Y + radius);
            if (hasWindow && min == lastMin && max == lastMax) return;
            lastMin = min; lastMax = max; hasWindow = true;
            var wanted = new HashSet<GameObject>();
            for (int y = min.Y; y <= max.Y; y++) for (int x = min.X; x <= max.X; x++)
                if (chunks.TryGetValue(new ChunkCoord(x, y), out var list)) wanted.UnionWith(list);
            foreach (var root in activeRoots)
                if (root != null && !wanted.Contains(root)) root.SetActive(false);
            foreach (var root in wanted)
                if (root != null && !activeRoots.Contains(root)) root.SetActive(true);
            activeRoots.Clear(); activeRoots.UnionWith(wanted);
        }
        private void LateUpdate() => Refresh();
        private void RestoreAll()
        {
            foreach (var root in chunks.Values.SelectMany(x => x).Distinct())
                if (root != null) { root.SetActive(true); activeRoots.Add(root); }
        }
        private void OnDisable() { RestoreAll(); hasWindow = false; }
    }
}
