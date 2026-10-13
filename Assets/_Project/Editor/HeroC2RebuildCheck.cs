using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NguThu.Editor
{
    /// <summary>Checks that rebuilding C2 cannot discard another open scene's edits.</summary>
    public static class HeroC2RebuildCheck
    {
        const string Trial = "Assets/_Project/Content/Characters/HeroC2/HeroC2Trial.unity";
        const string Report = "ArtDirection/CharacterProduction/experiments/C2/unity-rebuild-check.json";

        [MenuItem("Demo/Character C2/Check rebuild preserves other open scenes")]
        public static void Run()
        {
            var trial = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || SceneManager.sceneCount != 1 || trial.path != Trial || trial.isDirty)
                throw new InvalidOperationException("Open only the saved C2 trial, outside Play mode, before running this check.");
            var other = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var marker = new GameObject("C2 disposable unsaved edit");
            SceneManager.MoveGameObjectToScene(marker, other);
            marker.transform.position = new Vector3(3, 7, 11);
            EditorSceneManager.MarkSceneDirty(other);
            bool rejected = false;
            try
            {
                try { HeroC2Builder.Build(); }
                catch (InvalidOperationException) { rejected = true; }
                bool preserved = other.IsValid() && other.isLoaded && other.isDirty && marker != null
                    && marker.transform.position == new Vector3(3, 7, 11);
                var report = new JObject { ["rebuild_rejected"] = rejected, ["other_scene_and_unsaved_edit_preserved"] = preserved };
                File.WriteAllText(Report, report.ToString() + "\n");
                if (!rejected || !preserved) throw new Exception("C2 rebuild must refuse without closing another scene or losing its unsaved edit.");
                Debug.Log("C2 rebuild check passed: the other scene and its unsaved edit are preserved.");
            }
            finally
            {
                if (other.IsValid() && other.isLoaded) EditorSceneManager.CloseScene(other, true);
                var restored = SceneManager.GetSceneByPath(Trial);
                if (restored.IsValid() && restored.isLoaded) SceneManager.SetActiveScene(restored);
            }
        }
    }
}
