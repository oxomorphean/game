using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LinhThu.Prototype
{
    /// <summary>Local art-preview controls only; never reads or writes a gameplay save.</summary>
    public sealed class CartoonTrialDirector : MonoBehaviour
    {
        public Camera view;
        public SpriteRenderer ground;
        public CartoonCharacterRig hero, pet;
        public TMP_Text controlLabel;
        public Vector2 virtualMove;
        public bool ControlsPet { get; private set; }
        private InputAction move;
        private Vector3 heroSpawn, petSpawn, target;
        private bool hasTarget;
        private Vector2 halfView;

        private void Start()
        {
            if (view == null || ground == null || hero == null || pet == null)
                throw new InvalidOperationException("High-view trial requires camera, ground, hero and pet.");
            heroSpawn = hero.transform.position; petSpawn = pet.transform.position;
            move = new InputAction("Art preview move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.Enable(); FitView(); UpdateLabel();
            Application.runInBackground = true; Application.targetFrameRate = 60;
        }

        private void FitView()
        {
            var extents = ground.bounds.extents;
            view.orthographicSize = Mathf.Min(extents.y, extents.x / Mathf.Max(view.aspect, .1f));
            halfView = new Vector2(view.orthographicSize * view.aspect - .65f, view.orthographicSize - 1f);
        }

        private Vector3 Clamp(Vector3 position)
        {
            return new Vector3(Mathf.Clamp(position.x, -halfView.x, halfView.x), Mathf.Clamp(position.y, -halfView.y, halfView.y), 0);
        }

        private void Update()
        {
            if (move == null) return;
            FitView();
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) SwitchControl();
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) { ResetPreview(); return; }
            var active = ControlsPet ? pet : hero; var follower = ControlsPet ? hero : pet;
            Vector2 axis = Vector2.ClampMagnitude(move.ReadValue<Vector2>() + virtualMove, 1);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                var cursor = Mouse.current.position.ReadValue();
                target = Clamp(view.ScreenToWorldPoint(new Vector3(cursor.x, cursor.y, Mathf.Abs(view.transform.position.z))));
                hasTarget = true;
            }
            float dt = Mathf.Min(Time.deltaTime, .05f), speed = 3.3f;
            if (axis.sqrMagnitude > .01f) hasTarget = false;
            else if (hasTarget)
            {
                var delta = target - active.transform.position;
                if (delta.sqrMagnitude < .0016f) hasTarget = false;
                else axis = Vector2.ClampMagnitude((Vector2)delta / Mathf.Max(speed * dt, .0001f), 1);
            }
            var before = active.transform.position;
            active.transform.position = Clamp(before + (Vector3)axis * speed * dt);
            active.SetMotion(active.transform.position - before);
            var desired = Clamp(active.transform.position + new Vector3(-active.Facing * 1.15f, -.55f, 0));
            var followerBefore = follower.transform.position;
            if ((desired - followerBefore).sqrMagnitude > .04f)
                follower.transform.position = Vector3.MoveTowards(followerBefore, desired, 4.4f * dt);
            follower.SetMotion(follower.transform.position - followerBefore);
        }

        public void SwitchControl()
        {
            ControlsPet = !ControlsPet; hasTarget = false; virtualMove = Vector2.zero; UpdateLabel();
        }

        public void ResetPreview()
        {
            hero.transform.position = heroSpawn; pet.transform.position = petSpawn;
            hero.SetMotion(Vector2.zero); pet.SetMotion(Vector2.zero);
            ControlsPet = false; hasTarget = false; virtualMove = Vector2.zero; UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (controlLabel != null) controlLabel.text = ControlsPet ? "Đang điều khiển: Huyền Giáp Quy" : "Đang điều khiển: Ngự thú sư";
        }

        private void OnDestroy() { move?.Dispose(); }
    }
}
