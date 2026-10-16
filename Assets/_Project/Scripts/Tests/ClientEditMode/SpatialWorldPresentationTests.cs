using System;
using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Client.Bootstrap;
using LinhThu.Client.Camera;
using LinhThu.Client.Combat;
using LinhThu.Client.Environment;
using LinhThu.Client.World;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LinhThu.Tests.ClientEditMode
{
    public sealed class SpatialWorldPresentationTests
    {
        private Scene scene;
        private GameBootstrapper game;
        [SetUp] public void Open()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Content/Scenes/MainGame.unity");
            game = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameBootstrapper>()).Single(); game.Initialize();
        }
        [TearDown] public void Close() { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }
        private void Place(WorldPoint point)
        {
            typeof(Transform2D).GetProperty(nameof(Transform2D.Position)).SetValue(game.World.PlayerActor.Transform, point);
            typeof(Transform2D).GetProperty(nameof(Transform2D.CurrentVelocity)).SetValue(game.World.PlayerActor.Transform, default(WorldPoint));
            game.SyncViews();
        }
        private void Tick(float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) game.World.Tick(1f / 60);
            game.SyncViews();
        }

        [Test]
        public void AuthoredRegionsStreamWithoutRemovingWorldCollisionAndRecoverAfterDisable()
        {
            var world = game.World;
            var props = game.map.GetComponentsInChildren<ModularMapProp>(true);
            Assert.That(props.Length, Is.EqualTo(106));
            foreach (var coord in new[] { new ChunkCoord(0, 0), new ChunkCoord(1, 0), new ChunkCoord(0, 1), new ChunkCoord(1, 1) })
                Assert.That(props.Any(p => ChunkCoord.FromWorld(new WorldPoint(p.transform.position.x, p.transform.position.y)) == coord), Is.True, coord.ToString());
            Assert.That(game.map.WorldBounds.Max.X, Is.EqualTo(32)); Assert.That(game.chunkStreamer.activeChunkRadius, Is.EqualTo(1));
            var west = props.First(p => p.transform.position.x < -6 && p.transform.position.y < 0 && p.GetComponent<ObstacleAuthoring>() != null);
            var obstacle = west.GetComponent<ObstacleAuthoring>().Definition();
            int actors = game.World.Actors.Count, nodes = game.World.Nodes.Count;
            Place(new WorldPoint(30, 30)); game.gameCamera.transform.position = new Vector3(30, 30, -10);
            game.chunkStreamer.Refresh();
            Assert.That(west.gameObject.activeSelf, Is.False); Assert.That(game.chunkStreamer.ActiveRootCount, Is.LessThan(props.Length));
            Assert.That(game.World.Geometry.CanStand(obstacle.Center, .2f), Is.False);
            Assert.That(game.World.Actors.Count, Is.EqualTo(actors)); Assert.That(game.World.Nodes.Count, Is.EqualTo(nodes));
            game.chunkStreamer.enabled = false;
            // Preview scenes do not dispatch play-mode callbacks for a non-ExecuteAlways component.
            typeof(WorldChunkStreamer).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(game.chunkStreamer, null);
            Assert.That(west.gameObject.activeSelf, Is.True);
            game.chunkStreamer.enabled = true; game.chunkStreamer.Refresh(); Assert.That(west.gameObject.activeSelf, Is.False);
            Place(new WorldPoint(-8, -6)); game.gameCamera.transform.position = new Vector3(-8, -6, -10); game.chunkStreamer.Refresh();
            Assert.That(west.gameObject.activeSelf, Is.True); Assert.That(game.World.Geometry.CanStand(obstacle.Center, .2f), Is.False);
            Assert.That(game.World, Is.SameAs(world));
        }

        [Test]
        public void PlayerAndCompanionCrossChunkBoundaryWithoutRebuildingWorld()
        {
            var world = game.World; var beast = world.Player.Beasts.All.Single(); var actorId = beast.ActorId;
            Place(new WorldPoint(15, 12));
            Assert.That(world.Execute(new MoveCommand(world.LocalPlayerId, new WorldPoint(1, 0))).Succeeded, Is.True);
            Tick(1);
            Assert.That(ChunkCoord.FromWorld(world.PlayerActor.Transform.Position).X, Is.EqualTo(1));
            Assert.That(beast.ActorId, Is.EqualTo(actorId)); Assert.That(game.World, Is.SameAs(world));
            Assert.That((world.Actors[actorId.Value].Transform.Position - world.PlayerActor.Transform.Position).Length, Is.LessThan(8));
            world.Execute(new MoveCommand(world.LocalPlayerId, new WorldPoint(-1, 0))); Tick(1.2f);
            Assert.That(ChunkCoord.FromWorld(world.PlayerActor.Transform.Position).X, Is.EqualTo(0));
            Assert.That(beast.ActorId, Is.EqualTo(actorId));
        }

        [TestCase(DeliveryShape.ConeSector)] [TestCase(DeliveryShape.CircleAoE)] [TestCase(DeliveryShape.LineRay)]
        public void WarningOutlineMatchesDeliveryDimensions(DeliveryShape shape)
        {
            var d = new DeliverySpec(shape, 4, 1, 90);
            var outline = AttackTelegraphPresenter.Outline(d);
            Assert.That(outline.All(p => !float.IsNaN(p.x) && !float.IsNaN(p.y)), Is.True);
            if (shape == DeliveryShape.CircleAoE) Assert.That(outline.All(p => Mathf.Abs(p.magnitude - 1) < .0001f), Is.True);
            if (shape == DeliveryShape.ConeSector)
            {
                Assert.That(outline.Max(p => p.x), Is.EqualTo(4).Within(.0001f));
                Assert.That(outline.All(p => p.magnitude <= 4.0001f && p.x >= Mathf.Abs(p.y) - .0001f), Is.True);
            }
            if (shape == DeliveryShape.LineRay)
            {
                Assert.That(outline.Min(p => p.x), Is.EqualTo(-1).Within(.0001f));
                Assert.That(outline.Max(p => p.x), Is.EqualTo(5).Within(.0001f));
                Assert.That(outline.Max(p => p.y), Is.EqualTo(1).Within(.0001f));
            }
        }

        [Test]
        public void HuntingShowsGroundWarningAndWeaknessThenTamesAndSummonsThroughHud()
        {
            game.ToggleBeast(); var site = game.World.WildBeasts.Values.First(); var wild = game.World.Actors[site.ActorId.Value];
            Place(wild.Transform.Position + new WorldPoint(.8f, 0)); Tick(.05f); game.telegraphs.Refresh();
            Assert.That(site.State, Is.EqualTo(WildBeastAiState.WindupCast)); Assert.That(game.telegraphs.VisibleCount, Is.EqualTo(1));
            var mesh = game.telegraphs.GetComponentsInChildren<MeshFilter>().Single().sharedMesh;
            Assert.That(mesh.vertexCount, Is.GreaterThan(90)); Assert.That(mesh.colors.Max(c => c.a), Is.GreaterThan(.4f));
            Assert.That(game.telegraphs.GetComponentInChildren<MeshRenderer>().sortingOrder, Is.EqualTo(-7900));
            game.World.DealDamage(wild, game.World.LocalPlayerId, wild.Vitals.CurrentHp - wild.Vitals.Maximum(VitalType.Hp) * .3f);
            game.World.Events.Flush(); game.SyncViews();
            var view = game.GetComponentsInChildren<ActorView>().Single(v => v.Entity?.Id == wild.Id);
            view.overhead.Refresh(); Assert.That(view.overhead.statusText.text, Does.Contain("Suy yếu"));
            Assert.That(game.hud.tameButton.interactable, Is.True); game.hud.tameButton.onClick.Invoke();
            Assert.That(game.World.Player.Beasts.All.Count, Is.EqualTo(2)); Assert.That(game.World.Actors.ContainsKey(wild.Id), Is.False);
            game.telegraphs.Refresh(); Assert.That(game.telegraphs.VisibleCount, Is.Zero);
            Assert.That(game.hud.Layout.BeastButtons.Count, Is.EqualTo(2));
            game.hud.Layout.BeastButtons[1].onClick.Invoke();
            var tamed = game.World.Player.Beasts.All[1]; Assert.That(tamed.State, Is.EqualTo(BeastState.SummonedActive));
            Assert.That(game.SelectedBeastId, Is.EqualTo(tamed.Id)); Assert.That(game.World.Actors[tamed.ActorId.Value].OwnerId, Is.EqualTo(game.World.LocalPlayerId));
        }

        [Test]
        public void ExperienceBreakthroughAndInventoryToggleReadDomainAndReleaseHitTargets()
        {
            var hud = game.hud; var progress = game.World.Player.Cultivation;
            Assert.That(hud.experience.text, Does.Contain(" / " )); Assert.That(hud.breakthroughButton.interactable, Is.False);
            progress.AddExperience(game.registry.startingRealm.ToSpec().BreakthroughCost); hud.Refresh();
            Assert.That(hud.breakthroughButton.interactable, Is.True); Assert.That(hud.experienceFill.fillAmount, Is.EqualTo(1));
            Assert.That(hud.breakthroughButton.GetComponent<Image>().color, Is.EqualTo((Color)new Color32(242, 217, 141, 255)));
            string previous = progress.CurrentRealmId; hud.breakthroughButton.onClick.Invoke();
            Assert.That(progress.CurrentRealmId, Is.Not.EqualTo(previous)); Assert.That(hud.breakthroughButton.interactable, Is.False);
            var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var root = (RectTransform)canvas.transform; root.localScale = Vector3.one; root.sizeDelta = new Vector2(1280, 720); root.position = root.sizeDelta * .5f;
            Canvas.ForceUpdateCanvases();
            var bag = hud.Layout.InventoryPanel;
            var point = RectTransformUtility.WorldToScreenPoint(null, bag.TransformPoint(bag.rect.center));
            Assert.That(hud.BlocksPointer(point), Is.True);
            hud.sidePanelsButton.onClick.Invoke(); Assert.That(hud.Layout.SidePanelsVisible, Is.False);
            Assert.That(hud.Layout.BeastPanel.gameObject.activeInHierarchy, Is.False); Assert.That(hud.BlocksPointer(point), Is.False);
            hud.ToggleSidePanels(); Assert.That(hud.Layout.SidePanelsVisible, Is.True); Assert.That(hud.BlocksPointer(point), Is.True);
        }
    }
}
