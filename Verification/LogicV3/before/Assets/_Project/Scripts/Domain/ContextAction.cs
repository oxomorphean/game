namespace LinhThu.Domain
{
    public enum PrimaryActionKind { Attack, Capture, Gather, Pickup, Travel, Bank, RealmCell }
    public readonly struct PrimaryActionTarget
    {
        public readonly PrimaryActionKind kind;
        public readonly int id;
        public readonly string label;
        public PrimaryActionTarget(PrimaryActionKind kind, int id, string label)
        { this.kind = kind; this.id = id; this.label = label; }
    }

    public sealed partial class GameSession
    {
        /// <summary>Resolves the main button without spending items. Capture opens a separate confirmation panel.</summary>
        public PrimaryActionTarget PrimaryTarget()
        {
            if (State.inRealm) return new PrimaryActionTarget(PrimaryActionKind.RealmCell, 0, "Chăm sóc ô");
            var wild = NearbyWild();
            if (wild != null && (wild.injured || wild.affinity > 0))
                return new PrimaryActionTarget(PrimaryActionKind.Capture, wild.id, "Thu phục");
            foreach (var enemy in State.wild)
                if (enemy.room == State.room && !enemy.removed && !enemy.injured && enemy.affinity <= 0 && Distance(Controlled.x, Controlled.y, enemy.x, enemy.y) < 2.2f)
                    return new PrimaryActionTarget(PrimaryActionKind.Attack, enemy.id, "Tấn công");
            var drop = NearbyDrop();
            if (drop != null) return new PrimaryActionTarget(PrimaryActionKind.Pickup, drop.id, "Nhặt " + Content.Item(drop.itemId).name + " ×" + drop.amount);
            var node = NearbyNode();
            if (node != null) return new PrimaryActionTarget(PrimaryActionKind.Gather, node.id, "Nhặt " + Content.Item(node.itemId).name);
            if (Distance(Controlled.x, Controlled.y, 22, 7) < 2)
                return new PrimaryActionTarget(PrimaryActionKind.Travel, 0, "Qua cổng");
            if (State.room == 0 && TripCount > 0 && Distance(State.player.x, State.player.y, 3, 3) < 2.5f)
                return new PrimaryActionTarget(PrimaryActionKind.Bank, 0, "Bảo toàn đồ");
            return new PrimaryActionTarget(PrimaryActionKind.Attack, 0, "Tấn công");
        }
    }
}
