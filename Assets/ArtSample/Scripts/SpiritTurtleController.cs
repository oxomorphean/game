using UnityEngine;
using UnityEngine.InputSystem;

namespace LinhThu.ArtSample
{
    /// <summary>Moves the art sample's spirit turtle with keyboard or pointer; no gameplay systems.</summary>
    public sealed class SpiritTurtleController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private float idleFramesPerSecond = 4f;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private float runFramesPerSecond = 8f;
        [SerializeField] private float speed = 2.4f;
        [SerializeField] private Rect walkable = new Rect(-4.5f, -5.5f, 9f, 6.5f);
        private Vector2 spawn;
        private Vector2 destination;
        private bool followingPointer;
        private float animationClock;

        public bool IsMoving { get; private set; }
        public int CurrentFrame { get; private set; }

        private void Awake()
        {
            if (visual == null || shadow == null || viewCamera == null || idleFrames == null || idleFrames.Length != 4
                || System.Array.Exists(idleFrames, frame => frame == null)
                || runFrames == null || runFrames.Length < 4 || System.Array.Exists(runFrames, frame => frame == null))
                throw new System.InvalidOperationException("Turtle sample references are missing.");
            spawn = transform.position;
            destination = spawn;
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) ResetPet();
            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                input.x = (Down(keyboard.dKey) || Down(keyboard.rightArrowKey) ? 1 : 0)
                    - (Down(keyboard.aKey) || Down(keyboard.leftArrowKey) ? 1 : 0);
                input.y = (Down(keyboard.wKey) || Down(keyboard.upArrowKey) ? 1 : 0)
                    - (Down(keyboard.sKey) || Down(keyboard.downArrowKey) ? 1 : 0);
            }
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                Vector2 screen = pointer.position.ReadValue();
                if (viewCamera.pixelRect.Contains(screen))
                {
                    destination = Clamp(viewCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -viewCamera.transform.position.z)));
                    followingPointer = true;
                }
            }
            Vector2 before = transform.position;
            Vector2 next = before;
            if (input.sqrMagnitude > 0)
            {
                followingPointer = false;
                next = Clamp(before + input.normalized * (speed * Time.deltaTime));
            }
            else if (followingPointer)
            {
                next = Vector2.MoveTowards(before, destination, speed * Time.deltaTime);
                if ((next - destination).sqrMagnitude < 0.0001f) followingPointer = false;
            }
            bool wasMoving = IsMoving;
            IsMoving = (next - before).sqrMagnitude > 0.000001f;
            if (Mathf.Abs(next.x - before.x) > 0.0001f) visual.flipX = next.x < before.x;
            transform.position = new Vector3(next.x, next.y, 0);
            visual.sortingOrder = 1000 - Mathf.RoundToInt(next.y * 16);
            shadow.sortingOrder = visual.sortingOrder - 1;
            if (IsMoving != wasMoving) animationClock = 0;
            Sprite[] frames = IsMoving ? runFrames : idleFrames;
            animationClock += Time.deltaTime * (IsMoving ? runFramesPerSecond : idleFramesPerSecond);
            CurrentFrame = Mathf.FloorToInt(animationClock) % frames.Length;
            visual.sprite = frames[CurrentFrame];
        }

        private static bool Down(UnityEngine.InputSystem.Controls.ButtonControl key) => key.isPressed || key.wasPressedThisFrame;

        private Vector2 Clamp(Vector2 point) => new Vector2(
            Mathf.Clamp(point.x, walkable.xMin, walkable.xMax),
            Mathf.Clamp(point.y, walkable.yMin, walkable.yMax));

        public void ResetPet()
        {
            transform.position = spawn;
            destination = spawn;
            followingPointer = false;
            IsMoving = false;
            visual.flipX = false;
            animationClock = 0;
            CurrentFrame = 0;
            visual.sprite = idleFrames[0];
        }
    }
}
