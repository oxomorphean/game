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
        private void LateUpdate()
        {
            if (target == null || map == null || viewCamera == null)
                return;
            var point = target.transform.position;
            float y = Mathf.Max(0, map.size.y / 2 - viewCamera.orthographicSize);
            float x = Mathf.Max(0, map.size.x / 2 - viewCamera.orthographicSize * viewCamera.aspect);
            var destination = new Vector3(Mathf.Clamp(point.x, -x, x), Mathf.Clamp(point.y, -y, y), transform.position.z);
            transform.position = Vector3.Lerp(transform.position, destination, 1 - Mathf.Exp(-followSpeed * Time.deltaTime));
        }
    }
}
