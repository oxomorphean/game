using TMPro;
using UnityEngine;

namespace LinhThu.Client.Combat
{
    public sealed class FloatingTextPresenter : MonoBehaviour
    {
        public TMP_Text label;
        public float duration = .8f;
        private float remaining;
        private Vector3 origin;
        public void Show(string value, Vector3 position, Color color)
        {
            label.text = value;
            label.color = color;
            origin = position;
            transform.position = position;
            remaining = duration;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            float progress = 1 - remaining / duration;
            transform.position = origin + Vector3.up * progress * .6f;
            var color = label.color;
            color.a = Mathf.Clamp01(remaining / .2f);
            label.color = color;
            if (remaining <= 0)
                gameObject.SetActive(false);
        }
    }
}
