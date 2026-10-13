using System;

namespace LinhThu.Domain
{
    public sealed partial class GameSession
    {
        public const int MaxGroundDrops = 64;
        public GroundDrop NearbyDrop()
        {
            if (State.inRealm) return null;
            GroundDrop nearest = null; float best = 2;
            foreach (var drop in State.drops)
            {
                float distance = Distance(Controlled.x, Controlled.y, drop.x, drop.y);
                if (drop.room == State.room && distance < best) { nearest = drop; best = distance; }
            }
            return nearest;
        }

        /// <summary>Drops from one selected stack beside the physical player, including while the soul is in the realm.</summary>
        public ActionResult DropFromCell(int index, int amount)
        {
            if (!ValidCell(index) || amount < 1) return Result(false, "Chọn ô vật phẩm và số lượng dương.");
            var cell = State.cells[index];
            if (cell.kind != CellKind.Storage || amount > cell.amount) return Result(false, "Ô không đủ vật phẩm để thả.");
            int trip = Math.Min(amount, cell.tripAmount);
            float x = Clamp(State.player.x + .85f, .5f, 23.5f), y = Clamp(State.player.y, .5f, 13.5f);
            if (!PlaceDrop(cell.itemId, amount, trip, State.room, x, y)) return Result(false, "Đang có nhiều đồ trên đất. Nhặt bớt trước khi thả thêm.");
            string name = Content.Item(cell.itemId).name;
            cell.amount -= amount; cell.tripAmount -= trip;
            if (cell.amount == 0) { cell.kind = CellKind.Empty; cell.itemId = ""; }
            return Result(true, "Đã thả " + amount + " " + name + " cạnh thân thể ngoài map; có thể nhặt lại trước khi biến mất.");
        }

        public ActionResult DropItem(string item, int amount)
        {
            for (int i = 0; i < State.cells.Length; i++)
                if (State.cells[i].kind == CellKind.Storage && State.cells[i].itemId == item && State.cells[i].amount >= amount)
                    return DropFromCell(i, amount);
            return Result(false, "Không có stack đủ vật phẩm để thả.");
        }

        public ActionResult PickUpDrop(int id)
        {
            var drop = State.drops.Find(d => d.id == id);
            if (State.inRealm || drop == null || drop.room != State.room || Distance(Controlled.x, Controlled.y, drop.x, drop.y) > 2)
                return Result(false, "Đến gần đồ còn nằm trên đất để nhặt.");
            if (Capacity(drop.itemId) < drop.amount) return Result(false, "Thần Đình đầy; đồ vẫn nằm trên đất.");
            int safe = drop.amount - drop.tripAmount;
            if (safe > 0) AddItems(drop.itemId, safe, false);
            if (drop.tripAmount > 0) AddItems(drop.itemId, drop.tripAmount, true);
            State.drops.Remove(drop);
            return Result(true, "Đã nhặt " + drop.amount + " " + Content.Item(drop.itemId).name + ".");
        }

        private bool PlaceDrop(string item, int amount, int trip, int room, float x, float y)
        {
            var definition = Content.Item(item);
            foreach (var drop in State.drops)
                if (drop.room == room && drop.itemId == item && Distance(x, y, drop.x, drop.y) < 1.1f && drop.amount + amount <= definition.stackLimit)
                { drop.amount += amount; drop.tripAmount += trip; drop.remaining = Rules.dropLifetimeSeconds; return true; }
            if (State.drops.Count >= MaxGroundDrops || State.nextDropId == int.MaxValue) return false;
            State.drops.Add(new GroundDrop { id = State.nextDropId++, room = room, itemId = item, amount = amount,
                tripAmount = trip, x = x, y = y, remaining = Rules.dropLifetimeSeconds });
            return true;
        }

        private void UpdateDrops(float dt)
        {
            for (int i = State.drops.Count - 1; i >= 0; i--)
            {
                State.drops[i].remaining -= dt;
                if (State.drops[i].remaining <= 0) State.drops.RemoveAt(i);
            }
        }

        private GroundDrop NearbyFood(WildBeast beast)
        {
            GroundDrop result = null; float best = 5;
            foreach (var drop in State.drops)
            {
                if (drop.room != beast.room || Content.Item(drop.itemId).category != "Thức ăn" ||
                    Distance(drop.x, drop.y, beast.spawnX, beast.spawnY) > Rules.wildLeashRadius) continue;
                float distance = Distance(beast.x, beast.y, drop.x, drop.y);
                if (distance < best) { best = distance; result = drop; }
            }
            return result;
        }

        private void EatGroundFood(WildBeast beast, GroundDrop drop)
        {
            if (!State.drops.Contains(drop) || drop.amount < 1) return;
            drop.amount--; drop.tripAmount = Math.Max(0, drop.tripAmount - 1);
            if (drop.amount == 0) State.drops.Remove(drop);
            if (beast.affinity > 0 || chance.NextPercent() < Rules.affinityChance)
            {
                beast.affinity = Math.Min(100, beast.affinity + 20); beast.affinityRemaining = Rules.affinitySeconds;
                Note("Linh quy đã ăn thức ăn trên đất: hảo cảm " + beast.affinity + ". Có thể đến gần dùng Huyết Khế.");
            }
            else Note("Linh quy đã ăn thức ăn trên đất, chưa có hảo cảm.");
        }
    }
}
