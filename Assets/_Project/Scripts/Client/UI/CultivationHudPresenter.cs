using System;
using System.Linq;
using LinhThu.Client.Bootstrap;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Client.UI
{
    [DefaultExecutionOrder(105)]
    public sealed class CultivationHudPresenter : MonoBehaviour
    {
        public TMP_FontAsset font;
        [NonSerialized] public TMP_Text vitals, qiLabel, shieldLabel, secondaryStats, progression, inventory, beastState, message;
        [NonSerialized] public Image hpFill, qiFill, shieldFill, experienceFill;
        [NonSerialized] public TMP_Text experience, rosterTitle;
        [NonSerialized] public Button breakthroughButton, sidePanelsButton, tameButton;
        [NonSerialized] public Button[] hotbar;
        [NonSerialized] public TMP_Text[] hotbarLabels;
        [NonSerialized] public HudSkillView[] skills;
        [NonSerialized] public Button interactButton, dashButton, beastButton, outfitButton, saveButton, loadButton, meditateButton, fishButton, hairButton, eyeButton;
        [NonSerialized] public TMP_Text interactButtonLabel;
        [NonSerialized] public InteractionPromptPresenter interactionPrompt;
        public CultivationHudLayout Layout { get; private set; }
        private HudFrameSkin skin;
        private GameBootstrapper game;
        private float messageUntil;
        private string inventorySignature;
        private static readonly string[] Stages = { "Sơ kỳ", "Trung kỳ", "Hậu kỳ", "Viên mãn" };
        public bool BlocksPointer(Vector2 screenPoint)
        {
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            foreach (var graphic in GetComponentsInChildren<Graphic>())
                if (graphic.raycastTarget && graphic.isActiveAndEnabled
                    && RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screenPoint, camera)
                    && graphic.Raycast(screenPoint, camera)) return true;
            return false;
        }
        public void Bind(GameBootstrapper bootstrapper)
        {
            if (game != null) throw new InvalidOperationException("HUD already bound.");
            if (font == null) throw new InvalidOperationException("HUD requires the authored Vietnamese font.");
            game = bootstrapper;
            if (game.registry.hotbar.Length != 3) throw new InvalidOperationException("HUD requires three techniques.");
            skin = new HudFrameSkin(); Layout = new CultivationHudLayout(this, skin);
            interactionPrompt.actor = game.playerView;
            interactionPrompt.fixedHud = new[] { (RectTransform)transform.Find("CultivatorStatus"), (RectTransform)transform.Find("BeastStatus"),
                Layout.InventoryPanel, Layout.BeastPanel, (RectTransform)sidePanelsButton.transform, (RectTransform)tameButton.transform,
                (RectTransform)transform.Find("SaveActions"), (RectTransform)transform.Find("Skills"), Layout.MicroBar };
            for (int i = 0; i < hotbar.Length; i++)
            {
                int slot = i;
                hotbar[i].onClick.AddListener(() => game.CastFromHud(slot));
            }
            interactButton.onClick.AddListener(() => game.InteractFocused());
            dashButton.onClick.AddListener(() => game.Dash(game.World.PlayerActor.Transform.FacingDirection));
            beastButton.onClick.AddListener(() => game.ToggleBeast());
            outfitButton.onClick.AddListener(() => game.CycleOutfit());
            meditateButton.onClick.AddListener(() => game.ToggleMeditate());
            fishButton.onClick.AddListener(() => game.Fish());
            hairButton.onClick.AddListener(() => game.CycleHair());
            eyeButton.onClick.AddListener(() => game.CycleEyes());
            saveButton.onClick.AddListener(game.SaveGame); loadButton.onClick.AddListener(game.LoadGame);
            breakthroughButton.onClick.AddListener(() => game.Breakthrough());
            sidePanelsButton.onClick.AddListener(ToggleSidePanels);
            tameButton.onClick.AddListener(() => game.TameFocused());
        }
        public void ToggleSidePanels() => Layout.ToggleSidePanels();
        public void ToggleBeastAtIndex(int index)
        {
            var beasts = game.World.Player.Beasts.All;
            if (index >= 0 && index < beasts.Count) game.ToggleBeast(beasts[index].Id);
        }
        private void LateUpdate()
        {
            Layout?.FitScreen();
            if (game != null) Layout?.AvoidCoveringActor(game.playerView, game.gameCamera);
        }
        private void OnDestroy() => skin?.Dispose();
        public void Refresh()
        {
            if (game == null || game.World == null) return;
            var world = game.World; var actor = world.PlayerActor; var pool = actor.Vitals;
            hpFill.fillAmount = pool.CurrentHp / pool.Maximum(VitalType.Hp);
            qiFill.fillAmount = pool.Maximum(VitalType.Qi) > 0 ? pool.CurrentQi / pool.Maximum(VitalType.Qi) : 0;
            shieldFill.fillAmount = pool.Maximum(VitalType.Shield) > 0 ? pool.CurrentShield / pool.Maximum(VitalType.Shield) : 0;
            vitals.text = $"Sinh lực  {pool.CurrentHp:0} / {pool.Maximum(VitalType.Hp):0}";
            qiLabel.text = $"Linh lực  {pool.CurrentQi:0} / {pool.Maximum(VitalType.Qi):0}";
            shieldLabel.text = $"Hộ thuẫn  {pool.CurrentShield:0}";
            secondaryStats.text = $"Công {actor.Stats.Get(StatId.Attack):0}  ·  Thủ {actor.Stats.Get(StatId.Defense):0}";
            var cultivation = world.Player.Cultivation;
            progression.text = game.registry.Realm(cultivation.CurrentRealmId).displayName + " · " + Stages[(int)cultivation.SubStage];
            experience.text = $"Tu vi: {cultivation.CurrentCultivationExp} / {cultivation.NextThreshold}";
            experienceFill.fillAmount = Mathf.Clamp01((float)cultivation.CurrentCultivationExp / cultivation.NextThreshold);
            breakthroughButton.interactable = actor.CanAct && cultivation.CanBreakthrough;
            breakthroughButton.GetComponent<Image>().color = cultivation.CanBreakthrough ? new Color32(242, 217, 141, 255) : Color.white;
            breakthroughButton.GetComponentInChildren<TMP_Text>().color = cultivation.CanBreakthrough ? new Color32(7, 18, 14, 255) : new Color32(231, 234, 218, 255);
            tameButton.interactable = world.Taming.Select(world, world.LocalPlayerId) != null;
            inventory.text = $"TÚI ĐỒ ({world.Player.Inventory.Slots.Count}/{world.Player.Inventory.Capacity})";
            var items = world.Player.Inventory.Slots.GroupBy(i => i.DefinitionId).Select(g => new { Id = g.Key, Count = g.Sum(i => i.Count) }).ToArray();
            string signature = string.Join("|", items.Select(i => i.Id + ":" + i.Count));
            if (signature != inventorySignature)
            {
                Layout.FitInventory(items.Length);
                for (int i = 0; i < items.Length; i++)
                {
                    Layout.Rows[i].Name.text = game.registry.Item(items[i].Id).displayName;
                    Layout.Rows[i].Quantity.text = "×" + items[i].Count;
                }
                inventorySignature = signature;
            }
            var beasts = world.Player.Beasts.All;
            Layout.FitBeasts(beasts.Count); rosterTitle.text = $"LINH THÚ ({beasts.Count})";
            for (int i = 0; i < beasts.Count; i++)
            {
                var entry = beasts[i]; var button = Layout.BeastButtons[i];
                string state = entry.State == BeastState.SummonedActive ? "Xuất chiến · bấm thu hồi" : entry.State == BeastState.Resting ? "Đang nghỉ · bấm triệu hồi" : entry.State == BeastState.InEgg ? "Trong trứng" : "Chăm linh điền";
                button.GetComponentInChildren<TMP_Text>().text = game.registry.beasts.First(b => b.Id == entry.DefinitionId).displayName + " #" + entry.Id + "\n" + state;
                button.interactable = actor.CanAct && (entry.State == BeastState.Resting || entry.State == BeastState.SummonedActive);
            }
            var beast = game.SelectedBeast;
            bool summoned = beast?.State == BeastState.SummonedActive;
            beastState.text = beast == null ? "Chưa có linh thú" : game.registry.beasts.First(b => b.Id == beast.DefinitionId).displayName + (summoned ? " · Xuất chiến" : " · Đang nghỉ");
            skills[4].Detail.text = summoned ? "Thu hồi" : "Triệu hồi";
            var node = world.FocusedNode;
            string nodeName = node == null ? "" : game.registry.nodes.First(n => n.Id == node.DefinitionId).displayName;
            string action = node?.Spec.Kind == WorldNodeKind.FishingSpot
                ? actor.State == ActorActionState.Fishing ? actor.LifeActivity.FishBiting ? "Giật cần" : "Thu cần" : "Thả câu"
                : nodeName;
            LinhThu.Client.World.WorldNodeView nodeView = null;
            if (node != null) game.TryGetNodeView(node.Id, out nodeView);
            interactionPrompt.SetTarget(nodeView, game.gameCamera, node == null ? "" : "[E] " + action);
            interactButton.interactable = node != null && actor.CanAct;
            beastButton.interactable = actor.CanAct && beast != null;
            outfitButton.interactable = hairButton.interactable = eyeButton.interactable = actor.CanAct;
            meditateButton.interactable = actor.CanAct;
            fishButton.interactable = actor.CanAct && (node?.Spec.Kind == WorldNodeKind.FishingSpot || actor.HasLifeActivity && actor.State == ActorActionState.Fishing);
            for (int i = 0; i < hotbar.Length; i++)
            {
                var technique = game.registry.hotbar[i];
                float remaining = actor.Abilities.GetCooldownRemaining(technique.Id);
                skills[i].Detail.text = $"{technique.qiCost:0} LL";
                skills[i].SetCooldown(remaining, technique.cooldown);
                hotbar[i].interactable = actor.CanAct && !actor.Dash.Active && remaining <= 0 && pool.CurrentQi >= technique.qiCost;
            }
            skills[3].Detail.text = "Lướt";
            skills[3].SetCooldown(actor.Dash.CooldownRemaining, actor.Spec.Movement.DashCooldown);
            dashButton.interactable = actor.CanAct && actor.Abilities.Cast == null && !actor.Dash.Active && actor.Dash.CooldownRemaining <= 0;
            if (Time.unscaledTime > messageUntil)
                message.text = actor.State == ActorActionState.Meditating ? "Đang thiền · hồi Linh lực và Tu vi   |   X để đứng dậy"
                    : actor.State == ActorActionState.Fishing ? actor.LifeActivity.FishBiting ? "Cá cắn câu! Nhấn F hoặc E để giật cần." : $"Chờ cá cắn câu · {actor.LifeActivity.FishingRemaining:0.0}s"
                    : "";
            message.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(message.text));
            Layout.FitScreen();
        }
        public void Notify(string text, float seconds = 4)
        {
            message.text = text; messageUntil = Time.unscaledTime + seconds;
            message.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public static string ErrorText(DomainError error)
        {
            switch (error)
            {
                case DomainError.InsufficientResource:
                    return "Chưa đủ vật phẩm hoặc linh lực.";
                case DomainError.OnCooldown:
                    return "Chiêu thức đang hồi.";
                case DomainError.OutOfRange:
                    return "Hãy đến gần hơn.";
                case DomainError.Blocked:
                    return "Có vật cản trên đường.";
                case DomainError.CapacityExceeded:
                    return "Đã hết chỗ trống.";
                case DomainError.InvalidState:
                    return "Chưa thể thực hiện lúc này.";
                case DomainError.NotFound:
                    return "Không tìm thấy dữ liệu.";
                case DomainError.NoEffect:
                    return "Không có thay đổi.";
                case DomainError.InvalidSave:
                    return "Bản lưu không hợp lệ.";
                case DomainError.UnsupportedSchema:
                    return "Phiên bản bản lưu chưa được hỗ trợ.";
                default:
                    return "Không thể thực hiện: " + error;
            }
        }
    }
}
