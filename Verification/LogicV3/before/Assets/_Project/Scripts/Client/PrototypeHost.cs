using System;
using LinhThu.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LinhThu.Prototype
{
    /// <summary>Owns the local authority and persistence lifecycle; presentation only issues commands and reads state.</summary>
    [RequireComponent(typeof(PrototypeDashboard), typeof(GameplayHud))]
    public sealed class PrototypeHost : MonoBehaviour
    {
        public GameSession Session { get; private set; }
        public string Toast { get; private set; } = "";
        public bool ToastOk { get; private set; } = true;
        public string SaveStatus { get; private set; } = "Chưa lưu";
        public string LoadError { get; private set; } = "";
        public float LastSaveTime { get; private set; }
        public int SelectedWild { get; set; }
        public int SelectedCell { get; set; } = 22;
        public bool InputBlocked { get; set; }
        public bool OwnerMode { get; private set; }
        public Vector2 VirtualMove { get; set; }
        public string PrimaryLabel => Session == null ? "Tấn công" : Session.PrimaryTarget().label;
        public string SaveLocation => store.Location;
        public bool CanRestore => store.CanRestore;
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void LinhThuPresentation(string mode);
#endif
        private readonly LocalSaveStore store = new LocalSaveStore();
        private float autosave;
        private InputAction move, interact, swap, attack, skill, realm, close, owner;

        private void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            try
            {
                Session = store.Exists ? store.Load() : GameSession.New((int)DateTime.UtcNow.Ticks);
                Session.ApplyOffline(LocalSaveStore.Now);
                Save();
            }
            catch (Exception e)
            {
                Session = null;
                LoadError = "Không đọc được bản lưu. Giữ nguyên dữ liệu cũ; bạn có thể chọn tạo phiên mới có sao lưu. " + e.Message;
                SaveStatus = "Bản lưu cần kiểm tra";
            }
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            swap = new InputAction("Switch", InputActionType.Button, "<Keyboard>/tab");
            attack = new InputAction("Attack", InputActionType.Button, "<Keyboard>/j");
            attack.AddBinding("<Keyboard>/space");
            skill = new InputAction("Skill", InputActionType.Button, "<Keyboard>/k");
            realm = new InputAction("Realm", InputActionType.Button, "<Keyboard>/g");
            close = new InputAction("Close", InputActionType.Button, "<Keyboard>/escape");
            owner = new InputAction("Owner", InputActionType.Button, "<Keyboard>/f2");
            foreach (var a in new[] { move, interact, swap, attack, skill, realm, close, owner }) a.Enable();
            GetComponent<PrototypeDashboard>().Bind(this);
            GetComponent<GameplayHud>().Bind(this);
        }
        private void OnDestroy()
        {
            foreach (var a in new[] { move, interact, swap, attack, skill, realm, close, owner }) a?.Dispose();
        }
        private void Update()
        {
            if (owner.WasPressedThisFrame()) SetPresentation(OwnerMode ? "play" : "owner");
            if (Session == null || OwnerMode) return;
            if (close.WasPressedThisFrame()) CloseGameplayPanel();
            if (realm.WasPressedThisFrame()) ToggleRealmView();
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            if (!InputBlocked)
            {
                Vector2 axis = Vector2.ClampMagnitude(move.ReadValue<Vector2>() + VirtualMove, 1); Session.Move(axis.x, axis.y, dt);
                if (swap.WasPressedThisFrame()) Execute(() => Session.SwitchControl());
                if (attack.WasPressedThisFrame()) PrimaryAction();
                if (skill.WasPressedThisFrame()) Execute(() => Session.Strike(true));
                if (interact.WasPressedThisFrame() && !attack.WasPressedThisFrame()) PrimaryAction();
            }
            Session.Tick(dt);
            autosave += Time.unscaledDeltaTime;
            if (autosave >= 5) { Save(); autosave = 0; }
        }
        public void SetPresentation(string mode)
        {
            if (mode != "owner" && mode != "play" || OwnerMode == (mode == "owner")) return;
            Save(); OwnerMode = mode == "owner"; InputBlocked = OwnerMode; VirtualMove = Vector2.zero;
            GetComponent<GameplayHud>().OpenPanel(!OwnerMode && Session != null && Session.State.inRealm ? "realm" : "none");
            if (OwnerMode) GetComponent<PrototypeDashboard>().ResetDraft();
#if UNITY_WEBGL && !UNITY_EDITOR
            LinhThuPresentation(mode);
#endif
        }
        public void OpenGameplayPanel(string panel) => GetComponent<GameplayHud>().OpenPanel(panel);
        public void CloseGameplayPanel()
        {
            if (Session != null && Session.State.inRealm && !OwnerMode) Execute(() => Session.ToggleRealm());
            GetComponent<GameplayHud>().OpenPanel("none");
        }
        public void ToggleRealmView()
        {
            if (Session == null) return;
            bool before = Session.State.inRealm;
            Execute(() => Session.ToggleRealm());
            if (before != Session.State.inRealm) OpenGameplayPanel(Session.State.inRealm ? "realm" : "none");
        }
        public void PrimaryAction()
        {
            if (Session == null || OwnerMode) return;
            var target = Session.PrimaryTarget();
            switch (target.kind)
            {
                case PrimaryActionKind.Capture:
                    SelectedWild = target.id; OpenGameplayPanel("capture"); break;
                case PrimaryActionKind.Pickup: Execute(() => Session.PickUpDrop(target.id)); break;
                case PrimaryActionKind.Gather: Execute(() => Session.Gather(target.id)); break;
                case PrimaryActionKind.Travel: Execute(() => Session.Travel()); break;
                case PrimaryActionKind.Bank: Execute(() => Session.BankTrip()); break;
                case PrimaryActionKind.RealmCell:
                    SelectedCell = Mathf.Clamp(Mathf.FloorToInt(Session.State.soulY), 0, 9) * 10 + Mathf.Clamp(Mathf.FloorToInt(Session.State.soulX), 0, 9);
                    OpenGameplayPanel("realm"); break;
                case PrimaryActionKind.Attack: Execute(() => Session.Strike(false)); break;
                default: throw new InvalidOperationException("Unknown primary action.");
            }
        }
        public void Interact() => PrimaryAction();
        public void Execute(Func<ActionResult> action)
        {
            if (Session == null) return;
            try { var result = action(); Announce(result.message, result.ok); Save(); }
            catch (Exception e) { Announce("Thao tác thất bại: " + e.Message, false); Debug.LogException(e); }
        }
        public void Announce(string text, bool ok) { Toast = text; ToastOk = ok; }
        public void Save()
        {
            if (Session == null || !string.IsNullOrEmpty(LoadError)) return;
            try { store.Save(Session); SaveStatus = "Đã lưu " + DateTime.Now.ToString("HH:mm:ss"); LastSaveTime = Time.realtimeSinceStartup; }
            catch (Exception e) { SaveStatus = "Lưu thất bại: " + e.Message; Announce(SaveStatus, false); }
        }
        public void NewGame()
        {
            try { store.Backup(); Session = GameSession.New((int)DateTime.UtcNow.Ticks); LoadError = ""; SelectedWild = 1; SelectedCell = 22; Save(); Announce("Phiên mới đã tạo. Bản trước được giữ ở nút Khôi phục.", true); }
            catch (Exception e) { Announce("Không tạo được phiên mới: " + e.Message, false); }
        }
        public void Restore()
        {
            try { var restored = store.RestoreBeforeReset(); restored.ApplyOffline(LocalSaveStore.Now); Session = restored; LoadError = ""; Save(); Announce("Đã khôi phục tiến trình trước khi tạo phiên mới.", true); }
            catch (Exception e) { Announce("Khôi phục thất bại: " + e.Message, false); }
        }
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() { Save(); }
    }
}
