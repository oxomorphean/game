using System;
using System.Linq;
using LinhThu.Client.Bootstrap;
using LinhThu.Client.Environment;
using LinhThu.Client.World;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;
using LinhThu.Domain.Core;
using LinhThu.Domain.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LinhThu.Tests.ClientEditMode
{
    public sealed class CollisionAndHudTests
    {
        private Scene scene;
        private GameBootstrapper game;
        [SetUp] public void Open()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Content/Scenes/MainGame.unity");
            game = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameBootstrapper>()).Single();
            // These long motor/HUD fixtures do not simulate combat; hunting has its own integration tests.
            game.wildBeastSpawns = Array.Empty<WildBeastSpawnAuthoring>(); game.Initialize();
        }
        [TearDown] public void Close() { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }
        private static WorldPoint P(Vector3 p) => new WorldPoint(p.x, p.y);
        private void Place(WorldPoint point)
        {
            typeof(Transform2D).GetProperty(nameof(Transform2D.Position)).SetValue(game.World.PlayerActor.Transform, point);
            typeof(Transform2D).GetProperty(nameof(Transform2D.CurrentVelocity)).SetValue(game.World.PlayerActor.Transform, default(WorldPoint));
        }

        [TestCase(4)]
        [TestCase(10)]
        [TestCase(16)]
        public void InventoryCollapsesWhenItCoversThePlayerAndCanBeToggled(int rows)
        {
            ArrangeHudPixels();
            var layout = game.hud.Layout;
            layout.FitInventory(rows); Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4]; layout.InventoryPanel.GetWorldCorners(corners);
            var screen = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * .5f);
            var world = game.gameCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -game.gameCamera.transform.position.z));
            var center = game.playerView.VisualBounds.center;
            game.playerView.transform.position += world - center;
            layout.AvoidCoveringActor(game.playerView, game.gameCamera);
            Assert.That(layout.InventoryCollapsed, Is.True);
            Assert.That(layout.InventoryScroll.viewport.gameObject.activeSelf, Is.False);
            game.playerView.transform.position = Vector3.zero;
            layout.AvoidCoveringActor(game.playerView, game.gameCamera);
            Assert.That(layout.InventoryCollapsed, Is.False);
            layout.InventoryToggle.onClick.Invoke();
            Assert.That(layout.InventoryCollapsed, Is.True);
            layout.InventoryToggle.onClick.Invoke();
            Assert.That(layout.InventoryCollapsed, Is.False);
        }

        [TestCase(0f)]
        [TestCase(.5f)]
        public void PondAndFieldPromptsStayAboveTheObjectAndClearOfPlayerSilhouette(float canvasPivot)
        {
            ArrangeHudPixels(canvasPivot);
            foreach (var kind in new[] { WorldNodeKind.FishingSpot, WorldNodeKind.SpiritFieldPlot })
            {
                var node = game.sceneNodes.Single(n => n.Entity.Spec.Kind == kind);
                var prompt = game.hud.interactionPrompt;
                foreach (float x in new[] { -.7f, 0, .7f })
                {
                    game.playerView.transform.position = node.LabelPosition + new Vector3(x, -.2f, 0);
                    prompt.SetTarget(node, game.gameCamera, "[E] " + (kind == WorldNodeKind.FishingSpot ? "Thả câu" : "Linh điền"));
                    Canvas.ForceUpdateCanvases(); prompt.RefreshPosition();
                    Assert.That(prompt.IsVisible, Is.True, kind + " at " + x);
                    var corners = new Vector3[4]; prompt.bubble.GetWorldCorners(corners);
                    var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                    var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                    var rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    var a = game.gameCamera.WorldToScreenPoint(game.playerView.VisualBounds.min);
                    var b = game.gameCamera.WorldToScreenPoint(game.playerView.VisualBounds.max);
                    Assert.That(rect.Overlaps(Rect.MinMaxRect(a.x, a.y, b.x, b.y)), Is.False);
                    Assert.That(min.y, Is.GreaterThan(game.gameCamera.WorldToScreenPoint(node.LabelPosition).y));
                    Assert.That(prompt.AnchorPosition, Is.EqualTo(node.LabelPosition));
                }
            }
        }
        private void ArrangeHudPixels(float pivot = .5f)
        {
            // Preview scenes do not drive ScreenSpaceOverlay native sizing (its scale is zero).
            // Use a pixel-sized canvas so these assertions exercise real, nondegenerate rectangles.
            var canvas = game.hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)canvas.transform;
            rect.pivot = Vector2.one * pivot;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(game.gameCamera.pixelWidth, game.gameCamera.pixelHeight);
            rect.position = Vector2.Scale(rect.sizeDelta, rect.pivot);
        }
        [Test]
        public void EveryMainGameObstacleRecoversEmbeddedPlayerAndAllowsEscape()
        {
            var world = game.World; var actor = world.PlayerActor; var geometry = world.Geometry;
            var obstacles = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ObstacleAuthoring>()).ToArray();
            foreach (var authored in obstacles)
            {
                var obstacle = authored.Definition();
                Place(obstacle.Center);
                world.Execute(new MoveCommand(actor.Id, default)); world.Tick(1f / 60);
                Assert.That(geometry.CanStand(actor.Transform.Position, actor.Transform.CollisionRadius), Is.True, authored.name);
                Assert.That(actor.Transform.CurrentVelocity.Length, Is.LessThan(.001f), "Correction must not launch the player.");
                var start = actor.Transform.Position;
                var outward = (start - obstacle.Center).Normalized;
                world.Execute(new MoveCommand(actor.Id, outward));
                for (int i = 0; i < 30; i++) { world.Tick(1f / 60); Assert.That(geometry.CanStand(actor.Transform.Position, actor.Transform.CollisionRadius), Is.True, authored.name); }
                Assert.That((actor.Transform.Position - start).Length, Is.GreaterThan(.1f), authored.name + " must allow outward escape.");
            }
        }
        [Test]
        public void WalkAndDashAgainstEveryRockFromAllSidesRemainFreeAndCanRetreat()
        {
            var world = game.World; var actor = world.PlayerActor; var geometry = world.Geometry;
            var rocks = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ModularMapProp>()).Where(p => p.visual != null && p.visual.sprite.name == "rock" && p.GetComponent<ObstacleAuthoring>() != null).ToArray();
            Assert.That(rocks.Length, Is.GreaterThanOrEqualTo(4));
            foreach (var rock in rocks)
            {
                var obstacle = rock.GetComponent<ObstacleAuthoring>().Definition();
                for (int side = 0; side < 16; side++)
                {
                    var outward = new WorldPoint(Mathf.Cos(side * Mathf.PI / 8), Mathf.Sin(side * Mathf.PI / 8));
                    var origin = geometry.ResolvePenetration(obstacle.Center + outward * (obstacle.HalfSize.Length + .35f), actor.Transform.CollisionRadius);
                    foreach (bool dash in new[] { false, true })
                    {
                        world.Execute(new MoveCommand(actor.Id, default));
                        for (int i = 0; i < 80; i++) world.Tick(1f / 60);
                        Place(origin); var toward = (obstacle.Center - origin).Normalized;
                        if (dash) Assert.That(world.Execute(new DashCommand(actor.Id, toward)).Succeeded, Is.True);
                        else world.Execute(new MoveCommand(actor.Id, toward));
                        for (int i = 0; i < 40; i++) { world.Tick(1f / 60); Assert.That(geometry.CanStand(actor.Transform.Position, actor.Transform.CollisionRadius), Is.True, rock.name + " side " + side); }
                        world.Execute(new MoveCommand(actor.Id, default));
                        for (int i = 0; i < 15; i++) world.Tick(1f / 60);
                        var contact = actor.Transform.Position;
                        var escape = (contact - obstacle.Center).Normalized;
                        world.Execute(new MoveCommand(actor.Id, escape));
                        for (int i = 0; i < 30; i++) world.Tick(1f / 60);
                        Assert.That((actor.Transform.Position - contact).Length, Is.GreaterThan(.15f), rock.name + " retreat " + side);
                    }
                }
            }
        }
        [Test]
        public void RocksOnlyBlockLowerHalfAndPondRockHasAnOpenPassage()
        {
            foreach (var prop in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ModularMapProp>()).Where(p => p.visual != null && p.visual.sprite.name == "rock"))
            {
                var obstacle = prop.GetComponent<ObstacleAuthoring>().Definition(); var bounds = prop.visual.bounds;
                Assert.That(obstacle.HalfSize.Y * 2 / bounds.size.y, Is.InRange(.40f, .50f), prop.name);
                Assert.That(obstacle.Center.Y + obstacle.HalfSize.Y, Is.LessThanOrEqualTo(bounds.min.y + bounds.size.y * .5f));
                Assert.That(game.World.Geometry.CanStand(new WorldPoint(bounds.center.x, bounds.max.y + .23f), .22f), Is.True);
            }
            var rock = game.map.GetComponentsInChildren<ModularMapProp>().Single(p => p.name == "Đá góc dưới").GetComponent<ObstacleAuthoring>().Definition();
            var pond = game.map.GetComponentsInChildren<ModularMapProp>().Single(p => p.visual != null && p.visual.sprite.name == "pond-cluster").GetComponent<ObstacleAuthoring>().Definition();
            Assert.That((pond.Center.Y - pond.HalfSize.Y) - (rock.Center.Y + rock.HalfSize.Y), Is.GreaterThan(.7f));
        }
        [Test]
        public void RockSortingUsesBaseAndAllBodyPartsStayInsideOneGroup()
        {
            var actor = game.playerView; var actorSort = actor.GetComponent<YSortRenderer>();
            var group = actor.equipment.GetComponentInChildren<SortingGroup>();
            Assert.That(actorSort.group, Is.SameAs(group)); Assert.That(actorSort.foot, Is.SameAs(actor.transform));
            foreach (var slot in actor.equipment.slots) Assert.That(slot.renderer.GetComponentInParent<SortingGroup>(), Is.SameAs(group));
            foreach (var prop in game.map.GetComponentsInChildren<ModularMapProp>().Where(p => p.visual != null && p.visual.sprite.name == "rock"))
            {
                var sort = prop.GetComponent<YSortRenderer>(); sort.Refresh();
                Assert.That(sort.GroundY, Is.EqualTo(prop.visual.bounds.min.y + sort.groundContactOffset).Within(.00001));
                actor.transform.position = new Vector3(prop.transform.position.x, sort.GroundY - .05f); actorSort.Refresh();
                Assert.That(group.sortingOrder, Is.GreaterThan(prop.visual.sortingOrder), "Whole actor must render in front.");
                actor.transform.position += Vector3.up * .10f; actorSort.Refresh();
                Assert.That(group.sortingOrder, Is.LessThan(prop.visual.sortingOrder), "Whole actor must render behind.");
            }
        }
        [Test]
        public void HudHasFiveCompactSkillsReadableBarsDynamicRowsAndRealCooldowns()
        {
            var hud = game.hud;
            Assert.That(hud.skills.Length, Is.EqualTo(5));
            foreach (var skill in hud.skills) { Assert.That(((RectTransform)skill.Button.transform).sizeDelta, Is.EqualTo(new Vector2(88, 56))); Assert.That(skill.Cooldown.type, Is.EqualTo(Image.Type.Filled)); }
            foreach (var bar in new[] { hud.hpFill, hud.qiFill, hud.shieldFill }) Assert.That(((RectTransform)bar.transform.parent).sizeDelta.y, Is.EqualTo(18));
            Assert.That(hud.vitals.text, Does.Contain("240 / 240")); Assert.That(hud.qiLabel.text, Is.EqualTo($"Linh lực  {game.World.PlayerActor.Vitals.CurrentQi:0} / {game.World.PlayerActor.Vitals.Maximum(LinhThu.Domain.Stats.VitalType.Qi):0}"));
            Assert.That(hud.experience.text, Does.Contain("Tu vi:"));
            Assert.That(((RectTransform)hud.saveButton.transform).sizeDelta.y, Is.EqualTo(28));
            Assert.That(hud.Layout.MicroBar.sizeDelta.y, Is.EqualTo(26));
            Assert.That(hud.transform.Find("Actions") == null && hud.transform.Find("Controls") == null, Is.True);
            int before = hud.Layout.Rows.Count(r => r.Root.gameObject.activeSelf);
            Assert.That(hud.Layout.InventoryPanel.sizeDelta.y, Is.EqualTo(40 + before * 24));
            var newItem = game.registry.items.First(i => game.World.Player.Inventory.Count(i.Id) == 0);
            Assert.That(game.World.Player.Inventory.TryAddAll(new[] { new ItemStack(newItem.Id, 1) }).Succeeded, Is.True);
            hud.Refresh(); Assert.That(hud.Layout.InventoryPanel.sizeDelta.y, Is.EqualTo(40 + (before + 1) * 24));
            Assert.That(hud.Layout.Rows.Single(r => r.Name.text == newItem.displayName).Quantity.text, Is.EqualTo("×1"));
            hud.dashButton.onClick.Invoke(); hud.Refresh();
            Assert.That(hud.skills[3].Cooldown.fillAmount, Is.GreaterThan(.9f)); Assert.That(hud.skills[3].CooldownText.text, Does.EndWith("s"));
            Assert.That(hud.dashButton.interactable, Is.False);
            for (int i = 0; i < 80; i++) game.World.Tick(1f / 60); hud.Refresh();
            Assert.That(hud.skills[3].Cooldown.gameObject.activeSelf, Is.False); Assert.That(hud.dashButton.interactable, Is.True);
        }
        [Test]
        public void AuthoredEggAndPortalUseSmoothCraftedGeometryAndKeepTheirIdentity()
        {
            foreach (var kind in new[] { WorldNodeKind.SpiritEggNode, WorldNodeKind.TeleportPortal })
            {
                var node = game.sceneNodes.Single(n => n.config.kind == kind);
                Assert.That(node.visual.sprite.texture.width, Is.GreaterThanOrEqualTo(512));
                Assert.That(node.visual.sprite.texture.filterMode, Is.Not.EqualTo(FilterMode.Point));
                Assert.That(node.visual.sharedMaterial.shader.name, Is.EqualTo("LinhThu/Daylight Sprite"));
                Assert.That(node.config.displayName, Is.Not.Empty);
            }
        }

        [Test]
        public void EffectsAndOverheadStayAboveWorldAndCompactPromptRemainsClickable()
        {
            Assert.That(game.hud.interactButton.targetGraphic.raycastTarget, Is.True);
            int highestWorldOrder = WorldRenderOrder.EntityMaximum;
            var dummy = game.sceneActors.Single(v => v.config.Id == "training-dummy");
            Assert.That(dummy.overhead.GetComponentInChildren<Canvas>(true).sortingOrder, Is.GreaterThan(highestWorldOrder));
            game.CastFromHud(2);
            var line = game.effects.GetComponentsInChildren<LineRenderer>().First();
            Assert.That(line.sortingOrder, Is.GreaterThan(highestWorldOrder));
            var text = game.effects.floatingTextPrefab.label.GetComponent<Renderer>();
            Assert.That(text.sortingOrder, Is.GreaterThan(line.sortingOrder));
            var layout = game.hud.Layout;
            layout.FitInventory(12);
            Assert.That(layout.InventoryScroll.vertical, Is.True);
            Assert.That(layout.InventoryPanel.sizeDelta.y, Is.EqualTo(280));
            layout.InventoryContent.anchoredPosition = new Vector2(0, 48);
            layout.FitInventory(0);
            Assert.That(layout.InventoryPanel.sizeDelta.y, Is.EqualTo(40));
            Assert.That(layout.InventoryScroll.viewport.gameObject.activeSelf, Is.False);
            layout.FitInventory(1);
            Assert.That(layout.InventoryContent.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(layout.InventoryScroll.viewport.gameObject.activeSelf, Is.True);
        }
    }
}
