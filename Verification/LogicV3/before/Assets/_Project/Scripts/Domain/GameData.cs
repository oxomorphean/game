using System;
using System.Collections.Generic;

namespace LinhThu.Domain
{
    // Serializable DTOs contain no Unity types. Only GameSession mutates live gameplay state.
    [Serializable] public sealed class GameRules
    {
        public float captureChance = 85;
        public float digestionSeconds = 30;
        public float recoverySeconds = 600;
        public float cropSeconds = 180;
        public float wateringSeconds = 43200;
        public float offlineHours = 12;
        public float tripLossPercent = 50;
        public float cultivationPerSecond = 0.6f;
        public float breakthroughBase = 100;
        public float rootCapacityPercent = 100;
        public float resourceRespawnSeconds = 45;
        public float affinitySeconds = 180;
        public float affinityChance = 20;
        public float followerDelaySeconds = .45f, followerNearRadius = 1.4f, followerCatchupDistance = 3.5f;
        public float wildRoamRadius = 3, wildLeashRadius = 6, dropLifetimeSeconds = 300;

        public string Validate()
        {
            if (!Range(captureChance, 1, 100) || !Range(affinityChance, 1, 100)) return "Tỷ lệ phải trong khoảng 1–100%.";
            if (!Range(digestionSeconds, 1, 3600) || !Range(recoverySeconds, 10, 86400)) return "Thời gian ăn/nghỉ không hợp lệ.";
            if (!Range(cropSeconds, 10, 86400) || !Range(wateringSeconds, 10, 86400)) return "Thời gian cây/nước không hợp lệ.";
            if (!Range(offlineHours, 0, 12) || !Range(tripLossPercent, 0, 100)) return "Nghỉ tối đa 12 giờ; mất đồ từ 0–100%.";
            if (!Range(cultivationPerSecond, 0.01f, 10) || !Range(breakthroughBase, 10, 10000)) return "Tốc độ/giới hạn tu luyện không hợp lệ.";
            if (!Range(rootCapacityPercent, 0, 300) || !Range(resourceRespawnSeconds, 5, 3600)) return "Căn cốt/chu kỳ tài nguyên không hợp lệ.";
            if (!Range(followerDelaySeconds, .2f, 1.2f) || !Range(followerNearRadius, .8f, 2.5f) || !Range(followerCatchupDistance, 3, 6) || followerCatchupDistance <= followerNearRadius) return "Nhịp đi theo không hợp lệ.";
            if (!Range(wildRoamRadius, 1, 4) || !Range(wildLeashRadius, 4, 8) || wildLeashRadius <= wildRoamRadius || !Range(dropLifetimeSeconds, 30, 1800)) return "Vùng đi lại hoặc thời gian đồ rơi không hợp lệ.";
            if (!Range(affinitySeconds, 10, 3600)) return "Hảo cảm phải kéo dài 10–3600 giây.";
            return null;
        }
        public static bool Range(float n, float min, float max) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= min && n <= max;
    }

    [Serializable] public sealed class ItemDefinition
    {
        public string id, name, category;
        public float qi, attack, defense, root;
        public int stackLimit = 99;
        public ItemDefinition(string id, string name, string category, float qi = 0, float attack = 0, float defense = 0, float root = 0)
        { this.id = id; this.name = name; this.category = category; this.qi = qi; this.attack = attack; this.defense = defense; this.root = root; }
    }

    [Serializable] public sealed class SpeciesDefinition
    {
        public string id = "huyen-quy", name = "Huyền Giáp Quy";
        public float maxHp = 90, attack = 12, defense = 5, speed = 3.7f;
    }

