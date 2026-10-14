using System;
using UnityEngine;

namespace NguThu.Client.Preview
{
    /// <summary>Independent art trial. Appearance switches never restart the Animator.</summary>
    public sealed class HeroC2Preview : MonoBehaviour
    {
        [Serializable]
        public sealed class Part
        {
            public SpriteRenderer renderer;
            public string category;
            public string variant;
            public string[] hiddenBy;
        }

        public Animator animator;
        public Transform visual;
        public Part[] parts;
        public string outfit = "classic";
        public string hair = "silver";
        public string eyes = "jade";
        public bool showControls = true;
        bool running;

        void OnEnable() => ApplyAppearance();

        public void SetRunning(bool value)
        {
            running = value;
            animator.SetBool("Running", value);
        }

        public void SetAppearance(string nextOutfit, string nextHair, string nextEyes)
        {
            outfit = nextOutfit; hair = nextHair; eyes = nextEyes;
            ApplyAppearance();
        }

        public void ApplyAppearance()
        {
            if (parts == null) return;
            foreach (var part in parts)
            {
                bool visible;
                switch (part.category)
                {
                    case "outfit": visible = part.variant == outfit; break;
                    case "hair": visible = part.variant == hair; break;
                    case "eyes": visible = part.variant == eyes; break;
                    default: visible = Array.IndexOf(part.hiddenBy, outfit) < 0; break;
                }
                part.renderer.enabled = visible;
            }
        }

        void OnGUI()
        {
            if (!showControls) return;
            GUILayout.BeginArea(new Rect(18, 18, 330, 275), GUI.skin.box);
            GUILayout.Label("C2 · Thử nhân vật tách lớp");
            GUILayout.Label("Áo, tóc và mắt đổi trên cùng bộ xương");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Áo ngà")) SetAppearance("classic", hair, eyes);
            if (GUILayout.Button("Áo ngọc")) SetAppearance("jade", hair, eyes);
            if (GUILayout.Button("Cơ bản")) SetAppearance("basic", hair, eyes);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tóc bạc")) SetAppearance(outfit, "silver", eyes);
            if (GUILayout.Button("Tóc ngắn")) SetAppearance(outfit, "ink", eyes);
            if (GUILayout.Button("Mắt")) SetAppearance(outfit, hair, eyes == "jade" ? "amber" : "jade");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(running ? "Đứng" : "Chạy"))
            {
                SetRunning(!running);
            }
            if (GUILayout.Button("Lật hướng"))
                visual.localScale = new Vector3(-visual.localScale.x, 1, 1);
            GUILayout.EndHorizontal();
            GUILayout.Label("Mẫu một góc 3/4 · chưa thay player chính");
            GUILayout.EndArea();
        }
    }
}
