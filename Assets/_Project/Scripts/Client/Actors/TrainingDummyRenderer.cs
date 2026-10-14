using UnityEngine;

namespace LinhThu.Client.Actors
{
    /// <summary>The authored wooden pillar reads damage feedback without a human rig or wardrobe.</summary>
    public sealed class TrainingDummyRenderer : MonoBehaviour
    {
        public MeshRenderer visual;
        public float FlashAmount { get; private set; }
        private MaterialPropertyBlock properties;
        public void HitFlash() { FlashAmount = 1; Apply(); }
        public void AdvancePresentation(float deltaTime) { FlashAmount = Mathf.Max(0, FlashAmount - deltaTime * 8); Apply(); }
        private void LateUpdate() => AdvancePresentation(Time.deltaTime);
        private void Apply()
        {
            if (visual == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            visual.GetPropertyBlock(properties);
            properties.SetColor("_Color", Color.Lerp(Color.white, new Color(2.2f, 1.9f, 1.5f), FlashAmount));
            visual.SetPropertyBlock(properties);
        }
    }
}
