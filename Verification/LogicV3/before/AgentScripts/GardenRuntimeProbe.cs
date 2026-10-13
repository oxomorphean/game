using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinhThu.ArtSample;
using UnityEditor;
using UnityEngine;

/// <summary>Checks visible motion, sprite frames, pixel alignment and renderer reuse in Play Mode.</summary>
public static class GardenRuntimeProbe
{
    [Serializable] private sealed class Evidence
    {
        public string result;
        public int petals, ripples, renderers, movedPetals, rippleFrames, missingSprites, offGrid;
        public bool hiddenWhenDisabled, restoredWhenEnabled, poolCountStable;
        public float elapsedVisualTime;
        public string error;
    }

    public static string Begin()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required.");
        var garden = UnityEngine.Object.FindAnyObjectByType<LivingGarden>();
        if (garden == null) throw new InvalidOperationException("LivingGarden not found.");
        bool previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        var initial = garden.GetComponentsInChildren<SpriteRenderer>(true);
        var positions = initial.ToDictionary(renderer => renderer.name, renderer => renderer.transform.position);
        var frames = new HashSet<string>();
        double start = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                var renderers = garden.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var renderer in renderers.Where(renderer => renderer.name.StartsWith("Pond ripple ")))
                    if (renderer.sprite != null) frames.Add(renderer.sprite.name);
                if (EditorApplication.timeSinceStartup - start < 1.5) return;
                int missing = renderers.Count(renderer => renderer.sprite == null);
                int offGrid = renderers.Count(renderer =>
                    Mathf.Abs(renderer.transform.position.x * 16 - Mathf.Round(renderer.transform.position.x * 16)) > 0.0001f
                    || Mathf.Abs(renderer.transform.position.y * 16 - Mathf.Round(renderer.transform.position.y * 16)) > 0.0001f);
                int moved = renderers.Count(renderer => renderer.name.StartsWith("Blossom petal ")
                    && (renderer.transform.position - positions[renderer.name]).sqrMagnitude > 0.001f);
                bool hidden;
                bool restored;
                garden.enabled = false;
                try { hidden = renderers.All(renderer => !renderer.enabled); }
                finally { garden.enabled = true; }
                restored = renderers.All(renderer => renderer.enabled);
                bool stable = garden.GetComponentsInChildren<SpriteRenderer>(true).Length == initial.Length;
                var evidence = new Evidence {
                    petals = garden.ActivePetalCount, ripples = garden.ActiveRippleCount, renderers = renderers.Length,
                    movedPetals = moved, rippleFrames = frames.Count, missingSprites = missing, offGrid = offGrid,
                    hiddenWhenDisabled = hidden, restoredWhenEnabled = restored, poolCountStable = stable,
                    elapsedVisualTime = garden.ElapsedVisualTime, error = ""
                };
                evidence.result = evidence.petals == 14 && evidence.ripples == 3 && evidence.renderers == 17
                    && moved == 14 && frames.Count == 4 && missing == 0 && offGrid == 0 && hidden && restored && stable
                    ? "PASS" : "FAIL";
                File.WriteAllText("ArtDirection/ArtSample/ambient-verification-v7.json", JsonUtility.ToJson(evidence, true));
                Application.runInBackground = previousBackground;
                EditorApplication.update -= tick;
            }
            catch (Exception error)
            {
                EditorApplication.update -= tick;
                Application.runInBackground = previousBackground;
                File.WriteAllText("ArtDirection/ArtSample/ambient-verification-v7.json",
                    JsonUtility.ToJson(new Evidence { result = "FAIL", error = error.ToString() }, true));
            }
        };
        EditorApplication.update += tick;
        return "Garden runtime probe started.";
    }
}
