using System;
using System.IO;
using System.Collections.Generic;
using LinhThu.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>Checks presentation integration across real Editor frames without changing gameplay rules.</summary>
public static class HudTests
{
    [Serializable] private sealed class Report { public int passed, failed; public List<string> cases = new List<string>(); }
    public static string Run()
    {
        var host = UnityEngine.Object.FindAnyObjectByType<PrototypeHost>();
        if (!EditorApplication.isPlaying || host == null || host.Session == null) throw new InvalidOperationException("Play the logic scene first.");
        var report = new Report();
        Action<bool,string> check = (condition,name) => { if(condition)report.passed++;else report.failed++;report.cases.Add((condition?"PASS ":"FAIL ")+name); };
        var ui = EventSystem.current.GetComponent<InputSystemUIInputModule>();
        check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1 && ui.move==null && ui.submit==null && ui.cancel==null, "One EventSystem; gameplay keys cannot also submit/navigate UI");
        var session=host.Session; var actor=session.Controlled; float originalX=actor.x, originalY=actor.y;
        host.SetPresentation("owner"); float time=session.State.time; int stage=0; double next=EditorApplication.timeSinceStartup+.5;
        EditorApplication.CallbackFunction tick=null;
        tick=()=>{
            if(EditorApplication.timeSinceStartup<next)return;
            try {
                if(!EditorApplication.isPlaying || host==null)throw new InvalidOperationException("Play mode stopped during HUD checks.");
                if(stage==0){
                    check(host.OwnerMode&&session.State.time==time&&host.InputBlocked, "Owner dashboard pauses simulation and blocks movement");
                    host.SetPresentation("play");host.OpenGameplayPanel("inventory");host.VirtualMove=Vector2.left;
                    time=session.State.time;stage++;next=EditorApplication.timeSinceStartup+.5;return;
                }
                if(stage==1){
                    check(session.State.time>time&&actor.x==originalX&&actor.y==originalY&&host.InputBlocked, "Player modal blocks movement while simulation continues");
                    host.OpenGameplayPanel("none");
                    var direction=GameObject.Find("Move D").GetComponent<HudHoldDirection>();
                    var data=new PointerEventData(EventSystem.current);
                    direction.OnPointerDown(data);check(host.VirtualMove==Vector2.right,"D-pad pointer down starts movement intent");
                    direction.OnPointerUp(data);check(host.VirtualMove==Vector2.zero,"D-pad pointer up clears movement intent");
                    direction.OnPointerDown(data);direction.OnPointerExit(data);check(host.VirtualMove==Vector2.zero,"Leaving D-pad clears movement intent");
                    check(!host.InputBlocked&&!host.OwnerMode&&ReferenceEquals(host.Session,session), "Return to play retains same session and unblocks input");
                    EditorApplication.update-=tick;
                    Directory.CreateDirectory("Verification/LogicV1");File.WriteAllText("Verification/LogicV1/hud-tests.json",JsonUtility.ToJson(report,true)+"\n");
                    Debug.Log("HUD checks: "+report.passed+" passed, "+report.failed+" failed.");
                }
            }catch(Exception e){ EditorApplication.update-=tick;host.VirtualMove=Vector2.zero;check(false,e.Message);File.WriteAllText("Verification/LogicV1/hud-tests.json",JsonUtility.ToJson(report,true)+"\n");Debug.LogException(e); }
        };
        EditorApplication.update+=tick;
        return "HUD integration checks scheduled across Editor frames.";
    }
}
