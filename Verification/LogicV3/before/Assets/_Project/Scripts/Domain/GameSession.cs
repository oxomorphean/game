using System;
using System.Collections.Generic;

namespace LinhThu.Domain
{
    /// <summary>Single local authority for the prototype. Network and persistence adapters must call these rules, never trust client results.</summary>
    public sealed partial class GameSession
    {
        public GameState State { get; }
        public GameRules Rules { get; private set; }
        public GameContent Content { get; private set; }
        private readonly IChanceSource chance;
        public GameSession(GameState state, GameRules rules, GameContent content, IChanceSource chance)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            this.chance = chance ?? throw new ArgumentNullException(nameof(chance));
            var error = rules.Validate() ?? content.Validate() ?? ValidateState();
            if (error != null) throw new ArgumentException(error);
            ResetFollowTrail();
        }
        public static GameSession New(int seed = 2305)
        {
            var state = new GameState();
            for (int i = 0; i < state.cells.Length; i++) state.cells[i] = new RealmCell();
            for (int i = 0; i < 4; i++) state.wild.Add(new WildBeast { id = i + 1, room = i / 2, x = i % 2 == 0 ? 6 : 16, y = i % 2 == 0 ? 4 : 9, spawnX = i % 2 == 0 ? 6 : 16, spawnY = i % 2 == 0 ? 4 : 9, injured = i == 0, hp = i == 0 ? 0 : 45, timer = i == 0 ? 300 : 0 });
            string[] ids = { "herb", "herb", "wood", "stone", "jade-herb", "seed" };
            for (int room = 0; room < 2; room++)
                for (int i = 0; i < ids.Length; i++) state.nodes.Add(new ResourceNode { id = room * 10 + i, room = room, itemId = room == 1 && i == 5 ? "egg" : ids[i], x = 4 + i % 3 * 6, y = 6 + i / 3 * 5 });
            var session = new GameSession(state, new GameRules(), GameContent.Defaults(), new RandomChance(seed));
            session.AddItems("blood", 8, false); session.AddItems("herb", 6, false); session.AddItems("jade-herb", 3, false);
            session.AddItems("soil", 6, false); session.AddItems("seed", 6, false); session.AddItems("tree", 2, false); session.AddItems("pond", 1, false);
            session.Note("Chào mừng! Linh quy suy yếu ở cạnh trại. Đến gần rồi nhấn E để mở thu phục.");
            return session;
        }
        public Beast Beast(int id) => State.beasts.Find(b => b.id == id);
        public Cultivator Controlled => State.controlledBeast == 0 ? State.player : Beast(State.controlledBeast).stats;
        public int ContractLimit => Math.Min(3, State.player.realm + 1);
        public float QiCapacity(Cultivator c) => Rules.breakthroughBase * (c.realm + 1) * (1 + c.root / 100 * Rules.rootCapacityPercent / 100);
        public float CurrentQi(Cultivator c) => Math.Max(0, c.qi - c.spentQi);
        public float MaxHp(Cultivator c, bool pet) => (pet ? Content.turtle.maxHp : 100) + c.qi * 0.25f + c.realm * 20;
        public float Attack(Cultivator c, bool pet) => (pet ? Content.turtle.attack : 14 + State.weaponBonus) + c.qi * 0.06f + c.attackTraining;
        public float Defense(Cultivator c, bool pet) => (pet ? Content.turtle.defense : 2) + c.defenseTraining;
        public float TrainingCap(Cultivator c) => 20 * (c.realm + 1);
        public int Count(string item) { int n = 0; foreach (var c in State.cells) if (c.kind == CellKind.Storage && c.itemId == item) n += c.amount; return n; }
        public int TripCount { get { int n = 0; foreach (var c in State.cells) n += c.tripAmount; return n; } }
        public int UsedCells { get { int n = 0; foreach (var c in State.cells) if (c.kind != CellKind.Empty) n++; return n; } }
        public void Note(string message)
        {
            State.journal.Insert(0, message);
            if (State.journal.Count > 60) State.journal.RemoveAt(State.journal.Count - 1);
        }
        private ActionResult Result(bool ok, string message) { Note(message); return new ActionResult(ok, message); }
        public ActionResult ApplyConfiguration(GameRules rules, GameContent content)
        {
            if (rules == null || content == null) return Result(false, "Thiếu cấu hình.");
            string error = rules.Validate() ?? content.Validate();
            if (error != null) return Result(false, error);
            foreach (var cell in State.cells)
                if (cell.kind == CellKind.Storage && cell.amount > content.Item(cell.itemId).stackLimit) return Result(false, "Stack đang có lớn hơn giới hạn mới; hãy giữ giới hạn đủ lớn.");
            foreach (var drop in State.drops)
                if (drop.amount > content.Item(drop.itemId).stackLimit) return Result(false, "Stack trên đất lớn hơn giới hạn mới; nhặt hoặc giữ giới hạn đủ lớn.");
            // Existing jobs keep their captured duration and yield. New actions use the new content.
            Rules = rules; Content = content;
            foreach (var b in State.beasts) b.stats.hp = Math.Min(b.stats.hp, MaxHp(b.stats, true));
            return Result(true, "Đã áp dụng luật và dữ liệu. Bữa ăn/cây đã bắt đầu giữ thông số lúc tạo.");
        }
        public void Tick(float seconds)
        {
            if (!GameRules.Range(seconds, 0, 1)) throw new ArgumentOutOfRangeException(nameof(seconds));
            State.time += seconds;
            AdvanceGrowth(seconds, false);
            State.player.attackCooldown = Math.Max(0, State.player.attackCooldown - seconds);
            State.shieldRemaining = Math.Max(0, State.shieldRemaining - seconds);
            foreach (var b in State.beasts) { b.stats.attackCooldown = Math.Max(0, b.stats.attackCooldown - seconds); b.skillCooldown = Math.Max(0, b.skillCooldown - seconds); }
            foreach (var node in State.nodes) node.remaining = Math.Max(0, node.remaining - seconds);
            UpdateDrops(seconds);
            UpdateWild(seconds);
            UpdateFollowers(seconds);
        }
        public void ApplyOffline(double nowUtc)
        {
            if (double.IsNaN(nowUtc) || double.IsInfinity(nowUtc) || nowUtc < 0) throw new ArgumentOutOfRangeException(nameof(nowUtc));
            if (State.savedUtc > 0 && nowUtc > State.savedUtc)
            {
                float elapsed = (float)Math.Min(nowUtc - State.savedUtc, Rules.offlineHours * 3600);
                AdvanceGrowth(elapsed, true);
                if (elapsed >= 60) Note("Trở lại: cây và thú nghỉ ngơi đã tiến triển " + Math.Round(elapsed / 60) + " phút (tối đa 12 giờ).");
            }
            State.savedUtc = Math.Max(State.savedUtc, nowUtc);
        }
        public ActionResult AdminAdvance(float seconds)
        {
            if (!GameRules.Range(seconds, 1, 43200)) return Result(false, "Chỉ tua 1 giây đến 12 giờ.");
            AdvanceGrowth(seconds, false);
            return Result(true, "[Chủ dự án] Tua " + Math.Round(seconds / 60, 1) + " phút bồi dưỡng/trồng trọt; không mô phỏng chiến đấu.");
        }
        public ActionResult AdminGrant(string item, int amount)
        {
            if (amount < 1 || amount > 99 || Content.Item(item) == null) return Result(false, "Số lượng phải từ 1 đến 99.");
            if (!AddItems(item, amount, false)) return Result(false, "Không đủ ô chứa vật phẩm.");
            return Result(true, "[Chủ dự án] Thêm " + amount + " " + Content.Item(item).name + ".");
        }
        public ActionResult AdminRealm(int realm)
        {
            if (realm < 0 || realm > 2 || realm + 1 < State.beasts.Count + (State.eggRemaining > 0 ? 1 : 0)) return Result(false, "Cảnh giới không đủ số khế ước đang có hoặc đang ấp.");
            State.player.realm = realm;
            return Result(true, "[Chủ dự án] Cảnh giới " + (realm + 1) + "; giới hạn " + ContractLimit + " khế ước.");
        }
        public string ValidateState()
        {
            if (State.version != 2 || State.cells == null || State.cells.Length != 100 || State.player == null || State.beasts == null || State.nodes == null || State.wild == null || State.journal == null || State.drops == null) return "Bản lưu sai phiên bản/cấu trúc.";
            if (State.room < 0 || State.room > 1 || !StatsValid(State.player) || !GameRules.Range(State.time, 0, 1e9f) || double.IsNaN(State.savedUtc) || double.IsInfinity(State.savedUtc) || State.savedUtc < 0) return "Trạng thái người chơi/thời gian không hợp lệ.";
            if (State.beasts.Count + (State.eggRemaining > 0 ? 1 : 0) > ContractLimit || State.journal.Count > 60 || !GameRules.Range(State.eggRemaining, 0, 86400)) return "Trạng thái khế ước/nhật ký không hợp lệ.";
            var ids = new HashSet<int>();
            foreach (var b in State.beasts)
            {
                if (b == null || b.id < 1 || !ids.Add(b.id) || !StatsValid(b.stats) || b.recovering && b.summoned) return "Bản lưu linh thú không hợp lệ.";
                if (b.digestion != null && (Content.Item(b.digestion.itemId) == null || !GameRules.Range(b.digestion.duration, 0.01f, 3600) || !GameRules.Range(b.digestion.remaining, 0, b.digestion.duration) || !GameRules.Range(b.digestion.qi, 0, 1000) || !GameRules.Range(b.digestion.attack, 0, 100) || !GameRules.Range(b.digestion.defense, 0, 100) || !GameRules.Range(b.digestion.root, 0, 100))) return "Bữa ăn trong bản lưu không hợp lệ.";
            }
            if (State.controlledBeast != 0 && (Beast(State.controlledBeast) == null || !Beast(State.controlledBeast).summoned || Beast(State.controlledBeast).recovering)) return "Đối tượng điều khiển không hợp lệ.";
            if (State.nextBeastId < 1 || State.beasts.Exists(b => b.id >= State.nextBeastId)) return "ID linh thú bị trùng.";
            foreach (var c in State.cells)
            {
                if (c == null || c.kind < CellKind.Empty || c.kind > CellKind.Soil) return "Ô Thần Đình không hợp lệ.";
                if (c.kind == CellKind.Storage && (Content.Item(c.itemId) == null || c.amount < 1 || c.amount > Content.Item(c.itemId).stackLimit || c.tripAmount < 0 || c.tripAmount > c.amount)) return "Số lượng vật phẩm không hợp lệ.";
                if (c.kind != CellKind.Storage && (c.amount != 0 || c.tripAmount != 0 || !string.IsNullOrEmpty(c.itemId))) return "Ô đất/trống không được chứa stack.";
                if (c.kind != CellKind.Soil && (!string.IsNullOrEmpty(c.cropId) || !string.IsNullOrEmpty(c.decorationId))) return "Cây/trang trí cần có đất.";
                if (!string.IsNullOrEmpty(c.cropId) && (c.cropId != "jade-herb" || !GameRules.Range(c.cropDuration, 10, 86400) || !GameRules.Range(c.cropProgress, 0, c.cropDuration))) return "Cây trồng trong bản lưu không hợp lệ.";
                if (!string.IsNullOrEmpty(c.decorationId) && c.decorationId != "tree" && c.decorationId != "pond") return "Trang trí không hợp lệ.";
                if (!GameRules.Range(c.waterRemaining, 0, 86400)) return "Lượng nước không hợp lệ.";
            }
            var wildIds = new HashSet<int>();
            foreach (var w in State.wild) if (w == null || !wildIds.Add(w.id) || w.room < 0 || w.room > 1 || !GameRules.Range(w.hp, 0, 10000) || !GameRules.Range(w.x, 0, 24) || !GameRules.Range(w.y, 0, 14) || !GameRules.Range(w.spawnX, .5f, 23.5f) || !GameRules.Range(w.spawnY, .5f, 13.5f) || !GameRules.Range(w.timer, -1, 3600) || !GameRules.Range(w.affinity, 0, 100) || !GameRules.Range(w.affinityRemaining, 0, 3600) || w.pendingLoot < 0 || w.pendingLoot > 1 || w.pendingLoot > 0 && !w.removed) return "Linh thú hoang không hợp lệ.";
            var nodeIds = new HashSet<int>();
            foreach (var n in State.nodes) if (n == null || !nodeIds.Add(n.id) || Content.Item(n.itemId) == null || n.room < 0 || n.room > 1 || !GameRules.Range(n.remaining, 0, 3600)) return "Điểm tài nguyên không hợp lệ.";
            if (!GameRules.Range(State.soulX, 0, 10) || !GameRules.Range(State.soulY, 0, 10) || State.nextDropId < 1 || State.drops.Count > MaxGroundDrops) return "Thần hồn/đồ trên đất không hợp lệ.";
            var dropIds = new HashSet<int>();
            foreach (var drop in State.drops)
                if (drop == null || drop.id < 1 || drop.id >= State.nextDropId || !dropIds.Add(drop.id) || drop.room < 0 || drop.room > 1 || Content.Item(drop.itemId) == null || drop.amount < 1 || drop.amount > Content.Item(drop.itemId).stackLimit || drop.tripAmount < 0 || drop.tripAmount > drop.amount || !GameRules.Range(drop.x, .5f, 23.5f) || !GameRules.Range(drop.y, .5f, 13.5f) || (!GameRules.Range(drop.remaining, 0, 1800) || drop.remaining <= 0)) return "Vật phẩm trên đất không hợp lệ.";
            return null;
        }
        private static bool StatsValid(Cultivator c) => c != null && c.realm >= 0 && c.realm <= 2 && GameRules.Range(c.root, 0, 100) && GameRules.Range(c.qi, 0, 1e7f) && GameRules.Range(c.spentQi, 0, c.qi) && GameRules.Range(c.hp, 0, 1e7f) && GameRules.Range(c.x, 0, 24) && GameRules.Range(c.y, 0, 14) && GameRules.Range(c.attackTraining, 0, 10000) && GameRules.Range(c.defenseTraining, 0, 10000);
        private static float Distance(float ax, float ay, float bx, float by) => (float)Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
        private static float Clamp(float v, float min, float max) => Math.Max(min, Math.Min(max, v));
    }
}
