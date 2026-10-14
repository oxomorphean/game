using LinhThu.Domain.Actors;
using UnityEngine;

namespace LinhThu.Client.Actors
{
    /// <summary>Reads the actor's activity; the hand socket owns the rod grip, Domain owns the water target.</summary>
    [DefaultExecutionOrder(20), DisallowMultipleComponent]
    public sealed class CharacterToolAttachmentPresenter : MonoBehaviour
    {
        public ActorView view;
        public Transform toolSocket;
        public GameObject fishingVisuals;
        public LineRenderer bambooRod, fishingLine, floatBody, ripple;
        public LineRenderer[] bambooJoints = new LineRenderer[0];
        public ParticleSystem meditationParticles;
        private readonly Vector3[] ripplePoints = new Vector3[33];
        private bool meditating;

        private void LateUpdate() => Present(Time.time);

        public void Present(float time)
        {
            var actor = view != null ? view.Entity : null;
            bool fishing = actor != null && actor.State == ActorActionState.Fishing;
            fishingVisuals.SetActive(fishing);
            bool nextMeditating = actor != null && actor.State == ActorActionState.Meditating;
            if (nextMeditating != meditating)
            {
                if (nextMeditating) meditationParticles.Play(false);
                else meditationParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                meditating = nextMeditating;
            }
            if (!fishing) return;
            var target = new Vector3(actor.LifeActivity.FishingTarget.X, actor.LifeActivity.FishingTarget.Y, toolSocket.position.z);
            var grip = toolSocket.position;
            var tip = Vector3.Lerp(grip, target, .67f) + Vector3.up * .55f;
            bool bite = actor.LifeActivity.FishBiting;
            tip.y += Mathf.Sin(time * (bite ? 13 : 2)) * (bite ? .05f : .012f);
            bambooRod.SetPosition(0, grip);
            bambooRod.SetPosition(1, Vector3.Lerp(grip, tip, .55f) + Vector3.up * .06f);
            bambooRod.SetPosition(2, tip);
            var side = new Vector3(-(tip - grip).y, (tip - grip).x).normalized * .023f;
            for (int i = 0; i < bambooJoints.Length; i++)
            {
                var p = Vector3.Lerp(grip, tip, (i + 1f) / (bambooJoints.Length + 1));
                bambooJoints[i].SetPosition(0, p - side);
                bambooJoints[i].SetPosition(1, p + side);
            }
            var bobber = target + Vector3.up * (Mathf.Sin(time * (bite ? 15 : 2.4f)) * (bite ? .045f : .012f));
            floatBody.SetPosition(0, bobber + Vector3.up * .045f);
            floatBody.SetPosition(1, bobber - Vector3.up * .045f);
            fishingLine.SetPosition(0, tip);
            fishingLine.SetPosition(1, Vector3.Lerp(tip, bobber, .5f) - Vector3.up * .10f);
            fishingLine.SetPosition(2, bobber);
            float radius = .07f + Mathf.Repeat(time * (bite ? .8f : .35f), 1) * .13f;
            for (int i = 0; i < ripplePoints.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / (ripplePoints.Length - 1);
                ripplePoints[i] = target + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .42f, 0);
            }
            ripple.SetPositions(ripplePoints);
        }

        public void ResetPresentation()
        {
            if (fishingVisuals != null) fishingVisuals.SetActive(false);
            if (meditationParticles != null) meditationParticles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            meditating = false;
        }
        private void OnDisable() => ResetPresentation();
    }
}
