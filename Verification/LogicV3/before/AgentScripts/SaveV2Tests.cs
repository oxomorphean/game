using System;
using System.Collections.Generic;
using System.IO;
using LinhThu.Domain;
using LinhThu.Prototype;
using UnityEngine;

/// <summary>Save codec and migration checks using in-memory fixtures. Only the verification report is written; real save storage is never accessed.</summary>
public static class SaveV2Tests
{
    private sealed class FixedChance : IChanceSource { public float NextPercent() => 0; }
    [Serializable] private sealed class Report { public int passed, failed; public List<string> cases = new List<string>(); }
    [Serializable] private sealed class LegacySave
    {
        public int schema = 1;
        public LegacyRules rules;
        public GameContent content;
        public LegacyState state;
    }
    [Serializable] private sealed class LegacyRules
    {
        public float captureChance, digestionSeconds, recoverySeconds, cropSeconds, wateringSeconds, offlineHours,
            tripLossPercent, cultivationPerSecond, breakthroughBase, rootCapacityPercent, resourceRespawnSeconds,
            affinitySeconds, affinityChance;
    }
    [Serializable] private sealed class LegacyState
    {
        public int version = 1, room, controlledBeast, nextBeastId;
        public double savedUtc;
        public float time, lastCombat, soulX, soulY;
        public bool inRealm, cultivating;
        public int medicineXp, smithXp, charmXp, farmingXp, captures, harvests;
        public float eggRemaining, weaponBonus, shieldRemaining;
        public Cultivator player;
        public List<Beast> beasts;
        public RealmCell[] cells;
        public List<LegacyWild> wild;
        public List<ResourceNode> nodes;
        public List<string> journal;
    }
    [Serializable] private sealed class LegacyWild
    {
        public int id, room;
        public float x, y, spawnX, spawnY, hp, timer, cooldown, affinity, affinityRemaining;
        public bool injured, removed;
    }
    private static void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    private static bool Close(float a, float b) => Math.Abs(a - b) < .0001f;
    private static void Case(Report report, string name, Action action)
    {
        try { action(); report.passed++; report.cases.Add("PASS " + name); }
        catch (Exception e) { report.failed++; report.cases.Add("FAIL " + name + ": " + e.Message); }
    }
    private static GameSession Fixture()
    {
        var fresh = GameSession.New();
        var s = new GameSession(fresh.State, fresh.Rules, fresh.Content, new FixedChance());
        var w = s.State.wild[0]; s.State.player.x = w.x; s.State.player.y = w.y - 1;
        Check(s.Capture(w.id).ok, "fixture capture");
        Check(s.AdminRealm(2).ok && s.PlaceSoil(22).ok && s.Plant(22).ok && s.Water(22).ok && s.Decorate(22, "pond").ok, "fixture realm");
        Check(s.Feed(s.State.beasts[0].id, "jade-herb").ok, "fixture meal");
        s.Rules.captureChance = 77; s.Rules.digestionSeconds = 75; s.Rules.cropSeconds = 240;
        s.Rules.recoverySeconds = 800; s.Rules.tripLossPercent = 35; s.Rules.resourceRespawnSeconds = 55;
        var d = s.State; d.room = 1; d.inRealm = true; d.cultivating = true;
        d.time = 137; d.lastCombat = 100; d.savedUtc = 1000; d.soulX = 7.25f; d.soulY = 6.5f;
        d.player.x = 9; d.player.y = 7; d.player.qi = 240; d.player.spentQi = 100;
        d.player.hp = 130; d.player.attackTraining = 7; d.player.defenseTraining = 4; d.player.attackCooldown = .3f;
        d.eggRemaining = 37; d.weaponBonus = 8; d.shieldRemaining = 47;
        d.medicineXp = 15; d.smithXp = 10; d.charmXp = 20; d.farmingXp = 5; d.harvests = 2;
        var pet = d.beasts[0]; pet.name = "Quy giữ tiến trình"; pet.summoned = false; pet.recovering = true;
        pet.stats.x = 8; pet.stats.y = 6; pet.stats.realm = 1; pet.stats.qi = 75; pet.stats.spentQi = 20;
        pet.stats.root = 33; pet.stats.hp = 45; pet.stats.attackTraining = 9; pet.stats.defenseTraining = 7;
        pet.stats.attackCooldown = .4f; pet.skillCooldown = 2.5f;
        d.cells[22].cropProgress = 41; d.cells[22].waterRemaining = 500;
        foreach (var cell in d.cells) if (cell.kind == CellKind.Storage && cell.itemId == "herb") cell.tripAmount = 2;
        d.nodes[0].remaining = 23;
        d.wild[1].affinity = 40; d.wild[1].affinityRemaining = 90; d.wild[1].cooldown = .75f;
        d.journal.Insert(0, "V1 fixture: giữ đồ, cây, thú, nghề và thời gian.");
        return s;
    }
    private static LegacySave Legacy(GameSession s)
    {
        var d = s.State; var r = s.Rules; var wild = new List<LegacyWild>();
        foreach (var w in d.wild) wild.Add(new LegacyWild {
            id = w.id, room = w.room, x = w.x, y = w.y, spawnX = w.spawnX, spawnY = w.spawnY,
            hp = w.hp, timer = w.timer, cooldown = w.cooldown, affinity = w.affinity,
            affinityRemaining = w.affinityRemaining, injured = w.injured, removed = w.removed
        });
        return new LegacySave {
            content = s.Content,
            rules = new LegacyRules {
                captureChance = r.captureChance, digestionSeconds = r.digestionSeconds, recoverySeconds = r.recoverySeconds,
                cropSeconds = r.cropSeconds, wateringSeconds = r.wateringSeconds, offlineHours = r.offlineHours,
                tripLossPercent = r.tripLossPercent, cultivationPerSecond = r.cultivationPerSecond,
                breakthroughBase = r.breakthroughBase, rootCapacityPercent = r.rootCapacityPercent,
                resourceRespawnSeconds = r.resourceRespawnSeconds, affinitySeconds = r.affinitySeconds, affinityChance = r.affinityChance
            },
            state = new LegacyState {
                room = d.room, controlledBeast = d.controlledBeast, nextBeastId = d.nextBeastId, savedUtc = d.savedUtc,
                time = d.time, lastCombat = d.lastCombat, soulX = d.soulX, soulY = d.soulY, inRealm = d.inRealm,
                cultivating = d.cultivating, medicineXp = d.medicineXp, smithXp = d.smithXp, charmXp = d.charmXp,
                farmingXp = d.farmingXp, captures = d.captures, harvests = d.harvests,
                eggRemaining = d.eggRemaining, weaponBonus = d.weaponBonus, shieldRemaining = d.shieldRemaining,
                player = d.player, beasts = d.beasts, cells = d.cells, wild = wild, nodes = d.nodes, journal = d.journal
            }
        };
    }
    // run_script emits these legacy-only DTOs into a dynamic assembly; JsonUtility omits their nested fields.
    private static string RawV1(GameSession s) => Newtonsoft.Json.JsonConvert.SerializeObject(Legacy(s));
    private static string UncheckedV2(GameSession s) => JsonUtility.ToJson(new SaveEnvelope {
        schema = 2, state = s.State, rules = s.Rules, content = s.Content
    });
    private static void Reject(string json)
    {
        try { LocalSaveStore.Decode(json, 1); }
        catch (InvalidDataException) { return; }
        catch (ArgumentException) { return; }
        throw new Exception("invalid save was accepted");
    }
    private static GameSession WithDrop()
    {
        var s = Fixture();
        s.State.drops.Add(new GroundDrop { id = 4, room = 1, itemId = "herb", amount = 3, tripAmount = 2, x = 8.5f, y = 9.5f, remaining = 120 });
        s.State.nextDropId = 5;
        return s;
    }
    public static string Run()
    {
        var report = new Report();
        Case(report, "Raw v1 JSON has no v2 fields and gains explicit movement/drop defaults", () => {
            string json = RawV1(Fixture());
            Check(json.Contains("\"state\":{") && json.Contains("\"rules\":{") && json.Contains("\"version\":1"), "legacy fixture is incomplete");
            string[] added = { "followerDelaySeconds", "followerNearRadius", "followerCatchupDistance", "wildRoamRadius", "wildLeashRadius", "dropLifetimeSeconds", "drops", "nextDropId" };
            foreach (string field in added) Check(!json.Contains("\"" + field + "\""), "fixture contains " + field);
            var copy = LocalSaveStore.Decode(json, 1);
            Check(copy.State.version == 2 && copy.State.drops != null && copy.State.drops.Count == 0 && copy.State.nextDropId == 1, "missing v2 state defaults");
            Check(Close(copy.Rules.followerDelaySeconds, .45f) && Close(copy.Rules.followerNearRadius, 1.4f) &&
                Close(copy.Rules.followerCatchupDistance, 3.5f) && Close(copy.Rules.wildRoamRadius, 3) &&
                Close(copy.Rules.wildLeashRadius, 6) && Close(copy.Rules.dropLifetimeSeconds, 300), "missing v2 rule defaults");
        });
        Case(report, "Migration retains every v1 gameplay field, edited rule, content entry and saved timestamp", () => {
            string before = RawV1(Fixture()); var copy = LocalSaveStore.Decode(before, 1);
            Check(RawV1(copy) == before && copy.ValidateState() == null, "legacy data changed during migration");
        });
        Case(report, "Migrated saves encode schema/state v2 and can be decoded again", () => {
            var copy = LocalSaveStore.Decode(RawV1(Fixture()), 1); string json = LocalSaveStore.Encode(copy);
            var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
            Check(envelope.schema == 2 && envelope.state.version == 2, "encoder wrote a legacy version");
            var again = LocalSaveStore.Decode(json, 2);
            Check(LocalSaveStore.Encode(again) == json, "v2 migration round trip changed data");
        });
        Case(report, "V2 round trip retains ground drops, mixed trip amounts, IDs and custom movement rules", () => {
            var s = WithDrop();
            s.Rules.followerDelaySeconds = .75f; s.Rules.followerNearRadius = 1.8f; s.Rules.followerCatchupDistance = 3.8f;
            s.Rules.wildRoamRadius = 2.5f; s.Rules.wildLeashRadius = 7; s.Rules.dropLifetimeSeconds = 420;
            string json = LocalSaveStore.Encode(s); var copy = LocalSaveStore.Decode(json, 1);
            Check(LocalSaveStore.Encode(copy) == json && copy.State.drops.Count == 1 && copy.State.nextDropId == 5, "drop/custom-rule data lost");
            var drop = copy.State.drops[0]; Check(drop.id == 4 && drop.amount == 3 && drop.tripAmount == 2 && Close(drop.remaining, 120), "drop fields changed");
        });
        Case(report, "Unsupported, missing and mismatched schema/state versions are rejected", () => {
            Reject("{}"); Reject("{\"schema\":3}"); Reject("{\"schema\":2,\"state\":{\"version\":2}}");
            var s = Fixture(); var legacy = Legacy(s); legacy.state.version = 2; Reject(Newtonsoft.Json.JsonConvert.SerializeObject(legacy));
            s.State.version = 1; Reject(UncheckedV2(s));
        });
        Case(report, "Schema v2 cannot silently recreate missing drop fields from a legacy payload", () => {
            var legacy = Legacy(Fixture()); legacy.schema = 2; legacy.state.version = 2;
            Reject(Newtonsoft.Json.JsonConvert.SerializeObject(legacy));
        });
        Case(report, "V1 and v2 reject negative captured meal rewards before applying growth", () => {
            var s = Fixture(); s.State.beasts[0].digestion.qi = -1000;
            Reject(RawV1(s)); Reject(UncheckedV2(s));
        });
        Case(report, "V2 rejects malformed active meal timing", () => {
            var s = Fixture(); s.State.beasts[0].digestion.remaining = s.State.beasts[0].digestion.duration + 1;
            Reject(UncheckedV2(s));
        });
        Case(report, "V1 and v2 preserve the serialized inactive-meal representation", () => {
            var s = Fixture(); s.State.beasts[0].digestion = null;
            Check(LocalSaveStore.Decode(RawV1(s), 1).State.beasts[0].digestion == null, "legacy inactive meal became active");
            Check(LocalSaveStore.Decode(UncheckedV2(s), 1).State.beasts[0].digestion == null, "v2 inactive meal became active");
        });
        Case(report, "V2 rejects a drop with zero quantity", () => {
            var s = WithDrop(); s.State.drops[0].amount = 0; Reject(UncheckedV2(s));
        });
        Case(report, "V2 rejects a drop with more trip items than total items", () => {
            var s = WithDrop(); s.State.drops[0].tripAmount = 4; Reject(UncheckedV2(s));
        });
        Case(report, "V2 rejects a drop with an unknown item", () => {
            var s = WithDrop(); s.State.drops[0].itemId = "unknown"; Reject(UncheckedV2(s));
        });
        Case(report, "V2 rejects a drop outside a supported room or map", () => {
            var s = WithDrop(); s.State.drops[0].room = 2; Reject(UncheckedV2(s));
            s = WithDrop(); s.State.drops[0].x = -1; Reject(UncheckedV2(s));
        });
        Case(report, "V2 rejects a drop with a negative lifetime", () => {
            var s = WithDrop(); s.State.drops[0].remaining = -1; Reject(UncheckedV2(s));
        });
        Case(report, "V2 rejects duplicate drop IDs and a colliding next ID", () => {
            var s = WithDrop(); s.State.drops.Add(new GroundDrop { id = 4, room = 0, itemId = "wood", amount = 1, x = 3, y = 3, remaining = 100 });
            Reject(UncheckedV2(s)); s = WithDrop(); s.State.nextDropId = 4; Reject(UncheckedV2(s));
        });
        Case(report, "Migration and codec round trips do not replay offline growth or move the saved time", () => {
            var source = Fixture(); float qi = source.State.player.qi, meal = source.State.beasts[0].digestion.remaining, egg = source.State.eggRemaining;
            var s = LocalSaveStore.Decode(RawV1(source), 1);
            Check(s.State.savedUtc == 1000 && Close(s.State.cells[22].cropProgress, 41), "decode applied time");
            s.ApplyOffline(1020); float crop = s.State.cells[22].cropProgress, hp = s.State.beasts[0].stats.hp;
            Check(Close(crop, 61) && s.State.savedUtc == 1020 && hp > 45, "offline progress missing");
            var copy = LocalSaveStore.Decode(LocalSaveStore.Encode(s), 2);
            copy.ApplyOffline(1020); copy.ApplyOffline(1010);
            Check(copy.State.savedUtc == 1020 && Close(copy.State.cells[22].cropProgress, crop) && Close(copy.State.beasts[0].stats.hp, hp), "offline growth replayed");
            Check(Close(copy.State.time, 137) && Close(copy.State.player.qi, qi) && Close(copy.State.beasts[0].digestion.remaining, meal) && Close(copy.State.eggRemaining, egg), "offline advanced active systems");
        });
        string result = JsonUtility.ToJson(report, true);
        Directory.CreateDirectory("Verification/LogicV2");
        File.WriteAllText("Verification/LogicV2/save-tests.json", result + "\n");
        if (report.failed > 0) throw new Exception(result);
        return result;
    }
}
