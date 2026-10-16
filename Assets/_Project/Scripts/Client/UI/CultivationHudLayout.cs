using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Client.UI
{
    public sealed class HudSkillView
    {
        public Button Button;
        public TMP_Text Name, Detail, CooldownText, Key;
        public Image Cooldown;
        public void SetCooldown(float remaining, float duration)
        {
            bool cooling = remaining > 0;
            Name.gameObject.SetActive(!cooling);
            Detail.gameObject.SetActive(!cooling);
            CooldownText.gameObject.SetActive(cooling);
            Cooldown.gameObject.SetActive(cooling);
            Cooldown.fillAmount = duration > 0 ? Mathf.Clamp01(remaining / duration) : 0;
            CooldownText.text = cooling ? $"{remaining:0.0}s" : "";
        }
    }

    public sealed class HudInventoryRow
    {
        public RectTransform Root;
        public TMP_Text Name, Quantity;
    }

    /// <summary>Presentation-only layout. Input and all values are supplied by the presenter.</summary>
    public sealed class CultivationHudLayout
    {
        private readonly CultivationHudPresenter hud;
        private readonly HudFrameSkin skin;
        private readonly TMP_FontAsset font;
        public RectTransform InventoryPanel { get; private set; }
        public RectTransform InventoryContent { get; private set; }
        public RectTransform BeastPanel { get; private set; }
        public ScrollRect BeastScroll { get; private set; }
        public bool SidePanelsVisible { get; private set; } = true;
        public readonly List<Button> BeastButtons = new List<Button>();
        public RectTransform MicroBar { get; private set; }
        public ScrollRect InventoryScroll { get; private set; }
        public bool InventoryCollapsed => manuallyCollapsed || actorOccludesInventory;
        public Button InventoryToggle { get; private set; }
        private int inventoryCount;
        private bool manuallyCollapsed, actorOccludesInventory;
        private CanvasGroup inventoryVisibility;
        public readonly List<HudInventoryRow> Rows = new List<HudInventoryRow>();
        private static readonly Color TextColor = new Color32(231, 234, 218, 255);
        private static readonly Color Gold = new Color32(225, 202, 144, 255);
        public CultivationHudLayout(CultivationHudPresenter presenter, HudFrameSkin frame)
        {
            hud = presenter; skin = frame; font = hud.font;
            var status = Panel("CultivatorStatus", hud.transform, new Vector2(0, 1), new Vector2(16, -16), new Vector2(270, 190));
            hud.progression = Text("Realm", status, new Vector2(12, -8), new Vector2(246, 23), 13, Gold);
            hud.hpFill = Vital(status, "Hp", 39, new Color32(184, 59, 59, 255), out hud.vitals);
            hud.qiFill = Vital(status, "Qi", 63, new Color32(46, 119, 174, 255), out hud.qiLabel);
            hud.shieldFill = Vital(status, "Shield", 87, new Color32(122, 139, 153, 255), out hud.shieldLabel);
            hud.secondaryStats = Text("AttackAndDefense", status, new Vector2(12, -112), new Vector2(246, 18), 11, TextColor);
            hud.experienceFill = Vital(status, "Experience", 135, new Color32(150, 119, 53, 255), out hud.experience);
            hud.breakthroughButton = Button("Breakthrough", status, new Vector2(0, 1), new Vector2(12, -159), new Vector2(246, 25), "[B] Đột Phá", 12);
            var beast = Panel("BeastStatus", hud.transform, new Vector2(0, 1), new Vector2(16, -214), new Vector2(270, 27));
            hud.beastState = Text("Companion", beast, new Vector2(12, -3), new Vector2(246, 21), 11, Gold);
            hud.tameButton = Button("Tame", hud.transform, new Vector2(0, 1), new Vector2(16, -249), new Vector2(270, 28), "[T] Thuần Thú Quyết", 11);

            var saves = Rect("SaveActions", hud.transform, new Vector2(1, 1), new Vector2(-16, -66), new Vector2(248, 28));
            hud.saveButton = Button("Save", saves, new Vector2(0, 1), Vector2.zero, new Vector2(120, 28), "[F5] Lưu", 11);
            hud.loadButton = Button("Load", saves, new Vector2(0, 1), new Vector2(128, 0), new Vector2(120, 28), "[F9] Đọc", 11);
            hud.sidePanelsButton = Button("SidePanels", hud.transform, new Vector2(1, 1), new Vector2(-16, -102), new Vector2(248, 28), "[I] Túi đồ & Linh thú", 11);
            InventoryPanel = Panel("Inventory", hud.transform, new Vector2(1, 1), new Vector2(-16, -138), new Vector2(248, 36));
            hud.inventory = Text("InventoryTitle", InventoryPanel, new Vector2(12, -7), new Vector2(198, 22), 12, Gold);
            inventoryVisibility = InventoryPanel.gameObject.AddComponent<CanvasGroup>();
            InventoryToggle = Button("Toggle", InventoryPanel, new Vector2(1, 1), new Vector2(-5, -5), new Vector2(28, 26), "−", 15, false);
            InventoryToggle.onClick.AddListener(() => { manuallyCollapsed = !manuallyCollapsed; FitInventory(inventoryCount); });
            var viewport = Rect("ItemsViewport", InventoryPanel, new Vector2(0, 1), new Vector2(8, -32), new Vector2(232, 24));
            viewport.gameObject.AddComponent<RectMask2D>();
            InventoryContent = Rect("Items", viewport, new Vector2(0, 1), Vector2.zero, new Vector2(232, 24));
            InventoryScroll = viewport.gameObject.AddComponent<ScrollRect>();
            InventoryScroll.viewport = viewport; InventoryScroll.content = InventoryContent; InventoryScroll.horizontal = false;
            InventoryScroll.movementType = ScrollRect.MovementType.Clamped; InventoryScroll.scrollSensitivity = 24;
            var hitArea = viewport.gameObject.AddComponent<Image>(); hitArea.color = Color.clear;

            BeastPanel = Panel("BeastRoster", hud.transform, new Vector2(1, 1), new Vector2(-16, -182), new Vector2(248, 82));
            hud.rosterTitle = Text("Title", BeastPanel, new Vector2(12, -7), new Vector2(224, 22), 12, Gold);
            var scrollRoot = Rect("BeastsScroll", BeastPanel, new Vector2(0, 1), new Vector2(8, -32), new Vector2(232, 42));
            BeastScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            var beastViewport = Rect("Viewport", scrollRoot, new Vector2(0, 1), Vector2.zero, scrollRoot.sizeDelta);
            beastViewport.gameObject.AddComponent<Image>().color = Color.white;
            beastViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var beastContent = Rect("Content", beastViewport, new Vector2(0, 1), Vector2.zero, new Vector2(232, 0));
            var group = beastContent.gameObject.AddComponent<VerticalLayoutGroup>(); group.spacing = 4;
            group.childControlWidth = group.childControlHeight = true; group.childForceExpandWidth = true; group.childForceExpandHeight = false;
            beastContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            BeastScroll.content = beastContent; BeastScroll.viewport = beastViewport; BeastScroll.horizontal = false;
            BeastScroll.movementType = ScrollRect.MovementType.Clamped; BeastScroll.scrollSensitivity = 42;

            var hotbar = Rect("Skills", hud.transform, new Vector2(.5f, 0), new Vector2(0, 16), new Vector2(472, 56));
            hud.skills = new HudSkillView[5]; hud.hotbar = new Button[3]; hud.hotbarLabels = new TMP_Text[3];
            string[] keys = { "1", "2", "3", "Space", "R" };
            string[] names = { "Linh Kiếm", "Thanh Phong", "Hộ Thể", "Thân Pháp", "Linh Thú" };
            for (int i = 0; i < 5; i++)
            {
                var slot = new HudSkillView();
                slot.Button = Button("Skill" + (i + 1), hotbar, new Vector2(0, 1), new Vector2(i * 96, 0), new Vector2(88, 56), null, 11);
                var badge = Rect("Key", slot.Button.transform, new Vector2(0, 1), new Vector2(4, -3), new Vector2(i == 3 ? 34 : 16, 14));
                var badgeImage = badge.gameObject.AddComponent<UnityEngine.UI.Image>(); badgeImage.color = new Color32(7, 18, 14, 245); badgeImage.raycastTarget = false;
                var key = Text("Label", badge, Vector2.zero, badge.sizeDelta, 9, new Color32(242, 217, 141, 255)); key.text = keys[i]; key.alignment = TextAlignmentOptions.Center; slot.Key = key;
                slot.Name = Text("Name", slot.Button.transform, new Vector2(3, -19), new Vector2(82, 18), 11, TextColor); slot.Name.text = names[i]; slot.Name.alignment = TextAlignmentOptions.Center;
                slot.Detail = Text("Detail", slot.Button.transform, new Vector2(3, -38), new Vector2(82, 14), 10, Gold); slot.Detail.alignment = TextAlignmentOptions.Center;
                var overlay = Rect("Cooldown", slot.Button.transform, new Vector2(0, 1), new Vector2(3, -3), new Vector2(82, 50));
                slot.Cooldown = overlay.gameObject.AddComponent<Image>(); slot.Cooldown.sprite = skin.Solid;
                slot.Cooldown.type = Image.Type.Filled; slot.Cooldown.fillMethod = Image.FillMethod.Vertical;
                slot.Cooldown.fillOrigin = (int)Image.OriginVertical.Bottom; slot.Cooldown.color = new Color(.015f, .045f, .06f, .78f); slot.Cooldown.raycastTarget = false;
                slot.CooldownText = Text("Remaining", slot.Button.transform, new Vector2(3, -20), new Vector2(82, 22), 15, Gold); slot.CooldownText.alignment = TextAlignmentOptions.Center;
                badge.SetAsLastSibling();
                slot.SetCooldown(0, 1); hud.skills[i] = slot;
                if (i < 3) { hud.hotbar[i] = slot.Button; hud.hotbarLabels[i] = slot.Detail; }
            }
            hud.dashButton = hud.skills[3].Button; hud.beastButton = hud.skills[4].Button;

            MicroBar = Panel("Shortcuts", hud.transform, new Vector2(1, 0), new Vector2(-16, 16), new Vector2(354, 26));
            hud.meditateButton = Button("Meditate", MicroBar, new Vector2(0, 1), new Vector2(3, -2), new Vector2(76, 22), "[X] Thiền", 10, false);
            hud.fishButton = Button("Fish", MicroBar, new Vector2(0, 1), new Vector2(80, -2), new Vector2(80, 22), "[F] Câu cá", 10, false);
            hud.outfitButton = Button("Outfit", MicroBar, new Vector2(0, 1), new Vector2(161, -2), new Vector2(74, 22), "[Tab] Áo", 10, false);
            hud.hairButton = Button("Hair", MicroBar, new Vector2(0, 1), new Vector2(236, -2), new Vector2(57, 22), "[H] Tóc", 10, false);
            hud.eyeButton = Button("Eyes", MicroBar, new Vector2(0, 1), new Vector2(294, -2), new Vector2(57, 22), "[Y] Mắt", 10, false);
            for (int i = 0; i < 4; i++) { var dot = Text("Divider", MicroBar, new Vector2(new[] { 76, 157, 232, 290 }[i], -3), new Vector2(8, 20), 10, Gold); dot.text = "·"; }
            var notification = Panel("Notification", hud.transform, new Vector2(.5f, 0), new Vector2(0, 80), new Vector2(480, 28));
            notification.GetComponent<Image>().raycastTarget = false;
            hud.message = Text("Message", notification, new Vector2(10, -3), new Vector2(460, 22), 11, TextColor); hud.message.alignment = TextAlignmentOptions.Center;
            notification.gameObject.SetActive(false);
            var prompt = Panel("WorldInteractionPrompt", hud.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(204, 28));
            hud.interactButton = prompt.gameObject.AddComponent<Button>(); hud.interactButton.targetGraphic = prompt.GetComponent<Image>();
            prompt.GetComponent<Image>().raycastTarget = true;
            hud.interactButtonLabel = Text("Caption", prompt, new Vector2(8, -3), new Vector2(188, 22), 12, Gold); hud.interactButtonLabel.alignment = TextAlignmentOptions.Center;
            hud.interactionPrompt = hud.gameObject.AddComponent<InteractionPromptPresenter>();
            hud.interactionPrompt.bubble = prompt; hud.interactionPrompt.label = hud.interactButtonLabel;
            prompt.gameObject.SetActive(false);
        }
        public void FitInventory(int count)
        {
            inventoryCount = count;
            int visible = InventoryCollapsed ? 0 : Mathf.Clamp(count, 0, 10);
            InventoryPanel.sizeDelta = new Vector2(248, 40 + visible * 24);
            InventoryScroll.viewport.sizeDelta = new Vector2(232, visible * 24);
            InventoryContent.sizeDelta = new Vector2(232, Mathf.Max(count, 1) * 24);
            InventoryScroll.vertical = count > 10;
            InventoryScroll.viewport.gameObject.SetActive(visible > 0);
            InventoryToggle.GetComponentInChildren<TMP_Text>().text = InventoryCollapsed ? "+" : "−";
            if (count <= 10) { InventoryScroll.StopMovement(); InventoryContent.anchoredPosition = Vector2.zero; }
            while (Rows.Count < count)
            {
                int i = Rows.Count;
                var root = Rect("Item" + i, InventoryContent, new Vector2(0, 1), new Vector2(0, -i * 24), new Vector2(232, 22));
                var image = root.gameObject.AddComponent<Image>(); image.color = new Color(.37f, .62f, .55f, i % 2 == 0 ? .10f : .035f); image.raycastTarget = false;
                Rows.Add(new HudInventoryRow { Root = root, Name = Text("Name", root, new Vector2(5, -1), new Vector2(186, 20), 11, TextColor), Quantity = Text("Quantity", root, new Vector2(190, -1), new Vector2(36, 20), 11, Gold) });
                Rows[i].Quantity.alignment = TextAlignmentOptions.Right;
            }
            for (int i = 0; i < Rows.Count; i++) Rows[i].Root.gameObject.SetActive(i < count);
            PositionBeastPanel();
        }
        public void FitBeasts(int count)
        {
            while (BeastButtons.Count < count)
            {
                int index = BeastButtons.Count;
                var button = Button("Beast" + index, BeastScroll.content, new Vector2(0, 1), Vector2.zero, new Vector2(232, 38), "", 11);
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 38;
                button.onClick.AddListener(() => hud.ToggleBeastAtIndex(index));
                BeastButtons.Add(button);
            }
            for (int i = 0; i < BeastButtons.Count; i++) BeastButtons[i].gameObject.SetActive(i < count);
            float height = Mathf.Clamp(count, 0, 4) * 42;
            BeastPanel.sizeDelta = new Vector2(248, 40 + height);
            ((RectTransform)BeastScroll.transform).sizeDelta = BeastScroll.viewport.sizeDelta = new Vector2(232, height);
            BeastScroll.gameObject.SetActive(count > 0); BeastScroll.vertical = count > 4;
            if (count <= 4) { BeastScroll.StopMovement(); BeastScroll.content.anchoredPosition = Vector2.zero; }
            PositionBeastPanel();
        }
        private void PositionBeastPanel()
        {
            if (BeastPanel != null) BeastPanel.anchoredPosition = new Vector2(-16, InventoryPanel.anchoredPosition.y - InventoryPanel.sizeDelta.y - 8);
        }
        public void ToggleSidePanels()
        {
            SidePanelsVisible = !SidePanelsVisible;
            InventoryPanel.gameObject.SetActive(SidePanelsVisible); BeastPanel.gameObject.SetActive(SidePanelsVisible);
        }
        public void AvoidCoveringActor(Actors.ActorView actor, UnityEngine.Camera camera)
        {
            var body = HudScreenGeometry.Expand(HudScreenGeometry.Project(actor.VisualBounds, camera), 8);
            var panel = HudScreenGeometry.Project(InventoryPanel);
            float scale = panel.width / InventoryPanel.rect.width;
            var expanded = UnityEngine.Rect.MinMaxRect(panel.xMin, panel.yMax - (40 + Mathf.Min(inventoryCount, 10) * 24) * scale, panel.xMax, panel.yMax);
            bool overlaps = expanded.Overlaps(body);
            if (actorOccludesInventory != overlaps) { actorOccludesInventory = overlaps; FitInventory(inventoryCount); }
            // At the extreme corner even the collapsed title can cover the actor.
            // Fade that title while preserving its hit target for deliberate interaction.
            inventoryVisibility.alpha = HudScreenGeometry.Project(InventoryPanel).Overlaps(body) ? .15f : 1;
        }
        public void FitScreen()
        {
            // On narrower aspect ratios keep the shortcuts above the skills, clear of their hit targets.
            float width = ((RectTransform)hud.transform).rect.width;
            MicroBar.anchoredPosition = new Vector2(-16, width > 1250 ? 16 : 116);
        }
        private Image Vital(Transform parent, string name, float y, Color color, out TMP_Text label)
        {
            var frame = Rect(name + "Bar", parent, new Vector2(0, 1), new Vector2(12, -y), new Vector2(246, 18));
            var bg = frame.gameObject.AddComponent<Image>(); bg.sprite = skin.Solid; bg.color = new Color(.02f, .045f, .05f, .95f); bg.raycastTarget = false;
            var fill = Rect("Fill", frame, new Vector2(0, 1), new Vector2(1, -1), new Vector2(244, 16)).gameObject.AddComponent<Image>();
            fill.sprite = skin.Solid; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.color = color; fill.raycastTarget = false;
            label = Text("Value", frame, Vector2.zero, frame.sizeDelta, 11, TextColor); label.alignment = TextAlignmentOptions.Center;
            return fill;
        }
        private RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = Rect(name, parent, anchor, position, size); var image = rect.gameObject.AddComponent<Image>(); skin.Apply(image); image.raycastTarget = true; return rect;
        }
        private Button Button(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string caption, float fontSize, bool framed = true)
        {
            var rect = Rect(name, parent, anchor, position, size); var image = rect.gameObject.AddComponent<Image>();
            if (framed) skin.Apply(image); else image.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors; colors.highlightedColor = new Color(.72f, 1, .9f); colors.pressedColor = new Color(.56f, .8f, .7f); colors.disabledColor = new Color(.65f, .65f, .65f, .8f); button.colors = colors;
            if (caption != null) { var label = Text("Label", rect, Vector2.zero, size, fontSize, TextColor); label.text = caption; label.alignment = TextAlignmentOptions.Center; }
            return button;
        }
        private TMP_Text Text(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var text = Rect(name, parent, new Vector2(0, 1), position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = fontSize; text.color = color; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.MidlineLeft; return text;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
    }
}
