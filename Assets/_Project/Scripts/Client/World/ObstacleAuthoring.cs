using System;
using System.Linq;
using LinhThu.Domain.Navigation;
using UnityEngine;

namespace LinhThu.Client.World
{
    /// <summary>Ground footprint authored on a map prop, independent of sprite alpha.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "LinhThu.Prototype.Preview", "LinhThu.Prototype", "ObstacleAuthoring")]
    public sealed class ObstacleAuthoring : MonoBehaviour
    {
        [SerializeField]
        private bool box;
        [SerializeField]
        private Vector2 offset;
        [SerializeField]
        private Vector2 halfSize = new Vector2(.4f, .4f);
        [Tooltip("Convex ground footprint in this object's local space; empty keeps the legacy box/circle.")]
        [SerializeField]
        private Vector2[] polygon = Array.Empty<Vector2>();
        public WorldObstacle Definition()
        {
            if (polygon.Length > 0)
                return new WorldObstacle(polygon.Select(p =>
                {
                    var world = transform.TransformPoint(p);
                    return new WorldPoint(world.x, world.y);
                }));
            var center = transform.TransformPoint(offset);
            var scale = transform.lossyScale;
            return new WorldObstacle(new WorldPoint(center.x, center.y), new WorldPoint(halfSize.x * Mathf.Abs(scale.x), halfSize.y * Mathf.Abs(scale.y)), box);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, .72f, .2f, .8f);
            var old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            if (polygon.Length > 0)
                for (int i = 0; i < polygon.Length; i++)
                    Gizmos.DrawLine(polygon[i], polygon[(i + 1) % polygon.Length]);
            else if (box)
                Gizmos.DrawWireCube(offset, halfSize * 2);
            else
                Gizmos.DrawWireSphere(offset, halfSize.x);
            Gizmos.matrix = old;
        }
    }
}
