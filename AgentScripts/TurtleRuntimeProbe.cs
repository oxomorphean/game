using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LinhThu.ArtSample;
using UnityEditor;
using UnityEngine;

public static class TurtleRuntimeProbe
{
    [Serializable] private sealed class Evidence
    {
        public string result;
        public string[] idleFrames;
        public string[] walkFrames;
        public bool moved;
        public bool resetToSpawn;
        public string error;
    }

    public static string Begin()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required.");
        var pet = GameObject.Find("Pet").GetComponent<SpiritTurtleController>();
        var visual = pet.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Jade spirit turtle");
        pet.ResetPet();
        Application.runInBackground = true;
        Vector3 spawn = pet.transform.position;
        var idle = new HashSet<string>();
        var walk = new HashSet<string>();
        double start = EditorApplication.timeSinceStartup;
        bool walking = false;
        bool moved = false;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                double elapsed = EditorApplication.timeSinceStartup - start;
                if (!walking && elapsed < 1.5) { idle.Add(visual.sprite.name); return; }
                if (!walking)
                {
                    walking = true;
                    start = EditorApplication.timeSinceStartup;
                    typeof(SpiritTurtleController).GetField("destination", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(pet, new Vector2(3.5f, -3f));
                    typeof(SpiritTurtleController).GetField("followingPointer", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(pet, true);
                    return;
                }
                if (pet.IsMoving) walk.Add(visual.sprite.name);
                moved |= (pet.transform.position - spawn).sqrMagnitude > 0.05f;
                if (elapsed < 1.3) return;
                pet.ResetPet();
                bool reset = (pet.transform.position - spawn).sqrMagnitude < 0.000001f && !pet.IsMoving && !visual.flipX;
                var evidence = new Evidence { result = idle.Count == 4 && walk.Count == 6 && moved && reset ? "PASS" : "FAIL",
                    idleFrames = idle.OrderBy(n => n).ToArray(), walkFrames = walk.OrderBy(n => n).ToArray(),
                    moved = moved, resetToSpawn = reset };
                File.WriteAllText("ArtDirection/ArtSample/animation-verification-v4.json", JsonUtility.ToJson(evidence, true));
                EditorApplication.update -= tick;
            }
            catch (Exception error)
            {
                EditorApplication.update -= tick;
                File.WriteAllText("ArtDirection/ArtSample/animation-verification-v4.json",
                    JsonUtility.ToJson(new Evidence { result = "FAIL", error = error.ToString() }, true));
            }
        };
        EditorApplication.update += tick;
        return "Runtime probe started; evidence in animation-verification-v4.json.";
    }
}
