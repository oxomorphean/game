using System;
using System.Collections.Generic;

namespace LinhThu.Domain
{
    public sealed partial class GameSession
    {
        private sealed class MotionMind
        {
            public float targetX, targetY, wait, walkRemaining;
            public uint generation;
            public bool walking;
            public string mode = "Nghỉ";
        }
        private readonly struct TrailPoint
        {
            public readonly float time, x, y;
            public TrailPoint(float time, float x, float y) { this.time = time; this.x = x; this.y = y; }
        }
        private readonly Dictionary<int, MotionMind> followers = new Dictionary<int, MotionMind>();
        private readonly List<TrailPoint> trail = new List<TrailPoint>();
        private int trailLeader = -1, trailRoom = -1, defenseTarget;
        private float lastLeaderX, lastLeaderY, lastLeaderMove = -100, movementStarted, defenseRemaining;
        public float MoveSpeed(bool pet) => pet ? Content.turtle.speed : 4.5f;
        public string CompanionActivity(int id) => followers.TryGetValue(id, out var mind) ? mind.mode : "Nghỉ";

        private void ResetFollowTrail()
        {
            trailLeader = State.controlledBeast; trailRoom = State.room;
            var lead = Controlled; lastLeaderX = lead.x; lastLeaderY = lead.y;
            lastLeaderMove = State.time - 10; movementStarted = State.time;
            trail.Clear(); trail.Add(new TrailPoint(State.time - 4, lead.x, lead.y));
            trail.Add(new TrailPoint(State.time, lead.x, lead.y)); followers.Clear();
        }
        private void UpdateFollowers(float dt)
        {
            defenseRemaining = Math.Max(0, defenseRemaining - dt);
            if (State.inRealm || dt <= 0) return;
            if (trailLeader != State.controlledBeast || trailRoom != State.room) ResetFollowTrail();
            var lead = Controlled;
            if (Distance(lastLeaderX, lastLeaderY, lead.x, lead.y) > .001f)
            {
                if (State.time - lastLeaderMove > .3f) movementStarted = State.time - dt;
                lastLeaderMove = State.time;
            }
            lastLeaderX = lead.x; lastLeaderY = lead.y;
            trail.Add(new TrailPoint(State.time, lead.x, lead.y));
            while (trail.Count > 2 && trail[1].time < State.time - 4) trail.RemoveAt(0);
            if (State.controlledBeast != 0) FollowCompanion(0, State.player, false, dt);
            foreach (var beast in State.beasts)
            {
                if (!beast.summoned || beast.recovering || beast.id == State.controlledBeast) continue;
                if (!DefendLeader(beast, dt)) FollowCompanion(beast.id, beast.stats, true, dt);
            }
        }
        private MotionMind FollowerMind(int id)
        {
            if (!followers.TryGetValue(id, out var mind))
            { mind = new MotionMind { wait = 2 + MotionSample(id, 0) * 2 }; followers.Add(id, mind); }
            return mind;
        }
        private TrailPoint DelayedLeader(float delay)
        {
            float time = State.time - delay;
            for (int i = 1; i < trail.Count; i++)
            {
                var a = trail[i - 1]; var b = trail[i];
                if (b.time < time) continue;
                float t = Clamp((time - a.time) / Math.Max(.001f, b.time - a.time), 0, 1);
                return new TrailPoint(time, a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
            }
            return trail[trail.Count - 1];
        }
        private void FollowCompanion(int id, Cultivator actor, bool pet, float dt)
        {
            var mind = FollowerMind(id); var lead = Controlled;
            float distance = Distance(actor.x, actor.y, lead.x, lead.y);
            float delay = Rules.followerDelaySeconds + (id % 3) * .10f;
            bool leadMoving = State.time - lastLeaderMove < .6f;
            if (leadMoving)
            {
                mind.walking = false;
                if (State.time - movementStarted < delay && distance < Rules.followerCatchupDistance)
                { mind.mode = "Quan sát"; return; }
                var target = DelayedLeader(delay); var prior = DelayedLeader(delay + .2f);
                float dx = target.x - prior.x, dy = target.y - prior.y;
                float length = Math.Max(.01f, Distance(0, 0, dx, dy));
                float side = ((id % 3) - 1) * .4f;
                float x = target.x - dy / length * side, y = target.y + dx / length * side;
                float speed = distance > Rules.followerCatchupDistance ? Math.Max(MoveSpeed(pet), MoveSpeed(State.controlledBeast != 0)) * 1.5f : MoveSpeed(pet);
                mind.mode = distance > Rules.followerCatchupDistance ? "Đuổi kịp" : "Theo sau";
                StepCompanion(actor, x, y, speed, dt, .35f); mind.wait = 1 + MotionSample(id, mind.generation) * 2;
                return;
            }
            if (distance > Rules.followerNearRadius + 1)
            {
                mind.mode = "Về gần bạn"; mind.walking = false;
                StepCompanion(actor, lead.x, lead.y, MoveSpeed(pet), dt, Rules.followerNearRadius); return;
            }
            if (mind.walking)
            {
                mind.mode = "Đi quanh"; mind.walkRemaining -= dt;
                StepCompanion(actor, mind.targetX, mind.targetY, MoveSpeed(pet) * .4f, dt, .12f);
                if (Distance(actor.x, actor.y, mind.targetX, mind.targetY) < .2f || mind.walkRemaining <= 0)
                { mind.walking = false; mind.wait = 2 + MotionSample(id, mind.generation++) * 3.5f; }
            }
            else
            {
                mind.mode = "Nghỉ"; mind.wait -= dt;
                if (mind.wait > 0) return;
                float angle = MotionSample(id, ++mind.generation) * (float)Math.PI * 2;
                float radius = .8f + MotionSample(id, ++mind.generation) * Rules.followerNearRadius;
                mind.targetX = Clamp(lead.x + (float)Math.Cos(angle) * radius, .5f, 23.5f);
                mind.targetY = Clamp(lead.y + (float)Math.Sin(angle) * radius, .5f, 13.5f); mind.walking = true; mind.walkRemaining = 3.5f;
            }
        }
        private void StepCompanion(Cultivator actor, float x, float y, float speed, float dt, float stop)
        {
            float repelX = 0, repelY = 0;
            Separate(actor, State.player, ref repelX, ref repelY);
            foreach (var beast in State.beasts) if (beast.summoned && !beast.recovering) Separate(actor, beast.stats, ref repelX, ref repelY);
            StepToward(ref actor.x, ref actor.y, x + repelX, y + repelY, speed, dt, stop);
        }
        private static void Separate(Cultivator actor, Cultivator other, ref float x, ref float y)
        {
            if (ReferenceEquals(actor, other)) return;
            float distance = Distance(actor.x, actor.y, other.x, other.y);
            if (distance < .01f || distance >= .65f) return;
            x += (actor.x - other.x) / distance * (.65f - distance);
            y += (actor.y - other.y) / distance * (.65f - distance);
        }
        private static void StepToward(ref float x, ref float y, float targetX, float targetY, float speed, float dt, float stop = .1f)
        {
            float distance = Distance(x, y, targetX, targetY);
            if (distance <= stop) return;
            float step = Math.Min(speed * dt, distance - stop);
            x = Clamp(x + (targetX - x) / distance * step, .5f, 23.5f);
            y = Clamp(y + (targetY - y) / distance * step, .5f, 13.5f);
        }
        private static float MotionSample(int id, uint generation)
        {
            uint value = unchecked((uint)(id + 29) * 0x9E3779B9u + generation * 0x85EBCA6Bu);
            value ^= value >> 16; value = unchecked(value * 0x7FEB352Du); value ^= value >> 15;
            return (value & 0x00FFFFFFu) / 16777216f;
        }
        private void RequestDefense(WildBeast target) { defenseTarget = target.id; defenseRemaining = 6; }
        private bool DefendLeader(Beast beast, float dt)
        {
            if (defenseRemaining <= 0) return false;
            var target = State.wild.Find(w => w.id == defenseTarget);
            var lead = Controlled;
            if (target == null || target.removed || target.injured || target.affinity > 0 || target.room != State.room || Distance(target.x, target.y, lead.x, lead.y) > Rules.followerCatchupDistance + 2) return false;
            var mind = FollowerMind(beast.id); mind.mode = "Bảo vệ"; mind.walking = false;
            float distance = Distance(beast.stats.x, beast.stats.y, target.x, target.y);
            if (distance > 1.4f) StepCompanion(beast.stats, target.x, target.y, MoveSpeed(true), dt, 1.1f);
            else if (beast.stats.attackCooldown <= 0)
            {
                bool skill = beast.skillCooldown <= 0;
                beast.stats.attackCooldown = skill ? 1.5f : .7f;
                if (skill) beast.skillCooldown = 5;
                HitWild(target, Attack(beast.stats, true) * (skill ? 1.8f : 1), beast.id);
            }
            return true;
        }
    }
}
