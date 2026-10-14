using LinhThu.Domain.Actors;
using UnityEngine;

namespace LinhThu.Client.Actors
{
    /// <summary>Bounded particle feedback from committed motion state; never drives simulation.</summary>
    [DisallowMultipleComponent]
    public sealed class FootstepDust : MonoBehaviour
    {
        public ParticleSystem particles;
        public SpriteRenderer frontBoot, backBoot;
        public ActorAnimator actorAnimator;
        [Min(0)] public float strideDustMinSpeed = 1.4f;
        public Color dustColor = new Color(.72f, .66f, .46f, .55f);
        private Vector2 previousVelocity, previousPosition;
        private bool initialized, braking;
        private ActorActionState previousState;
        private ActorEntity currentActor;
        private bool trackingStride;
        private int previousHalfStep;

        public void ResetMotion()
        {
            initialized = braking = false;
            trackingStride = false;
            currentActor = null;
            if (particles != null) particles.Clear();
        }

        public void Present(ActorEntity actor)
        {
            currentActor = actor;
            if (particles == null)
                throw new System.InvalidOperationException("Footstep dust requires its authored particle system.");
            var velocity = new Vector2(actor.Transform.CurrentVelocity.X, actor.Transform.CurrentVelocity.Y);
            var position = new Vector2(actor.Transform.Position.X, actor.Transform.Position.Y);
            if (!initialized || (position - previousPosition).sqrMagnitude > 4 || !actor.Vitals.IsAlive)
            {
                initialized = true;
                previousVelocity = velocity;
                previousPosition = position;
                previousState = actor.State;
                braking = false;
                trackingStride = false;
                return;
            }

            float speed = velocity.magnitude, before = previousVelocity.magnitude;
            bool slowing = before > .7f && speed < before - .08f;
            bool turning = speed > .3f && before > .3f && Vector2.Dot(velocity.normalized, previousVelocity.normalized) < .35f;
            int count = actor.State == ActorActionState.Dashing && previousState != ActorActionState.Dashing ? 10
                : turning ? 5 : slowing && !braking ? 4 : before <= .08f && speed > .08f ? 3 : 0;
            if (count > 0)
            {
                var direction = actor.State == ActorActionState.Dashing
                    ? new Vector2(actor.Dash.Direction.X, actor.Dash.Direction.Y) : before > speed ? previousVelocity : velocity;
                Emit(position, direction.normalized, count);
            }
            if (speed != before || velocity != previousVelocity)
                braking = slowing;
            previousVelocity = velocity;
            previousPosition = position;
            previousState = actor.State;
        }

        private void LateUpdate()
        {
            if (currentActor != null) PresentStride(currentActor);
        }

        public void PresentStride(ActorEntity actor)
        {
            // The Run clip lands front/back at phases 0/.5. Observe its evaluated phase once,
            // after Animator and action poses; repeated Sync or frame rate cannot duplicate a step.
            float speed = actor.Transform.CurrentVelocity.Length;
            if (actorAnimator == null || actor.State != ActorActionState.Moving || speed < strideDustMinSpeed || !actorAnimator.TryGetRunCycle(out float cycle))
            {
                trackingStride = false;
                return;
            }
            int halfStep = Mathf.FloorToInt(cycle * 2 + .00001f);
            if (trackingStride && halfStep > previousHalfStep && halfStep - previousHalfStep <= 4)
                for (int step = previousHalfStep + 1; step <= halfStep; step++)
                    Emit(new Vector2(actor.Transform.Position.X, actor.Transform.Position.Y),
                        new Vector2(actor.Transform.CurrentVelocity.X, actor.Transform.CurrentVelocity.Y).normalized, 2, step % 2 == 0);
            previousHalfStep = halfStep;
            trackingStride = true;
        }

        private void Emit(Vector2 position, Vector2 direction, int count, bool? front = null)
        {
            // EmitParams requires a running system, including after a view has been pooled/rebound.
            if (!particles.isPlaying) particles.Play(false);
            for (int i = 0; i < count; i++)
            {
                var boot = (front ?? i % 2 == 0) ? frontBoot : backBoot;
                float x = boot != null ? boot.bounds.center.x : position.x + (i % 2 == 0 ? -.08f : .08f);
                float y = boot != null ? boot.bounds.min.y + .015f : position.y + .015f;
                float spread = (i - (count - 1) * .5f) / count;
                var emit = new ParticleSystem.EmitParams
                {
                    position = new Vector3(x, y, transform.position.z),
                    velocity = new Vector3(-direction.x * .35f + spread * .5f, .12f - direction.y * .15f, 0),
                    startLifetime = .25f + .14f * (i % 3) / 2,
                    startSize = .055f + .06f * (i % 4) / 3,
                    startColor = dustColor,
                    rotation = i * 67
                };
                particles.Emit(emit, 1);
            }
        }

        private void OnDisable() => ResetMotion();
    }
}
