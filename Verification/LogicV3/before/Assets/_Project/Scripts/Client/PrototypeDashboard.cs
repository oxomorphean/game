using System;
using LinhThu.Domain;
using UnityEngine;

namespace LinhThu.Prototype
{
    /// <summary>Owner-facing local dashboard. Rendering is deliberately graybox; commands always go through GameSession.</summary>
    public sealed class PrototypeDashboard : MonoBehaviour
    {
        private PrototypeHost host;
        private GameSession S => host.Session;
        private GameState D => S.State;
        private readonly string[] pages = { "Tổng quan", "Thần Đình", "Linh thú", "Vật phẩm & nghề", "Luật & dữ liệu", "Nhật ký" };
        private int page, selectedBeast, itemIndex, transferFrom = -1, grantCount = 5;
        private bool initialized, confirmNew, movementRules;
        private GameRules draftRules;
        private GameContent draftContent;
        private GUIStyle body, small, title, heading, white, button, activeButton, dangerButton, field;
        private readonly Color ink = C("263046"), muted = C("697366"), background = C("ECEBDD"), card = C("FAF7ED"), green = C("456F56"), gold = C("D6A958"), line = C("D3D5C7");
        private Texture2D buttonTex, hoverTex, activeTex, dangerTex;
        public void Bind(PrototypeHost value) { host = value; }
        public int Page { get => page; set { page = Mathf.Clamp(value, 0, pages.Length - 1); if (page == 4 && host?.Session != null) Draft(); } }
        public void ResetDraft() => Draft();
        private static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var color); return color; }
        private Texture2D Tex(Color color)
        {
            var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, color); tex.Apply(); return tex;
        }
        private void Init()
        {
            initialized = true;
            var font = Resources.Load<Font>("Fonts/NotoSans-Regular");
            if (font == null) throw new InvalidOperationException("Thiếu font Noto Sans trong bản build.");
            body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16, wordWrap = true, richText = false, normal = { textColor = ink } };
            small = new GUIStyle(body) { fontSize = 13, normal = { textColor = muted } };
            title = new GUIStyle(body) { fontSize = 28, fontStyle = FontStyle.Bold };
            heading = new GUIStyle(body) { fontSize = 18, fontStyle = FontStyle.Bold };
            white = new GUIStyle(body) { fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter };
            buttonTex = Tex(C("E7E9DC")); hoverTex = Tex(C("D1DCC9")); activeTex = Tex(green); dangerTex = Tex(C("9A5E52"));
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 14, wordWrap = true, border = new RectOffset(), padding = new RectOffset(8, 8, 5, 5) };
            button.normal.background = buttonTex; button.normal.textColor = ink; button.hover.background = hoverTex; button.hover.textColor = ink; button.active.background = activeTex; button.active.textColor = Color.white;
            activeButton = new GUIStyle(button); activeButton.normal.background = activeTex; activeButton.normal.textColor = Color.white;
            dangerButton = new GUIStyle(button); dangerButton.normal.background = dangerTex; dangerButton.normal.textColor = Color.white;
            field = new GUIStyle(GUI.skin.textField) { font = font, fontSize = 16, padding = new RectOffset(8, 8, 6, 6) };
        }
        private void OnDestroy()
        {
            foreach (var t in new[] { buttonTex, hoverTex, activeTex, dangerTex }) if (t != null) Destroy(t);
        }
        private void OnGUI()
        {
            if (host == null || !host.OwnerMode) return;
            if (!initialized) Init();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 800 * scale) / 2), Quaternion.identity, new Vector3(scale, scale, 1));
            Fill(new Rect(0, 0, 1280, 800), background);
            DrawNav();
            if (S == null)
            {
                Box(new Rect(184, 140, 1072, 360)); Text(210, 170, 1010, 90, host.LoadError, heading);
                if (Btn(210, 300, 330, 44, "Tạo phiên mới, giữ bản cũ")) host.NewGame();
                Footer(); return;
            }
            host.InputBlocked = true;
            switch (page)
            {
                case 0: OverviewPage(); break;
                case 1: RealmPage(); break;
                case 2: BeastPage(); break;
                case 3: InventoryPage(); break;
                case 4: RulesPage(); break;
                case 5: JournalPage(); break;
            }
            Footer();
        }
        private void DrawNav()
        {
            Fill(new Rect(0, 0, 160, 800), ink);
            Text(18, 28, 126, 58, "LINH THÚ\nKÝ", new GUIStyle(title) { fontSize = 22, normal = { textColor = C("F1C86D") } });
            Text(18, 98, 125, 30, "XƯỞNG GAME · V1", new GUIStyle(small) { fontSize = 11, normal = { textColor = C("AEB9BC") } });
            for (int i = 0; i < pages.Length; i++)
            {
                bool selected = page == i;
                if (GUI.Button(new Rect(12, 153 + i * 57, 136, 45), pages[i], selected ? activeButton : new GUIStyle(button) { normal = { background = null, textColor = C("DDE3DA") } })) { Page = i; GUI.FocusControl(null); }
            }
            Text(18, 562, 125, 75, "Dành cho chủ dự án\nMô phỏng tạm dừng\nChỉnh rồi chơi thử", new GUIStyle(small) { normal = { textColor = C("BBC6C3") } });
            Text(18, 710, 125, 55, "Logic thử nghiệm\nAsset tạm thời", new GUIStyle(small) { normal = { textColor = C("AEB9BC") } });
            Text(184, 23, 710, 40, "Xưởng dự án · Linh Thú Ký", title);
            Text(185, 65, 740, 28, "Hiểu hệ thống, chỉnh dữ liệu, kiểm tra hành vi", small);
            Fill(new Rect(1076, 26, 180, 31), C("DBE7D5")); Text(1086, 30, 160, 24, "LOCAL  /  CHỦ DỰ ÁN", small);
            Text(1010, 67, 246, 26, host.SaveStatus, small);
        }
        private void Footer()
        {
            Fill(new Rect(180, 746, 1080, 1), line);
            string toast = string.IsNullOrEmpty(host.Toast) ? "Mô phỏng đang tạm dừng · Tua thời gian để thử tăng trưởng · F2 trở lại màn chơi" : host.Toast;
            Text(184, 756, 930, 42, toast, new GUIStyle(small) { normal = { textColor = host.ToastOk ? green : C("A04C3C") } });
            if (Btn(1136, 757, 120, 30, "Lưu ngay")) { host.Save(); host.Announce(host.SaveStatus, true); }
        }
        private void PageTitle(string text, string subtitle)
        {
            Text(184, 108, 900, 30, text, heading); Text(184, 137, 1000, 25, subtitle, small);
        }
        [Serializable] private sealed class TestEvidence { public int passed, failed; }
        private void OverviewPage()
        {
            PageTitle("Bản đồ dự án", "Đây là công cụ của bạn trong quá trình phát triển. HUD người chơi ở màn Chơi thử riêng.");
            Box(new Rect(184, 177, 1072, 100));
            Text(204, 192, 720, 28, "Khám phá  /  thu phục  /  bồi dưỡng  /  đột phá", heading);
            Text(204, 230, 740, 30, "Phiên hiện tại: " + D.beasts.Count + "/" + S.ContractLimit + " khế ước, " + S.UsedCells + "/100 ô, " + S.TripCount + " đồ chuyến đi", body);
            if (Btn(990, 197, 242, 48, "Chuyển sang chơi thử [F2]", true)) host.SetPresentation("play");
            SystemCard(184, 295, "01 · Thế giới & tương tác", "2 vùng thử nghiệm. Nút chính đổi theo mục tiêu gần: đánh, nhặt, thu phục hoặc qua cổng.", "CÓ LOGIC LOCAL", -1);
            SystemCard(546, 295, "02 · Khế ước & linh thú", "1–3 thú: theo sau có trễ, đi quanh, tự bảo vệ. Thú hoang đi/nghỉ, tới ăn đồ thả; thu phục và ấp trứng.", "Xem thú và tu luyện", 2);
            SystemCard(908, 295, "03 · Thần Đình & trồng trọt", "100 ô: đồ hoặc đất; cây, nước, lớp trang trí. Offline cây/thú nghỉ tối đa 12 giờ.", "Xem lưới và thử tăng trưởng", 1);
            SystemCard(184, 470, "04 · Nguyên liệu & nghề", "Thả/chọn số lượng từ Thần Đình, nhặt lại giữ dấu chuyến đi. 3 nghề; đồ trên đất vẫn có rủi ro khi gục.", "Xem kho và công thức", 3);
            SystemCard(546, 470, "05 · Cân bằng dữ liệu", "Chỉnh nhịp theo sau, phạm vi đi lại, thời gian đồ rơi, thức ăn và chỉ số. Cấp đồ/tua giờ ở xưởng.", "Mở bàn điều chỉnh", 4);
            SystemCard(908, 470, "06 · Lưu & quan sát", "Lưu local, nhật ký thao tác, tạo phiên có sao lưu và khôi phục. Chưa có cloud save.", "Xem nhật ký và bản lưu", 5);
            var report = Resources.Load<TextAsset>("Project/logic-tests");
            string checks = "Chưa có báo cáo kiểm thử nhúng trong bản này.";
            if (report != null) { var result = JsonUtility.FromJson<TestEvidence>(report.text); checks = "Kiểm thử logic lúc build: " + result.passed + " đạt, " + result.failed + " lỗi."; }
            Text(184, 659, 1055, 26, checks, heading);
            Text(184, 693, 1055, 42, "Giai đoạn sau: server quyết định luật, lưu server, 20–30 vùng và đo tải 100–300 người. Các phần này chưa được triển khai/kiểm chứng.", small);
        }
        private void SystemCard(float x, float y, string name, string description, string action, int target)
        {
            Box(new Rect(x, y, 348, 157)); Text(x + 16, y + 13, 318, 30, name, heading);
            Text(x + 16, y + 48, 318, 63, description, small);
            if (target < 0) Text(x + 16, y + 122, 318, 26, action, small);
            else if (Btn(x + 16, y + 116, 316, 29, action)) Page = target;
        }
        private void RealmPage()
        {
            PageTitle("Thần Đình · không gian 10 × 10", "Ô chứa đồ giữ vật phẩm. Ô đất có cây trồng và một lớp trang trí độc lập. Bấm ô để quản lý.");
            Box(new Rect(184, 177, 596, 545));
            DrawRealmMap(new Rect(225, 192, 510, 510), true);
            Box(new Rect(800, 177, 456, 545));
            int i = host.SelectedCell; var cell = D.cells[i];
            Text(822, 194, 400, 28, "Ô " + (i % 10 + 1) + ", " + (i / 10 + 1) + "  ·  " + CellName(cell), heading);
            Text(822, 230, 410, 54, D.inRealm ? "Thần hồn đang ở bên trong. Chuyển sang Chơi thử để di chuyển." : "Xưởng dự án cho phép kiểm tra/sắp đặt trực tiếp. Mô phỏng đang tạm dừng.", body);
            if (Btn(822, 294, 410, 34, D.inRealm ? "Xuất Thần Đình" : "Nhập Thần Đình", true)) host.Execute(() => S.ToggleRealm());
            if (cell.kind == CellKind.Empty)
            {
                Text(822, 347, 410, 48, "Ô trống có thể chứa một stack đồ hoặc một mảnh đất linh.", body);
                if (Btn(822, 411, 410, 36, "Đặt đất linh  ·  còn " + S.Count("soil"))) host.Execute(() => S.PlaceSoil(i));
            }
            else if (cell.kind == CellKind.Storage)
            {
                Text(822, 349, 410, 36, S.Content.Item(cell.itemId).name + " × " + cell.amount, heading);
                Text(822, 397, 410, 64, "Chưa bảo toàn: " + cell.tripAmount + "\nÔ vật phẩm không trồng cây hoặc tưới nước được.", body);
            }
            else
            {
                string crop = string.IsNullOrEmpty(cell.cropId) ? "Chưa gieo hạt" : "Ngọc linh thảo · " + F(cell.cropProgress / cell.cropDuration * 100) + "%";
                Text(822, 346, 410, 27, crop, heading);
                Text(822, 380, 410, 27, "Nước còn " + Duration(cell.waterRemaining) + "  ·  Trang trí: " + (string.IsNullOrEmpty(cell.decorationId) ? "chưa có" : S.Content.Item(cell.decorationId).name), small);
                if (Btn(822, 421, 128, 34, "Gieo hạt")) host.Execute(() => S.Plant(i));
                if (Btn(963, 421, 128, 34, "Tưới nước")) host.Execute(() => S.Water(i));
                if (Btn(1104, 421, 128, 34, "Thu hoạch", true)) host.Execute(() => S.Harvest(i));
                if (Btn(822, 471, 198, 34, "Đặt cây cảnh")) host.Execute(() => S.Decorate(i, "tree"));
                if (Btn(1034, 471, 198, 34, "Đặt ao cảnh")) host.Execute(() => S.Decorate(i, "pond"));
                if (Btn(822, 517, 410, 32, "Gỡ lớp trang trí, giữ cây trồng")) host.Execute(() => S.RemoveDecoration(i));
            }
            Text(822, 572, 410, 44, transferFrom < 0 ? "Di chuyển nguyên ô: chọn nguồn, bấm nút dưới, rồi chọn một ô đích trống." : "Đang chuyển ô " + (transferFrom + 1) + ". Hãy bấm ô trống trên lưới.", small);
            if (Btn(822, 624, 198, 33, transferFrom < 0 ? "Chuyển ô đang chọn" : "Hủy chuyển ô")) transferFrom = transferFrom < 0 ? i : -1;
            if (Btn(1034, 624, 198, 33, "Tua 5 phút [chủ dự án]")) host.Execute(() => S.AdminAdvance(300));
            Text(822, 674, 410, 30, "Offline tối đa 12h · cây cần nước · thu hoạch thủ công", small);
        }
        private void DrawRealmMap(Rect area, bool editor)
        {
            float size = Mathf.Min(area.width, area.height) / 10;
            float ox = area.x + (area.width - size * 10) / 2, oy = area.y + (area.height - size * 10) / 2;
            Fill(area, C("DCE4D0"));
            for (int y = 0; y < 10; y++) for (int x = 0; x < 10; x++)
            {
                int i = y * 10 + x; var cell = D.cells[i];
                var r = new Rect(ox + x * size + 1, oy + (9 - y) * size + 1, size - 2, size - 2);
                Fill(r, cell.kind == CellKind.Soil ? C("B79A6E") : cell.kind == CellKind.Storage ? C("DDE0D4") : C("EEF0E3"));
                if (cell.kind == CellKind.Storage)
                {
                    Fill(new Rect(r.x + 9, r.y + 8, r.width - 18, r.height - 23), ItemColor(cell.itemId));
                    Text(r.x + 2, r.y + r.height - 20, r.width - 4, 20, ShortItem(cell.itemId) + " " + cell.amount, new GUIStyle(small) { fontSize = 10, alignment = TextAnchor.MiddleCenter });
                    if (cell.tripAmount > 0) Fill(new Rect(r.xMax - 7, r.y + 3, 4, 4), gold);
                }
                if (!string.IsNullOrEmpty(cell.decorationId))
                {
                    if (cell.decorationId == "pond") Fill(new Rect(r.x + 4, r.y + 6, r.width - 8, r.height - 14), C("8FCBE0"));
                    else { Fill(new Rect(r.center.x - 3, r.center.y - 2, 6, 15), C("7E6650")); Fill(new Rect(r.x + 6, r.y + 5, r.width - 12, r.height / 2), C("E6A0B3")); }
                }
                if (!string.IsNullOrEmpty(cell.cropId))
                {
                    float percent = cell.cropProgress / cell.cropDuration;
                    Fill(new Rect(r.center.x - 5, r.yMax - 12 - percent * 15, 10, 7 + percent * 15), percent >= 1 ? C("50764F") : C("78A85F"));
                    Fill(new Rect(r.x + 3, r.yMax - 5, (r.width - 6) * percent, 3), C("F1C86D"));
                }
                if (cell.waterRemaining > 0) Fill(new Rect(r.x + 3, r.y + 3, 5, 5), C("86C5D7"));
                if (host.SelectedCell == i) Outline(r, gold, 3);
                if (editor && GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    if (transferFrom >= 0) { int from = transferFrom; host.Execute(() => S.MoveCell(from, i)); transferFrom = -1; }
                    host.SelectedCell = i;
                }
            }
            if (D.inRealm)
            {
                float px = ox + D.soulX * size, py = oy + (10 - D.soulY) * size;
                Fill(new Rect(px - 8, py - 11, 16, 22), D.controlledBeast == 0 ? C("547FA0") : C("30374C"));
                Outline(new Rect(px - 8, py - 11, 16, 22), gold, 2);
                if (D.beasts.Count > 0) Fill(new Rect(px - 25, py + 7, 12, 12), D.controlledBeast == 0 ? C("30374C") : C("547FA0"));
            }
        }
        private void BeastPage()
        {
            PageTitle("Đồng hành & tu luyện", "Cùng loài dùng cùng kỹ năng. Nguyên liệu tốt bồi dưỡng nhanh hơn; nền tảng có thể bù dần, không hỏng vĩnh viễn.");
            Box(new Rect(184, 177, 330, 547));
            Text(204, 196, 290, 30, "Ngự thú sư", heading);
            Text(204, 235, 290, 27, "Cảnh giới " + (D.player.realm + 1) + " · Khế ước " + D.beasts.Count + "/" + S.ContractLimit, body);
            Bar(new Rect(204, 280, 290, 24), S.CurrentQi(D.player) / S.QiCapacity(D.player), gold, "Tu vi " + F(S.CurrentQi(D.player)) + " / " + F(S.QiCapacity(D.player)));
            Text(204, 322, 290, 70, "Căn cốt " + F(D.player.root) + "\nHấp thu ×" + (1 + D.player.root / 100).ToString("0.00") + "\nCăn cốt cao tích lũy sâu hơn trước đột phá.", body);
            if (Btn(204, 417, 290, 34, D.cultivating ? "Dừng tu luyện" : "Tu luyện tại trại / Thần Đình", true)) host.Execute(() => S.Cultivate());
            if (Btn(204, 465, 290, 34, "Dùng Tụ khí đan · còn " + S.Count("pill"))) host.Execute(() => S.UseItem("pill"));
            if (Btn(204, 513, 290, 34, "Đột phá · cần đủ tu vi + 3 khoáng")) host.Execute(() => S.Breakthrough(0));
            Text(204, 568, 290, 62, "Bản thử: đột phá khi đủ điều kiện; tối đa ba cảnh giới, mở lần lượt 1, 2, 3 khế ước.", small);
            if (Btn(204, 667, 290, 33, "Tua 5 phút [chủ dự án]")) host.Execute(() => S.AdminAdvance(300));
            Box(new Rect(535, 177, 721, 547));
            if (D.beasts.Count == 0)
            {
                Text(568, 225, 648, 65, "Chưa có khế ước. Thu phục linh quy bị thương gần trại để bắt đầu bồi dưỡng.", heading);
                if (Btn(568, 324, 260, 38, "Chơi để thu phục", true)) host.SetPresentation("play");
                if (Btn(568, 378, 260, 38, "Ấp trứng · còn " + S.Count("egg"))) host.Execute(() => S.Incubate());
                return;
            }
            if (S.Beast(selectedBeast) == null) selectedBeast = D.beasts[0].id;
            for (int i = 0; i < D.beasts.Count; i++)
            {
                var b = D.beasts[i]; if (Btn(556 + i * 225, 195, 212, 37, "Quy " + b.id + (b.recovering ? " · nghỉ" : ""), selectedBeast == b.id)) selectedBeast = b.id;
            }
            var pet = S.Beast(selectedBeast); var c = pet.stats;
            Text(558, 251, 674, 31, pet.name, heading);
            Text(558, 288, 674, 27, "Cảnh giới " + (c.realm + 1) + "  ·  " + (pet.recovering ? "Hồi sức trong Thần Đình" : pet.summoned ? "Đang đồng hành" : "Đang ở Thần Đình"), small);
            Bar(new Rect(558, 329, 315, 25), c.hp / S.MaxHp(c, true), C("699976"), "Máu " + F(c.hp) + " / " + F(S.MaxHp(c, true)));
            Bar(new Rect(897, 329, 335, 25), S.CurrentQi(c) / S.QiCapacity(c), gold, "Tu vi " + F(S.CurrentQi(c)) + " / " + F(S.QiCapacity(c)));
            Text(558, 373, 660, 32, "Tấn công " + F(S.Attack(c, true)) + "    Phòng thủ " + F(S.Defense(c, true)) + "    Căn cốt " + F(c.root), body);
            Text(558, 412, 665, 42, "Bồi dưỡng công " + F(c.attackTraining) + "/" + F(S.TrainingCap(c)) + "  ·  thủ " + F(c.defenseTraining) + "/" + F(S.TrainingCap(c)), small);
            if (pet.digestion != null) Bar(new Rect(558, 453, 674, 24), 1 - pet.digestion.remaining / pet.digestion.duration, C("8FAB9E"), "Đang tiêu hóa · còn " + Duration(pet.digestion.remaining));
            else Text(558, 452, 674, 28, "Sẵn sàng ăn. Ngọc linh thảo cho nhiều tu vi và chỉ số hơn.", small);
            if (Btn(558, 498, 322, 36, "Ăn linh thảo thường · " + S.Count("herb"))) host.Execute(() => S.Feed(pet.id, "herb"));
            if (Btn(898, 498, 334, 36, "Ăn ngọc linh thảo · " + S.Count("jade-herb"), true)) host.Execute(() => S.Feed(pet.id, "jade-herb"));
            if (Btn(558, 550, 210, 34, pet.summoned ? "Thu vào Thần Đình" : "Gọi ra đồng hành")) host.Execute(() => S.Summon(pet.id));
            if (Btn(787, 550, 210, 34, "Điều khiển con này")) host.Execute(() => S.SwitchControl(pet.id));
            if (Btn(1016, 550, 216, 34, "Đột phá")) host.Execute(() => S.Breakthrough(pet.id));
            Text(558, 610, 674, 46, "Cạn máu: tự thu hồi, nghỉ khoảng " + Duration(S.Rules.recoverySeconds) + ". Hồi đầy mới gọi ra; giữ liên kết và chỉ số.", small);
            if (Btn(558, 667, 322, 33, "Mô phỏng thú cạn máu [chủ dự án]")) host.Execute(() => { S.DamageBeast(pet.id, 100000); return new ActionResult(true, "Đã mô phỏng cạn máu; quan sát tự hồi phục."); });
            if (Btn(898, 667, 334, 33, "Ấp trứng · còn " + S.Count("egg"))) host.Execute(() => S.Incubate());
        }
        private void InventoryPage()
        {
            PageTitle("Vật phẩm & nghề", "Mọi nghề đều học được. Ba công thức thử nghiệm có đầu vào, đầu ra và kinh nghiệm nghề thực.");
            Box(new Rect(184, 177, 656, 547));
            Text(204, 195, 295, 26, "Kho Thần Đình", heading);
            Text(655, 200, 165, 24, S.UsedCells + " / 100 ô đã dùng", small);
            Text(204, 237, 420, 24, "VẬT PHẨM", small); Text(663, 237, 145, 24, "TỔNG / CHUYẾN ĐI", small);
            for (int i = 0; i < S.Content.items.Count; i++)
            {
                var item = S.Content.items[i]; float y = 270 + i * 29; int trip = 0;
                foreach (var c in D.cells) if (c.itemId == item.id) trip += c.tripAmount;
                Fill(new Rect(204, y + 3, 16, 16), ItemColor(item.id));
                Text(233, y, 390, 25, item.name, body); Text(674, y, 130, 25, S.Count(item.id) + " / " + trip, body);
            }
            Text(204, 673, 609, 35, "Chấm vàng trong lưới = chiến lợi phẩm chưa bảo toàn. Về trại đầu tiên và hoàn tất chuyến đi.", small);
            Box(new Rect(860, 177, 396, 547));
            Text(881, 196, 350, 29, "Chế tạo & sử dụng", heading);
            Recipe(881, 244, "Luyện đan · " + D.medicineXp + " KN", "3 linh thảo = 1 Tụ khí đan", "alchemy", "pill");
            Recipe(881, 365, "Binh khí sư · " + D.smithXp + " KN", "4 linh khoáng = 1 linh kiếm (+8 công)", "smith", "weapon");
            Recipe(881, 486, "Luyện phù · " + D.charmXp + " KN", "3 linh mộc = hộ thân phù (60s)", "charm", "charm");
            Text(881, 617, 350, 56, "Trồng trọt: " + D.farmingXp + " KN\nChế tạo từ đồ chưa bảo toàn giữ dấu chuyến đi.", small);
            if (Btn(881, 680, 354, 28, "Chỉnh vật phẩm / cấp đồ thử")) Page = 4;
        }
        private void Recipe(float x, float y, string name, string description, string recipe, string output)
        {
            Text(x, y, 350, 25, name, heading); Text(x, y + 30, 350, 25, description, small);
            if (Btn(x, y + 65, 170, 32, "Chế tạo", true)) host.Execute(() => S.Craft(recipe));
            if (Btn(x + 185, y + 65, 169, 32, "Dùng · có " + S.Count(output))) host.Execute(() => S.UseItem(output));
        }
        private void Draft()
        {
            if (S == null) return;
            draftRules = JsonUtility.FromJson<GameRules>(JsonUtility.ToJson(S.Rules)); draftContent = JsonUtility.FromJson<GameContent>(JsonUtility.ToJson(S.Content));
        }
        private void RulesPage()
        {
            if (draftRules == null) Draft();
            PageTitle("Bàn điều chỉnh của chủ dự án", "Kéo thanh để sửa, rồi bấm Áp dụng. Luật được lưu cùng phiên chơi; số đang nhập chưa ảnh hưởng game.");
            Box(new Rect(184, 177, 505, 547)); Box(new Rect(709, 177, 547, 547));
            if (Btn(204, 193, 217, 34, "Nuôi trồng & chiến đấu", !movementRules)) movementRules = false;
            if (Btn(451, 193, 217, 34, "Di chuyển & đồ rơi", movementRules)) movementRules = true;
            float y = 242;
            if (!movementRules)
            {
            draftRules.captureChance = Slider(204, ref y, 217, "Thu phục", draftRules.captureChance, 1, 100, "%");
            draftRules.digestionSeconds = Slider(204, ref y, 217, "Tiêu hóa cơ bản", draftRules.digestionSeconds, 1, 180, "s");
            draftRules.recoverySeconds = Slider(204, ref y, 217, "Hồi phục", draftRules.recoverySeconds, 10, 3600, "s");
            draftRules.cropSeconds = Slider(204, ref y, 217, "Cây trưởng thành", draftRules.cropSeconds, 10, 86400, "s");
            draftRules.wateringSeconds = Slider(204, ref y, 217, "Một lần tưới", draftRules.wateringSeconds, 10, 86400, "s");
            draftRules.offlineHours = Slider(204, ref y, 217, "Tối đa khi nghỉ", draftRules.offlineHours, 0, 12, "h");
            draftRules.tripLossPercent = Slider(204, ref y, 217, "Mất chiến lợi phẩm", draftRules.tripLossPercent, 0, 100, "%");
            y = 242;
            draftRules.cultivationPerSecond = Slider(451, ref y, 217, "Tu luyện / giây", draftRules.cultivationPerSecond, .01f, 10, "");
            draftRules.breakthroughBase = Slider(451, ref y, 217, "Tu vi nền đột phá", draftRules.breakthroughBase, 10, 1000, "");
            draftRules.rootCapacityPercent = Slider(451, ref y, 217, "Căn cốt tăng sức chứa", draftRules.rootCapacityPercent, 0, 300, "%");
            draftRules.resourceRespawnSeconds = Slider(451, ref y, 217, "Tài nguyên tái sinh", draftRules.resourceRespawnSeconds, 5, 300, "s");
            draftRules.affinityChance = Slider(451, ref y, 217, "Xuất hiện hảo cảm", draftRules.affinityChance, 1, 100, "%");
            draftRules.affinitySeconds = Slider(451, ref y, 217, "Duy trì hảo cảm", draftRules.affinitySeconds, 10, 600, "s");
            Text(204, 645, 464, 56, "Giới hạn căn cốt và cảnh giới được kiểm tra trong logic. Mẫu: căn cốt 0 cần 100 tu vi, căn cốt 100 tích được 200.", small);
            }
            else
            {
                draftRules.followerDelaySeconds = Slider(204, ref y, 217, "Trễ theo sau", draftRules.followerDelaySeconds, .2f, 1.2f, "s");
                draftRules.followerNearRadius = Slider(204, ref y, 217, "Khoảng đi quanh", draftRules.followerNearRadius, .8f, 2.5f, "ô");
                draftRules.followerCatchupDistance = Slider(204, ref y, 217, "Bắt đầu đuổi kịp", draftRules.followerCatchupDistance, 3, 6, "ô");
                y = 242;
                draftRules.wildRoamRadius = Slider(451, ref y, 217, "Thú hoang đi quanh", draftRules.wildRoamRadius, 1, 4, "ô");
                draftRules.wildLeashRadius = Slider(451, ref y, 217, "Giới hạn đuổi", draftRules.wildLeashRadius, 4, 8, "ô");
                draftRules.dropLifetimeSeconds = Slider(451, ref y, 217, "Đồ rơi tồn tại", draftRules.dropLifetimeSeconds, 30, 1800, "s");
                Text(204, 451, 464, 132, "Thú theo dấu đường với độ trễ rồi dừng/đi quanh. Khi bạn đánh hoặc bị đánh, thú hỗ trợ trong phạm vi gần.\nThú hoang trở về nơi ở khi bị kéo quá xa. Đồ trên đất chỉ đếm thời gian khi game chạy.", body);
                Text(204, 628, 464, 65, "Đồ trên đất hiện tại: " + D.drops.Count + "/" + GameSession.MaxGroundDrops + ". Thả từ ô đồ cạnh thân thể; không cần đứng gần thú. Bấm Áp dụng ở bên phải.", small);
            }
            Text(729, 194, 500, 28, "Danh mục & chỉ số loài", heading);
            if (Btn(729, 236, 37, 31, "<")) itemIndex = (itemIndex + draftContent.items.Count - 1) % draftContent.items.Count;
            if (Btn(1200, 236, 35, 31, ">")) itemIndex = (itemIndex + 1) % draftContent.items.Count;
            var item = draftContent.items[itemIndex];
            GUI.SetNextControlName("editItem"); item.name = GUI.TextField(new Rect(778, 236, 410, 31), item.name, 50, field);
            Text(729, 280, 500, 24, item.category + " · " + item.id, small);
            y = 315;
            item.qi = Slider(729, ref y, 234, "Tu vi thức ăn", item.qi, 0, 100, "");
            item.attack = Slider(729, ref y, 234, "Bồi dưỡng công", item.attack, 0, 10, "");
            y = 315;
            item.defense = Slider(1001, ref y, 234, "Bồi dưỡng thủ", item.defense, 0, 10, "");
            item.root = Slider(1001, ref y, 234, "Tăng căn cốt", item.root, 0, 10, "");
            y = 435;
            draftContent.turtle.maxHp = Slider(729, ref y, 234, "Quy · máu cơ bản", draftContent.turtle.maxHp, 10, 500, "");
            draftContent.turtle.attack = Slider(729, ref y, 234, "Quy · tấn công", draftContent.turtle.attack, 1, 100, "");
            y = 435;
            draftContent.turtle.defense = Slider(1001, ref y, 234, "Quy · phòng thủ", draftContent.turtle.defense, 0, 100, "");
            draftContent.turtle.speed = Slider(1001, ref y, 234, "Quy · tốc độ", draftContent.turtle.speed, 1, 10, "");
            Text(729, 554, 500, 23, "CẤP ĐỒ THỬ  /  TĂNG CẢNH GIỚI", small);
            grantCount = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(729, 589, 172, 20), grantCount, 1, 99));
            if (Btn(918, 580, 145, 31, "Cấp ×" + grantCount)) host.Execute(() => S.AdminGrant(item.id, grantCount));
            if (Btn(1077, 580, 158, 31, "Mở 3 khế ước")) host.Execute(() => S.AdminRealm(2));
            if (Btn(729, 628, 245, 31, "Bỏ phần chưa áp dụng")) Draft();
            if (Btn(990, 628, 245, 31, "Áp dụng & lưu", true))
            {
                var rules = JsonUtility.FromJson<GameRules>(JsonUtility.ToJson(draftRules));
                var content = JsonUtility.FromJson<GameContent>(JsonUtility.ToJson(draftContent));
                host.Execute(() => S.ApplyConfiguration(rules, content));
            }
            Text(729, 675, 504, 27, "Các nút cấp đồ/tua giờ chỉ phục vụ chủ dự án ở bản local.", small);
        }
        private void JournalPage()
        {
            PageTitle("Nhật ký & phiên chơi", "Xem kết quả thật của thao tác, vị trí lưu và các quyết định tạm dùng cho bản thử.");
            Box(new Rect(184, 177, 670, 547));
            Text(204, 195, 620, 28, "Hoạt động gần nhất", heading);
            for (int i = 0; i < Math.Min(10, D.journal.Count); i++)
            {
                Text(204, 243 + i * 45, 628, 41, D.journal[i], i == 0 ? body : small);
                if (i < 9) Fill(new Rect(204, 284 + i * 45, 628, 1), line);
            }
            Box(new Rect(874, 177, 382, 547));
            Text(894, 196, 342, 30, "Quyết định tạm dùng", heading);
            Text(894, 240, 342, 205, "• Một Thần Đình 10×10 trước.\n• Ba cảnh giới mở từ 1 đến 3 khế ước.\n• Khế ước chưa có nút gỡ.\n• Đột phá đủ tu vi + 3 khoáng.\n• Cây và thú nghỉ tiến triển offline.\n• Tu luyện / tiêu hóa cần mở game.\n• Nguyên liệu mới được bảo toàn khi về trại.\n• Mọi nghề đều học được.", body);
            Text(894, 461, 342, 75, "Lưu tại:\n" + host.SaveLocation, small);
            Text(894, 543, 342, 39, "Bản web và Editor lưu riêng. Xóa dữ liệu trình duyệt sẽ mất phiên web.", small);
            if (Btn(894, 598, 342, 33, "Khôi phục trước lần tạo mới")) { host.Restore(); Draft(); }
            if (GUI.Button(new Rect(894, 650, 342, 34), confirmNew ? "Xác nhận tạo mới (có sao lưu)" : "Tạo phiên chơi mới…", dangerButton))
            {
                if (confirmNew) { host.NewGame(); confirmNew = false; Draft(); } else confirmNew = true;
            }
            if (confirmNew && Btn(894, 690, 342, 24, "Hủy")) confirmNew = false;
        }
        private float Slider(float x, ref float y, float width, string label, float value, float min, float max, string unit)
        {
            Text(x, y, width, 24, label + " · " + value.ToString(value < 10 ? "0.##" : "0") + unit, small);
            float next = GUI.HorizontalSlider(new Rect(x, y + 28, width, 18), value, min, max);
            y += 55; return next;
        }
        private bool Btn(float x, float y, float w, float h, string text, bool active = false) => GUI.Button(new Rect(x, y, w, h), text, active ? activeButton : button);
        private void Text(float x, float y, float w, float h, string text, GUIStyle style) => GUI.Label(new Rect(x, y, w, h), text, style);
        private static void Fill(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        private static void Outline(Rect r, Color c, float width) { Fill(new Rect(r.x, r.y, r.width, width), c); Fill(new Rect(r.x, r.yMax - width, r.width, width), c); Fill(new Rect(r.x, r.y, width, r.height), c); Fill(new Rect(r.xMax - width, r.y, width, r.height), c); }
        private void Box(Rect r) { Fill(r, card); Outline(r, line, 1); }
        private void Bar(Rect r, float value, Color color, string text)
        {
            Fill(r, C("E2E4D9")); Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(value), r.height), color);
            Text(r.x + 5, r.y + 1, r.width - 10, r.height, text, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, normal = { textColor = ink } });
        }
        private static string F(float n) => n.ToString("0.#");
        private static string Duration(float seconds) => seconds >= 3600 ? (seconds / 3600).ToString("0.#") + "h" : seconds >= 60 ? (seconds / 60).ToString("0.#") + " phút" : Mathf.CeilToInt(seconds) + "s";
        private string CellName(RealmCell c) => c.kind == CellKind.Empty ? "Trống" : c.kind == CellKind.Soil ? "Đất linh" : S.Content.Item(c.itemId).name;
        private static string ShortItem(string id)
        {
            switch (id) { case "herb": return "THẢO"; case "jade-herb": return "NGỌC"; case "soil": return "ĐẤT"; case "blood": return "KHẾ"; case "seed": return "HẠT"; case "wood": return "MỘC"; case "stone": return "KHOÁNG"; case "pill": return "ĐAN"; case "tree": return "CÂY"; case "pond": return "AO"; case "egg": return "TRỨNG"; case "charm": return "PHÙ"; default: return "KIẾM"; }
        }
        private static Color ItemColor(string id)
        {
            switch (id) { case "herb": return C("68944C"); case "jade-herb": return C("438D7B"); case "soil": return C("B79A6E"); case "blood": return C("C45E72"); case "seed": return C("B9A456"); case "wood": return C("9A785B"); case "stone": return C("899CBD"); case "pill": return C("D5AD63"); case "tree": return C("E6A0B3"); case "pond": return C("8FCBE0"); default: return C("9890AF"); }
        }
    }
}
