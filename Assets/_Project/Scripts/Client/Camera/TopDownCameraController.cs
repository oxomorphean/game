using LinhThu.Client.Actors;
using LinhThu.Client.Environment;
using UnityEngine;

namespace LinhThu.Client.Camera
{
    [DefaultExecutionOrder(100)]
    public sealed class TopDownCameraController : MonoBehaviour
    {
        public UnityEngine.Camera viewCamera;
        public ActorView target;
        public ModularMapLayout map;
        public float followSpeed = 8;
        private float desiredSize;
        private void LateUpdate() => Follow(Time.deltaTime);
        public void Follow(float deltaTime)
        {
            if (target == null || map == null || viewCamera == null)
                return;
            if (desiredSize <= 0) desiredSize = viewCamera.orthographicSize;
            var bounds = map.WorldBounds;
            viewCamera.orthographicSize = Mathf.Min(desiredSize, bounds.HalfSize.Y, bounds.HalfSize.X / Mathf.Max(.01f, viewCamera.aspect));
            float y = Mathf.Max(0, bounds.HalfSize.Y - viewCamera.orthographicSize);
            float x = Mathf.Max(0, bounds.HalfSize.X - viewCamera.orthographicSize * viewCamera.aspect);
            var point = target.transform.position;
            var destination = new Vector3(Mathf.Clamp(point.x, bounds.Center.X - x, bounds.Center.X + x),
                Mathf.Clamp(point.y, bounds.Center.Y - y, bounds.Center.Y + y), transform.position.z);
            var next = Vector3.Lerp(transform.position, destination, 1 - Mathf.Exp(-followSpeed * Mathf.Max(0, deltaTime)));
            next.x = Mathf.Clamp(next.x, bounds.Center.X - x, bounds.Center.X + x);
            next.y = Mathf.Clamp(next.y, bounds.Center.Y - y, bounds.Center.Y + y);
            var body = target.VisualBounds;
            float halfY = viewCamera.orthographicSize, halfX = halfY * viewCamera.aspect;
            // Limit smoothing at the edge so even a teleport cannot clip the head or feet.
            float marginX = Mathf.Max(.2f, halfX * .02f), marginY = Mathf.Max(.2f, halfY * .02f);
            if (body.min.x < bounds.Min.X + marginX || body.max.x > bounds.Max.X - marginX)
                next.x = Mathf.Clamp(next.x, body.max.x - halfX + marginX, body.min.x + halfX - marginX);
            if (body.min.y < bounds.Min.Y + marginY || body.max.y > bounds.Max.Y - marginY)
                next.y = Mathf.Clamp(next.y, body.max.y - halfY + marginY, body.min.y + halfY - marginY);
            var render = map.PresentationBounds;
            next.x = Mathf.Clamp(next.x, render.Min.X + halfX, render.Max.X - halfX);
            next.y = Mathf.Clamp(next.y, render.Min.Y + halfY, render.Max.Y - halfY);
            transform.position = next;
        }
    }
}
