using System;
using System.Collections.Generic;
using System.IO;
using LinhThu.Domain;
using LinhThu.Prototype;
using UnityEngine;

public static class LogicTests
{
    private sealed class FixedChance : IChanceSource { private readonly float value; public FixedChance(float value) { this.value = value; } public float NextPercent() => value; }
    [Serializable] private sealed class Report { public int passed, failed; public List<string> cases = new List<string>(); }
    private static void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    private static GameSession Fresh(float roll = 0)
    {
        var s = GameSession.New(); return new GameSession(s.State, s.Rules, s.Content, new FixedChance(roll));
    }
    private static void Near(GameSession s, int wild = 1) { var w = s.State.wild.Find(v => v.id == wild); s.State.room = w.room; s.State.player.x = w.x; s.State.player.y = w.y - 1; }
    private static GameSession WithBeast()
    {
        var s = Fresh(); Near(s); Check(s.Capture(1).ok, "first capture"); return s;
    }
    private static int Land(GameSession s)
    {
        int i = 22; Check(s.PlaceSoil(i).ok, "place soil"); return i;
    }
    private static void Case(Report report, string name, Action action)
    {
        try { action(); report.passed++; report.cases.Add("PASS " + name); }
        catch (Exception e) { report.failed++; report.cases.Add("FAIL " + name + ": " + e.Message); }
    }
    public static string Run()
    {
        var r = new Report();
        Case(r, "One contract initially; failed duplicate/cap does not consume seals", () => {
            var s = WithBeast(); int seals = s.Count("blood"); Check(!s.Capture(1).ok && s.Count("blood") == seals, "duplicate consumed");
            Near(s, 2); s.State.wild[1].injured = true; Check(!s.Capture(2).ok && s.Count("blood") == seals, "cap consumed");
        });
        Case(r, "Three simultaneous contracts and control cycling", () => {
            var s = WithBeast(); s.AdminRealm(2);
            for (int id = 2; id <= 3; id++) { Near(s, id); s.State.wild[id - 1].injured = true; Check(s.Capture(id).ok, "capture " + id); }
            Check(s.State.beasts.TrueForAll(b => b.summoned), "not simultaneous");
            for (int id = 1; id <= 3; id++) { s.SwitchControl(); Check(s.State.controlledBeast == id, "cycle " + id); }
            s.SwitchControl(); Check(s.State.controlledBeast == 0, "cycle back"); Check(!s.AdminRealm(0).ok, "lowered below contract count");
        });
        Case(r, "Player follows controlled beast; diagonal speed normalized", () => {
            var s = WithBeast(); s.SwitchControl(1); var c = s.Controlled; float x = c.x, y = c.y; s.Move(1, 1, 1);
            Check(Math.Abs(Math.Sqrt(Math.Pow(c.x-x,2)+Math.Pow(c.y-y,2)) - s.Content.turtle.speed) < .01, "diagonal speed");
            float px = s.State.player.x; s.Tick(1); Check(s.State.player.x > px, "player did not follow");
            bool refused = false; try { s.Move(float.NaN, 0, 1); } catch (ArgumentOutOfRangeException) { refused = true; } Check(refused, "NaN input accepted");
        });
        Case(r, "Summoning at the room edge remains inside the map and reloads", () => {
            var s = WithBeast(); s.Summon(1); s.State.player.x = 23.5f;
            Check(s.Summon(1).ok && s.Beast(1).stats.x <= 23.5f, "summoned outside map");
            var loaded = LocalSaveStore.Decode(LocalSaveStore.Encode(s), 1);
            Check(loaded.Beast(1).summoned, "summoned state lost on reload");
        });
        Case(r, "Exploration supplies eggs for incubation without owner grants", () => {
            var s = Fresh(); var egg = s.State.nodes.Find(n => n.itemId == "egg");
            Check(egg != null && egg.room == 1, "no discoverable egg");
            s.State.room = egg.room; s.State.player.x = egg.x; s.State.player.y = egg.y;
            Check(s.Gather(egg.id).ok && s.Count("egg") == 3 && s.Incubate().ok, "egg exploration loop");
        });
        Case(r, "Main button chooses capture, loot, portal, combat and realm without spending", () => {
            var s = Fresh(); Near(s); int seals = s.Count("blood");
            Check(s.PrimaryTarget().kind == PrimaryActionKind.Capture && s.Count("blood") == seals, "capture context");
            var n = s.State.nodes[3]; s.State.player.x = n.x; s.State.player.y = n.y;
            Check(s.PrimaryTarget().kind == PrimaryActionKind.Gather && s.PrimaryTarget().id == n.id, "loot context");
            s.State.player.x = 22; s.State.player.y = 7;
            Check(s.PrimaryTarget().kind == PrimaryActionKind.Travel, "portal context");
            Near(s, 2); Check(s.PrimaryTarget().kind == PrimaryActionKind.Attack, "combat context");
            s.State.inRealm = true; Check(s.PrimaryTarget().kind == PrimaryActionKind.RealmCell, "realm context");
        });
        Case(r, "Main button prioritizes a close hostile over resource pickup", () => {
            var s = Fresh(); var n = s.State.nodes[0]; s.State.player.x = n.x; s.State.player.y = n.y;
            var enemy = s.State.wild[1]; enemy.x = n.x + 1; enemy.y = n.y;
            Check(s.PrimaryTarget().kind == PrimaryActionKind.Attack, "unsafe pickup in melee");
        });
        Case(r, "Food is spent once; digestion continues while moving", () => {
            var s = WithBeast(); int count = s.Count("jade-herb"); Check(s.Feed(1,"jade-herb").ok, "feed"); Check(!s.Feed(1,"jade-herb").ok && s.Count("jade-herb") == count-1, "double feeding");
            s.Move(1,0,1); s.AdminAdvance(60); Check(s.Beast(1).digestion == null && s.Beast(1).stats.qi > 0 && s.Beast(1).stats.defenseTraining > 0, "no growth");
        });
        Case(r, "Better food gives better growth; ordinary food can be compensated", () => {
            var a=WithBeast(); var b=WithBeast(); a.Feed(1,"herb"); b.Feed(1,"jade-herb"); a.AdminAdvance(60); b.AdminAdvance(60);
            Check(b.Beast(1).stats.attackTraining > a.Beast(1).stats.attackTraining, "quality not reflected");
            a.AdminGrant("jade-herb",20); b.AdminGrant("jade-herb",20);
            for(int i=0;i<20;i++){ a.Feed(1,"jade-herb"); a.AdminAdvance(60); b.Feed(1,"jade-herb"); b.AdminAdvance(60); }
            Check(a.Beast(1).stats.attackTraining == b.Beast(1).stats.attackTraining, "early food permanently harms training");
        });
        Case(r, "Root 0 stores 100 qi, root 100 stores 200 qi", () => {
            var s=Fresh(); s.State.player.root=0; Check(s.QiCapacity(s.State.player)==100,"root0"); s.State.player.root=100; Check(s.QiCapacity(s.State.player)==200,"root100");
        });
        Case(r, "Breakthrough retains earned total qi and consumes only required minerals", () => {
            var s=Fresh(); s.State.inRealm=true; s.State.player.qi=s.QiCapacity(s.State.player); s.AdminGrant("stone",3); float total=s.State.player.qi;
            Check(s.Breakthrough(0).ok && s.State.player.realm==1 && s.State.player.qi==total && s.Count("stone")==0,"breakthrough");
            Check(!s.Breakthrough(0).ok,"duplicate breakthrough");
        });
        Case(r, "Zero HP recalls beast, blocks control/summon and recovers slowly", () => {
            var s=WithBeast(); s.SwitchControl(1); s.DamageBeast(1,999); Check(s.Beast(1).recovering&&!s.Beast(1).summoned&&s.State.controlledBeast==0,"faint transition");
            Check(!s.Summon(1).ok&&!s.SwitchControl(1).ok,"recalled pet controlled"); s.AdminAdvance(60); Check(s.Beast(1).recovering,"healed too soon");
            s.AdminAdvance(600); Check(!s.Beast(1).recovering&&!s.Beast(1).summoned&&s.Summon(1).ok,"recovery");
        });
        Case(r, "Item cells reject plants; soil holds plant plus decoration", () => {
            var s=Fresh(); Check(!s.Plant(0).ok&&!s.Water(0).ok,"storage farming"); int i=Land(s); Check(s.Plant(i).ok&&s.Decorate(i,"pond").ok,"layering");
            Check(s.State.cells[i].cropId=="jade-herb"&&s.State.cells[i].decorationId=="pond","overwritten layer");
            Check(!s.Decorate(i,"tree").ok,"overwrote decoration");
        });
        Case(r, "Whole-cell move retains plant and decoration, refuses occupied target", () => {
            var s=Fresh(); int i=Land(s); s.Plant(i); s.Decorate(i,"tree"); Check(!s.MoveCell(i,0).ok,"overwrites stored items");
            Check(s.MoveCell(i,23).ok&&s.State.cells[23].cropId=="jade-herb"&&s.State.cells[23].decorationId=="tree"&&s.State.cells[i].kind==CellKind.Empty,"lost layer");
        });
        Case(r, "Dry crops stop; watered crops grow and harvest only once", () => {
            var s=Fresh(); int i=Land(s); s.Plant(i); s.AdminAdvance(600); Check(s.State.cells[i].cropProgress==0,"dry growth");
            s.Water(i); s.AdminAdvance(600); int n=s.Count("jade-herb"); Check(s.Harvest(i).ok&&s.Count("jade-herb")==n+3,"yield"); Check(!s.Harvest(i).ok&&s.Count("jade-herb")==n+3,"duplicate harvest");
        });
        Case(r, "Offline cap is 12h and the same timestamp is applied once", () => {
            var s=Fresh(); s.Rules.cropSeconds=86400; s.Rules.wateringSeconds=86400; int i=Land(s); s.Plant(i); s.Water(i); s.State.savedUtc=1000;
            s.ApplyOffline(1000+20*3600); Check(s.State.cells[i].cropProgress==43200,"offline cap");
            s.ApplyOffline(1000+20*3600); Check(s.State.cells[i].cropProgress==43200,"double offline");
            s.ApplyOffline(500); Check(s.State.cells[i].cropProgress==43200&&s.State.savedUtc==1000+20*3600,"clock rollback");
        });
        Case(r, "Offline time respects water and does not advance cultivation/digestion", () => {
            var s=WithBeast(); int i=Land(s); s.Plant(i); s.State.cells[i].waterRemaining=20; s.Feed(1,"herb"); float digest=s.Beast(1).digestion.remaining;
            s.State.cultivating=true; s.State.savedUtc=1000; s.ApplyOffline(2000);
            Check(s.State.cells[i].cropProgress==20&&s.State.player.qi==0&&s.Beast(1).digestion.remaining==digest,"offline progressed unauthorized systems");
        });
        Case(r, "Offline recovery heals recalled beasts without auto-summoning or replaying time", () => {
            var s=WithBeast(); Check(s.SwitchControl(1).ok,"control before recall"); s.DamageBeast(1,99999);
            var pet=s.Beast(1); float max=s.MaxHp(pet.stats,true), half=s.Rules.recoverySeconds/2; s.State.savedUtc=1000;
            s.ApplyOffline(1000+half);
            Check(Math.Abs(pet.stats.hp-max/2)<.001&&pet.recovering&&!pet.summoned&&s.State.controlledBeast==0,"partial offline recovery");
            float hp=pet.stats.hp; s.ApplyOffline(1000+half); Check(pet.stats.hp==hp,"replayed recovery time");
            s.ApplyOffline(1000+s.Rules.recoverySeconds);
            Check(pet.stats.hp==max&&!pet.recovering&&!pet.summoned&&s.State.controlledBeast==0,"offline recovery auto-summoned or left incomplete");
            Check(s.Summon(1).ok,"recovered beast cannot be called");
        });
        Case(r, "Offline incubation stays pending and resumes only during online ticks", () => {
            var s=Fresh(); Check(s.AdminGrant("egg",1).ok&&s.Incubate().ok,"start incubation");
            float remaining=s.State.eggRemaining; s.State.savedUtc=1000; s.ApplyOffline(1000+12*3600);
            Check(s.State.eggRemaining==remaining&&s.State.beasts.Count==0&&s.Count("egg")==0,"offline hatched or refunded egg");
            s.Tick(1); Check(s.State.eggRemaining==remaining-1&&s.State.beasts.Count==0,"incubation did not resume");
            for(int i=1;i<remaining;i++)s.Tick(1);
            Check(s.State.eggRemaining==0&&s.State.beasts.Count==1,"online incubation did not finish once");
        });
        Case(r, "Full inventory does not destroy harvest or consume resource node", () => {
            var s=Fresh(); int i=Land(s); s.Plant(i); s.Water(i); s.AdminAdvance(600);
            foreach(var cell in s.State.cells) if(cell.kind!=CellKind.Soil){cell.kind=CellKind.Storage; cell.itemId="wood"; cell.amount=99;cell.tripAmount=0;}
            Check(!s.Harvest(i).ok&&s.State.cells[i].cropProgress==s.State.cells[i].cropDuration,"lost crop");
            var node=s.State.nodes[0]; s.State.player.x=node.x; s.State.player.y=node.y; Check(!s.Gather(node.id).ok&&node.remaining==0,"lost node");
        });
        Case(r, "Death only removes new trip resources, not established stacks", () => {
            var s=Fresh(); var node=s.State.nodes[0]; s.State.player.x=node.x;s.State.player.y=node.y; int old=s.Count("herb"); s.Gather(node.id);
            s.DamagePlayer(99999); Check(s.Count("herb")==old+1&&s.TripCount==1,"wrong trip loss"); s.DamagePlayer(99999); Check(s.Count("herb")==old,"old stock lost");
        });
        Case(r, "Completing trip requires camp; realm does not launder new loot", () => {
            var s=Fresh(); var n=s.State.nodes[0]; s.State.player.x=n.x;s.State.player.y=n.y;s.Gather(n.id);
            s.State.inRealm=true; Check(!s.BankTrip().ok&&s.TripCount==3,"realm bank");s.State.inRealm=false;s.State.player.x=3;s.State.player.y=3;
            Check(s.BankTrip().ok&&s.TripCount==0,"camp bank"); int before=s.Count("herb");s.DamagePlayer(9999);Check(s.Count("herb")==before,"banked loot lost");
        });
        Case(r, "Incubation reserves a contract and cannot be overbooked", () => {
            var s=Fresh();s.AdminGrant("egg",1);Check(s.Incubate().ok,"egg start");Near(s);int seals=s.Count("blood");Check(!s.Capture(1).ok&&s.Count("blood")==seals,"reservation ignored");
            s.AdminAdvance(60);Check(s.State.beasts.Count==1&&s.State.eggRemaining==0,"hatch");
        });
        Case(r, "Failed taming spends a seal and success cannot be replayed", () => {
            var s=Fresh(99);Near(s);int seals=s.Count("blood");Check(!s.Capture(1).ok&&s.Count("blood")==seals-1&&s.State.beasts.Count==0,"failure cost");
        });
        Case(r, "Affection is temporary and repeated feeding increases it", () => {
            var s=Fresh();Near(s,2);Check(s.Lure(2).ok&&s.State.wild[1].affinity==20,"lure");s.Lure(2);Check(s.State.wild[1].affinity==40,"increment");
            for(int i=0;i<181;i++)s.Tick(1);Check(s.State.wild[1].affinity==0,"expiration");
        });
        Case(r, "Crafting carries trip exposure and refuses insufficient inputs", () => {
            var s=Fresh();var n=s.State.nodes[0];s.State.player.x=n.x;s.State.player.y=n.y;s.Gather(n.id);
            Check(s.Craft("alchemy").ok,"craft"); Check(Array.Exists(s.State.cells,c=>c.itemId=="pill"&&c.tripAmount==1),"washed trip status");
            int amount=s.Count("stone");Check(!s.Craft("smith").ok&&s.Count("stone")==amount,"spent insufficient");
        });
        Case(r, "Invalid configuration is rejected before live replacement", () => {
            var s=Fresh();var rules=new GameRules{offlineHours=24};Check(!s.ApplyConfiguration(rules,GameContent.Defaults()).ok&&s.Rules.offlineHours==12,"invalid settings applied");
            rules=new GameRules{captureChance=float.NaN};Check(!s.ApplyConfiguration(rules,GameContent.Defaults()).ok,"NaN config");
        });
        Case(r, "Jobs keep their original duration after rules change", () => {
            var s=WithBeast();s.Feed(1,"herb");int i=Land(s);s.Plant(i);float before=s.Beast(1).digestion.duration;
            s.ApplyConfiguration(new GameRules{digestionSeconds=1,cropSeconds=10},GameContent.Defaults());Check(s.Beast(1).digestion.duration==before&&s.State.cells[i].cropDuration==180,"retroactive settings");
        });
        Case(r, "Active meals keep captured rewards; subsequent meals use edited food", () => {
            var baseline=WithBeast(); var edited=WithBeast();
            Check(baseline.Feed(1,"herb").ok&&edited.Feed(1,"herb").ok,"start reference meals");
            var content=GameContent.Defaults(); var food=content.Item("herb"); food.qi=80; food.attack=7; food.defense=6; food.root=4;
            Check(edited.ApplyConfiguration(new GameRules{digestionSeconds=180},content).ok,"apply edited food");
            baseline.AdminAdvance(30); edited.AdminAdvance(30); var oldStats=baseline.Beast(1).stats; var keptStats=edited.Beast(1).stats;
            Check(baseline.Beast(1).digestion==null&&edited.Beast(1).digestion==null,"active meal duration changed");
            Check(keptStats.qi==oldStats.qi&&keptStats.root==oldStats.root&&keptStats.attackTraining==oldStats.attackTraining&&keptStats.defenseTraining==oldStats.defenseTraining,"edited food altered active meal rewards");
            Check(baseline.Feed(1,"herb").ok&&edited.Feed(1,"herb").ok,"start subsequent meals");
            baseline.AdminAdvance(180); edited.AdminAdvance(180);
            Check(edited.Beast(1).digestion==null&&keptStats.qi>oldStats.qi&&keptStats.root>oldStats.root&&keptStats.attackTraining>oldStats.attackTraining&&keptStats.defenseTraining>oldStats.defenseTraining,"subsequent meal ignored edited food");
        });
        Case(r, "Save round trip keeps all layers, timers, stats and edited rules", () => {
            var s=WithBeast();int i=Land(s);s.Plant(i);s.Water(i);s.Decorate(i,"pond");s.Feed(1,"jade-herb");s.Rules.captureChance=77;
            var copy=LocalSaveStore.Decode(LocalSaveStore.Encode(s),1);Check(copy.ValidateState()==null&&copy.Rules.captureChance==77&&copy.State.cells[i].decorationId=="pond"&&copy.Beast(1).digestion!=null,"roundtrip");
        });
        Case(r, "Inactive meals survive save/reload; malformed meals are not discarded", () => {
            var s=WithBeast(); var copy=LocalSaveStore.Decode(LocalSaveStore.Encode(s),1);
            Check(copy.Beast(1).digestion==null&&copy.Feed(1,"herb").ok,"inactive meal blocked feeding after reload");
            s.Beast(1).digestion=new Digestion { qi=10 }; bool refused=false;
            try { LocalSaveStore.Decode(LocalSaveStore.Encode(s),1); } catch (Exception) { refused=true; }
            Check(refused,"malformed meal silently removed");
        });
        Case(r, "Malformed saves are refused rather than silently reset", () => {
            bool bad=false;try{LocalSaveStore.Decode("{\"schema\":2}",1);}catch(Exception){bad=true;}Check(bad,"schema");
            var s=Fresh();s.State.cells[0].amount=-1;bad=false;try{LocalSaveStore.Decode(LocalSaveStore.Encode(s),1);}catch(Exception){bad=true;}Check(bad,"negative stack");
        });
        Case(r, "Room transfer preserves party, and distant requests are rejected", () => {
            var s=WithBeast();Check(!s.Travel().ok,"remote portal");s.State.player.x=22;s.State.player.y=7;Check(s.Travel().ok&&s.State.room==1&&s.State.beasts.Count==1,"party transfer");
            Check(!s.Gather(0).ok,"cross-room loot");
        });
        Directory.CreateDirectory("Verification/LogicV1");string json=JsonUtility.ToJson(r,true);File.WriteAllText("Verification/LogicV1/domain-tests.json",json+"\n");
        if(r.failed>0) throw new Exception(json);
        return json;
    }
}
