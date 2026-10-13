using System;
using System.Linq;
using LinhThu.Domain.Navigation;
using LinhThu.Prototype.Characters;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LinhThu.Prototype.Preview
{
    /// <summary>Local art-preview input and camera. Owns no combat, progression, inventory or save state.</summary>
    public sealed class ArtPreviewHost : MonoBehaviour
    {
        public ModularMapLayout map;
        public Camera viewCamera;
        public DirectionalHeroDefinition character;
        public Vector2 spawn = new Vector2(-3, -2);
        [Min(.1f)] public float moveSpeed = 3.3f;
        [Min(.01f)] public float footRadius = .28f;
        public PreviewMovement Movement { get; private set; }
        public DirectionalHeroView Hero { get; private set; }
        public float VisualTime { get; private set; }
        public bool OwnerDashboardOpen { get; private set; }
        private InputAction move;
        private Vector2 target;
        private bool hasTarget;

        private void Start()
        {
            if (map == null || viewCamera == null || character == null)
                throw new InvalidOperationException("Art preview requires its map, camera and character definition.");
            var arena = new MovementArena(new WorldPoint(map.size.x / 2, map.size.y / 2),
                map.GetComponentsInChildren<ObstacleAuthoring>().Select(o => o.Definition()));
            Movement = new PreviewMovement(arena, new WorldPoint(spawn.x, spawn.y), moveSpeed, footRadius);
            var root = new GameObject("Nhân vật · thử di chuyển");
            root.transform.SetParent(transform, false);
            Hero = root.AddComponent<DirectionalHeroView>();
            Hero.Initialize(character);
            move = new InputAction("Art preview move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.Enable();
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            Present();
        }

        private void Update()
        {
            if (Movement == null) return;
            float dt = Mathf.Min(Time.deltaTime, .1f);
            VisualTime += dt;
            if (OwnerDashboardOpen)
            {
                Movement.Step(new WorldPoint(0, 0), dt);
                Present();
                return;
            }
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ResetPreview();
            Vector2 axis = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                target = viewCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                hasTarget = true;
            }
            if (axis.sqrMagnitude > .0001f) hasTarget = false;
            else if (hasTarget)
            {
                var delta = target - new Vector2(Movement.Position.X, Movement.Position.Y);
                if (delta.sqrMagnitude < .0025f) hasTarget = false;
                else axis = Vector2.ClampMagnitude(delta / Mathf.Max(moveSpeed * dt, .0001f), 1);
            }
            Movement.Step(new WorldPoint(axis.x, axis.y), dt);
            if (hasTarget && Movement.Displacement.LengthSquared < .00000001f) hasTarget = false;
            Present();
        }

        private void Present()
        {
            var point = Movement.Position;
            Hero.transform.position = new Vector3(point.X, point.Y, 0);
            Hero.Refresh(Movement.Displacement, VisualTime);
            float halfHeight = Mathf.Min(5.875f, map.size.y / 2, map.size.x / (2 * Mathf.Max(.1f, viewCamera.aspect)));
            viewCamera.orthographicSize = halfHeight;
            float limitX = Mathf.Max(0, map.size.x / 2 - halfHeight * viewCamera.aspect);
            float limitY = Mathf.Max(0, map.size.y / 2 - halfHeight);
            viewCamera.transform.position = new Vector3(Mathf.Clamp(point.X, -limitX, limitX), Mathf.Clamp(point.Y, -limitY, limitY), -10);
        }

        public void ResetPreview()
        {
            if (Movement == null) return;
            Movement.Reset();
            hasTarget = false;
            Hero.ResetPose(VisualTime);
            Present();
        }

        /// <summary>Captures preview input while the owner reads the dashboard; cancels any old click target.</summary>
        public void SetOwnerDashboardOpen(bool open)
        {
            OwnerDashboardOpen = open;
            hasTarget = false;
        }

        private void OnDisable() { move?.Disable(); hasTarget = false; }
        private void OnEnable() { move?.Enable(); }
        private void OnDestroy() { move?.Dispose(); }
    }
}
