using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Navigation
{
    /// <summary>Broad phase only. Queries visit covered cells; callers retain exact shape/LOS rules.</summary>
    public sealed class SpatialChunkGrid
    {
        private sealed class Index<T>
        {
            private readonly Dictionary<ChunkCoord, HashSet<long>> cells = new Dictionary<ChunkCoord, HashSet<long>>();
            private readonly Dictionary<long, (T value, ChunkCoord first, ChunkCoord last)> entries = new Dictionary<long, (T, ChunkCoord, ChunkCoord)>();
            public void Set(long id, T value, WorldPoint min, WorldPoint max)
            {
                var first = ChunkCoord.FromWorld(min); var last = ChunkCoord.FromWorld(max);
                if (entries.TryGetValue(id, out var old) && old.first == first && old.last == last)
                { entries[id] = (value, first, last); return; }
                Remove(id);
                entries.Add(id, (value, first, last));
                for (int y = first.Y; y <= last.Y; y++) for (int x = first.X; x <= last.X; x++)
                {
                    var cell = new ChunkCoord(x, y);
                    if (!cells.TryGetValue(cell, out var ids)) cells.Add(cell, ids = new HashSet<long>());
                    ids.Add(id);
                }
            }
            public void Remove(long id)
            {
                if (!entries.TryGetValue(id, out var old)) return;
                for (int y = old.first.Y; y <= old.last.Y; y++) for (int x = old.first.X; x <= old.last.X; x++)
                {
                    var cell = new ChunkCoord(x, y); var ids = cells[cell]; ids.Remove(id);
                    if (ids.Count == 0) cells.Remove(cell);
                }
                entries.Remove(id);
            }
            public IEnumerable<T> Query(WorldPoint min, WorldPoint max)
            {
                var ids = new SortedSet<long>();
                foreach (var cell in ChunkCoord.Cover(min, max))
                    if (cells.TryGetValue(cell, out var bucket)) ids.UnionWith(bucket);
                // Snapshot: effects may move/remove entities while a query is consumed.
                return ids.Select(id => entries[id].value).ToArray();
            }
        }
        private readonly Index<ActorEntity> actors = new Index<ActorEntity>();
        private readonly Index<WorldNodeEntity> nodes = new Index<WorldNodeEntity>();
        private readonly Index<(WorldObstacle footprint, EntityId? node)> obstacles = new Index<(WorldObstacle, EntityId?)>();
        private readonly Dictionary<EntityId, (ActorEntity actor, Action update)> subscriptions = new Dictionary<EntityId, (ActorEntity, Action)>();
        private readonly HashSet<EntityId> depleted = new HashSet<EntityId>();
        private long nextObstacle = 1;
        private static WorldPoint Extent(float radius) => new WorldPoint(radius, radius);
        private static WorldPoint Bounded(WorldPoint p) => new WorldPoint(Math.Max(-100000, Math.Min(100000, p.X)), Math.Max(-100000, Math.Min(100000, p.Y)));
        public void RegisterActor(ActorEntity actor)
        {
            if (actor == null || subscriptions.ContainsKey(actor.Id)) throw new ArgumentException("Duplicate or missing actor.");
            Action update = () => actors.Set(actor.Id.Value, actor, Bounded(actor.Transform.Position - Extent(actor.Transform.CollisionRadius)), Bounded(actor.Transform.Position + Extent(actor.Transform.CollisionRadius)));
            subscriptions.Add(actor.Id, (actor, update)); actor.Transform.PositionChanged += update; update();
        }
        public void RemoveActor(EntityId id)
        {
            if (!subscriptions.TryGetValue(id, out var entry)) return;
            entry.actor.Transform.PositionChanged -= entry.update; subscriptions.Remove(id); actors.Remove(id.Value);
        }
        internal void ClearActors()
        {
            foreach (var id in subscriptions.Keys.ToArray()) RemoveActor(id);
        }
        public void RegisterNode(WorldNodeEntity node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var min = node.Position; var max = min;
            foreach (var local in node.Spec.FishingShoreline)
            {
                var p = node.Position + local;
                min = new WorldPoint(Math.Min(min.X, p.X), Math.Min(min.Y, p.Y));
                max = new WorldPoint(Math.Max(max.X, p.X), Math.Max(max.Y, p.Y));
            }
            var margin = Extent(node.Spec.InteractionRadius + .02f);
            nodes.Set(node.Id.Value, node, Bounded(min - margin), Bounded(max + margin));
        }
        public void RemoveNode(EntityId id) => nodes.Remove(id.Value);
        internal void RegisterObstacle(WorldObstacle obstacle, EntityId? node = null) => obstacles.Set(nextObstacle++, (obstacle, node), Bounded(obstacle.Center - obstacle.HalfSize), Bounded(obstacle.Center + obstacle.HalfSize));
        internal void SetNodeDepleted(EntityId node, bool value) { if (value) depleted.Add(node); else depleted.Remove(node); }
        private static void Validate(WorldPoint center, float radius)
        {
            if (!center.Finite || !GeometryRules.Range(radius, 0, 10000)) throw new ArgumentException("Invalid spatial query.");
        }
        public IEnumerable<ActorEntity> QueryActors(WorldPoint center, float radius)
        {
            Validate(center, radius);
            return actors.Query(Bounded(center - Extent(radius)), Bounded(center + Extent(radius)));
        }
        public IEnumerable<ActorEntity> QueryActorsAlong(WorldPoint from, WorldPoint to, float radius)
        {
            Validate(from, radius); Validate(to, radius);
            return actors.Query(Bounded(new WorldPoint(Math.Min(from.X, to.X) - radius, Math.Min(from.Y, to.Y) - radius)), Bounded(new WorldPoint(Math.Max(from.X, to.X) + radius, Math.Max(from.Y, to.Y) + radius)));
        }
        public IEnumerable<WorldNodeEntity> QueryNodes(WorldPoint center, float pickupRadius)
        {
            Validate(center, pickupRadius);
            return nodes.Query(Bounded(center - Extent(pickupRadius)), Bounded(center + Extent(pickupRadius)));
        }
        internal IEnumerable<WorldObstacle> QueryObstacles(WorldPoint min, WorldPoint max) => obstacles.Query(Bounded(min), Bounded(max)).Where(o => !o.node.HasValue || !depleted.Contains(o.node.Value)).Select(o => o.footprint);
    }
}
