using System;

namespace LinhThu.Domain
{
    public sealed partial class GameSession
    {
        public void Move(float dx, float dy, float seconds)
        {
            if (!GameRules.Range(dx, -1, 1) || !GameRules.Range(dy, -1, 1) || !GameRules.Range(seconds, 0, 1)) throw new ArgumentOutOfRangeException(nameof(dx));
            float length = (float)Math.Sqrt(dx * dx + dy * dy); if (length < 0.01f) return;
            if (length > 1) { dx /= length; dy /= length; }
            State.cultivating = false;
            float speed = MoveSpeed(State.controlledBeast != 0);
            if (State.inRealm) { State.soulX = Clamp(State.soulX + dx * speed * seconds, 0.25f, 9.75f); State.soulY = Clamp(State.soulY + dy * speed * seconds, 0.25f, 9.75f); }
            else { Controlled.x = Clamp(Controlled.x + dx * speed * seconds, 0.5f, 23.5f); Controlled.y = Clamp(Controlled.y + dy * speed * seconds, 0.5f, 13.5f); }
        }
        public ActionResult SwitchControl(int id = -1)
        {
            if (State.inRealm) return Result(false, "Rời Thần Đình trước khi đổi người hoặc linh thú điều khiển.");
            if (id == -1)
            {
                int old = State.controlledBeast; id = 0;
                foreach (var b in State.beasts) if (b.id > old && b.summoned && !b.recovering) { id = b.id; break; }
            }
            if (id != 0 && (Beast(id) == null || !Beast(id).summoned || Beast(id).recovering)) return Result(false, "Linh thú phải đang đồng hành và khỏe mạnh.");
            State.controlledBeast = id; State.cultivating = false; ResetFollowTrail();
            return Result(true, id == 0 ? "Đang điều khiển người chơi; linh thú đi theo." : "Đang điều khiển " + Beast(id).name + "; người chơi đi theo.");
        }
        public ResourceNode NearbyNode()
        {
            if (State.inRealm) return null;
            ResourceNode result = null; float best = 2;
            foreach (var n in State.nodes) { float d = Distance(Controlled.x, Controlled.y, n.x, n.y); if (n.room == State.room && n.remaining <= 0 && d < best) { best = d; result = n; } }
            return result;
        }
        public WildBeast NearbyWild()
        {
            if (State.inRealm) return null;
            WildBeast result = null; float best = 2.5f;
            foreach (var w in State.wild) { float d = Distance(Controlled.x, Controlled.y, w.x, w.y); if (w.room == State.room && !w.removed && d < best) { best = d; result = w; } }
            return result;
        }
        public ActionResult Gather(int id)
        {
            var node = State.nodes.Find(n => n.id == id);
            if (State.inRealm || node == null || node.room != State.room || node.remaining > 0 || Distance(Controlled.x, Controlled.y, node.x, node.y) > 2) return Result(false, "Đến gần tài nguyên còn sẵn để thu thập.");
            if (!AddItems(node.itemId, 3, true)) return Result(false, "Thần Đình đã đầy; điểm tài nguyên không bị mất.");
            node.remaining = Rules.resourceRespawnSeconds;
            return Result(true, "Nhặt 3 " + Content.Item(node.itemId).name + ". Về trại để bảo toàn chiến lợi phẩm.");
        }
        public ActionResult Capture(int id)
        {
            var wild = State.wild.Find(w => w.id == id);
            if (State.inRealm || wild == null || wild.removed || wild.room != State.room || Distance(Controlled.x, Controlled.y, wild.x, wild.y) > 2.5f) return Result(false, "Đến gần linh thú để thu phục.");
            if (!wild.injured && wild.affinity <= 0) return Result(false, "Linh thú cần bị thương hoặc có hảo cảm trước.");
            if (State.beasts.Count + (State.eggRemaining > 0 ? 1 : 0) >= ContractLimit) return Result(false, "Đã đủ khế ước của cảnh giới hiện tại.");
            if (!Spend("blood", 1)) return Result(false, "Cần 1 Huyết Khế.");
            float percent = Math.Min(100, Rules.captureChance + wild.affinity * 0.2f);
            if (chance.NextPercent() >= percent) return Result(false, "Huyết Khế chưa thành công. Đã tiêu hao một khế; có thể thử lại.");
            wild.removed = true; wild.timer = 180;
            CreateBeast(Content.turtle.name + (State.beasts.Count > 0 ? " " + State.nextBeastId : ""));
            return Result(true, "Khế ước thành công! Linh thú sẽ gắn bó lâu dài với bạn. Tab để đổi điều khiển.");
        }
        public ActionResult Strike(bool skill)
        {
            if (State.inRealm) return Result(false, "Thần Đình là nơi nghỉ ngơi.");
            var c = Controlled; var pet = Beast(State.controlledBeast);
            if (c.attackCooldown > 0 || skill && pet != null && pet.skillCooldown > 0) return new ActionResult(false, "Kỹ năng đang hồi.");
            WildBeast target = null; float best = skill ? 4 : 2.2f;
            foreach (var w in State.wild) { float d = Distance(c.x, c.y, w.x, w.y); if (w.room == State.room && !w.removed && !w.injured && d < best) { target = w; best = d; } }
            if (target == null) return Result(false, "Không có mục tiêu trong tầm đánh.");
            c.attackCooldown = skill ? 1.5f : 0.55f;
            if (skill && pet != null) pet.skillCooldown = 5;
            State.lastCombat = State.time; State.cultivating = false;
            float damage = Attack(c, pet != null) * (skill ? 1.8f : 1);
            HitWild(target, damage, State.controlledBeast);
            if (target.hp == 0) return Result(true, target.injured ? "Linh thú bị thương. Đến gần để thử thu phục." : "Đã hạ mục tiêu. Linh khoáng nằm trên đất để nhặt.");
            return Result(true, (skill ? "Thi triển kỹ năng: " : "Đánh thường: ") + Math.Round(Math.Max(1, damage - WildDefense)) + " sát thương.");
        }
        public ActionResult Travel()
        {
            if (State.inRealm || Distance(Controlled.x, Controlled.y, 22, 7) > 2) return Result(false, "Đến cổng phía đông để chuyển vùng.");
            if (State.time - State.lastCombat < 3) return Result(false, "Đang giao tranh, chưa thể qua cổng.");
            State.room = 1 - State.room; State.player.x = 3; State.player.y = 3;
            foreach (var b in State.beasts) { b.stats.x = 3.6f + (b.id % 3) * .7f; b.stats.y = 3.7f; }
            ResetFollowTrail(); defenseRemaining = 0;
            return Result(true, State.room == 0 ? "Đã đến Trúc Lâm." : "Đã đến Bờ Linh Hồ.");
        }
    }
}
