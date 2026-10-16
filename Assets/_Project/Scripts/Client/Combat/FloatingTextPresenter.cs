using LinhThu.Client.Actors;
using TMPro;
using UnityEngine;

namespace LinhThu.Client.Combat
{
    [DefaultExecutionOrder(110)]
    public sealed class FloatingTextPresenter : MonoBehaviour
    {
        public TMP_Text label;
        public float duration = .8f;
        private float remaining;
        private ActorView actor;
        private float stackOffset;
        public ActorView Owner => actor;
        public float TopOffset => stackOffset + (1 - remaining / duration) * .6f + label.bounds.size.y * label.transform.lossyScale.y;
        public void Show(string value, ActorView owner, Color color, float heightOffset = 0)
        {
            actor = owner != null ? owner : throw new System.ArgumentNullException(nameof(owner));
            stackOffset = heightOffset;
            label.text = value;
            label.color = color;
            remaining = duration;
            gameObject.SetActive(true);
            label.ForceMeshUpdate();
            PositionAboveHead(0);
        }

        private void LateUpdate()
        {
            if (actor == null || actor.Entity == null)
            {
                gameObject.SetActive(false);
                return;
            }
            remaining -= Time.deltaTime;
            float progress = 1 - remaining / duration;
            PositionAboveHead(progress);
            var color = label.color;
            color.a = Mathf.Clamp01(remaining / .2f);
            label.color = color;
            if (remaining <= 0)
                gameObject.SetActive(false);
        }

        private void PositionAboveHead(float progress)
        {
            float informationHeight = actor.overhead != null ? actor.overhead.HeightAboveAnchor : 0;
            var bottom = actor.OverheadPosition + Vector3.up * (informationHeight + (informationHeight > 0 ? .08f : 0)
                + stackOffset + progress * .6f);
            transform.position = bottom;
            // TMP bounds are local to the label, unlike SpriteRenderer.bounds.
            transform.position += Vector3.up * (bottom.y - label.transform.TransformPoint(label.bounds.min).y);
        }
    }
}
