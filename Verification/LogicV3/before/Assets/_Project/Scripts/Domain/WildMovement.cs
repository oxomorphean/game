using System;
using System.Collections.Generic;

namespace LinhThu.Domain
{
    public sealed partial class GameSession
    {
        private sealed class WildMind
        {
            public float targetX, targetY, wait, aggro, eating;
            public int targetBeast, food;
            public uint generation;
            public bool walking, returning;
            public string mode = "Nghỉ";
        }
        private readonly Dictionary<int, WildMind> wildMinds = new Dictionary<int, WildMind>();
        public float WildMaxHp => 45;
        public float WildAttack => 12;
        public float WildDefense => 2;
        public string WildActivity(int id) => wildMinds.TryGetValue(id, out var mind) ? mind.mode : "Nghỉ";
        private WildMind Mind(WildBeast wild)
        {
            if (!wildMinds.TryGetValue(wild.id, out var mind))
            { mind = new WildMind { wait = .5f + MotionSample(wild.id + 100, 0) * 2 }; wildMinds.Add(wild.id, mind); }
            return mind;
        }
        private void UpdateWild(float dt)
        {
            foreach (var wild in State.wild)
            {
                wild.cooldown = Math.Max(0, wild.cooldown - dt);
                wild.affinityRemaining = Math.Max(0, wild.affinityRemaining - dt);
                if (wild.affinityRemaining <= 0) wild.affinity = 0;
                if (wild.removed || wild.injured)
                {
                    if (wild.pendingLoot > 0)
                    {
                        if (PlaceDrop("stone", wild.pendingLoot, wild.pendingLoot, wild.room, wild.x, wild.y)) wild.pendingLoot = 0;
                        else continue;
                    }
                    wild.timer -= dt;
                    if (wild.timer <= 0)
                    {
                        wild.removed = wild.injured = false; wild.hp = WildMaxHp;
                        wild.x = wild.spawnX; wild.y = wild.spawnY; wild.affinity = wild.affinityRemaining = 0;
                        wildMinds.Remove(wild.id);
                    }
                    continue;
                }
                if (wild.room != State.room || dt <= 0) continue;
                var mind = Mind(wild); mind.aggro = Math.Max(0, mind.aggro - dt);
                float home = Distance(wild.x, wild.y, wild.spawnX, wild.spawnY);
                if (home >= Rules.wildLeashRadius) mind.returning = true;
                if (mind.returning)
                {
                    mind.mode = "Về nơi ở"; mind.aggro = 0; mind.food = 0; mind.walking = false;
                    StepToward(ref wild.x, ref wild.y, wild.spawnX, wild.spawnY, 1.8f, dt, .35f);
                    if (Distance(wild.x, wild.y, wild.spawnX, wild.spawnY) <= .5f)
                    { mind.returning = false; mind.wait = 2; wild.hp = WildMaxHp; }
                    continue;
                }
                if (mind.aggro <= 0)
                {
                    var food = NearbyFood(wild);
                    if (food != null)
                    {
                        mind.mode = "Tìm thức ăn"; mind.walking = false;
                        if (mind.food != food.id) { mind.food = food.id; mind.eating = 1.3f; }
                        if (Distance(wild.x, wild.y, food.x, food.y) > .65f)
                            StepToward(ref wild.x, ref wild.y, food.x, food.y, 1.35f, dt, .5f);
                        else
                        {
                            mind.mode = "Ăn"; mind.eating -= dt;
                            if (mind.eating <= 0) { EatGroundFood(wild, food); mind.eating = 2.2f; }
                        }
                        continue;
                    }
                    mind.food = 0;
                }
                if (!State.inRealm && wild.affinity <= 0)
                {
                    var beast = Beast(mind.targetBeast);
                    bool targetPet = mind.aggro > 0 && beast != null && beast.summoned && !beast.recovering;
                    var target = targetPet ? beast.stats : Controlled;
                    int targetId = targetPet ? beast.id : State.controlledBeast;
                    float distance = Distance(target.x, target.y, wild.x, wild.y);
                    if (distance < 2.4f || mind.aggro > 0)
                    {
                        if (distance > 5 || Distance(target.x, target.y, wild.spawnX, wild.spawnY) > Rules.wildLeashRadius + 1)
                        { mind.returning = true; continue; }
                        mind.mode = "Giao tranh"; mind.walking = false; mind.aggro = Math.Max(mind.aggro, 1);
                        if (distance > 1.05f) StepToward(ref wild.x, ref wild.y, target.x, target.y, 1.6f, dt, 1);
                        if (distance <= 1.2f && wild.cooldown <= 0)
                        {
                            wild.cooldown = 1.5f; State.lastCombat = State.time; State.cultivating = false;
                            float damage = Math.Max(1, WildAttack - Defense(target, targetId != 0));
                            if (State.shieldRemaining > 0) damage *= .5f;
                            RequestDefense(wild);
                            if (targetId == 0) DamagePlayer(damage); else DamageBeast(targetId, damage);
                        }
                        continue;
                    }
                }
                if (mind.walking)
                {
                    mind.mode = "Đi quanh";
                    StepToward(ref wild.x, ref wild.y, mind.targetX, mind.targetY, .9f, dt, .15f);
                    if (Distance(wild.x, wild.y, mind.targetX, mind.targetY) < .2f)
                    { mind.walking = false; mind.wait = 1.5f + MotionSample(wild.id + 100, mind.generation++) * 3; }
                }
                else
                {
                    mind.mode = "Nghỉ"; mind.wait -= dt;
                    if (mind.wait > 0) continue;
                    float angle = MotionSample(wild.id + 100, ++mind.generation) * (float)Math.PI * 2;
                    float radius = (.25f + .7f * MotionSample(wild.id + 100, ++mind.generation)) * Rules.wildRoamRadius;
                    mind.targetX = Clamp(wild.spawnX + (float)Math.Cos(angle) * radius, .5f, 23.5f);
                    mind.targetY = Clamp(wild.spawnY + (float)Math.Sin(angle) * radius, .5f, 13.5f); mind.walking = true;
                }
            }
        }
        private void HitWild(WildBeast wild, float rawDamage, int attacker)
        {
            wild.hp = Math.Max(0, wild.hp - Math.Max(1, rawDamage - WildDefense));
            State.lastCombat = State.time; State.cultivating = false;
            wild.affinity = wild.affinityRemaining = 0;
            var mind = Mind(wild); mind.aggro = 6; mind.targetBeast = attacker; mind.food = 0;
            RequestDefense(wild);
            if (wild.hp > 0) return;
            wild.injured = chance.NextPercent() < 65; wild.removed = !wild.injured;
            wild.timer = wild.injured ? 300 : 90;
            if (wild.removed && !PlaceDrop("stone", 1, 1, wild.room, wild.x, wild.y)) wild.pendingLoot = 1;
        }
    }
}
