using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LinhThu.Domain;
using LinhThu.Prototype;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Exercises real HUD button bindings across frames using a temporary session; restores the live session and never saves the fixture.</summary>
public static class HudV2Tests
{
    [Serializable] private sealed class Report { public int passed, failed; public List<string> cases = new List<string>(); }
    private static void Set(PrototypeHost host, string property, object value) => typeof(PrototypeHost).GetProperty(property).SetValue(host, value);
    private static void Click(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) throw new Exception("Visible button missing: " + name);
        var button = go.GetComponent<UnityEngine.UI.Button>();
        if (button == null || !button.interactable) throw new Exception("Button unavailable: " + name);
        button.onClick.Invoke();
    }
    public static string Run()
    {
        var host = UnityEngine.Object.FindAnyObjectByType<PrototypeHost>();
        if (!EditorApplication.isPlaying || host == null || host.Session == null) throw new InvalidOperationException("Play the logic scene first.");
        var report = new Report();
        Action<bool,string> check = (ok,name) => { if(ok) report.passed++; else report.failed++; report.cases.Add((ok?"PASS ":"FAIL ")+name); };
        var original = host.Session; bool owner = host.OwnerMode; string loadError = host.LoadError;
        int cell = host.SelectedCell, wildId = host.SelectedWild;
        string originalPanel = (string)typeof(GameplayHud).GetField("panel", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(host.GetComponent<GameplayHud>());
        var fixture = GameSession.New(); fixture.State.wild.Clear();
        fixture.State.beasts.Add(new Beast { id=1, name="Quy kiểm chứng", stats=new Cultivator { x=4,y=3,hp=60 } });
        fixture.State.nextBeastId=2;
        Set(host,"LoadError","HUD verification fixture — saving disabled");
        Set(host,"Session",fixture); host.SetPresentation("play"); host.OpenGameplayPanel("none");
        int stage=0, dropId=0, frames=Time.frameCount; float x=fixture.State.player.x,y=fixture.State.player.y;
        double next=EditorApplication.timeSinceStartup+.25;
        EditorApplication.CallbackFunction tick=null;
        Action finish=()=>{
            EditorApplication.update-=tick;
            host.VirtualMove=Vector2.zero;
            host.SetPresentation("owner"); Set(host,"Session",original); Set(host,"LoadError",loadError);
            host.SelectedCell=cell;host.SelectedWild=wildId;
            host.SetPresentation(owner?"owner":"play"); host.OpenGameplayPanel(originalPanel);
            Directory.CreateDirectory("Verification/LogicV2");
            File.WriteAllText("Verification/LogicV2/hud-tests.json",JsonUtility.ToJson(report,true)+"\n");
            Debug.Log("HUD v2: "+report.passed+" passed, "+report.failed+" failed.");
        };
        tick=()=>{
            if(EditorApplication.timeSinceStartup<next)return;
            try {
                if(!EditorApplication.isPlaying||host==null)throw new Exception("Editor stopped during HUD checks.");
                if(stage==0){
                    check(Time.frameCount>frames&&fixture.State.time>0,"Real runtime frames advance the temporary session");
                    check(GameObject.Find("Combat Stats").GetComponent<TMP_Text>().text.Contains("Thủ"),"Controlled actor shows attack/defense/root/speed");
                    check(GameObject.Find("Party 0").GetComponentInChildren<TMP_Text>().text.Contains("60/90"),"Party card displays current and maximum HP");
                    var partyLabel = GameObject.Find("Party 0").GetComponentInChildren<TMP_Text>();
                    partyLabel.ForceMeshUpdate();
                    check(!partyLabel.isTextOverflowing,"Party name and HP both fit the visible card without ellipsis");
                    int health=0;foreach(var text in UnityEngine.Object.FindObjectsByType<TMP_Text>())if(text.name=="Health value"&&text.gameObject.activeInHierarchy&&text.text.Contains("/"))health++;
                    check(health>=2,"Player and summoned beast show world health values");
                    Click("Inner Realm");
                }else if(stage==1){
                    check(fixture.State.inRealm&&host.InputBlocked&&GameObject.Find("realm")!=null,"Realm button enters the realm and opens its grid");
                    Click("Close");
                    check(!fixture.State.inRealm&&!host.InputBlocked&&fixture.State.player.x==x&&fixture.State.player.y==y,"Close exits the realm to the unchanged physical body");
                    
                }else if(stage==2){
                    Click("Bait");
                }else if(stage==3){
                    check(fixture.State.inRealm&&GameObject.Find("realm")!=null,"Drop button opens the realm without a nearby beast");
                    Click("Cell 1");
                }else if(stage==4){
                    check(GameObject.Find("Drop stack controls")!=null&&GameObject.Find("Plant")==null,"Storage selection shows drop controls instead of farming controls");
                    Click("Drop plus");
                }else if(stage==5){
                    check(GameObject.Find("Drop quantity").GetComponent<TMP_Text>().text.Contains("2 / 6"),"Quantity control selects two items from the chosen stack");
                    Click("Drop selected");
                    check(fixture.Count("herb")==4&&fixture.State.drops.Count==1&&fixture.State.drops[0].amount==2,"Drop binding removes exactly the selected quantity");
                    dropId=fixture.State.drops[0].id;Click("Close");
                }else if(stage==6){
                    check(!fixture.State.inRealm&&fixture.PrimaryTarget().kind==PrimaryActionKind.Pickup,"Closing shows dropped items and the primary pickup action");
                    Click("Primary Action");
                    check(fixture.State.drops.Count==0&&fixture.Count("herb")==6&&!fixture.PickUpDrop(dropId).ok,"Primary button picks up the drop exactly once");
                    Click("Inner Realm");
                }else if(stage==7){
                    Click("Walk Realm");check(fixture.State.inRealm&&!host.InputBlocked,"Walk Realm hides the grid while retaining soul movement");
                    host.CloseGameplayPanel();check(!fixture.State.inRealm&&!host.InputBlocked,"Shared Escape/Close handler exits even after hiding the realm grid");
                    host.ToggleRealmView();host.ToggleRealmView();check(!fixture.State.inRealm&&!host.InputBlocked,"G handler enters and exits consistently");
                    fixture.State.lastCombat=fixture.State.time;host.OpenGameplayPanel("inventory");host.ToggleRealmView();
                    check(!fixture.State.inRealm&&GameObject.Find("inventory")!=null,"Rejected realm entry during combat retains the current panel");
                    host.CloseGameplayPanel();host.SetPresentation("owner");frames=Time.frameCount;x=fixture.State.time;
                }else if(stage==8){
                    check(host.OwnerMode&&fixture.State.time==x,"Owner dashboard pauses the new AI and drop simulation");
                    check(fixture.ValidateState()==null,"HUD actions leave a valid persistent state");
                    finish();return;
                }
                stage++;next=EditorApplication.timeSinceStartup+.3;
            }catch(Exception e){check(false,e.Message);finish();Debug.LogException(e);}
        };
        EditorApplication.update+=tick;
        return "HUD v2 checks scheduled. Temporary session is excluded from persistence.";
    }
}
