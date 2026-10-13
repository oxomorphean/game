using System;

namespace LinhThu.Domain
{
    public sealed partial class GameSession
    {
        public ActionResult Feed(int beastId, string food)
        {
            var b = Beast(beastId); var item = Content.Item(food);
            if (b == null || item == null || item.category != "Thức ăn") return Result(false, "Chọn linh thú và thức ăn phù hợp.");
            if (b.recovering) return Result(false, "Linh thú đang hồi phục, hãy để nó nghỉ.");
            if (b.digestion != null) return Result(false, "Linh thú đang tiêu hóa bữa trước.");
            if (!Spend(food, 1)) return Result(false, "Không đủ " + item.name + ".");
            float duration = Rules.digestionSeconds / (1 + b.stats.root / 100);
            b.digestion = new Digestion { itemId = food, remaining = duration, duration = duration, qi = item.qi, attack = item.attack, defense = item.defense, root = item.root };
            return Result(true, b.name + " đang tiêu hóa " + item.name + "; vẫn có thể đồng hành.");
        }
        private void AddQi(Cultivator stats, float qi) { stats.qi += Math.Min(qi, Math.Max(0, QiCapacity(stats) - CurrentQi(stats))); }
        private void AdvanceGrowth(float seconds, bool offline)
        {
            foreach (var cell in State.cells)
            {
                if (cell.kind != CellKind.Soil) continue;
                float wetSeconds = Math.Min(seconds, cell.waterRemaining);
                if (!string.IsNullOrEmpty(cell.cropId)) cell.cropProgress = Math.Min(cell.cropDuration, cell.cropProgress + wetSeconds);
                cell.waterRemaining = Math.Max(0, cell.waterRemaining - seconds);
            }
            foreach (var b in State.beasts)
            {
                if (b.recovering)
                {
                    b.stats.hp = Math.Min(MaxHp(b.stats, true), b.stats.hp + seconds * MaxHp(b.stats, true) / Rules.recoverySeconds);
                    if (b.stats.hp >= MaxHp(b.stats, true)) { b.recovering = false; Note(b.name + " đã hồi phục. Có thể gọi ra đồng hành."); }
                }
                if (!offline && b.digestion != null && !b.recovering)
                {
                    b.digestion.remaining -= seconds;
                    if (b.digestion.remaining <= 0)
                    {
                        var food = b.digestion; b.stats.root = Math.Min(100, b.stats.root + food.root);
                        AddQi(b.stats, food.qi * (1 + b.stats.root / 100));
                        b.stats.attackTraining = Math.Min(TrainingCap(b.stats), b.stats.attackTraining + food.attack);
                        b.stats.defenseTraining = Math.Min(TrainingCap(b.stats), b.stats.defenseTraining + food.defense);
                        b.digestion = null; Note(b.name + " hấp thu xong: tu vi và nền tảng đã tăng.");
                    }
                }
            }
            if (!offline && State.cultivating) AddQi(State.player, seconds * Rules.cultivationPerSecond * (1 + State.player.root / 100));
            if (!offline && State.eggRemaining > 0)
            {
                State.eggRemaining = Math.Max(0, State.eggRemaining - seconds);
                if (State.eggRemaining == 0) { CreateBeast("Linh quy mới nở"); Note("Trứng đã nở; linh quy mới đã liên kết với bạn."); }
            }
        }
        public ActionResult Cultivate()
        {
            if (!State.inRealm && (State.room != 0 || Distance(State.player.x, State.player.y, 3, 3) > 3)) return Result(false, "Tu luyện tại trại hoặc trong Thần Đình.");
            State.cultivating = !State.cultivating;
            return Result(true, State.cultivating ? "Bắt đầu tu luyện. Di chuyển sẽ dừng." : "Đã dừng tu luyện.");
        }
        public ActionResult Breakthrough(int id)
        {
            var b = id == 0 ? null : Beast(id);
            if (id != 0 && b == null) return Result(false, "Không tìm thấy linh thú.");
            var c = b == null ? State.player : b.stats;
            if (c.realm >= 2) return Result(false, "Bản thử có ba cảnh giới.");
            if (b != null && b.recovering) return Result(false, "Cần hồi phục trước khi đột phá.");
            if (!State.inRealm) return Result(false, "Đột phá trong Thần Đình.");
            float need = QiCapacity(c);
            if (CurrentQi(c) + 0.001f < need) return Result(false, "Chưa tích lũy đủ " + Math.Ceiling(need) + " tu vi.");
            if (!Spend("stone", 3)) return Result(false, "Cần 3 linh khoáng để đột phá.");
            c.spentQi += need; c.realm++; c.hp = MaxHp(c, b != null);
            return Result(true, "Đột phá thành công. Tổng tu vi và chỉ số bồi dưỡng được giữ nguyên.");
        }
        public ActionResult Summon(int id)
        {
            var b = Beast(id);
            if (b == null || b.recovering) return Result(false, "Linh thú chưa hồi phục hoặc không tồn tại.");
            b.summoned = !b.summoned;
            if (!b.summoned && State.controlledBeast == id) State.controlledBeast = 0;
            if (b.summoned) { b.stats.x = Clamp(State.player.x + 0.8f, 0.5f, 23.5f); b.stats.y = State.player.y; }
            return Result(true, b.summoned ? b.name + " đang đồng hành." : b.name + " đã nghỉ trong Thần Đình.");
        }
        public ActionResult Incubate()
        {
            if (State.eggRemaining > 0) return Result(false, "Đang ấp một quả trứng.");
            if (State.beasts.Count >= ContractLimit) return Result(false, "Cần một ô khế ước trống để ấp trứng.");
            if (!Spend("egg", 1)) return Result(false, "Chưa có trứng linh quy.");
            State.eggRemaining = 60;
            return Result(true, "Đang ấp trứng trong 60 giây; đã giữ một ô khế ước.");
        }
        private void CreateBeast(string name)
        {
            var b = new Beast { id = State.nextBeastId++, name = name };
            b.stats.x = Clamp(State.player.x + 1, 0.5f, 23.5f); b.stats.y = State.player.y; b.stats.hp = MaxHp(b.stats, true);
            State.beasts.Add(b); State.captures++;
        }
        public void DamageBeast(int id, float damage)
        {
            if (!GameRules.Range(damage, 0, 1e6f)) throw new ArgumentOutOfRangeException(nameof(damage));
            var b = Beast(id); if (b == null || b.recovering) return;
            b.stats.hp = Math.Max(0, b.stats.hp - damage);
            if (b.stats.hp == 0)
            {
                b.recovering = true; b.summoned = false;
                if (State.controlledBeast == id) State.controlledBeast = 0;
                Note(b.name + " cạn máu, tự trở về Thần Đình nghỉ ngơi.");
            }
        }
        public void DamagePlayer(float damage)
        {
            if (!GameRules.Range(damage, 0, 1e6f)) throw new ArgumentOutOfRangeException(nameof(damage));
            State.player.hp = Math.Max(0, State.player.hp - damage);
            if (State.player.hp > 0) return;
            int lost = 0;
            foreach (var cell in State.cells)
            {
                int amount = (int)Math.Ceiling(cell.tripAmount * Rules.tripLossPercent / 100);
                cell.amount -= amount; cell.tripAmount -= amount; lost += amount;
                if (cell.kind == CellKind.Storage && cell.amount == 0) { cell.kind = CellKind.Empty; cell.itemId = ""; }
            }
            State.room = 0; State.inRealm = false; State.cultivating = false; State.controlledBeast = 0;
            State.player.hp = MaxHp(State.player, false); State.player.x = 3; State.player.y = 3;
            foreach (var b in State.beasts) { b.stats.x = 3; b.stats.y = 4; }
            Note("Gục ngã: mất " + lost + " nguyên liệu của chuyến đi. Đồ đã bảo toàn không mất; trở về trại.");
        }
    }
}
