using UnityEngine;

namespace LinhThu.Client.Actors
{
    /// <summary>The authored wooden pillar reads damage feedback without a human rig or wardrobe.</summary>
    public sealed class TrainingDummyRenderer : MonoBehaviour
    {
        public MeshRenderer visual;
        public float FlashAmount { get; private set; }
        private MaterialPropertyBlock properties;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private bool captured;
        private float shakeTime;
        public void HitFlash() { CaptureRest(); FlashAmount = 1; shakeTime = 0; Apply(); }
        public void AdvancePresentation(float deltaTime)
        { CaptureRest(); shakeTime += Mathf.Max(0, deltaTime); FlashAmount = Mathf.Max(0, FlashAmount - Mathf.Max(0, deltaTime) * 6); Apply(); }
        private void CaptureRest()
        {
            if (captured || visual == null) return;
            restPosition = visual.transform.localPosition; restRotation = visual.transform.localRotation; captured = true;
        }
        private void LateUpdate() => AdvancePresentation(Time.deltaTime);
        private void Apply()
        {
            if (visual == null) return;
            float shake = Mathf.Sin(shakeTime * 90) * FlashAmount;
            visual.transform.localPosition = restPosition + Vector3.right * (shake * .045f);
            visual.transform.localRotation = restRotation * Quaternion.Euler(0, 0, shake * 3);
            if (properties == null) properties = new MaterialPropertyBlock();
            visual.GetPropertyBlock(properties);
            properties.SetColor("_Color", Color.Lerp(Color.white, new Color(2.2f, 1.9f, 1.5f), FlashAmount));
            visual.SetPropertyBlock(properties);
        }
        private void OnDisable() { if (!captured) return; FlashAmount = 0; Apply(); }
    }
}
