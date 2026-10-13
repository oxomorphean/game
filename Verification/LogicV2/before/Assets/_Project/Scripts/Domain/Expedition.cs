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
            float speed = State.controlledBeast == 0 ? 4.5f : Content.turtle.speed;
            if (State.inRealm) { State.soulX = Clamp(State.soulX + dx * speed * seconds, 0.25f, 9.75f); State.soulY = Clamp(State.soulY + dy * speed * seconds, 0.25f, 9.75f); }
            else { Controlled.x = Clamp(Controlled.x + dx * speed * seconds, 0.5f, 23.5f); Controlled.y = Clamp(Controlled.y + dy * speed * seconds, 0.5f, 13.5f); }
        }
        public ActionResult SwitchControl(int id = -1)
        {
            if (id == -1)
            {
                int old = State.controlledBeast; id = 0;
                foreach (var b in State.beasts) if (b.id > old && b.summoned && !b.recovering) { id = b.id; break; }
            }
            if (id != 0 && (Beast(id) == null || !Beast(id).summoned || Beast(id).recovering)) return Result(false, "Linh thú phải đang đồng hành và khỏe mạnh.");
            State.controlledBeast = id; State.cultivating = false;
            return Result(true, id == 0 ? "Đang điều khiển người chơi; linh thú đi theo." : "Đang điều khiển " + Beast(id).name + "; người chơi đi theo.");
        }
        private void UpdateFollowers(float dt)
        {
            if (State.inRealm) return;
            var lead = Controlled;
            if (State.controlledBeast != 0) Follow(State.player, lead.x - 1.1f, lead.y, 4.5f, dt);
            int i = 0;
            foreach (var b in State.beasts)
            {
                if (!b.summoned || b.recovering || b.id == State.controlledBeast) continue;
                Follow(b.stats, lead.x - 0.7f + i * 0.8f, lead.y - 1.2f - (i % 2) * 0.4f, Content.turtle.speed + 1, dt); i++;
            }
        }
        private static void Follow(Cultivator c, float x, float y, float speed, float dt)
        {
            float dist = Distance(c.x, c.y, x, y); if (dist < 0.3f) return;
            float move = Math.Min(speed * dt, dist);
            c.x = Clamp(c.x + (x - c.x) / dist * move, 0.5f, 23.5f); c.y = Clamp(c.y + (y - c.y) / dist * move, 0.5f, 13.5f);
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
        public ActionResult Lure(int id)
        {
            var w = State.wild.Find(b => b.id == id);
            if (State.inRealm || w == null || w.removed || w.injured || w.room != State.room || Distance(Controlled.x, Controlled.y, w.x, w.y) > 2.5f) return Result(false, "Đến gần linh thú hoang còn khỏe để thả thức ăn.");
            if (!Spend("herb", 1)) return Result(false, "Cần một linh thảo thường.");
            if (w.affinity > 0 || chance.NextPercent() < Rules.affinityChance) { w.affinity = Math.Min(100, w.affinity + 20); w.affinityRemaining = Rules.affinitySeconds; return Result(true, "Linh thú ăn và tăng hảo cảm. Tiếp tục cho ăn hoặc dùng Huyết Khế."); }
            return Result(true, "Linh thú đã ăn, chưa xuất hiện hảo cảm.");
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
            target.hp = Math.Max(0, target.hp - damage);
            if (target.hp == 0)
            {
                target.injured = chance.NextPercent() < 65; target.removed = !target.injured; target.timer = target.injured ? 300 : 90;
                if (!target.injured) AddItems("stone", 1, true);
                return Result(true, target.injured ? "Linh thú bị thương. Đến gần để thử thu phục." : "Đã hạ mục tiêu.");
            }
            return Result(true, (skill ? "Thi triển kỹ năng: " : "Đánh thường: ") + Math.Round(damage) + " sát thương.");
        }
        public ActionResult Travel()
        {
            if (State.inRealm || Distance(Controlled.x, Controlled.y, 22, 7) > 2) return Result(false, "Đến cổng phía đông để chuyển vùng.");
            if (State.time - State.lastCombat < 3) return Result(false, "Đang giao tranh, chưa thể qua cổng.");
            State.room = 1 - State.room; State.player.x = 3; State.player.y = 3;
            foreach (var b in State.beasts) { b.stats.x = 4; b.stats.y = 3; }
            return Result(true, State.room == 0 ? "Đã đến Trúc Lâm." : "Đã đến Bờ Linh Hồ.");
        }
        private void UpdateWild(float dt)
        {
            foreach (var w in State.wild)
            {
                w.cooldown = Math.Max(0, w.cooldown - dt);
                w.affinityRemaining = Math.Max(0, w.affinityRemaining - dt); if (w.affinityRemaining == 0) w.affinity = 0;
                if (w.removed || w.injured)
                {
                    w.timer -= dt;
                    if (w.timer <= 0) { w.removed = false; w.injured = false; w.hp = 45; w.x = w.spawnX; w.y = w.spawnY; }
                    continue;
                }
                if (State.inRealm || w.room != State.room || w.affinity > 0) continue;
                var target = Controlled; float dist = Distance(target.x, target.y, w.x, w.y);
                // Quiet exploration: beasts only engage very close, and leash to their habitat.
                if (dist > 2.4f || Distance(w.x, w.y, w.spawnX, w.spawnY) > 6) continue;
                if (dist > 1) { w.x += (target.x - w.x) / dist * dt * 1.6f; w.y += (target.y - w.y) / dist * dt * 1.6f; }
                if (dist <= 1.2f && w.cooldown == 0)
                {
                    w.cooldown = 1.5f; State.lastCombat = State.time;
                    float damage = Math.Max(1, 12 - Defense(target, State.controlledBeast != 0));
                    if (State.shieldRemaining > 0) damage *= 0.5f;
                    if (State.controlledBeast == 0) DamagePlayer(damage); else DamageBeast(State.controlledBeast, damage);
                }
            }
        }
    }
}
