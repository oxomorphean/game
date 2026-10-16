using System;
using System.Linq;
using LinhThu.Client.Configs;
using LinhThu.Domain.Actors;
using UnityEngine;

namespace LinhThu.Client.Actors
{
    /// <summary>Temporary facial attachments read committed state; they never change appearance IDs or animation clips.</summary>
    [DefaultExecutionOrder(-25), DisallowMultipleComponent]
    public sealed class FacialExpressionPresenter : MonoBehaviour
    {
        public ActorView view;
        private SpriteRenderer eyes, mouth, frontBrow, backBrow;
        private Quaternion frontRest, backRest;
        private float blinkElapsed;
        private bool captured;

        private void LateUpdate() => Present(Time.deltaTime);

        private void Capture()
        {
            if (captured) return;
            if (view == null || view.equipment == null)
                throw new InvalidOperationException("Facial presentation requires an actor and modular rig.");
            SpriteRenderer Find(string id) => view.equipment.slots.Single(s => s.id == id).renderer;
            eyes = Find("eyes"); mouth = Find("mouth");
            frontBrow = Find("eyebrow-front"); backBrow = Find("eyebrow-back");
            frontRest = frontBrow.transform.localRotation;
            backRest = backBrow.transform.localRotation;
            captured = true;
        }

        public void Present(float deltaTime)
        {
            if (view == null || view.Entity == null || view.equipment == null || view.equipment.CurrentOutfit == null) return;
            Capture();
            var outfit = view.equipment.CurrentOutfit;
            var face = outfit.face;
            if (!face.enabled) { RestoreRest(outfit); return; }
            // A palette/outfit swap keeps the current blink phase.
            blinkElapsed = (blinkElapsed + Mathf.Max(0, deltaTime)) % face.blinkInterval;
            bool resting = view.Entity.State == ActorActionState.Meditating || !view.Entity.Vitals.IsAlive;
            bool hit = view.equipment.FlashAmount > 0;
            bool calling = !resting && (hit || (view.actionFeedback != null && view.actionFeedback.CastWeight > 0));
            bool closed = resting || hit || blinkElapsed >= face.blinkInterval - face.blinkDuration;
            var variant = outfit.eyeVariants.Single(e => e.id == view.equipment.CurrentAppearance.EyeId);
            eyes.sprite = closed ? variant.closedSprite : variant.sprite;
            mouth.sprite = calling ? face.callingMouth : RestMouth(outfit);
            float angle = calling ? face.browAngle : 0;
            frontBrow.transform.localRotation = frontRest * Quaternion.Euler(0, 0, -angle);
            backBrow.transform.localRotation = backRest * Quaternion.Euler(0, 0, angle);
        }

        private static Sprite RestMouth(OutfitConfigSO outfit) => outfit.slots.Single(s => s.slotId == "mouth").sprite;

        private void RestoreRest(OutfitConfigSO outfit)
        {
            eyes.sprite = outfit.eyeVariants.Single(e => e.id == view.equipment.CurrentAppearance.EyeId).sprite;
            mouth.sprite = RestMouth(outfit);
            frontBrow.transform.localRotation = frontRest;
            backBrow.transform.localRotation = backRest;
        }

        public void ResetPresentation()
        {
            blinkElapsed = 0;
            if (captured && view != null && view.equipment != null && view.equipment.CurrentOutfit != null)
                RestoreRest(view.equipment.CurrentOutfit);
        }

        private void OnDisable() => ResetPresentation();
    }
}
