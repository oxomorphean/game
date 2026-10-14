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
        [MenuItem("Demo/Mỹ thuật/Kiểm tra bóng dùng chung")]
        public static void CheckUnifiedShadows()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before the isolated GPU checks.");
            var passed = new List<string>();
            void Check(string name, bool condition) { if (!condition) throw new InvalidOperationException(name); passed.Add(name); }
            string folder = "Verification/UnifiedShadows/";
            Directory.CreateDirectory(folder);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Art/DaylightShadow.mat");
            var hero = AssetDatabase.LoadAssetAtPath<DirectionalHeroDefinition>(ArtPreviewBuilder.Hero);
            var modular = AssetDatabase.LoadAssetAtPath<ModularHeroDefinition>("Assets/_Project/Content/Characters/ModularHeroFront.asset");
            var authored = AssetDatabase.LoadAssetAtPath<GameObject>(HeroProductionBuilder.Prefab).GetComponent<AuthoredHeroView>();
            var props = AssetDatabase.LoadAssetAtPath<GameObject>(ArtPreviewBuilder.Map).GetComponentsInChildren<ModularMapProp>().Where(p => !p.floorLayer).ToArray();
            Check("Props and both layered hero paths use the shared material", props.All(p => p.castShadow.sharedMaterial == material)
                && modular.shadowMaterial == material && authored.allShadows.All(s => s.sharedMaterial == material));
            Check("Standing props have authored geometry and no legacy ellipse", props.All(p => p.shadowSettings.depth > 0 && p.shadowSettings.contactHeight > 0 && p.contactShadow == null));
            Check("Hero geometry is authored on its definition", hero.shadowSettings.depth > 0 && hero.shadowReferenceHeight > 0);
            foreach (string shader in new[] { "LinhThu/Daylight Shadow", "LinhThu/Modular Hero Shadow" })
                Check("Shader compiles: " + shader, !ShaderUtil.ShaderHasError(Shader.Find(shader)));

            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Isolated sun-shadow GPU check");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var cameraNode = new GameObject("Shadow check camera"); cameraNode.transform.SetParent(root.transform, false);
            var camera = cameraNode.AddComponent<Camera>(); camera.enabled = false; camera.scene = scene;
            camera.orthographic = true; camera.orthographicSize = 2.5f; camera.transform.position = new Vector3(0,0,-10);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white;
            var target = new RenderTexture(512,512,24); camera.targetTexture = target;
            var texture = new Texture2D(32,64,TextureFormat.RGBA32,false);
            texture.SetPixels(Enumerable.Repeat(Color.white, 32*64).ToArray()); texture.Apply();
            var sprite = Sprite.Create(texture,new Rect(0,0,32,64),new Vector2(.5f,0),64);
            var silhouette = new GameObject("Caster").AddComponent<SpriteRenderer>(); silhouette.transform.SetParent(root.transform,false);
            silhouette.sprite = sprite; silhouette.sharedMaterial = material;
            var shadow = new SunShadowMesh(silhouette);
            var readback = new Texture2D(512,512,TextureFormat.RGBA32,false);
            var oldTarget = RenderTexture.active;
            var samples = new List<object>();
            var settings = hero.shadowSettings;
            float referenceHeight = 1;
            Color32[] Render(float hour)
            {
                DaylightPresentation.ApplyLighting(hour);
                shadow.Refresh(settings, Vector3.zero, referenceHeight);
                camera.Render(); RenderTexture.active = target;
                readback.ReadPixels(new Rect(0,0,512,512),0,0); readback.Apply();
                return readback.GetPixels32();
            }
            (int area, float x, int width) Metrics(Color32[] pixels)
            {
                int area=0, min=512, max=-1; float sum=0;
                for (int i=0;i<pixels.Length;i++) if (pixels[i].r < 245)
                { int x=i%512; area++; sum+=x; min=Mathf.Min(min,x); max=Mathf.Max(max,x); }
                return (area, area==0 ? 0 : sum/area, max-min+1);
            }
            try
            {
                var morning=Metrics(Render(8)); var noon=Metrics(Render(12)); var evening=Metrics(Render(16));
                Check("Rendered morning and afternoon shadows lie on opposite sides", morning.x > noon.x+12 && evening.x < noon.x-12);
                Check("Rendered noon footprint has area and is shorter than low-sun shadows", noon.area > 500 && noon.width < morning.width && noon.width < evening.width);
                for (int step=0;step<=48;step++)
                {
                    float hour=6+step*.25f; var pixels=Render(hour); var m=Metrics(pixels);
                    Check("Ground contact and nonzero footprint at " + hour.ToString("0.00"), m.area > 500 && pixels[256*512+256].r < 245);
                    samples.Add(new {hour,m.area,m.x,m.width});
                }
                var single=Render(12);
                var duplicate = new GameObject("Overlapping layer").AddComponent<SpriteRenderer>(); duplicate.transform.SetParent(root.transform,false);
                duplicate.sprite=sprite; duplicate.sharedMaterial=material;
                using (var second = new SunShadowMesh(duplicate))
                {
                    second.Refresh(hero.shadowSettings,Vector3.zero,1);
                    var combined=Render(12);
                    Check("Overlapping clothing layers do not double shadow opacity", single.Select((pixel,i)=>pixel.Equals(combined[i])).All(v=>v));
                }
                silhouette.flipX=true;
                var mirrored=Metrics(Render(8));
                Check("Mirroring the caster does not reverse the sun direction", Mathf.Abs(mirrored.x-morning.x) < 1);
                silhouette.flipX = false; camera.orthographicSize = 1.5f;
                settings = new SunShadowSettings { heightScale=1, width=1.3f, depth=.64f, center=.49f, contactHeight=.10f, baseCutoff=-1, canopyBlueRatio=.65f };
                void ColorTexture(Color32 color)
                {
                    texture.SetPixels32(Enumerable.Repeat(color, texture.width * texture.height).ToArray()); texture.Apply();
                }
                ColorTexture(new Color32(145, 92, 54, 255)); var woodPixels = Render(8);
                ColorTexture(new Color32(20, 33, 42, 255)); var outlinePixels = Render(8);
                ColorTexture(new Color32(0, 0, 0, 0)); var trunkPixels = Render(8);
                Check("Warm roots do not enter the foliage shadow", woodPixels.Select((p,i) => p.Equals(trunkPixels[i])).All(v => v));
                Check("Dark root outlines do not enter the foliage shadow", outlinePixels.Select((p,i) => p.Equals(trunkPixels[i])).All(v => v));
                Check("Elevated canopy keeps an independent foot-to-crown trunk caster", Metrics(trunkPixels).area > 10);
                var tree = props.First(p => p.visual.sprite.name == "cherry-tree");
                silhouette.sprite = tree.visual.sprite; silhouette.flipX = false;
                silhouette.transform.localScale = tree.visual.transform.lossyScale;
                referenceHeight = tree.visual.sprite.rect.height / tree.visual.sprite.pixelsPerUnit * Mathf.Abs(tree.visual.transform.lossyScale.y);
                settings = tree.shadowSettings; camera.orthographicSize = 4.5f;
                Directory.CreateDirectory("Verification/TreeGrounding");
                var treeSamples = new List<object>();
                foreach (float hour in new[] { 8f, 11.5f, 12f, 16f })
                {
                    var pixels = Render(hour); var m = Metrics(pixels);
                    int longest = 0, run = 0, previousBottom = -100;
                    for (int x = 0; x < 512; x++)
                    {
                        int bottom = -1;
                        for (int y = 0; y < 512; y++)
                            if (pixels[y * 512 + x].r < 245) { bottom = y; break; }
                        run = bottom >= 0 && bottom == previousBottom ? run + 1 : 1;
                        longest = Mathf.Max(longest, bottom >= 0 ? run : 0); previousBottom = bottom;
                    }
                    var visited = new bool[pixels.Length];
                    var queue = new Queue<int>();
                    int mainArea = 0, nearestFootSquared = int.MaxValue;
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        if (visited[i] || pixels[i].r >= 245) continue;
                        visited[i] = true; queue.Enqueue(i);
                        int area = 0, nearest = int.MaxValue;
                        while (queue.Count > 0)
                        {
                            int current = queue.Dequeue(), x = current % 512, y = current / 512;
                            area++; nearest = Mathf.Min(nearest, (x - 256) * (x - 256) + (y - 256) * (y - 256));
                            void Enqueue(int neighbour)
                            {
                                if (!visited[neighbour] && pixels[neighbour].r < 245)
                                { visited[neighbour] = true; queue.Enqueue(neighbour); }
                            }
                            if (x > 0) Enqueue(current - 1); if (x < 511) Enqueue(current + 1);
                            if (y > 0) Enqueue(current - 512); if (y < 511) Enqueue(current + 512);
                        }
                        if (area > mainArea) { mainArea = area; nearestFootSquared = nearest; }
                    }
                    float footGap = Mathf.Sqrt(nearestFootSquared) * camera.orthographicSize * 2 / 512;
                    treeSamples.Add(new { hour, m.area, m.width, horizontalBottomRun = longest, mainArea, footGap });
                    File.WriteAllBytes("Verification/TreeGrounding/tree-" + hour.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png", readback.EncodeToPNG());
                    File.WriteAllText("Verification/TreeGrounding/shadow-checks.json", Newtonsoft.Json.JsonConvert.SerializeObject(treeSamples, Newtonsoft.Json.Formatting.Indented) + "\n");
                    Check("Real tree silhouette has no broad horizontal bottom edge at " + hour.ToString("0.0"), longest < m.width * .10f);
                    Check("Main tree shadow remains joined to the root region at " + hour.ToString("0.0"), mainArea > m.area * .95f && footGap < .12f);
                }
                File.WriteAllText("Verification/TreeGrounding/shadow-checks.json", Newtonsoft.Json.JsonConvert.SerializeObject(treeSamples, Newtonsoft.Json.Formatting.Indented) + "\n");
                File.WriteAllText(folder+"checks.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {passed=passed.Count,checks=passed,samples},Newtonsoft.Json.Formatting.Indented)+"\n");
            }
            finally
            {
                RenderTexture.active=oldTarget; camera.targetTexture=null;
                shadow.Dispose(); UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(readback); target.Release(); UnityEngine.Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(scene); DaylightPresentation.ApplyLighting(12);
            }
            Debug.Log($"Unified shadows: {passed.Count} checks passed, including GPU projection over the whole day.");
        }

        [MenuItem("Demo/Mỹ thuật/Kiểm tra gốc anh đào")]
        public static void CheckTreeGrounding()
        {
            var passed = new List<string>();
            void Check(string name, bool condition) { if (!condition) throw new InvalidOperationException(name); passed.Add(name); }
            foreach (string path in new[] { ModularMapBuilder.PrefabRoot + "cherry-tree.prefab", ArtPreviewBuilder.Map })
            {
                var trees = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<ModularMapProp>()
                    .Where(p => p.visual.sprite.name == "cherry-tree").ToArray();
                Check("Tree assets exist: " + path, trees.Length > 0);
                foreach (var tree in trees)
                {
                    var shape = tree.GetComponent<ObstacleAuthoring>().Definition();
                    WorldPoint P(float x, float y)
                    { var world = tree.transform.TransformPoint(new Vector3(x, y)); return new WorldPoint(world.x, world.y); }
                    string label = path + " / " + tree.name;
                    Check("Left and right roots block the player's foot: " + label, shape.Overlaps(P(-.43f, -.10f), .28f) && shape.Overlaps(P(.51f, -.08f), .28f));
                    Check("Trunk origin is inside the ground footprint: " + label, shape.Overlaps(P(0, 0), .01f));
                    Check("The player can still walk under the outer canopy: " + label, !shape.Overlaps(P(-1.1f, .75f), .28f) && !shape.Overlaps(P(1.2f, .75f), .28f));
                    var arena = new MovementArena(new WorldPoint(100, 100), new[] { shape });
                    var position = P(0, -1.5f);
                    for (int step = 0; step < 80; step++)
                    {
                        position = arena.Move(position, new WorldPoint(0, .03f), .28f, false);
                        if (!arena.CanStand(position, .28f)) throw new InvalidOperationException("Walking entered the tree roots: " + label);
                    }
                    Check("Approaching from below stops before the roots: " + label, position.Y < P(0, -.25f).Y);
                }
            }
            Directory.CreateDirectory("Verification/TreeGrounding");
            File.WriteAllText("Verification/TreeGrounding/collision-checks.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { passed = passed.Count, checks = passed }, Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log($"Tree grounding: {passed.Count} collision checks passed.");
        }

        [MenuItem("Demo/Mỹ thuật/Kiểm tra nắng và bờ hồ")]
        public static void CheckSunAndShore()
        {
            var passed = new List<string>();
            void Check(string name, bool condition) { if (!condition) throw new InvalidOperationException(name); passed.Add(name); }
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(ArtPreviewBuilder.Map);
            var pond = root.GetComponentsInChildren<ModularMapProp>().Single(p => p.visual.sprite.name == "pond-cluster");
            var shape = pond.GetComponent<ObstacleAuthoring>().Definition();
            var center = pond.transform.position;
            WorldPoint P(float x, float y) => new WorldPoint(center.x + x, center.y + y);
            Check("Player feet can reach the north shore without a gap", !shape.Overlaps(P(0, .87f), .28f));
            Check("Water and the low north bank remain impassable", shape.Overlaps(P(0, .72f), .28f) && shape.Overlaps(P(0, 0), .28f));
            var arena = new MovementArena(new WorldPoint(10, 6), new[] { shape });
            var position = P(0, 1.4f);
            for (int i = 0; i < 25; i++) position = arena.Move(position, new WorldPoint(0, -.025f), .28f, false);
            Check("Approaching the north bank stops within 0.9 world units of pond origin", position.Y - center.y < .9f && arena.CanStand(position, .28f));
            DaylightPresentation.ApplyLighting(8);
            Check("Morning sun is left and casts right", DaylightPresentation.SunHorizontal < 0 && DaylightPresentation.ShadowDirection.x > 0);
            float morningLength = DaylightPresentation.ShadowLength;
            DaylightPresentation.ApplyLighting(12);
            var noonColor = DaylightPresentation.LightTint;
            Check("Noon shadow has no lateral throw", DaylightPresentation.ShadowLength < .001f && DaylightPresentation.Overhead > .999f);
            DaylightPresentation.ApplyLighting(17);
            Check("Afternoon sun is right and casts left", DaylightPresentation.SunHorizontal > 0 && DaylightPresentation.ShadowDirection.x < 0 && DaylightPresentation.ShadowLength > morningLength);
            Check("Sunset warms both props and water reflection", DaylightPresentation.GoldenHour > .8f && DaylightPresentation.LightTint.b < noonColor.b);
            foreach (string name in new[] { "LinhThu/Daylight Sprite", "LinhThu/Daylight Shadow", "LinhThu/Meadow Terrain" })
            {
                var shader = Shader.Find(name);
                Check("Shader compiles: " + name, shader != null && !ShaderUtil.ShaderHasError(shader));
            }
            DaylightPresentation.ApplyLighting(12);
            Directory.CreateDirectory("Verification/SunAndShore");
            File.WriteAllText("Verification/SunAndShore/checks.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                passed = passed.Count, northShoreStop = position.Y - center.y, checks = passed
            }, Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log($"Sun and shore: {passed.Count} checks passed.");
        }

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
            var host = UnityEngine.Object.FindAnyObjectByType<ArtPreviewHost>();
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
