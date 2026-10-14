using LinhThu.Domain.Actors;
using LinhThu.Domain.World;
using UnityEngine;

namespace LinhThu.Client.Environment
{
    /// <summary>Visual-only contact response. The authored root and Domain footprint never move.</summary>
    [DisallowMultipleComponent]
    public sealed class InteractiveFoliage : MonoBehaviour
    {
        public Transform visual;
        [Min(.1f)] public float contactRadius = .8f;
        [Range(0, 45)] public float contactBend = 28;
        [Range(0, 10)] public float windBend = 2;
        [Min(.1f)] public float response = 16, recovery = 4;
        public float ContactAngle { get; private set; }
        private WorldState world;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private bool captured;
        private float windPhase;

        public void Bind(WorldState value)
        {
            CaptureRest();
            world = value;
            Restore();
        }

        private void CaptureRest()
        {
            if (captured || visual == null) return;
            if (visual.parent != transform)
                throw new System.InvalidOperationException("Foliage visual must be a direct child of its ground-contact root.");
            restPosition = visual.localPosition;
            restRotation = visual.localRotation;
            windPhase = transform.position.x * 2.37f + transform.position.y * 1.13f;
            captured = true;
        }

        private void LateUpdate() => Present(Time.deltaTime, Time.time);
        public void Present(float deltaTime, float time)
        {
            CaptureRest();
            if (!captured || world == null || deltaTime <= 0) return;
            float target = 0;
            foreach (var pair in world.Actors)
            {
                var actor = pair.Value;
                if (!actor.Vitals.IsAlive) continue;
                var velocity = actor.Transform.CurrentVelocity;
                if (velocity.LengthSquared < .01f) continue;
                var position = actor.Transform.Position;
                var offset = new Vector2(transform.position.x - position.X, transform.position.y - position.Y);
                float reach = contactRadius + actor.Transform.CollisionRadius;
                if (offset.sqrMagnitude >= reach * reach) continue;
                float influence = 1 - offset.magnitude / reach;
                float side = Mathf.Abs(offset.x) > .01f ? Mathf.Sign(offset.x) : Mathf.Sign(velocity.X + .001f);
                float drive = Mathf.Clamp(velocity.X / 4 + side * Mathf.Abs(velocity.Y) / 6, -1, 1);
                float dash = actor.State == ActorActionState.Dashing ? 1.4f : 1;
                target -= drive * influence * contactBend * dash;
            }
            target = Mathf.Clamp(target, -contactBend, contactBend);
            float rate = Mathf.Abs(target) > Mathf.Abs(ContactAngle) ? response : recovery;
            ContactAngle = Mathf.Lerp(ContactAngle, target, 1 - Mathf.Exp(-rate * deltaTime));
            float wind = (Mathf.Sin(time * 1.45f + windPhase) + .3f * Mathf.Sin(time * .61f + windPhase * .7f)) * windBend;
            var rotation = Quaternion.Euler(0, 0, ContactAngle + wind);
            // Rotate about the prop's ground contact, not the centre of its sprite canvas.
            visual.localPosition = rotation * restPosition;
            visual.localRotation = rotation * restRotation;
        }

        private void Restore()
        {
            ContactAngle = 0;
            if (!captured || visual == null) return;
            visual.localPosition = restPosition;
            visual.localRotation = restRotation;
        }
        private void OnDisable() => Restore();
    }
}
