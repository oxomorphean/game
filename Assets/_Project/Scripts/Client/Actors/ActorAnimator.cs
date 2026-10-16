using LinhThu.Domain.Actors;
using UnityEngine;
using UnityEngine.Serialization;

namespace LinhThu.Client.Actors
{
    public sealed class ActorAnimator : MonoBehaviour
    {
        public Animator animator;
        private static readonly int Running = Animator.StringToHash("Running");
        public Transform visual;
        // Run's planted foot travels .6 rig units in .4s at Visual scale .625.
        [FormerlySerializedAs("authoredRunSpeed"), Min(.01f)] public float baseStrideSpeed = .9375f;
        [Min(.01f)] public float highSpeedSoftCap = 6;
        [Min(.01f)] public float softKneeWidth = 2;
        public float dashLean = 10;
        private Quaternion restRotation;
        private bool hasRestRotation;
        public void Present(ActorEntity actor)
        {
            if (animator == null)
                return;
            float speed = actor.Transform.CurrentVelocity.Length;
            bool moving = speed > .001f;
            animator.SetBool(Running, moving);
            animator.speed = !actor.Vitals.IsAlive ? 0 : moving ? PlaybackSpeed(speed) : 1;
            if (visual == null) return;
            if (!hasRestRotation) { restRotation = visual.localRotation; hasRestRotation = true; }
            float horizontal = actor.Transform.CurrentVelocity.X / Mathf.Max(.01f, highSpeedSoftCap * .5f);
            // Run leans at the hips in the clip, keeping the planted feet level.
            // Dash rotation is in the visual's unmirrored parent space.
            float target = actor.State == ActorActionState.Dashing ? -Mathf.Clamp(horizontal, -1, 1) * dashLean : 0;
            visual.localRotation = Quaternion.Slerp(visual.localRotation, restRotation * Quaternion.Euler(0, 0, target), 1 - Mathf.Exp(-18 * Time.deltaTime));
        }

        public float PlaybackSpeed(float currentSpeed)
        {
            float speed = Mathf.Max(0, currentSpeed), knee = Mathf.Max(.01f, highSpeedSoftCap);
            if (speed > knee)
            {
                float width = Mathf.Max(.01f, softKneeWidth);
                speed = knee + width * Mathf.Log(1 + (speed - knee) / width);
            }
            return speed / Mathf.Max(.01f, baseStrideSpeed);
        }

        public bool TryGetRunCycle(out float cycle)
        {
            cycle = 0;
            if (animator == null || !animator.isInitialized) return false;
            var state = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != Animator.StringToHash("Run")) return false;
            cycle = state.normalizedTime;
            return true;
        }
    }
}
