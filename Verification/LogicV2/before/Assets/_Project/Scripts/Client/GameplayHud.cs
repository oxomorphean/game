using System;
using System.Collections.Generic;
using LinhThu.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace LinhThu.Prototype
{
    /// <summary>Player-only Canvas UI. Reads the shared session and sends gameplay commands through the host.</summary>
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private PrototypeMapArt art = new PrototypeMapArt();
        private PrototypeHost host;
        private GameSession S => host.Session;
        private GameState D => S.State;
        private TMP_FontAsset font;
        private RectTransform canvasRoot, root, world, realmWorld, modal, sheet;
        private GameObject canvasObject, fieldObject;
        private string panel = "none";
        private int beastId, transferFrom = -1;
        private float nextPaint;
        private TMP_Text status, hpText, qiText, region, partyCount, toast, primaryText, skillText, realmText, modalTitle, modalFeedback;
        private UnityEngine.UI.Image hpFill, qiFill, pond;
        private UnityEngine.UI.Button primaryButton, skillButton, baitButton;
        private readonly List<Marker> markers = new List<Marker>();
        private readonly List<UnityEngine.UI.Button> party = new List<UnityEngine.UI.Button>();
        private readonly Dictionary<string, RectTransform> panels = new Dictionary<string, RectTransform>();
        private readonly List<UnityEngine.UI.Button> realmCells = new List<UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.Image> realmTiles = new List<UnityEngine.UI.Image>();
        private readonly List<TMP_Text> inventoryRows = new List<TMP_Text>();
        private readonly List<UnityEngine.UI.Button> beastTabs = new List<UnityEngine.UI.Button>();
        private TMP_Text cellInfo, cellHint, beastInfo, digestion, eggInfo, inventoryInfo, captureInfo, playerInfo;
        private UnityEngine.UI.Button plant, water, harvest, soil, tree, pondButton, removeDecor, moveCell;
        private UnityEngine.UI.Button feedHerb, feedJade, summon, control, breakthrough, capture, lure;
        private RectTransform soul;
        private UnityEngine.UI.Image soulImage;
        private Vector2 lastSoulPosition;
        private int soulFacing;
        private readonly Color ink = C("263046"), cream = C("FAF7ED"), gold = C("F1C86D"), green = C("456F56");
        private sealed class Marker { public RectTransform rect; public UnityEngine.UI.Image image, shadow; public TMP_Text label; public Vector2 lastPosition; public string key; public bool flip; public int facing; }

        public void Bind(PrototypeHost value) { host = value; Build(); }
        public void OpenPanel(string value)
        {
            if (value != "none" && !panels.ContainsKey(value)) return;
            panel = value; transferFrom = -1;
            if (host != null) { host.InputBlocked = host.OwnerMode || panel != "none"; host.VirtualMove = Vector2.zero; }
            if (modal != null) modal.gameObject.SetActive(panel != "none");
            foreach (var entry in panels) entry.Value.gameObject.SetActive(entry.Key == panel);
            if (modalTitle != null) modalTitle.text = panel == "realm" ? "THẦN ĐÌNH" : panel == "beasts" ? "LINH THÚ ĐỒNG HÀNH" : panel == "inventory" ? "VẬT PHẨM & TU LUYỆN" : "THU PHỤC LINH THÚ";
            nextPaint = 0;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        private void Build()
        {
            art.Validate();
            font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-TMP");
            if (font == null) throw new InvalidOperationException("Thiếu font NotoSans-TMP của HUD.");
            canvasObject = new GameObject("Player HUD", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasRoot = (RectTransform)canvasObject.transform;
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            if (EventSystem.current == null)
            {
                var es = new GameObject("Gameplay EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
                var input = es.GetComponent<InputSystemUIInputModule>(); input.AssignDefaultActions();
                input.move = null; input.submit = null; input.cancel = null;
            }
            root = Node(canvasRoot, "Game Layout", new Rect(0, 0, 1280, 800));
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f); root.anchoredPosition = Vector2.zero;
            PrototypeMapArt.Picture(root, "Grass", art.grass, new Rect(0, 0, 1280, 800));
            world = Node(root, "Exploration Map", new Rect(42, 152, 1196, 428)); fieldObject = world.gameObject;
            art.BuildGround(world);
            pond = PrototypeMapArt.Picture(world, "Garden pond", art.pond, PrototypeMapArt.PondRect);
            art.BuildGarden(world);
            for (int i = 0; i < 25; i++)
            {
                var r = Node(world, "World Marker " + i, new Rect(0, 0, 32, 32));
                var contact = PrototypeMapArt.Picture(r, "Contact shadow", art.shadow, new Rect(-22, -6, 44, 18));
                contact.color = new Color(.16f, .19f, .27f, .22f);
                var box = Image(r, "Sprite", new Rect(0, 0, 64, 64), Color.white);
                var label = Label(r, "Label", new Rect(-70, 13, 140, 26), "", 13, TextAlignmentOptions.Center);
                markers.Add(new Marker { rect = r, image = box, shadow = contact, label = label });
            }
            art.BuildAmbient(world);
            realmWorld = Node(root, "Inner Realm Map", new Rect(406, 145, 468, 468));
            for (int i = 0; i < 100; i++) realmTiles.Add(Image(realmWorld, "Land " + i, new Rect(i % 10 * 46.8f, (9 - i / 10) * 46.8f, 45, 45), C("DCE4D0")));
            soulImage = PrototypeMapArt.Picture(realmWorld, "Cultivator soul", art.playerFrames[0], new Rect(0, 0, 48, 72));
            soul = soulImage.rectTransform; soul.pivot = new Vector2(.5f, 4f / 48);
            var stats = Card(root, "Player Status", new Rect(24, 20, 392, 106));
            status = Label(stats, "Name", new Rect(16, 6, 360, 33), "", 17);
            hpFill = Image(stats, "HP", new Rect(16, 43, 360, 21), C("C45E72"));
            hpText = Label(stats, "HP Label", new Rect(16, 43, 360, 21), "", 14, TextAlignmentOptions.Center);
            qiFill = Image(stats, "Qi", new Rect(16, 73, 360, 18), C("D6A958"));
            qiText = Label(stats, "Qi Label", new Rect(16, 70, 360, 24), "", 13, TextAlignmentOptions.Center);
            for (int i = 0; i < 3; i++) { int slot = i; party.Add(Button(root, "Party " + i, new Rect(435 + i * 128, 24, 120, 50), "", () => { if (slot < D.beasts.Count) { host.Execute(() => S.SwitchControl(D.beasts[slot].id)); } }, false)); }
            partyCount = Label(root, "Party Count", new Rect(435, 85, 380, 30), "", 14);
            var mapInfo = Card(root, "Region", new Rect(961, 20, 295, 106));
            region = Label(mapInfo, "Region Name", new Rect(14, 12, 268, 36), "", 20);
            Label(mapInfo, "Legend", new Rect(14, 50, 268, 46), "Tên xanh: bạn · vàng: thú\nĐỏ: hoang · Bị thương: thu phục", 13);
            var controls = Card(root, "Movement", new Rect(24, 620, 210, 158));
            Direction(controls, "W", new Rect(78, 9, 54, 47), Vector2.up);
            Direction(controls, "A", new Rect(17, 61, 54, 47), Vector2.left);
            Direction(controls, "S", new Rect(78, 61, 54, 47), Vector2.down);
            Direction(controls, "D", new Rect(139, 61, 54, 47), Vector2.right);
            Label(controls, "Move Hint", new Rect(10, 121, 190, 29), "WASD / phím mũi tên", 13, TextAlignmentOptions.Center);
            Button(root, "Beasts", new Rect(258, 697, 139, 62), "Linh thú", () => OpenPanel("beasts"));
            Button(root, "Inventory", new Rect(409, 697, 139, 62), "Vật phẩm", () => OpenPanel("inventory"));
            Button(root, "Swap", new Rect(560, 697, 139, 62), "Đổi người/thú\nTab", () => host.Execute(() => S.SwitchControl()));
            var realmButton = Button(root, "Inner Realm", new Rect(756, 672, 150, 86), "Thần Đình\nG", () => host.ToggleRealmView());
            realmText = realmButton.GetComponentInChildren<TMP_Text>();
            skillButton = Button(root, "Skill", new Rect(920, 672, 137, 86), "Kỹ năng\nK", () => host.Execute(() => S.Strike(true)));
            skillText = skillButton.GetComponentInChildren<TMP_Text>();
            primaryButton = Button(root, "Primary Action", new Rect(1073, 624, 183, 134), "Tấn công\nJ / Space", () => host.PrimaryAction(), true);
            primaryText = primaryButton.GetComponentInChildren<TMP_Text>();
            baitButton = Button(root, "Bait", new Rect(908, 594, 149, 50), "Thả mồi", () => { var w = S.NearbyWild(); if (w != null) { host.SelectedWild = w.id; OpenPanel("capture"); } });
            toast = Label(root, "Feedback", new Rect(258, 605, 621, 76), "", 16);
            Label(root, "Hint", new Rect(258, 763, 970, 28), "J / Space / E: đánh hoặc tương tác gần · K: kỹ năng · G: Thần Đình · Esc: đóng bảng", 12);
            modal = Image(root, "Modal Blocker", new Rect(0, 0, 1280, 800), new Color(.08f, .12f, .15f, .66f), true).rectTransform;
            sheet = Card(modal, "Player Panel", new Rect(96, 88, 1088, 624));
            modalTitle = Label(sheet, "Title", new Rect(24, 14, 895, 43), "", 23);
            Button(sheet, "Close", new Rect(974, 12, 92, 40), "Đóng", () => OpenPanel("none"));
            modalFeedback = Label(modal, "Panel Feedback", new Rect(116, 725, 1048, 58), "", 17);
            modalFeedback.color = cream;
            BuildRealm(); BuildBeasts(); BuildInventory(); BuildCapture();
            OpenPanel("none");
        }
        private void Direction(Transform parent, string label, Rect r, Vector2 axis)
        {
            var button = Button(parent, "Move " + label, r, label, () => { });
            button.gameObject.AddComponent<HudHoldDirection>().Bind(host, axis);
        }
        private RectTransform Panel(string name)
        {
            var p = Node(sheet, name, new Rect(0, 66, 1088, 552)); panels.Add(name, p); return p;
        }
        private void BuildRealm()
        {
            var p = Panel("realm");
            for (int i = 0; i < 100; i++)
            {
                int cell = i;
                realmCells.Add(Button(p, "Cell " + i, new Rect(24 + i % 10 * 48, 14 + (9 - i / 10) * 48, 46, 46), "", () => {
                    if (transferFrom >= 0) { int source = transferFrom; host.Execute(() => S.MoveCell(source, cell)); transferFrom = -1; }
                    host.SelectedCell = cell;
                }));
                realmCells[i].GetComponentInChildren<TMP_Text>().fontSize = 11;
            }
            Label(p, "Grid Guide", new Rect(24, 504, 480, 39), "Chọn ô để xem và chăm sóc. Đóng bảng để đi trong Thần Đình.", 13);
            cellInfo = Label(p, "Cell Information", new Rect(536, 18, 524, 112), "", 18);
            soil = Button(p, "Place Soil", new Rect(536, 142, 524, 38), "Đặt đất linh", () => host.Execute(() => S.PlaceSoil(host.SelectedCell)));
            plant = Button(p, "Plant", new Rect(536, 197, 164, 44), "Gieo hạt", () => host.Execute(() => S.Plant(host.SelectedCell)));
            water = Button(p, "Water", new Rect(716, 197, 164, 44), "Tưới nước", () => host.Execute(() => S.Water(host.SelectedCell)));
            harvest = Button(p, "Harvest", new Rect(896, 197, 164, 44), "Thu hoạch", () => host.Execute(() => S.Harvest(host.SelectedCell)), true);
            tree = Button(p, "Place Tree", new Rect(536, 259, 252, 40), "Đặt cây cảnh", () => host.Execute(() => S.Decorate(host.SelectedCell, "tree")));
            pondButton = Button(p, "Place Pond", new Rect(808, 259, 252, 40), "Đặt ao cảnh", () => host.Execute(() => S.Decorate(host.SelectedCell, "pond")));
            removeDecor = Button(p, "Remove Decoration", new Rect(536, 315, 524, 39), "Cất trang trí, giữ cây trồng", () => host.Execute(() => S.RemoveDecoration(host.SelectedCell)));
            moveCell = Button(p, "Move Cell", new Rect(536, 372, 524, 39), "Di chuyển ô đang chọn", () => transferFrom = transferFrom < 0 ? host.SelectedCell : -1);
            cellHint = Label(p, "Cell Guidance", new Rect(536, 427, 524, 90), "", 15);
        }
        private void BuildBeasts()
        {
            var p = Panel("beasts");
            for (int i = 0; i < 3; i++) { int slot = i; beastTabs.Add(Button(p, "Beast " + i, new Rect(24 + i * 346, 10, 322, 43), "", () => { if (slot < D.beasts.Count) beastId = D.beasts[slot].id; })); }
            beastInfo = Label(p, "Beast Information", new Rect(24, 73, 1040, 112), "", 19);
            digestion = Label(p, "Digestion", new Rect(24, 188, 1040, 48), "", 16);
            feedHerb = Button(p, "Feed Herb", new Rect(24, 252, 500, 44), "Ăn linh thảo thường", () => host.Execute(() => S.Feed(beastId, "herb")));
            feedJade = Button(p, "Feed Jade", new Rect(548, 252, 512, 44), "Ăn ngọc linh thảo", () => host.Execute(() => S.Feed(beastId, "jade-herb")), true);
            summon = Button(p, "Summon", new Rect(24, 318, 326, 43), "Gọi / thu hồi", () => host.Execute(() => S.Summon(beastId)));
            control = Button(p, "Control Beast", new Rect(378, 318, 326, 43), "Điều khiển", () => { host.Execute(() => S.SwitchControl(beastId)); OpenPanel("none"); });
            breakthrough = Button(p, "Beast Breakthrough", new Rect(732, 318, 328, 43), "Đột phá", () => host.Execute(() => S.Breakthrough(beastId)));
            eggInfo = Label(p, "Egg State", new Rect(24, 394, 703, 78), "", 17);
            Button(p, "Incubate", new Rect(754, 400, 306, 49), "Ấp trứng linh quy", () => host.Execute(() => S.Incubate()));
            Label(p, "Bond Rule", new Rect(24, 488, 1040, 46), "Khế ước gắn bó lâu dài. Cạn máu: tự về Thần Đình nghỉ, hồi đầy mới gọi ra. Nguyên liệu tốt giúp bồi dưỡng nhanh hơn.", 14);
        }
        private void BuildInventory()
        {
            var p = Panel("inventory");
            inventoryInfo = Label(p, "Inventory Information", new Rect(24, 9, 496, 40), "", 16);
            for (int i = 0; i < 13; i++) inventoryRows.Add(Label(p, "Item " + i, new Rect(24, 59 + i * 31, 498, 29), "", 16));
            Label(p, "Inventory Rule", new Rect(24, 475, 498, 63), "Số trong ngoặc là đồ mới của chuyến đi. Về trại để bảo toàn; Thần Đình là không gian chứa đồ.", 14);
            playerInfo = Label(p, "Cultivator", new Rect(552, 10, 508, 72), "", 16);
            Button(p, "Cultivate", new Rect(552, 96, 245, 38), "Bắt đầu / dừng tu luyện", () => host.Execute(() => S.Cultivate()));
            Button(p, "Player Breakthrough", new Rect(815, 96, 245, 38), "Đột phá", () => host.Execute(() => S.Breakthrough(0)));
            Recipe(p, 160, "Luyện đan: 3 linh thảo = 1 Tụ khí đan", "alchemy", "pill");
            Recipe(p, 282, "Rèn: 4 linh khoáng = 1 linh kiếm", "smith", "weapon");
            Recipe(p, 404, "Luyện phù: 3 linh mộc = 1 hộ thân phù", "charm", "charm");
        }
        private void Recipe(Transform p, float y, string title, string recipe, string item)
        {
            Label(p, "Recipe " + recipe, new Rect(552, y, 508, 31), title, 16);
            Button(p, "Craft " + recipe, new Rect(552, y + 43, 245, 40), "Chế tạo", () => host.Execute(() => S.Craft(recipe)), true);
            Button(p, "Use " + item, new Rect(815, y + 43, 245, 40), "Sử dụng", () => host.Execute(() => S.UseItem(item)));
        }
        private void BuildCapture()
        {
            var p = Panel("capture");
            captureInfo = Label(p, "Capture Information", new Rect(45, 38, 996, 203), "", 22);
            capture = Button(p, "Blood Seal", new Rect(45, 268, 476, 66), "Dùng 1 Huyết Khế", () => { host.Execute(() => S.Capture(host.SelectedWild)); if (D.wild.Exists(w => w.id == host.SelectedWild && w.removed)) OpenPanel("none"); }, true);
            lure = Button(p, "Lure", new Rect(561, 268, 480, 66), "Thả 1 linh thảo", () => host.Execute(() => S.Lure(host.SelectedWild)));
            Label(p, "Capture Rule", new Rect(45, 377, 996, 110), "Huyết Khế có thể thất bại và vẫn bị tiêu hao. Linh thú khỏe cần được cho ăn để tạo hảo cảm, hoặc đánh tới bị thương. Liên kết là gắn bó lâu dài, không thay thú tùy tiện.", 18);
        }
        private void LateUpdate()
        {
            if (host == null || canvasObject == null) return;
            canvasObject.SetActive(!host.OwnerMode);
            if (host.OwnerMode) { host.VirtualMove = Vector2.zero; return; }
            host.InputBlocked = panel != "none" || S == null;
            if (S == null)
            {
                toast.text = host.LoadError + " Mở dashboard để khôi phục hoặc tạo phiên có sao lưu.";
                primaryButton.interactable = skillButton.interactable = false; return;
            }
            DrawWorld();
            if (Time.unscaledTime < nextPaint) return;
            nextPaint = Time.unscaledTime + .1f;
            Refresh();
        }
        private void Refresh()
        {
            var actor = S.Controlled; bool isPet = D.controlledBeast != 0;
            status.text = (isPet ? S.Beast(D.controlledBeast).name : "Ngự thú sư") + " · Cảnh giới " + (actor.realm + 1);
            hpText.text = "Sinh lực " + F(actor.hp) + " / " + F(S.MaxHp(actor, isPet));
            qiText.text = "Tu vi " + F(S.CurrentQi(actor)) + " / " + F(S.QiCapacity(actor));
            hpFill.rectTransform.sizeDelta = new Vector2(360 * Mathf.Clamp01(actor.hp / S.MaxHp(actor, isPet)), 21);
            qiFill.rectTransform.sizeDelta = new Vector2(360 * Mathf.Clamp01(S.CurrentQi(actor) / S.QiCapacity(actor)), 18);
            partyCount.text = "Khế ước " + D.beasts.Count + " / " + S.ContractLimit + " · Tab đổi điều khiển";
            for (int i = 0; i < party.Count; i++)
            {
                var b = i < D.beasts.Count ? D.beasts[i] : null;
                SetLabel(party[i], b == null ? (i < S.ContractLimit ? "Ô trống" : "Chưa mở") : "Quy " + b.id + (b.recovering ? " · nghỉ" : " · " + F(b.stats.hp) + " HP"));
                party[i].interactable = b != null && b.summoned && !b.recovering;
            }
            region.text = D.inRealm ? "Thần Đình · 10 × 10" : D.room == 0 ? "Trúc Lâm" : "Bờ Linh Hồ";
            primaryText.text = host.PrimaryLabel + "\nJ / Space";
            primaryButton.interactable = actor.attackCooldown <= 0 || S.PrimaryTarget().kind != PrimaryActionKind.Attack;
            float cooldown = isPet ? Mathf.Max(actor.attackCooldown, S.Beast(D.controlledBeast).skillCooldown) : actor.attackCooldown;
            skillText.text = cooldown > 0 ? "Hồi " + F(cooldown) + "s" : "Kỹ năng\nK";
            skillButton.interactable = !D.inRealm && cooldown <= 0;
            realmText.text = D.inRealm ? "Rời Thần Đình\nG" : "Thần Đình\nG";
            var nearby = S.NearbyWild(); baitButton.gameObject.SetActive(nearby != null && !nearby.injured && !D.inRealm);
            toast.text = string.IsNullOrEmpty(host.Toast) ? "Khám phá, thu thập nguyên liệu và bồi dưỡng linh thú. Đến gần vật thể để xem hành động chính." : host.Toast;
            toast.color = host.ToastOk ? ink : C("922E31");
            modalFeedback.text = host.Toast;
            modalFeedback.color = host.ToastOk ? cream : C("FFD2C8");
            if (panel == "realm") RefreshRealm();
            if (panel == "beasts") RefreshBeasts();
            if (panel == "inventory") RefreshInventory();
            if (panel == "capture") RefreshCapture();
        }
        private void DrawWorld()
        {
            fieldObject.SetActive(!D.inRealm); realmWorld.gameObject.SetActive(D.inRealm);
            if (D.inRealm)
            {
                for (int i = 0; i < 100; i++) realmTiles[i].color = CellColor(D.cells[i]);
                var point = new Vector2(D.soulX * 46.8f, -(10 - D.soulY) * 46.8f);
                var delta = point - lastSoulPosition; bool moving = delta.sqrMagnitude > .001f;
                if (moving) soulFacing = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? 2 : delta.y > 0 ? 1 : 0;
                if (moving && soulFacing == 2) soul.localScale = new Vector3(delta.x < 0 ? -1 : 1, 1, 1);
                else if (soulFacing != 2) soul.localScale = Vector3.one;
                soulImage.sprite = art.Frame(moving, Time.unscaledTime, true, soulFacing);
                soul.anchoredPosition = point; lastSoulPosition = point; return;
            }
            pond.gameObject.SetActive(true);
            int n = 0;
            Mark(n++, 3, 3, ink, "TRẠI", art.lantern, 1.5f, "camp");
            Mark(n++, 22, 7, ink, "CỔNG", art.gate, 1.5f, "gate");
            foreach (var node in D.nodes) if (node.room == D.room)
            {
                float scale = node.itemId == "wood" ? 1f : PrototypeMapArt.ActorScale;
                Mark(n++, node.x, node.y, ink, node.remaining > 0 ? F(node.remaining) + "s" : Short(node.itemId), art.Resource(node.itemId), scale, "node" + node.id);
                markers[n - 1].image.color = node.remaining > 0 ? new Color(1, 1, 1, .3f) : Color.white;
            }
            foreach (var wild in D.wild) if (wild.room == D.room && !wild.removed)
                Mark(n++, wild.x, wild.y, wild.injured ? C("9B672E") : C("A64059"), wild.injured ? "Bị thương" : "Linh quy hoang", null, PrototypeMapArt.ActorScale, "wild" + wild.id, true);
            foreach (var beast in D.beasts) if (beast.summoned && !beast.recovering)
                Mark(n++, beast.stats.x, beast.stats.y, C("946122"), "Quy " + beast.id, null, PrototypeMapArt.ActorScale, "pet" + beast.id, true);
            Mark(n++, D.player.x, D.player.y, C("245A45"), "Bạn", null, PrototypeMapArt.ActorScale, "player", true, true);
            for (; n < markers.Count; n++) markers[n].rect.gameObject.SetActive(false);
        }
        private void Mark(int index, float x, float y, Color color, string label, Sprite sprite, float scale, string key, bool actor = false, bool human = false)
        {
            var m = markers[index]; m.rect.gameObject.SetActive(true);
            var point = new Vector2(x / 24 * 1196, -(428 - y / 14 * 428));
            Vector2 delta = point - m.lastPosition;
            bool moving = m.key == key && delta.sqrMagnitude > .001f;
            if (m.key != key) { m.flip = false; m.facing = 0; }
            if (moving && Mathf.Abs(delta.x) > .001f) m.flip = delta.x < 0;
            if (human && moving) m.facing = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? 2 : delta.y > 0 ? 1 : 0;
            m.key = key; m.lastPosition = point; m.rect.anchoredPosition = point;
            if (actor) sprite = art.Frame(moving, Time.unscaledTime, human, m.facing);
            m.image.sprite = sprite; m.image.color = Color.white;
            var rect = m.image.rectTransform;
            rect.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = sprite.rect.size * scale;
            rect.localScale = new Vector3(actor && m.flip && (!human || m.facing == 2) ? -1 : 1, 1, 1);
            m.shadow.gameObject.SetActive(actor);
            m.label.text = label; m.label.color = color;
        }
        private void RefreshRealm()
        {
            for (int i = 0; i < realmCells.Count; i++)
            {
                var c = D.cells[i]; realmCells[i].GetComponent<UnityEngine.UI.Image>().color = host.SelectedCell == i ? gold : CellColor(c);
                SetLabel(realmCells[i], c.kind == CellKind.Storage ? Short(c.itemId) + "\n" + c.amount : c.kind == CellKind.Soil ? (string.IsNullOrEmpty(c.cropId) ? "Đất" : F(100 * c.cropProgress / c.cropDuration) + "%") + (c.decorationId == "pond" ? "\nAo" : c.decorationId == "tree" ? "\nCây" : "") : "");
            }
            var cell = D.cells[host.SelectedCell]; bool land = cell.kind == CellKind.Soil;
            cellInfo.text = "Ô " + (host.SelectedCell % 10 + 1) + ", " + (host.SelectedCell / 10 + 1) + " · " + (land ? "Đất linh" : cell.kind == CellKind.Empty ? "Trống" : S.Content.Item(cell.itemId).name + " × " + cell.amount) + "\n" + (land ? "Nước: " + Mathf.CeilToInt(cell.waterRemaining / 60) + " phút\n" + (string.IsNullOrEmpty(cell.cropId) ? "Chưa gieo hạt" : "Ngọc linh thảo: " + F(cell.cropProgress / cell.cropDuration * 100) + "%") : "Ô đồ chứa một stack. Ô đất có cây trồng và trang trí.");
            soil.interactable = cell.kind == CellKind.Empty && S.Count("soil") > 0;
            plant.interactable = land && string.IsNullOrEmpty(cell.cropId) && S.Count("seed") > 0;
            water.interactable = land;
            harvest.interactable = land && !string.IsNullOrEmpty(cell.cropId) && cell.cropProgress >= cell.cropDuration;
            tree.interactable = land && string.IsNullOrEmpty(cell.decorationId) && S.Count("tree") > 0;
            pondButton.interactable = land && string.IsNullOrEmpty(cell.decorationId) && S.Count("pond") > 0;
            removeDecor.interactable = land && !string.IsNullOrEmpty(cell.decorationId);
            moveCell.interactable = cell.kind != CellKind.Empty || transferFrom >= 0;
            SetLabel(moveCell, transferFrom >= 0 ? "Hủy di chuyển" : "Di chuyển ô đang chọn");
            cellHint.text = transferFrom >= 0 ? "Đang chuyển ô " + (transferFrom + 1) + ". Chọn một ô đích trống trên lưới." : "Đất còn " + S.Count("soil") + " · Hạt " + S.Count("seed") + "\nCây cần nước, vẫn lớn khi nghỉ tối đa 12 giờ. Trang trí giữ riêng, không ghi đè cây trồng.";
        }
        private void RefreshBeasts()
        {
            if (S.Beast(beastId) == null && D.beasts.Count > 0) beastId = D.beasts[0].id;
            for (int i = 0; i < beastTabs.Count; i++) { SetLabel(beastTabs[i], i < D.beasts.Count ? "Quy " + D.beasts[i].id : "Chưa liên kết"); beastTabs[i].interactable = i < D.beasts.Count; }
            var b = S.Beast(beastId);
            beastInfo.text = b == null ? "Chưa có khế ước. Thu phục linh thú hoặc tìm trứng ở Bờ Linh Hồ để bắt đầu." : b.name + " · Cảnh giới " + (b.stats.realm + 1) + (b.recovering ? " · Đang nghỉ hồi phục" : b.summoned ? " · Đồng hành" : " · Trong Thần Đình") + "\nSinh lực " + F(b.stats.hp) + "/" + F(S.MaxHp(b.stats, true)) + " · Tu vi " + F(S.CurrentQi(b.stats)) + "/" + F(S.QiCapacity(b.stats)) + "\nCông " + F(S.Attack(b.stats, true)) + " · Thủ " + F(S.Defense(b.stats, true)) + " · Căn cốt " + F(b.stats.root);
            digestion.text = b == null ? "" : b.digestion == null ? "Sẵn sàng ăn. Thức ăn tốt giúp bù nền tảng và tăng căn cốt." : "Đang tiêu hóa: còn " + Mathf.CeilToInt(b.digestion.remaining) + " giây. Vẫn có thể khám phá.";
            feedHerb.interactable = b != null && !b.recovering && b.digestion == null && S.Count("herb") > 0;
            feedJade.interactable = b != null && !b.recovering && b.digestion == null && S.Count("jade-herb") > 0;
            SetLabel(feedHerb, "Ăn linh thảo thường · " + S.Count("herb")); SetLabel(feedJade, "Ăn ngọc linh thảo · " + S.Count("jade-herb"));
            summon.interactable = b != null && !b.recovering; SetLabel(summon, b != null && b.summoned ? "Thu vào Thần Đình" : "Gọi ra đồng hành");
            control.interactable = b != null && b.summoned && !b.recovering; breakthrough.interactable = b != null && !b.recovering;
            eggInfo.text = "Trứng: " + S.Count("egg") + " · Khế ước " + D.beasts.Count + "/" + S.ContractLimit + "\n" + (D.eggRemaining > 0 ? "Đang ấp, còn " + Mathf.CeilToInt(D.eggRemaining) + " giây" : "Ấp trứng giữ trước một ô khế ước.");
        }
        private void RefreshInventory()
        {
            inventoryInfo.text = "Thần Đình: " + S.UsedCells + "/100 ô · Đồ chuyến đi: " + S.TripCount;
            for (int i = 0; i < inventoryRows.Count; i++) { var item = S.Content.items[i]; int trip = 0; foreach (var c in D.cells) if (c.itemId == item.id) trip += c.tripAmount; inventoryRows[i].text = item.name + "  × " + S.Count(item.id) + (trip > 0 ? "  (" + trip + " mới)" : ""); }
            playerInfo.text = "Ngự thú sư · Tu vi " + F(S.CurrentQi(D.player)) + "/" + F(S.QiCapacity(D.player)) + "\nCăn cốt " + F(D.player.root) + (D.cultivating ? " · Đang tu luyện" : " · Tu luyện tại trại / Thần Đình");
        }
        private void RefreshCapture()
        {
            var w = D.wild.Find(v => v.id == host.SelectedWild && !v.removed && v.room == D.room);
            bool near = w != null && !D.inRealm && Vector2.Distance(new Vector2(S.Controlled.x, S.Controlled.y), new Vector2(w.x, w.y)) <= 2.5f;
            captureInfo.text = !near ? "Mục tiêu đã rời xa. Đóng bảng và tới gần linh thú để tương tác." : "Linh quy " + (w.injured ? "bị thương" : "hoang") + "\nHảo cảm " + F(w.affinity) + " · còn " + Mathf.CeilToInt(w.affinityRemaining) + " giây\nHuyết Khế: " + S.Count("blood") + " · Tỷ lệ thu phục " + F(Mathf.Min(100, S.Rules.captureChance + w.affinity * .2f)) + "%\nKhế ước " + D.beasts.Count + "/" + S.ContractLimit;
            capture.interactable = near && (w.injured || w.affinity > 0) && S.Count("blood") > 0;
            lure.interactable = near && !w.injured && S.Count("herb") > 0;
        }
        private RectTransform Node(Transform parent, string name, Rect r)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var t = (RectTransform)go.transform; t.anchorMin = t.anchorMax = t.pivot = new Vector2(0, 1); t.anchoredPosition = new Vector2(r.x, -r.y); t.sizeDelta = r.size; return t;
        }
        private UnityEngine.UI.Image Image(Transform parent, string name, Rect r, Color color, bool raycast = false)
        {
            var image = Node(parent, name, r).gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = raycast; return image;
        }
        private RectTransform Card(Transform parent, string name, Rect r) => Image(parent, name, r, cream).rectTransform;
        private TMP_Text Label(Transform parent, string name, Rect r, string text, int size = 16, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var t = Node(parent, name, r).gameObject.AddComponent<TextMeshProUGUI>(); t.font = font; t.text = text; t.fontSize = size; t.color = ink; t.alignment = alignment; t.richText = false; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis; return t;
        }
        private UnityEngine.UI.Button Button(Transform parent, string name, Rect r, string label, Action action, bool primary = false)
        {
            var image = Image(parent, name, r, primary ? gold : C("E4E6D8"), true);
            var b = image.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = image;
            var colors = b.colors; colors.normalColor = Color.white; colors.highlightedColor = C("D1DCC9"); colors.pressedColor = C("B1C5A9"); colors.disabledColor = new Color(.7f, .7f, .7f, .45f); b.colors = colors;
            b.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            b.onClick.AddListener(() => { if (S != null) action(); nextPaint = 0; });
            Label(image.transform, "Text", new Rect(7, 4, r.width - 14, r.height - 8), label, 16, TextAlignmentOptions.Center); return b;
        }
        private static void SetLabel(UnityEngine.UI.Button b, string value) => b.GetComponentInChildren<TMP_Text>().text = value;
        private static string F(float n) => n.ToString("0.#");
        private static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        private static Color CellColor(RealmCell c) => c.decorationId == "pond" ? C("8FCBE0") : c.decorationId == "tree" ? C("E6A0B3") : !string.IsNullOrEmpty(c.cropId) ? C("79AA5E") : c.kind == CellKind.Soil ? C("B79A6E") : c.kind == CellKind.Storage ? C("D3D5C7") : C("E7E9DC");
        private static string Short(string id) { switch (id) { case "herb": return "THẢO"; case "jade-herb": return "NGỌC"; case "stone": return "KHOÁNG"; case "wood": return "MỘC"; case "seed": return "HẠT"; case "soil": return "ĐẤT"; case "blood": return "KHẾ"; case "egg": return "TRỨNG"; case "pond": return "AO"; case "tree": return "CÂY"; case "pill": return "ĐAN"; case "charm": return "PHÙ"; default: return "KIẾM"; } }
        private static Color ItemColor(string id) => id == "jade-herb" ? C("438D7B") : id == "stone" ? C("899CBD") : id == "wood" ? C("9A785B") : id == "egg" || id == "seed" ? C("F1C86D") : C("4C8063");
    }

    /// <summary>Touch/mouse hold direction; keyboard movement remains in PrototypeHost.</summary>
    public sealed class HudHoldDirection : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private PrototypeHost host;
        private Vector2 direction;
        public void Bind(PrototypeHost value, Vector2 axis) { host = value; direction = axis; }
        public void OnPointerDown(PointerEventData data) { host.VirtualMove = direction; }
        public void OnPointerUp(PointerEventData data) { host.VirtualMove = Vector2.zero; }
        public void OnPointerExit(PointerEventData data) { host.VirtualMove = Vector2.zero; }
        private void OnDisable() { if (host != null) host.VirtualMove = Vector2.zero; }
    }
}
