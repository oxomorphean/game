using LinhThu.Client.Actors;
using LinhThu.Client.Environment;
using UnityEngine;

namespace LinhThu.Client.Camera
{
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
            transform.position = next;
        }
    }
}
