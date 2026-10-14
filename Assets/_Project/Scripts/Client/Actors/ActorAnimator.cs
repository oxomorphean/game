using LinhThu.Domain.Actors;
using UnityEngine;

namespace LinhThu.Client.Actors
{
    public sealed class ActorAnimator : MonoBehaviour
    {
        public Animator animator;
        private static readonly int Running = Animator.StringToHash("Running");
        public void Present(ActorEntity actor)
        {
            if (animator == null)
                return;
            animator.SetBool(Running, actor.State == ActorActionState.Moving);
            animator.speed = actor.Vitals.IsAlive ? 1 : 0;
        }
    }
}
