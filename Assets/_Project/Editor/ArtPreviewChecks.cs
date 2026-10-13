using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinhThu.Domain.Navigation;
using LinhThu.Prototype;
using LinhThu.Prototype.Characters;
using LinhThu.Prototype.Preview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LinhThu.Editor
{
    /// <summary>Focused regression checks for retained movement, scene bindings and art-only runtime.</summary>
    public static class ArtPreviewChecks
    {
        [MenuItem("Demo/Quản trị/Kiểm tra danh mục")]
        public static void CheckOwnerCatalog()
        {
            OwnerCatalogBuilder.Generate();
            var catalog = JsonUtility.FromJson<OwnerCatalog>(File.ReadAllText(OwnerCatalogBuilder.Output));
            var passed = new List<string>();
            void Check(string name, bool value) { if (!value) throw new InvalidOperationException(name); passed.Add(name); }
            Check("Each asset has a unique path and GUID", catalog.assets.Length == catalog.assets.Select(a => a.path).Distinct().Count()
                && catalog.assets.Length == catalog.assets.Select(a => a.id).Distinct().Count());
            Check("Every catalogue item resolves to its file and GUID", catalog.assets.All(a => File.Exists(a.path)
                && AssetDatabase.AssetPathToGUID(a.path) == a.id && new FileInfo(a.path).Length == a.bytes));
            var pngs = OwnerCatalogBuilder.Roots.SelectMany(r => Directory.GetFiles(r, "*.png", SearchOption.AllDirectories)).ToArray();
            Check("All source PNG files appear once in the image group", pngs.Length == catalog.assets.Count(a => a.group == "image" && a.path.EndsWith(".png", StringComparison.Ordinal)));
            Check("Catalogue includes C2 trial without marking it as the main player", catalog.Search("HeroC2", "all", false).Length > 0 && catalog.Search("HeroC2", "all", true).Length == 0);
            Check("Search accepts whitespace and case-insensitive paths", catalog.Search("  hErOc2  ", "all", false).Length == catalog.Search("HeroC2", "all", false).Length);
            Check("An absent search returns an empty collection", catalog.Search("__missing_asset_check__", "all", false).Length == 0);
            Check("Map metadata matches the saved prefabs", catalog.maps.Length > 0 && catalog.maps.All(m => {
                var map = AssetDatabase.LoadAssetAtPath<GameObject>(m.path).GetComponent<ModularMapLayout>();
                return map.size == new Vector2(m.width, m.height) && map.GetComponentsInChildren<ModularMapProp>(true).Length == m.props;
            }));
            Directory.CreateDirectory("Verification/OwnerDashboard");
            File.WriteAllText("Verification/OwnerDashboard/catalog-checks.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                passed = passed.Count, assets = catalog.assets.Length, maps = catalog.maps.Length, checks = passed
            }, Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log($"Owner dashboard: {passed.Count} catalogue checks passed; {catalog.assets.Length} assets and {catalog.maps.Length} maps.");
        }

        [MenuItem("Demo/Quản trị/Ghi trạng thái kiểm tra trong Play")]
        public static void CaptureOwnerState()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play in ArtPreview first.");
            var host = UnityEngine.Object.FindFirstObjectByType<ArtPreviewHost>();
            var dashboard = host.GetComponent<OwnerDashboard>();
            var texts = dashboard.GetComponentsInChildren<TMPro.TMP_Text>().Where(t => t.isActiveAndEnabled).Select(t => t.text).ToArray();
            Directory.CreateDirectory("Verification/OwnerDashboard");
            File.AppendAllText("Verification/OwnerDashboard/interaction.jsonl", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                utc = DateTime.UtcNow, dashboard = dashboard.IsOpen, page = dashboard.CurrentPage,
                x = host.Movement.Position.X, y = host.Movement.Position.Y, inputBlocked = host.OwnerDashboardOpen, texts
            }) + "\n");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Verification/OwnerDashboard/editor-" + dashboard.CurrentPage + (dashboard.IsOpen ? "-open" : "-closed") + ".png"));
            Debug.Log("Owner dashboard interaction state recorded.");
        }

        public static object Run()
        {
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Stop Play and save the current scene before running scene checks.");
            var passed = new List<string>();
            void Check(string name, bool condition)
            { if (!condition) throw new InvalidOperationException(name); passed.Add(name); }
            WorldPoint P(float x, float y = 0) => new WorldPoint(x, y);
            var empty = new MovementArena(P(10, 6), Array.Empty<WorldObstacle>());
            var walk = new PreviewMovement(empty, P(0), 3, .28f);
            walk.Step(P(1, 1), .1f);
            Check("Diagonal movement remains normalized", Math.Abs(walk.Position.Length - .3f) < .0001f);
            walk.Step(P(0), .1f);
            Check("Idle has zero displacement", walk.Displacement.LengthSquared == 0);
            walk.Reset();
            Check("Reset returns to spawn", walk.Position.LengthSquared == 0 && walk.Displacement.LengthSquared == 0);
            bool invalid = false;
            try { walk.Step(P(float.NaN), .02f); } catch (ArgumentException) { invalid = true; }
            Check("Invalid movement cannot corrupt position", invalid && walk.Position.Finite);
            var circle = new WorldObstacle(P(0), P(1, .2f), false);
            Check("Circle broad-phase uses its radius on both axes", circle.Overlaps(P(0, .85f), .05f));
            Check("Circle line query uses the same footprint", circle.Crosses(P(-2, .8f), P(2, .8f)));
            var arena = new MovementArena(P(10, 6), new[] { circle });
            var position = P(0, 2);
            for (int i = 0; i < 130; i++)
            {
                position = arena.Move(position, P(0, -.05f), .28f, true);
                if (!arena.CanStand(position, .28f)) throw new InvalidOperationException("Walking entered the rock.");
            }
            Check("Holding down slides around the rock", position.Y < -1.4f);
            var wall = new MovementArena(P(10, 6), new[] { new WorldObstacle(P(0), P(.03f, 5), true) });
            var stopped = wall.Move(P(-2), P(5), .28f, true);
            Check("Thin walls block a long movement step", stopped.X < -.30f && wall.CanStand(stopped, .28f));
            var boundary = new PreviewMovement(empty, P(9.7f, 5.7f), 3, .28f);
            for (int i=0; i<60; i++) boundary.Step(P(1,1), .05f);
            Check("Map boundary keeps the whole foot inside", boundary.Position.X <= 9.7201f && boundary.Position.Y <= 5.7201f);
            var expected = new[] { P(1), P(1,1), P(0,1), P(-1,1), P(-1), P(-1,-1), P(0,-1), P(1,-1) };
            Check("Facing covers all eight movement directions", expected.Select((v,i) => DirectionalHeroView.Facing(v,6)==i).All(v=>v));
            Check("Idle preserves facing", DirectionalHeroView.Facing(P(0),3)==3);
            Check("Combat and progression assemblies no longer supply runtime types", typeof(PreviewMovement).Assembly.GetTypes().Where(t => t.FullName.StartsWith("LinhThu.", StringComparison.Ordinal)).All(t => t.Namespace == "LinhThu.Domain.Navigation"));
            var hero = AssetDatabase.LoadAssetAtPath<DirectionalHeroDefinition>(ArtPreviewBuilder.Hero);
            hero.Validate();
            Check("Idle and run frames exist in all eight rows", new[] { HeroMotion.Idle, HeroMotion.Run }.All(m => Enumerable.Range(0,8).All(d => hero.Clip(m).Sample(d,0)!=null)));
            foreach (string scene in new[] { ArtPreviewBuilder.Scene, ModularHeroBuilder.Scene, "Assets/_Project/Content/Scenes/CartoonArtTrial.unity" })
            {
                var opened = EditorSceneManager.OpenScene(scene);
                Check("No missing scripts: " + Path.GetFileName(scene), opened.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0));
            }
            EditorSceneManager.OpenScene(ArtPreviewBuilder.Scene);
            var host = UnityEngine.Object.FindAnyObjectByType<ArtPreviewHost>();
            var layout = host.map;
            var sceneArena = new MovementArena(P(layout.size.x/2,layout.size.y/2), layout.GetComponentsInChildren<ObstacleAuthoring>().Select(o=>o.Definition()));
            Check("Preview spawn is outside authored footprints", sceneArena.CanStand(P(host.spawn.x,host.spawn.y),host.footRadius));
            Check("Editable ground/path and props remain", layout.GetComponentInChildren<MeadowGroundSurface>()!=null && layout.GetComponentInChildren<MeadowTerrainPath>()!=null && layout.GetComponentsInChildren<ModularMapProp>().Length==21);
            var result = new { passed = passed.Count, checks = passed.ToArray() };
            Directory.CreateDirectory("Verification/ArtOnly");
            File.WriteAllText("Verification/ArtOnly/checks.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented)+"\n");
            return result;
        }
    }
}
