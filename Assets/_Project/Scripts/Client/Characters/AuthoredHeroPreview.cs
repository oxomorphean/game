using System;
using System.Linq;
using LinhThu.Domain.Navigation;
using LinhThu.Prototype.Preview;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LinhThu.Prototype.Characters
{
    /// <summary>Separate art trial: resolved movement, two visual outfits and an explicitly labelled clip demonstration.</summary>
    public sealed class AuthoredHeroPreview : MonoBehaviour
    {
        public AuthoredHeroView hero;
        public ModularMapLayout map;
        public Camera view;
        public TMP_Text status;
        public bool CloseView { get; private set; }
        public bool RunDemo { get; private set; }
        public PreviewMovement Movement { get; private set; }
        InputAction move;
        readonly Vector2 spawn = new Vector2(-4, -2);
        bool paused;

        void Start()
        {
            if (hero == null || map == null || view == null)
                throw new InvalidOperationException("Authored hero preview requires its saved character, map and camera.");
            var arena = new MovementArena(new WorldPoint(map.size.x / 2, map.size.y / 2),
                map.GetComponentsInChildren<ObstacleAuthoring>().Select(o => o.Definition()));
            Movement = new PreviewMovement(arena, new WorldPoint(spawn.x, spawn.y), 1.2f, .28f);
            move = new InputAction("Authored hero preview", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.Enable();
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
        }

        void Update()
        {
            if (Movement == null) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Ivory();
                if (kb.digit2Key.wasPressedThisFrame) Navy();
                if (kb.zKey.wasPressedThisFrame) Zoom();
                if (kb.rKey.wasPressedThisFrame) { Movement.Reset(); RunDemo = false; }
            }
            var axis = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1);
            if (paused) axis = Vector2.zero;
            Movement.Step(new WorldPoint(axis.x, axis.y), Mathf.Min(Time.deltaTime, .05f));
            hero.transform.position = new Vector3(Movement.Position.X, Movement.Position.Y, 0);
            bool resolved = Movement.Displacement.LengthSquared > .0000001f;
            hero.SetMoving(resolved || axis.sqrMagnitude < .0001f && RunDemo);
            if (axis.x < -.01f) hero.SetFacing(-1);
            else if (axis.x > .01f) hero.SetFacing(1);
            hero.animator.speed = paused ? 0 : 1;
            float halfHeight = Mathf.Min(CloseView ? 2.15f : 5.875f, map.size.y / 2, map.size.x / (2 * Mathf.Max(.1f, view.aspect)));
            view.orthographicSize = halfHeight;
            float xLimit = Mathf.Max(0, map.size.x / 2 - halfHeight * view.aspect), yLimit = Mathf.Max(0, map.size.y / 2 - halfHeight);
            view.transform.position = new Vector3(Mathf.Clamp(hero.transform.position.x + .5f, -xLimit, xLimit),
                Mathf.Clamp(hero.transform.position.y + .5f, -yLimit, yLimit), -10);
            if (status != null)
                status.text = (paused ? "Đang dừng hình" : resolved ? "Đang di chuyển" : RunDemo ? "Xem vòng chạy tại chỗ" : "Đang đứng") +
                    "  ·  " + (hero.Outfit == "Ivory" ? "Trắng ngà" : "Xanh đen");
        }

        public void Stand() { RunDemo = false; paused = false; }
        public void Run() { RunDemo = true; paused = false; }
        public void Ivory() => hero.SetOutfit("Ivory");
        public void Navy() => hero.SetOutfit("Navy");
        public void Zoom() => CloseView = !CloseView;
        public void Pause() => paused = !paused;
        void OnDisable() => move?.Disable();
        void OnEnable() => move?.Enable();
        void OnDestroy() => move?.Dispose();
    }
}
