using System;

namespace LinhThu.Domain
{
    public sealed partial class GameSession
    {
        private int Capacity(string item)
        {
            var definition = Content.Item(item);
            if (definition == null) return 0;
            int capacity = 0;
            foreach (var c in State.cells)
                if (c.kind == CellKind.Empty) capacity += definition.stackLimit;
                else if (c.kind == CellKind.Storage && c.itemId == item) capacity += definition.stackLimit - c.amount;
            return capacity;
        }
        private bool AddItems(string item, int amount, bool trip)
        {
            if (amount <= 0 || Capacity(item) < amount) return false;
            int left = amount, limit = Content.Item(item).stackLimit;
            // Merge before occupying another cell, preserving planted land and item layers.
            foreach (var c in State.cells)
            {
                if (c.kind != CellKind.Storage || c.itemId != item) continue;
                int add = Math.Min(left, limit - c.amount); c.amount += add;
                if (trip) c.tripAmount += add;
                left -= add; if (left == 0) return true;
            }
            foreach (var c in State.cells)
            {
                if (c.kind != CellKind.Empty) continue;
                int add = Math.Min(left, limit); c.kind = CellKind.Storage; c.itemId = item; c.amount = add; c.tripAmount = trip ? add : 0;
                left -= add; if (left == 0) return true;
            }
            throw new InvalidOperationException("Preflight capacity diverged from inventory write.");
        }
        private bool Spend(string item, int count)
        {
            if (count < 1 || Count(item) < count) return false;
            foreach (var c in State.cells)
            {
                if (c.kind != CellKind.Storage || c.itemId != item) continue;
                int take = Math.Min(c.amount, count); c.amount -= take; c.tripAmount -= Math.Min(take, c.tripAmount); count -= take;
                if (c.amount == 0) { c.kind = CellKind.Empty; c.itemId = ""; }
                if (count == 0) break;
            }
            return true;
        }
        public ActionResult MoveCell(int from, int to)
        {
            if (from < 0 || from >= 100 || to < 0 || to >= 100 || from == to) return Result(false, "Chọn hai ô khác nhau trong lưới.");
            if (State.cells[to].kind != CellKind.Empty) return Result(false, "Ô đích đang có đồ hoặc đất.");
            if (State.cells[from].kind == CellKind.Empty) return Result(false, "Ô nguồn trống.");
            var cell = State.cells[from]; State.cells[from] = State.cells[to]; State.cells[to] = cell;
            return Result(true, "Đã chuyển ô, giữ nguyên đồ/cây/trang trí và tiến độ.");
        }
        public ActionResult PlaceSoil(int index)
        {
            if (!ValidCell(index) || State.cells[index].kind != CellKind.Empty) return Result(false, "Chỉ đặt đất vào ô trống.");
            if (!Spend("soil", 1)) return Result(false, "Cần 1 mảnh đất linh.");
            State.cells[index].kind = CellKind.Soil;
            return Result(true, "Đã đặt đất ở ô " + (index + 1) + ". Có thể gieo hạt và đặt trang trí phía trên.");
        }
        public ActionResult Plant(int index)
        {
            if (!ValidCell(index) || State.cells[index].kind != CellKind.Soil || !string.IsNullOrEmpty(State.cells[index].cropId)) return Result(false, "Cần ô đất chưa có cây trồng.");
            if (!Spend("seed", 1)) return Result(false, "Cần 1 hạt linh thảo.");
            var c = State.cells[index]; c.cropId = "jade-herb"; c.cropProgress = 0; c.cropDuration = Rules.cropSeconds;
            return Result(true, "Đã gieo hạt. Tưới nước để cây bắt đầu lớn.");
        }
        public ActionResult Water(int index)
        {
            if (!ValidCell(index) || State.cells[index].kind != CellKind.Soil) return Result(false, "Chỉ tưới được ô đất.");
            State.cells[index].waterRemaining = Rules.wateringSeconds;
            return Result(true, "Đã tưới nước. Nước miễn phí trong bản thử.");
        }
        public ActionResult Harvest(int index)
        {
            if (!ValidCell(index)) return Result(false, "Ô không tồn tại.");
            var c = State.cells[index];
            if (string.IsNullOrEmpty(c.cropId) || c.cropProgress < c.cropDuration) return Result(false, "Cây chưa trưởng thành.");
            if (!AddItems(c.cropId, 3, false)) return Result(false, "Thần Đình đầy; cây vẫn được giữ nguyên.");
            c.cropId = ""; c.cropProgress = 0; c.cropDuration = 0; State.farmingXp += 5; State.harvests++;
            return Result(true, "Thu hoạch 3 ngọc linh thảo; nghề trồng trọt +5 kinh nghiệm.");
        }
        public ActionResult Decorate(int index, string item)
        {
            if (!ValidCell(index) || State.cells[index].kind != CellKind.Soil) return Result(false, "Cần ô đất để đặt trang trí.");
            if (item != "tree" && item != "pond") return Result(false, "Trang trí không hợp lệ.");
            var c = State.cells[index];
            if (!string.IsNullOrEmpty(c.decorationId)) return Result(false, "Gỡ trang trí hiện có trước.");
            if (!Spend(item, 1)) return Result(false, "Chưa có " + Content.Item(item).name + ".");
            c.decorationId = item; return Result(true, "Đã đặt " + Content.Item(item).name + " trên đất; cây trồng vẫn được giữ.");
        }
        public ActionResult RemoveDecoration(int index)
        {
            if (!ValidCell(index) || string.IsNullOrEmpty(State.cells[index].decorationId)) return Result(false, "Ô không có trang trí.");
            var c = State.cells[index];
            if (!AddItems(c.decorationId, 1, false)) return Result(false, "Không đủ chỗ cất trang trí.");
            c.decorationId = ""; return Result(true, "Đã cất trang trí vào ô vật phẩm.");
        }
        public ActionResult ToggleRealm()
        {
            if (!State.inRealm && State.time - State.lastCombat < 5) return Result(false, "Rời giao tranh ít nhất 5 giây trước khi nhập Thần Đình.");
            State.inRealm = !State.inRealm; State.cultivating = false;
            if (State.inRealm) { State.soulX = 4.5f; State.soulY = 1; }
            return Result(true, State.inRealm ? "Thần hồn đã vào Thần Đình. Thân thể được bảo hộ trong bản thử local." : "Đã trở về thân thể, tiếp tục chuyến đi.");
        }
        public ActionResult BankTrip()
        {
            if (State.inRealm || State.room != 0 || Distance(State.player.x, State.player.y, 3, 3) > 2.5f) return Result(false, "Đưa người chơi về trại đầu tiên để hoàn tất chuyến đi.");
            int n = TripCount; foreach (var c in State.cells) c.tripAmount = 0;
            return Result(true, "Hoàn tất chuyến đi: " + n + " nguyên liệu được bảo toàn.");
        }
        public ActionResult Craft(string recipe)
        {
            string input, output; int needed;
            if (recipe == "alchemy") { input = "herb"; output = "pill"; needed = 3; }
            else if (recipe == "smith") { input = "stone"; output = "weapon"; needed = 4; }
            else if (recipe == "charm") { input = "wood"; output = "charm"; needed = 3; }
            else return Result(false, "Công thức chưa có.");
            if (Count(input) < needed) return Result(false, "Cần " + needed + " " + Content.Item(input).name + ".");
            // Conservative capacity check: no ingredients are spent if the output cannot fit now.
            if (Capacity(output) < 1) return Result(false, "Dành một ô trống cho sản phẩm trước khi chế tạo.");
            bool trip = false; foreach (var c in State.cells) if (c.itemId == input && c.tripAmount > 0) trip = true;
            Spend(input, needed); AddItems(output, 1, trip);
            if (recipe == "alchemy") State.medicineXp += 5; else if (recipe == "smith") State.smithXp += 5; else State.charmXp += 5;
            return Result(true, "Chế tạo " + Content.Item(output).name + "; nghề +5 kinh nghiệm.");
        }
        public ActionResult UseItem(string item)
        {
            if (item != "pill" && item != "weapon" && item != "charm") return Result(false, "Vật phẩm này không dùng trực tiếp cho người chơi.");
            if (item == "pill" && CurrentQi(State.player) >= QiCapacity(State.player)) return Result(false, "Tu vi đã đầy; hãy đột phá trước.");
            if (item == "weapon" && State.weaponBonus >= 8) return Result(false, "Đã trang bị linh kiếm thô.");
            if (!Spend(item, 1)) return Result(false, "Chưa có " + Content.Item(item).name + ".");
            if (item == "pill") AddQi(State.player, 35); else if (item == "weapon") State.weaponBonus = 8; else State.shieldRemaining = 60;
            return Result(true, "Đã dùng " + Content.Item(item).name + ".");
        }
        private static bool ValidCell(int index) => index >= 0 && index < 100;
    }
}
