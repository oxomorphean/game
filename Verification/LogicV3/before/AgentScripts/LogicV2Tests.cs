using System;
using System.Collections.Generic;
using System.IO;
using LinhThu.Domain;
using UnityEngine;

/// <summary>Local v2 behavior checks with isolated fixtures. No live scene, save file or browser data is accessed.</summary>
public static class LogicV2Tests
{
    private sealed class FixedChance : IChanceSource
    {
        private readonly float value;
        public FixedChance(float value) { this.value = value; }
        public float NextPercent() => value;
    }
    private sealed class SequenceChance : IChanceSource
    {
        private readonly float[] values;
        public int calls;
        public SequenceChance(params float[] values) { this.values = values; }
        public float NextPercent() { int index = calls++; return values[Math.Min(index, values.Length - 1)]; }
    }
    [Serializable] private sealed class Report { public int passed, failed; public List<string> cases = new List<string>(); }
    [Serializable] private sealed class InventoryView { public RealmCell[] cells; }
    [Serializable] private sealed class GroundView { public List<GroundDrop> drops; }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static float Distance(float x, float y, float a, float b) => (float)Math.Sqrt((x-a)*(x-a)+(y-b)*(y-b));
    private static float Distance(Cultivator a, Cultivator b) => Distance(a.x,a.y,b.x,b.y);
    private static bool Close(float a, float b) => Math.Abs(a-b) < .001f;
    private static void Case(Report report, string name, Action action)
    {
        try { action(); report.passed++; report.cases.Add("PASS " + name); }
        catch (Exception e) { report.failed++; report.cases.Add("FAIL " + name + ": " + e.Message); }
    }
    private static GameSession Quiet(float roll = 0, IChanceSource chance = null)
    {
        var initial = GameSession.New(); initial.State.wild.Clear();
        initial.State.player.x = 10; initial.State.player.y = 7;
        return new GameSession(initial.State, initial.Rules, initial.Content, chance ?? new FixedChance(roll));
    }
    private static GameSession Party(int count = 1)
    {
        var s = Quiet(); s.State.player.realm = Math.Max(0, count - 1);
        for (int i = 0; i < count; i++) s.State.beasts.Add(new Beast { id = i + 1, name = "Quy test " + (i + 1), stats = new Cultivator {
            x = i == 2 ? 10 : 9, y = i == 0 ? 7 : 6.2f, hp = 90
        }});
        s.State.nextBeastId = count + 1;
        return new GameSession(s.State, s.Rules, s.Content, new FixedChance(0));
    }
    private static WildBeast AddWild(GameSession s, int id, float x, float y)
    {
        var wild = new WildBeast { id = id, room = s.State.room, x = x, y = y, spawnX = x, spawnY = y, hp = s.WildMaxHp };
        s.State.wild.Add(wild); return wild;
    }
    private static void Advance(GameSession s, float seconds)
    {
        int steps = (int)Math.Ceiling(seconds / .1f);
        for (int i = 0; i < steps; i++) { float dt = Math.Min(.1f, seconds - i * .1f); if (dt > 0) s.Tick(dt); }
    }
    private static void Walk(GameSession s, float x, float y, int ticks)
    { for (int i = 0; i < ticks; i++) { s.Move(x,y,.1f); s.Tick(.1f); } }
    private static int Cell(GameSession s, string item) => Array.FindIndex(s.State.cells,c=>c.kind==CellKind.Storage&&c.itemId==item);
    private static int Trip(GameSession s, string item)
    { int total=0; foreach(var c in s.State.cells) if(c.kind==CellKind.Storage&&c.itemId==item)total+=c.tripAmount; return total; }
    private static int OnGround(GameSession s, string item)
    { int total=0; foreach(var d in s.State.drops)if(d.itemId==item)total+=d.amount; return total; }
    private static string Inventory(GameSession s) => JsonUtility.ToJson(new InventoryView { cells = s.State.cells });
    private static string Ground(GameSession s) => JsonUtility.ToJson(new GroundView { drops = s.State.drops });
    private static void ClearInventory(GameSession s)
    { for(int i=0;i<s.State.cells.Length;i++)s.State.cells[i]=new RealmCell(); }
    private static void Store(GameSession s, int cell, string item, int amount, int trip=0)
    { s.State.cells[cell]=new RealmCell { kind=CellKind.Storage,itemId=item,amount=amount,tripAmount=trip }; }
    private static void FillInventory(GameSession s)
    { for(int i=0;i<s.State.cells.Length;i++)Store(s,i,"wood",99); }
    private static void FillGround(GameSession s)
    {
        s.State.drops.Clear();
        for(int i=0;i<GameSession.MaxGroundDrops;i++)s.State.drops.Add(new GroundDrop {
            id=i+1,room=1,itemId="wood",amount=99,x=3,y=3,remaining=120
        });
        s.State.nextDropId=GameSession.MaxGroundDrops+1;
    }
    private static void WaitForAffinity(GameSession s, WildBeast wild, float amount)
    {
        for(int i=0;i<100&&wild.affinity<amount;i++)s.Tick(.1f);
        Check(wild.affinity>=amount,"food was not eaten within 10 seconds");
    }
    public static string Run()
    {
        var report = new Report();
        Case(report,"A nearby companion observes the first movement ticks, then follows",()=>{
            var s=Party();var pet=s.Beast(1).stats;float x=pet.x,y=pet.y;
            Walk(s,1,0,3);Check(Close(pet.x,x)&&Close(pet.y,y),"companion moved immediately");
            Walk(s,1,0,7);Check(pet.x>x+.5f&&pet.x<s.State.player.x,"companion did not follow after delay");
        });
        Case(report,"Following changes side with a reversal and follows a subsequent vertical turn",()=>{
            var s=Party();var pet=s.Beast(1).stats;
            Walk(s,1,0,12);Check(pet.x<s.State.player.x-.2f,"rightward trail missing");
            Walk(s,-1,0,12);Check(pet.x>s.State.player.x+.2f,"fixed world-side offset survived reversal");
            float y=pet.y;Walk(s,0,1,10);Check(pet.y>y+.5f&&pet.y<s.State.player.y,"vertical trail missing");
        });
        Case(report,"Idle companions take several short walks, rest more often and remain nearby",()=>{
            var s=Party();var pet=s.Beast(1).stats;int moved=0,rested=0,bouts=0;bool wasMoving=false;
            for(int i=0;i<400;i++){
                float x=pet.x,y=pet.y;s.Tick(.1f);bool moving=Distance(x,y,pet.x,pet.y)>.001f;
                if(moving){moved++;if(!wasMoving)bouts++;}else rested++;
                wasMoving=moving;
                Check(Distance(pet,s.State.player)<=s.Rules.followerNearRadius+1.2f,"idle companion wandered away");
            }
            Check(moved>=10&&bouts>=3&&rested>moved,"idle movement was absent or continuous");
        });
        Case(report,"Three companions move in bounded steps; changing control keeps positions and the player follows",()=>{
            var s=Party(3);float[] x=new float[4],y=new float[4];
            for(int tick=0;tick<50;tick++){
                x[0]=s.State.player.x;y[0]=s.State.player.y;
                for(int i=0;i<3;i++){x[i+1]=s.State.beasts[i].stats.x;y[i+1]=s.State.beasts[i].stats.y;}
                s.Move(tick<15?1:tick<30?0:-1,tick<15?0:tick<30?1:0,.1f);s.Tick(.1f);
                for(int i=0;i<3;i++)Check(Distance(x[i+1],y[i+1],s.State.beasts[i].stats.x,s.State.beasts[i].stats.y)<=s.MoveSpeed(false)*.2f+.001f,"pet teleported");
            }
            for(int i=0;i<3;i++)Check(Distance(s.State.beasts[i].stats,s.State.player)<s.Rules.followerCatchupDistance+1,"party was left behind");
            string before=JsonUtility.ToJson(s.State);Check(s.SwitchControl(2).ok,"cannot control second pet");
            for(int i=0;i<3;i++){x[i+1]=s.State.beasts[i].stats.x;y[i+1]=s.State.beasts[i].stats.y;}
            float px=s.State.player.x,py=s.State.player.y;
            Check(JsonUtility.FromJson<GameState>(before).player.x==px,"switch moved the physical player");
            var prior=JsonUtility.FromJson<GameState>(before);
            for(int i=0;i<3;i++)Check(Close(prior.beasts[i].stats.x,x[i+1])&&Close(prior.beasts[i].stats.y,y[i+1]),"switch teleported a pet");
            for(int tick=0;tick<20;tick++){
                float oldX=s.State.player.x,oldY=s.State.player.y;s.Move(-1,0,.1f);s.Tick(.1f);
                Check(Distance(oldX,oldY,s.State.player.x,s.State.player.y)<=s.MoveSpeed(false)*.2f+.001f,"following player teleported");
            }
            Check(Distance(px,py,s.State.player.x,s.State.player.y)>.5f&&s.State.controlledBeast==2,"player did not follow controlled pet");
        });
        Case(report,"A distant companion catches up by movement instead of teleporting",()=>{
            var s=Party();var pet=s.Beast(1).stats;pet.x=2;pet.y=7;
            float start=Distance(pet,s.State.player);s.Move(1,0,.1f);s.Tick(.1f);
            Check(pet.x>2&&pet.x<3&&Distance(pet,s.State.player)>5,"catch-up teleported");
            Advance(s,8);Check(Distance(pet,s.State.player)<start-3,"distant pet did not return nearby");
        });
        Case(report,"Recovering beasts are not pulled along while another companion follows",()=>{
            var s=Party(2);var pet=s.Beast(1);s.DamageBeast(1,1000);float x=pet.stats.x,y=pet.stats.y;
            Walk(s,1,0,20);
            Check(pet.recovering&&!pet.summoned&&Close(pet.stats.x,x)&&Close(pet.stats.y,y),"recovering beast moved");
            Check(!s.Summon(1).ok&&!s.SwitchControl(1).ok&&s.Beast(2).stats.x>9,"recall state or healthy companion failed");
        });
        Case(report,"Wild beasts roam within habitat/map limits and include stationary pauses",()=>{
            float[] homesX={12,.5f,23.5f},homesY={7,.5f,13.5f};
            for(int home=0;home<homesX.Length;home++){
                var s=Quiet();s.State.player.x=home==1?23:1;s.State.player.y=home==1?13:1;
                var wild=AddWild(s,10,homesX[home],homesY[home]);int moved=0,rested=0;
                for(int i=0;i<300;i++){
                    float x=wild.x,y=wild.y;s.Tick(.1f);
                    if(Distance(x,y,wild.x,wild.y)>.001f)moved++;else rested++;
                    Check(Distance(wild.x,wild.y,wild.spawnX,wild.spawnY)<=s.Rules.wildRoamRadius+.01f,"roam escaped habitat");
                    Check(wild.x>=.5f&&wild.x<=23.5f&&wild.y>=.5f&&wild.y<=13.5f,"roam escaped map");
                }
                Check(moved>10&&rested>10,"wildlife did not roam and pause");
            }
        });
        Case(report,"A beast beyond its leash returns home and recovers instead of freezing",()=>{
            var s=Quiet();s.State.player.x=23;s.State.player.y=13;var wild=AddWild(s,10,10,7);
            wild.x=17;wild.hp=10;s.Tick(.1f);Check(wild.x<17,"out-of-leash beast froze");
            for(int i=0;i<60&&Distance(wild.x,wild.y,wild.spawnX,wild.spawnY)>.5f;i++)s.Tick(.1f);
            Check(Distance(wild.x,wild.y,wild.spawnX,wild.spawnY)<=.5f&&Close(wild.hp,s.WildMaxHp),"return/home recovery missing");
        });
        Case(report,"Chasing a fleeing player ends with return to habitat",()=>{
            var s=Quiet();s.State.player.x=11.3f;var wild=AddWild(s,10,10,7);
            Walk(s,1,0,20);Check(wild.x>10.1f,"beast never engaged the nearby player");
            s.State.player.x=23;s.State.player.y=13;Advance(s,6);
            Check(Distance(wild.x,wild.y,wild.spawnX,wild.spawnY)<=s.Rules.wildRoamRadius+.01f,"beast stayed stranded after pursuit");
        });
        Case(report,"Roaming never consumes capture/food probability draws",()=>{
            var chance=new SequenceChance(99,0);var s=Quiet(0,chance);s.State.player.x=1;s.State.player.y=1;
            var wild=AddWild(s,10,12,7);Advance(s,30);Check(chance.calls==0,"motion consumed gameplay RNG");
            wild.injured=true;wild.hp=0;wild.timer=300;s.State.player.x=wild.x;s.State.player.y=wild.y;
            int blood=s.Count("blood");Check(!s.Capture(wild.id).ok&&chance.calls==1&&s.Count("blood")==blood-1,"capture RNG changed");
            Check(s.Capture(wild.id).ok&&chance.calls==2&&s.State.beasts.Count==1,"second capture draw changed");
        });
        Case(report,"Dropping inside the realm uses the physical player even while controlling a distant beast",()=>{
            var s=Party();Check(s.SwitchControl(1).ok,"control");s.Beast(1).stats.x=20;s.Beast(1).stats.y=11;
            float x=s.State.player.x,y=s.State.player.y;Check(s.ToggleRealm().ok,"realm entry");
            s.State.soulX=2;s.State.soulY=2;Check(s.DropFromCell(Cell(s,"herb"),2).ok,"realm drop");
            var drop=s.State.drops[0];Check(Distance(drop.x,drop.y,x,y)<1.1f&&Distance(drop.x,drop.y,20,11)>5,"drop followed soul or controlled pet");
            Check(Close(s.State.player.x,x)&&Close(s.State.player.y,y)&&s.State.inRealm,"drop moved the body or exited realm");
            Check(!s.PickUpDrop(drop.id).ok&&s.NearbyDrop()==null,"soul picked outside-world loot");
        });
        Case(report,"Selected-cell quantity consumes only the requested stack and retains exposure during merging",()=>{
            var s=Quiet();ClearInventory(s);Store(s,4,"wood",7,3);Store(s,9,"wood",9);
            Check(s.DropFromCell(9,4).ok&&s.State.cells[9].amount==5&&s.State.cells[4].amount==7,"wrong selected stack");
            Check(s.DropFromCell(4,2).ok&&s.State.cells[4].amount==5&&s.State.cells[4].tripAmount==1,"wrong selected quantity");
            Check(s.State.drops.Count==1&&s.State.drops[0].amount==6&&s.State.drops[0].tripAmount==2,"merge lost amount/exposure");
        });
        Case(report,"Invalid cell, quantity, item and pickup IDs neither consume nor duplicate items",()=>{
            var s=Quiet();string items=Inventory(s),ground=Ground(s);int next=s.State.nextDropId;
            Check(!s.DropFromCell(-1,1).ok&&!s.DropFromCell(100,1).ok&&!s.DropFromCell(Cell(s,"herb"),0).ok,"invalid drop accepted");
            Check(!s.DropFromCell(Cell(s,"herb"),-1).ok&&!s.DropFromCell(Cell(s,"herb"),99).ok&&!s.DropItem("missing",1).ok,"invalid amount/item accepted");
            Check(!s.PickUpDrop(-1).ok&&!s.PickUpDrop(999).ok,"unknown pickup accepted");
            Check(Inventory(s)==items&&Ground(s)==ground&&s.State.nextDropId==next,"invalid command changed items");
        });
        Case(report,"Full ground and exhausted IDs reject new drops before consuming inventory",()=>{
            var s=Quiet();FillGround(s);string items=Inventory(s),ground=Ground(s);int next=s.State.nextDropId;
            Check(!s.DropItem("herb",1).ok&&Inventory(s)==items&&Ground(s)==ground&&s.State.nextDropId==next,"full ground consumed items");
            s=Quiet();s.State.nextDropId=int.MaxValue;items=Inventory(s);
            Check(!s.DropItem("herb",1).ok&&Inventory(s)==items&&s.State.drops.Count==0,"exhausted ID consumed items");
        });
        Case(report,"A full ground record list still permits a merge into a compatible stack",()=>{
            var s=Quiet();FillGround(s);var first=s.State.drops[0];first.room=s.State.room;first.itemId="herb";first.amount=1;
            first.x=s.State.player.x+.85f;first.y=s.State.player.y;int next=s.State.nextDropId;
            Check(s.DropItem("herb",1).ok&&s.State.drops.Count==GameSession.MaxGroundDrops&&first.amount==2&&s.State.nextDropId==next,"compatible merge rejected");
        });
        Case(report,"Full inventory preserves a ground drop; freeing space permits one pickup only",()=>{
            var s=Quiet();Check(s.DropItem("herb",2).ok,"fixture drop");var drop=s.State.drops[0];FillInventory(s);string items=Inventory(s);
            Check(!s.PickUpDrop(drop.id).ok&&s.State.drops.Contains(drop)&&drop.amount==2&&Inventory(s)==items,"full inventory lost drop/items");
            s.State.cells[99]=new RealmCell();Check(s.PickUpDrop(drop.id).ok&&s.Count("herb")==2,"pickup after freeing cell");
            Check(!s.PickUpDrop(drop.id).ok&&s.Count("herb")==2&&s.State.drops.Count==0,"pickup replay duplicated items");
        });
        Case(report,"Pickup rejects another room and a distant position without changing the drop",()=>{
            var s=Quiet();Check(s.DropItem("herb",1).ok,"fixture drop");var drop=s.State.drops[0];string items=Inventory(s);
            drop.room=1;Check(!s.PickUpDrop(drop.id).ok,"cross-room pickup");drop.room=0;drop.x=23;drop.y=13;
            Check(!s.PickUpDrop(drop.id).ok&&Inventory(s)==items&&s.State.drops.Contains(drop)&&drop.amount==1,"distant pickup changed data");
        });
        Case(report,"Drop and pickup preserve both safe and trip portions of a mixed stack",()=>{
            var s=Quiet();var node=s.State.nodes[0];s.State.player.x=node.x;s.State.player.y=node.y;
            Check(s.Gather(node.id).ok&&s.Count("herb")==9&&Trip(s,"herb")==3,"fixture trip loot");
            Check(s.DropFromCell(Cell(s,"herb"),5).ok,"mixed drop");var drop=s.State.drops[0];
            Check(drop.amount==5&&drop.tripAmount==3&&s.Count("herb")==4&&Trip(s,"herb")==0,"drop lost mixed exposure");
            Check(s.PickUpDrop(drop.id).ok&&s.Count("herb")==9&&Trip(s,"herb")==3&&!s.PickUpDrop(drop.id).ok,"pickup laundered or duplicated loot");
        });
        Case(report,"Food attracts a beast first; affinity changes only after approach and time spent eating",()=>{
            var s=Quiet();s.State.player.x=4;var wild=AddWild(s,10,8,7);Check(s.DropItem("herb",1).ok,"drop food");
            var drop=s.State.drops[0];float initial=Distance(wild.x,wild.y,drop.x,drop.y);
            Check(wild.affinity==0&&drop.amount==1,"drop immediately added affection");Advance(s,.5f);
            Check(wild.affinity==0&&drop.amount==1&&Distance(wild.x,wild.y,drop.x,drop.y)<initial,"approach consumed food or did not move");
            for(int i=0;i<60&&Distance(wild.x,wild.y,drop.x,drop.y)>.65f;i++){s.Tick(.1f);Check(wild.affinity==0,"affection before arrival");}
            Check(Distance(wild.x,wild.y,drop.x,drop.y)<=.65f,"beast failed to reach food");Advance(s,.4f);
            Check(wild.affinity==0&&s.State.drops.Contains(drop)&&drop.amount==1,"food was instant instead of an eating action");
            WaitForAffinity(s,wild,20);Check(wild.affinity==20&&!s.State.drops.Contains(drop),"meal did not consume exactly one food");
        });
        Case(report,"Two beasts competing for one food consume it only once",()=>{
            var s=Quiet();s.State.player.x=4;Check(s.DropItem("herb",1).ok,"drop food");var drop=s.State.drops[0];
            var a=AddWild(s,10,drop.x+.5f,drop.y);var b=AddWild(s,11,drop.x+.5f,drop.y);
            Advance(s,1.6f);
            Check(a.affinity+b.affinity==20&&s.State.drops.Count==0&&s.Count("herb")==5,"single food was eaten twice");
        });
        Case(report,"Food in another room does not attract or feed a beast",()=>{
            var s=Quiet();s.State.player.x=1;s.State.player.y=1;var wild=AddWild(s,10,12,7);
            s.State.drops.Add(new GroundDrop { id=1,room=1,itemId="herb",amount=1,x=12,y=7,remaining=100 });s.State.nextDropId=2;
            Advance(s,5);Check(wild.affinity==0&&s.State.drops.Count==1&&s.State.drops[0].amount==1,"beast ate cross-room food");
        });
        Case(report,"Death applies trip loss to ground loot and keeps all safe portions",()=>{
            var s=Quiet();ClearInventory(s);Store(s,8,"wood",10,4);Store(s,9,"herb",6);
            Check(s.DropFromCell(8,6).ok,"trip drop");var drop=s.State.drops[0];s.DamagePlayer(10000);
            Check(drop.amount==4&&drop.tripAmount==2&&s.Count("wood")==4&&s.Count("herb")==6,"death laundered drops or lost safe items");
            Check(s.State.room==0&&s.State.controlledBeast==0&&s.ValidateState()==null,"death left invalid state");
        });
        Case(report,"Death removes an empty pure-trip drop rather than persisting a zero stack",()=>{
            var s=Quiet();ClearInventory(s);Store(s,8,"wood",1,1);Check(s.DropFromCell(8,1).ok,"pure trip drop");
            s.DamagePlayer(10000);Check(s.State.drops.Count==0&&s.ValidateState()==null,"zero drop survived death");
        });
        Case(report,"Combat rewards stay in the world when inventory is full",()=>{
            var s=Quiet(99);FillInventory(s);var wild=AddWild(s,10,s.State.player.x+1.5f,s.State.player.y);wild.hp=1;
            Check(s.Strike(false).ok&&wild.removed&&s.Count("stone")==0&&OnGround(s,"stone")==1,"full inventory destroyed combat reward");
            var drop=s.State.drops.Find(d=>d.itemId=="stone");Check(!s.PickUpDrop(drop.id).ok&&OnGround(s,"stone")==1,"failed reward pickup destroyed loot");
            s.State.cells[99]=new RealmCell();Check(s.PickUpDrop(drop.id).ok&&s.Count("stone")==1&&Trip(s,"stone")==1,"reward pickup/exposure failed");
        });
        Case(report,"Full ground queues combat loot and materializes it once after a slot expires",()=>{
            var s=Quiet(99);FillGround(s);var wild=AddWild(s,10,s.State.player.x+1.5f,s.State.player.y);wild.hp=1;
            Check(s.Strike(false).ok&&wild.removed&&wild.pendingLoot==1&&OnGround(s,"stone")==0,"full ground lost pending reward");
            s.Tick(.1f);Check(wild.pendingLoot==1&&OnGround(s,"stone")==0,"pending reward vanished");
            s.State.drops[0].remaining=.05f;s.Tick(.1f);
            Check(wild.pendingLoot==0&&OnGround(s,"stone")==1&&s.State.drops.Count==GameSession.MaxGroundDrops,"queued reward was not released");
            s.Tick(.1f);Check(OnGround(s,"stone")==1&&wild.pendingLoot==0,"pending reward replayed");
        });
        Case(report,"Crafting from an earlier safe stack does not mark output because a later fresh stack exists",()=>{
            var s=Quiet();ClearInventory(s);Store(s,0,"herb",99);Store(s,1,"herb",3,3);
            Check(s.Craft("alchemy").ok&&s.Count("pill")==1&&Trip(s,"pill")==0&&s.State.cells[0].amount==96&&s.State.cells[1].tripAmount==3,"old ingredients inherited unrelated exposure");
            s.DamagePlayer(10000);Check(s.Count("pill")==1,"safe crafted item lost on death");
        });
        Case(report,"Crafting that actually consumes fresh ingredients keeps output trip exposure",()=>{
            var s=Quiet();ClearInventory(s);Store(s,0,"herb",3,3);Store(s,1,"herb",6);
            Check(s.Craft("alchemy").ok&&s.Count("pill")==1&&Trip(s,"pill")==1&&s.State.cells[1].amount==6,"fresh crafting laundered exposure");
        });
        Case(report,"Companions do not hunt before intent and assist the target the player attacks",()=>{
            var s=Party();s.Beast(1).stats.x=8.8f;var target=AddWild(s,10,7.5f,7);var other=AddWild(s,11,19,12);
            Advance(s,.3f);Check(Close(target.hp,s.WildMaxHp)&&s.Beast(1).stats.attackCooldown==0,"companion hunted before intent");
            Check(s.Strike(true).ok,"player attack");float afterPlayer=target.hp;s.Tick(.1f);
            Check(target.hp<afterPlayer&&Close(other.hp,s.WildMaxHp)&&s.State.controlledBeast==0,"companion failed selected-target assistance");
        });
        Case(report,"Companions protect a player being attacked without requiring a manual attack",()=>{
            var s=Party();var target=AddWild(s,10,10.8f,7);float hp=s.State.player.hp;
            Advance(s,.5f);Check(s.State.player.hp<hp&&target.hp<s.WildMaxHp&&s.State.controlledBeast==0,"automatic defense missing");
        });
        Case(report,"Controlled fainting returns control and recovery never auto-summons the beast",()=>{
            var s=Party();Check(s.SwitchControl(1).ok,"control");s.DamageBeast(1,10000);var pet=s.Beast(1);
            Check(s.State.controlledBeast==0&&pet.recovering&&!pet.summoned,"faint transition");
            s.AdminAdvance(60);Check(pet.recovering&&pet.stats.hp>0&&!s.Summon(1).ok,"partial recovery recalled too early");
            s.AdminAdvance(s.Rules.recoverySeconds);Check(!pet.recovering&&!pet.summoned&&s.Summon(1).ok,"manual summon after recovery failed");
        });
        Case(report,"Removed and injured wildlife respawn once with full health at their habitat",()=>{
            float[] rolls={99,0};
            foreach(float roll in rolls){
                var s=Quiet(roll);var wild=AddWild(s,10,s.State.player.x+1.5f,s.State.player.y);wild.hp=1;
                Check(s.Strike(false).ok&&(wild.removed||wild.injured),"wild defeat");s.State.inRealm=true;
                for(int i=0;i<3100&&(wild.removed||wild.injured);i++)s.Tick(.1f);
                Check(!wild.removed&&!wild.injured&&Close(wild.hp,s.WildMaxHp)&&Close(wild.x,wild.spawnX)&&Close(wild.y,wild.spawnY),"respawn failed");
                Check(OnGround(s,"stone")== (roll==99?1:0),"respawn duplicated or lost reward");
            }
        });
        Case(report,"Gathered resources cannot be replayed and become gatherable after respawn",()=>{
            var s=Quiet();var node=s.State.nodes[0];s.State.player.x=node.x;s.State.player.y=node.y;
            Check(s.Gather(node.id).ok,"gather");int count=s.Count(node.itemId);
            Check(!s.Gather(node.id).ok&&s.Count(node.itemId)==count,"gather replay duplicated items");
            Advance(s,s.Rules.resourceRespawnSeconds+.2f);
            Check(s.Gather(node.id).ok&&s.Count(node.itemId)==count+3,"resource did not respawn");
        });
        Case(report,"Expired ground items cannot be picked up and subsequent drops use fresh IDs",()=>{
            var s=Quiet();s.Rules.dropLifetimeSeconds=30;Check(s.DropItem("herb",1).ok,"drop");int old=s.State.drops[0].id;
            Advance(s,30.2f);Check(s.State.drops.Count==0&&!s.PickUpDrop(old).ok&&s.Count("herb")==5,"expired item survived or duplicated");
            Check(s.DropItem("herb",1).ok&&s.State.drops[0].id>old,"drop ID reused");
        });
        string result=JsonUtility.ToJson(report,true);
        Directory.CreateDirectory("Verification/LogicV2");File.WriteAllText("Verification/LogicV2/domain-tests.json",result+"\n");
        if(report.failed>0)throw new Exception(result);
        return result;
    }
}