    [Serializable] public sealed class GameContent
    {
        public List<ItemDefinition> items = new List<ItemDefinition>();
        public SpeciesDefinition turtle = new SpeciesDefinition();
        public static GameContent Defaults()
        {
            var c = new GameContent();
            c.items.Add(new ItemDefinition("herb", "Linh thảo thường", "Thức ăn", 8, 0.5f, 0.4f, 0.2f));
            c.items.Add(new ItemDefinition("jade-herb", "Ngọc linh thảo", "Thức ăn", 18, 2, 2.5f, 2));
            c.items.Add(new ItemDefinition("blood", "Huyết Khế", "Thu phục"));
            c.items.Add(new ItemDefinition("seed", "Hạt linh thảo", "Trồng trọt"));
            c.items.Add(new ItemDefinition("soil", "Mảnh đất linh", "Thần Đình"));
            c.items.Add(new ItemDefinition("stone", "Linh khoáng", "Nguyên liệu"));
            c.items.Add(new ItemDefinition("wood", "Linh mộc", "Nguyên liệu"));
            c.items.Add(new ItemDefinition("pill", "Tụ khí đan", "Đan dược"));
            c.items.Add(new ItemDefinition("charm", "Hộ thân phù", "Luyện phù"));
            c.items.Add(new ItemDefinition("weapon", "Linh kiếm thô", "Binh khí"));
            c.items.Add(new ItemDefinition("tree", "Cây cảnh", "Trang trí"));
            c.items.Add(new ItemDefinition("pond", "Ao cảnh", "Trang trí"));
            c.items.Add(new ItemDefinition("egg", "Trứng linh quy", "Thu phục"));
            return c;
        }
        public ItemDefinition Item(string id) => items.Find(i => i.id == id);
        public string Validate()
        {
            var expected = Defaults().items;
            if (items == null || items.Count != expected.Count) return "Danh mục vật phẩm thiếu hoặc thừa ID.";
            var seen = new HashSet<string>();
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.name) || item.name.Length > 50 || !seen.Add(item.id) || expected.Find(i => i.id == item.id) == null) return "Vật phẩm không hợp lệ/trùng ID.";
                if (item.stackLimit < 1 || item.stackLimit > 999 || !GameRules.Range(item.qi, 0, 1000) || !GameRules.Range(item.attack, 0, 100) || !GameRules.Range(item.defense, 0, 100) || !GameRules.Range(item.root, 0, 100)) return "Chỉ số vật phẩm ngoài giới hạn.";
            }
            if (turtle == null || turtle.id != "huyen-quy" || string.IsNullOrWhiteSpace(turtle.name) || turtle.name.Length > 50 || !GameRules.Range(turtle.maxHp, 10, 10000) || !GameRules.Range(turtle.attack, 1, 1000) || !GameRules.Range(turtle.defense, 0, 1000) || !GameRules.Range(turtle.speed, 1, 10)) return "Chỉ số loài không hợp lệ.";
            return null;
        }
    }

    public enum CellKind { Empty, Storage, Soil }
    [Serializable] public sealed class RealmCell
    {
        public CellKind kind;
        public string itemId = "", cropId = "", decorationId = "";
        public int amount, tripAmount;
        public float cropProgress, cropDuration, waterRemaining;
    }
    [Serializable] public sealed class Cultivator
    {
        public float x = 3, y = 3, hp = 100, qi, spentQi, root = 20, attackTraining, defenseTraining;
        public int realm;
        public float attackCooldown;
    }
    [Serializable] public sealed class Digestion
    {
        public string itemId;
        public float remaining, duration, qi, attack, defense, root;
    }
    [Serializable] public sealed class Beast
    {
        public int id;
        public string name = "Huyền Giáp Quy";
        public Cultivator stats = new Cultivator();
        public bool summoned = true, recovering;
        public Digestion digestion;
        public float skillCooldown;
    }
    [Serializable] public sealed class WildBeast
    {
        public int id, room, pendingLoot;
        public float x, y, spawnX, spawnY, hp = 45, timer, cooldown, affinity, affinityRemaining;
        public bool injured, removed;
    }
    [Serializable] public sealed class ResourceNode
    {
        public int id, room;
        public string itemId;
        public float x, y, remaining;
    }
    [Serializable] public sealed class GroundDrop
    {
        public int id, room, amount, tripAmount;
        public string itemId;
        public float x, y, remaining;
    }
    [Serializable] public sealed class GameState
    {
        public int version = 2, room, controlledBeast, nextBeastId = 1, nextDropId = 1;
        public double savedUtc;
        public float time, lastCombat = -100, soulX = 4, soulY = 2;
        public bool inRealm, cultivating;
        public int medicineXp, smithXp, charmXp, farmingXp, captures, harvests;
        public float eggRemaining, weaponBonus, shieldRemaining;
        public Cultivator player = new Cultivator();
        public List<Beast> beasts = new List<Beast>();
        public RealmCell[] cells = new RealmCell[100];
        public List<WildBeast> wild = new List<WildBeast>();
        public List<ResourceNode> nodes = new List<ResourceNode>();
        public List<GroundDrop> drops = new List<GroundDrop>();
        public List<string> journal = new List<string>();
    }
    [Serializable] public sealed class SaveEnvelope
    {
        public int schema = 2;
        public GameRules rules = new GameRules();
        public GameContent content = GameContent.Defaults();
        public GameState state;
    }
    public readonly struct ActionResult
    {
        public readonly bool ok;
        public readonly string message;
        public ActionResult(bool ok, string message) { this.ok = ok; this.message = message; }
    }
    public interface IChanceSource { float NextPercent(); }
    public sealed class RandomChance : IChanceSource
    {
        private readonly Random random;
        public RandomChance(int seed) { random = new Random(seed); }
        public float NextPercent() => (float)(random.NextDouble() * 100);
    }
}
