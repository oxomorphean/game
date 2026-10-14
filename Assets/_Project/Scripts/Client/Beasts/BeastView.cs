using LinhThu.Client.Actors;
using LinhThu.Domain.Actors;
using UnityEngine;

namespace LinhThu.Client.Beasts
{
    public sealed class BeastView : MonoBehaviour
    {
        public ActorView actor;
        public Transform motionRoot;
        public float bounce = .035f, tilt = 2;
        private Vector3 origin;
        private void Awake()
        {
            if (motionRoot != null)
                origin = motionRoot.localPosition;
        }

        private void LateUpdate()
        {
            if (actor == null || actor.Entity == null || motionRoot == null)
                return;
            bool moving = actor.Entity.State == ActorActionState.Moving;
            float phase = Time.time * (moving ? 12 : 3);
            motionRoot.localPosition = origin + Vector3.up * (moving ? Mathf.Abs(Mathf.Sin(phase)) * bounce : 0);
            motionRoot.localRotation = Quaternion.Euler(0, 0, moving ? Mathf.Sin(phase) * tilt : 0);
        }
    }
}
