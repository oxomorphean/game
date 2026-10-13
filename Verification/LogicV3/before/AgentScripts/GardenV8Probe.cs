using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LinhThu.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Observes actual animation over Editor frames and restores the player's location after movement checks.</summary>
public static class GardenV8Probe
{
    [Serializable] private sealed class Report { public int passed, failed; public List<string> cases = new List<string>(); public string[] glintFrames, humanWalkFrames; public float elapsed; }
    public static string Run()
    {
        var host = UnityEngine.Object.FindAnyObjectByType<PrototypeHost>();
        var garden = UnityEngine.Object.FindAnyObjectByType<PrototypeGardenAmbient>();
        if (!EditorApplication.isPlaying || host == null || host.Session == null || garden == null)
            throw new InvalidOperationException("Play GardenV8 logic scene first.");
        var images = UnityEngine.Object.FindObjectsByType<Image>();
        var glint = images.Single(x => x.name == "Pond glint 0");
        var petal = images.Single(x => x.name == "Falling cherry petal 0");
        var human = images.Single(x => x.name == "Sprite" && x.sprite != null && x.sprite.name.StartsWith("cultivator-"));
        var session = host.Session;
        var actor = session.State.player; float x = actor.x, y = actor.y;
        float start = garden.VisualTime; Vector2 first = petal.rectTransform.anchoredPosition;
        var report = new Report(); var glints = new HashSet<string>(); var walks = new HashSet<string>();
        Action<bool, string> check = (ok, name) => { if (ok) report.passed++; else report.failed++; report.cases.Add((ok ? "PASS " : "FAIL ") + name); };
        check(garden.PetalCount == 14 && garden.RippleCount == 3, "14 reused petals and 3 pond glints initialized");
        host.OpenGameplayPanel("none"); host.SetPresentation("play");
        int control = session.State.controlledBeast; session.State.controlledBeast = 0;
        host.VirtualMove = actor.x > 12 ? Vector2.left : Vector2.right;
        int initialFrame = Time.frameCount;
        EditorApplication.CallbackFunction tick = null;
        tick = () => {
            try
            {
                if (!EditorApplication.isPlaying || host == null) throw new InvalidOperationException("Play stopped during garden checks.");
                glints.Add(glint.sprite.name);
                if (int.Parse(human.sprite.name.Substring("cultivator-".Length)) >= 24) walks.Add(human.sprite.name);
                float elapsed = garden.VisualTime - start;
                if (elapsed >= 1 && host.VirtualMove != Vector2.zero)
                {
                    check(Mathf.Abs(actor.x - x) > .1f, "Player moves through unchanged command/input loop");
                    host.VirtualMove = Vector2.zero;
                }
                if (elapsed < 1.7f) return;
                check(Time.frameCount > initialFrame + 10 && elapsed > 1.7f, "Player and ambient visual clocks advance across frames");
                check(petal.rectTransform.anchoredPosition != first, "Cherry petals fall/drift instead of static decoration");
                check(glints.Count == 4, "All four existing pond glint frames render in the loop");
                check(walks.Count >= 3, "Human directional walk advances through multiple frames");
                check(human.sprite.name == "cultivator-20" || human.sprite.name == "cultivator-21" || human.sprite.name == "cultivator-22" || human.sprite.name == "cultivator-23", "Stopping movement returns human to side idle");
                report.glintFrames = glints.OrderBy(v => v).ToArray(); report.humanWalkFrames = walks.OrderBy(v => v).ToArray(); report.elapsed = elapsed;
                actor.x = x; actor.y = y; session.State.controlledBeast = control; host.Save();
                EditorApplication.update -= tick;
                Directory.CreateDirectory("Verification/LogicV1/GardenV8");
                File.WriteAllText("Verification/LogicV1/GardenV8/runtime-probe.json", JsonUtility.ToJson(report, true) + "\n");
                Debug.Log("GardenV8: " + report.passed + " passed, " + report.failed + " failed.");
            }
            catch (Exception e)
            {
                EditorApplication.update -= tick; host.VirtualMove = Vector2.zero; actor.x = x; actor.y = y; session.State.controlledBeast = control;
                check(false, e.Message); Directory.CreateDirectory("Verification/LogicV1/GardenV8");
                File.WriteAllText("Verification/LogicV1/GardenV8/runtime-probe.json", JsonUtility.ToJson(report, true) + "\n"); Debug.LogException(e);
            }
        };
        EditorApplication.update += tick;
        return "Garden animation checks scheduled across real frames; player location restored after check.";
    }
}
