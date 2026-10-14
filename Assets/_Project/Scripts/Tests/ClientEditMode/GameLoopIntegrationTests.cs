using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LinhThu.Client.Bootstrap;
using LinhThu.Client.Persistence;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LinhThu.Tests.ClientEditMode
{
    public sealed class GameLoopIntegrationTests
    {
        private const string ScenePath = "Assets/_Project/Content/Scenes/MainGame.unity";
        private Scene scene;
        private GameBootstrapper game;
        [SetUp]
        public void OpenIsolatedAuthoredScene()
        {
            scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            game = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameBootstrapper>()).Single();
            game.Initialize();
        }

        [TearDown]
        public void CloseIsolatedScene()
        {
            if (scene.IsValid())
                EditorSceneManager.ClosePreviewScene(scene);
        }

        private void Tick(float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds / GameBootstrapper.StepSeconds); i++)
                game.World.Tick(GameBootstrapper.StepSeconds);
            game.SyncViews();
        }

        private void Approach(WorldNodeEntity node)
        {
            var actor = game.World.PlayerActor;
            var destination = node.Position + new WorldPoint(0, -.8f);
            game.World.Motor.Displace(actor.Transform, destination - actor.Transform.Position);
            Assert.That((actor.Transform.Position - node.Position).Length, Is.LessThan(node.Spec.InteractionRadius));
            game.SyncViews();
        }

        [Test]
        public void SceneBootsWithSinglePlayerCompanionHudAndBuildEntry()
        {
            Assert.That(game.World.Actors.Count, Is.EqualTo(3));
            Assert.That(game.World.Nodes.Count, Is.EqualTo(5));
            Assert.That(game.World.Player.Beasts.All.Single().State, Is.EqualTo(BeastState.SummonedActive));
            Assert.That(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>()).Count(), Is.EqualTo(1));
            Assert.That(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path), Is.EqualTo(new[]{ScenePath}));
            Assert.That(game.hud.vitals.text, Does.Contain("Sinh lực"));
            Assert.That(game.hud.inventory.text, Does.Contain("TÚI ĐỒ"));
        }

        [Test]
        public void MovementSyncsViewAndHudButtonCastsRealProjectileDamage()
        {
            var start = game.World.PlayerActor.Transform.Position;
            game.World.Execute(new MoveCommand(game.World.Player.ActorId, new WorldPoint(1, 0)));
            Tick(.2f);
            game.World.Execute(new MoveCommand(game.World.Player.ActorId, default));
            Assert.That(game.playerView.transform.position.x, Is.GreaterThan(start.X + .5f));
            Assert.That(game.playerView.transform.position.x, Is.EqualTo(game.World.PlayerActor.Transform.Position.X));
            var target = game.World.Actors.Values.Single(a => a.DefinitionId == "training-dummy");
            float hp = target.Vitals.CurrentHp, qi = game.World.PlayerActor.Vitals.CurrentQi;
            game.hud.hotbar[0].onClick.Invoke();
            Assert.That(game.World.PlayerActor.Vitals.CurrentQi, Is.LessThan(qi));
            Assert.That(game.World.PlayerActor.Abilities.GetCooldownRemaining(game.registry.hotbar[0].Id), Is.GreaterThan(0));
            Tick(.5f);
            Assert.That(target.Vitals.CurrentHp, Is.LessThan(hp));
        }

        [Test]
        public void HarvestPlantAndPortalAreConnectedThroughClientCommands()
        {
            foreach (var id in new[]{"herb-node", "ore-node"})
            {
                var node = game.World.Nodes.Values.Single(n => n.DefinitionId == id);
                Approach(node);
                Assert.That(game.InteractFocused().Succeeded, Is.True, id);
                Assert.That(game.World.Player.Inventory.Count(node.Spec.Rewards[0].DefinitionId), Is.EqualTo(2));
                Assert.That(game.World.Execute(new HarvestCommand(game.World.Player.ActorId, node.Id)).Succeeded, Is.False, "No duplicate harvest while this node regrows");
            }

            var field = game.World.Nodes.Values.Single(n => n.Spec.Kind == WorldNodeKind.SpiritFieldPlot);
            Approach(field);
            Assert.That(game.InteractFocused().Succeeded, Is.True);
            Assert.That(field.CropDefinitionId, Is.EqualTo(game.registry.fieldCrop.Id));
            Tick(game.registry.fieldCrop.growthSeconds + .1f);
            Assert.That(game.InteractFocused().Succeeded, Is.True);
            Assert.That(field.CropDefinitionId, Is.Null);
            var portal = game.World.Nodes.Values.Single(n => n.Spec.Kind == WorldNodeKind.TeleportPortal);
            Approach(portal);
            Assert.That(game.InteractFocused().Succeeded, Is.True);
            Assert.That((game.World.PlayerActor.Transform.Position - portal.Spec.PortalDestination).Length, Is.LessThan(.001f));
        }

        [Test]
        public void HudRecallResummonAndOutfitSwapDoNotAccumulateStats()
        {
            float attack = game.World.PlayerActor.Stats.Get(StatId.Attack);
            float defense = game.World.PlayerActor.Stats.Get(StatId.Defense);
            var outfit = game.playerView.equipment.CurrentOutfit;
            game.hud.beastButton.onClick.Invoke();
            Assert.That(game.World.Actors.Count, Is.EqualTo(2));
            Assert.That(game.sceneActors.Single(v => v.rosterCompanion).gameObject.activeSelf, Is.False);
            game.hud.beastButton.onClick.Invoke();
            Assert.That(game.World.Actors.Count, Is.EqualTo(3));
            Assert.That(game.World.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(attack));
            game.hud.outfitButton.onClick.Invoke();
            Assert.That(game.playerView.equipment.CurrentOutfit, Is.Not.SameAs(outfit));
            game.hud.outfitButton.onClick.Invoke();
            game.hud.outfitButton.onClick.Invoke();
            Assert.That(game.playerView.equipment.CurrentOutfit, Is.SameAs(outfit));
            Assert.That(game.World.PlayerActor.Stats.Get(StatId.Defense), Is.EqualTo(defense));
        }

        [Test]
        public void SaveStoreRoundTripsAtomicallyAndRejectsCorruption()
        {
            string folder = Path.Combine(Path.GetTempPath(), "linhthu-save-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new WorldSaveStore(Path.Combine(folder, "save.json"));
                store.Write(game.World);
                string original = File.ReadAllText(store.FilePath);
                Assert.That(game.CycleOutfit().Succeeded, Is.True);
                store.Write(game.World);
                Assert.That(File.ReadAllText(store.FilePath + ".bak"), Is.EqualTo(original));
                Assert.That(Directory.GetFiles(folder, "*.pending"), Is.Empty);
                var restored = store.Read(game.World.Content, game.World.Geometry, 7);
                Assert.That(restored.Succeeded, Is.True, restored.Message);
                Assert.That(restored.Value.Player.Equipment.Slots["body"].ItemId, Is.EqualTo(game.World.Player.Equipment.Slots["body"].ItemId));
                Assert.That(restored.Value.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(game.World.PlayerActor.Stats.Get(StatId.Attack)));
                File.WriteAllText(store.FilePath, "{broken");
                Assert.That(store.Read(game.World.Content, game.World.Geometry, 7).Succeeded, Is.False);
                Assert.That(game.World.PlayerActor.Vitals.IsAlive, Is.True);
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        [Test]
        public void LoadingThroughHudRebindsViewsAndCorruptSaveKeepsLiveWorld()
        {
            string folder = Path.Combine(Path.GetTempPath(), "linhthu-hud-save-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new WorldSaveStore(Path.Combine(folder, "save.json"));
                typeof(GameBootstrapper).GetField("saveStore", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, store);
                var outfit = game.playerView.equipment.CurrentOutfit;
                float attack = game.World.PlayerActor.Stats.Get(StatId.Attack);
                game.hud.saveButton.onClick.Invoke();
                game.hud.outfitButton.onClick.Invoke();
                game.hud.beastButton.onClick.Invoke();
                var previous = game.World;
                game.hud.loadButton.onClick.Invoke();
                Assert.That(game.World, Is.Not.SameAs(previous));
                Assert.That(game.World.Actors.Count, Is.EqualTo(3));
                Assert.That(game.playerView.Entity, Is.SameAs(game.World.PlayerActor));
                Assert.That(game.playerView.equipment.CurrentOutfit, Is.SameAs(outfit));
                Assert.That(game.World.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(attack));
                Assert.That(game.sceneActors.Single(v => v.rosterCompanion).gameObject.activeSelf, Is.True);
                previous = game.World;
                File.WriteAllText(store.FilePath, "bad save");
                game.hud.loadButton.onClick.Invoke();
                Assert.That(game.World, Is.SameAs(previous));
                Assert.That(game.hud.message.text, Does.Contain("Không đọc được"));
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        [Test]
        public void MainGameUsesModularGroundAndSolidPropsHaveFootprints()
        {
            var dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            Assert.That(dependencies.Any(p => p.EndsWith("/field.png")), Is.False, "Legacy image is a whole map, not a field prop.");
            Assert.That(game.map.GetComponentInChildren<LinhThu.Client.Environment.MeadowGroundSurface>(), Is.Not.Null);
            Assert.That(game.map.GetComponentInChildren<LinhThu.Client.Environment.MeadowTerrainPath>(), Is.Not.Null);
            Assert.That(game.hud.hpFill.sprite, Is.Not.Null, "Filled uGUI image requires a sprite.");
            Assert.That(game.hud.qiFill.sprite, Is.Not.Null);
            var ore = game.sceneNodes.Single(n => n.config.kind == WorldNodeKind.OreNode);
            Assert.That(game.World.Geometry.CanStand(new WorldPoint(ore.transform.position.x, ore.transform.position.y), .22f), Is.False);
            Assert.That(game.World.Geometry.CanStand(new WorldPoint(ore.transform.position.x, ore.transform.position.y + .8f), .22f), Is.True);
            foreach (var prop in game.map.GetComponentsInChildren<LinhThu.Client.Environment.ModularMapProp>().Where(p => p.visual != null && (p.visual.sprite.name == "grass-tuft" || p.visual.sprite.name == "white-flowers")))
            {
                Assert.That(prop.floorLayer, Is.True, prop.name);
                Assert.That(prop.castShadow == null && prop.contactShadow == null, Is.True, "Ground accents must not retain old shadow sprites: " + prop.name);
            }
        }

        [Test]
        public void OneFloatingPromptAndHudFollowTheCommandTargetImmediatelyAfterHarvest()
        {
            var egg = game.World.Nodes.Values.Single(n => n.Spec.Kind == WorldNodeKind.SpiritEggNode);
            var field = game.World.Nodes.Values.Single(n => n.Spec.Kind == WorldNodeKind.SpiritFieldPlot);
            var player = game.World.PlayerActor;
            var between = egg.Position + (field.Position - egg.Position) * .4f;
            game.World.Motor.Displace(player.Transform, between - player.Transform.Position);
            game.SyncViews();
            var prompt = game.hud.interactionPrompt;
            Assert.That(game.hud.GetComponentsInChildren<LinhThu.Client.UI.InteractionPromptPresenter>(true).Length, Is.EqualTo(1));
            Assert.That(game.FocusedNodeId, Is.EqualTo(egg.Id));
            Assert.That(prompt.FocusedNodeId, Is.EqualTo(egg.Id));
            Assert.That(prompt.IsVisible, Is.True);
            Assert.That(prompt.label.text, Is.EqualTo("[E] Trứng Huyền Quy"));
            Assert.That(game.hud.interactButtonLabel.text, Does.Contain("Trứng Huyền Quy"));
            // Overlay canvas size/scale are zero in PreviewScene; pixel anchoring is checked in Editor Play.
            var position = player.Transform.Position;
            game.hud.interactButton.onClick.Invoke();
            Assert.That(egg.IsDepleted, Is.True);
            Assert.That(player.Transform.Position, Is.EqualTo(position));
            Assert.That(game.FocusedNodeId, Is.EqualTo(field.Id));
            Assert.That(prompt.FocusedNodeId, Is.EqualTo(field.Id));
            Assert.That(prompt.label.text, Is.EqualTo("[E] Linh Điền"));
            Assert.That(game.hud.interactButtonLabel.text, Does.Contain("Linh Điền"));
            game.World.Motor.Displace(player.Transform, new WorldPoint(0, -4.8f) - player.Transform.Position);
            game.SyncViews();
            Assert.That(game.FocusedNodeId, Is.Null);
            Assert.That(prompt.FocusedNodeId, Is.Null);
            Assert.That(prompt.IsVisible, Is.False);
            Assert.That(game.hud.interactButton.interactable, Is.False);
        }

        [Test]
        public void IncapacitatedPlayerHasNoPromptOrEnabledInteractionButton()
        {
            Approach(game.World.Nodes.Values.Single(n => n.Spec.Kind == WorldNodeKind.SpiritEggNode));
            Assert.That(game.hud.interactionPrompt.IsVisible, Is.True);
            game.World.DealDamage(game.World.PlayerActor, game.World.Player.ActorId, 100000);
            game.SyncViews();
            Assert.That(game.FocusedNodeId, Is.Null);
            Assert.That(game.hud.interactionPrompt.IsVisible, Is.False);
            Assert.That(game.hud.interactButton.interactable, Is.False);
        }

        [Test]
        public void HostileOverheadShowsActualHpStatusesAndHidesOnDeath()
        {
            var target = game.sceneActors.Single(v => v.faction == ActorFaction.HostileMonster);
            var overhead = target.overhead;
            Assert.That(overhead.panel.activeSelf, Is.True);
            Assert.That(overhead.actorName.text, Is.EqualTo("Mộc Nhân Luyện Công"));
            Assert.That(game.playerView.overhead.panel.activeSelf, Is.False);
            game.ToggleBeast(); // Keep the expiry check independent of companion projectiles.
            game.hud.hotbar[0].onClick.Invoke();
            Tick(.6f);
            Assert.That(overhead.hpFill.fillAmount, Is.LessThan(1));
            Assert.That(overhead.hpFill.fillAmount, Is.EqualTo(target.Entity.Vitals.CurrentHp / target.Entity.Vitals.Maximum(VitalType.Hp)).Within(.0001f));
            Assert.That(overhead.transform.position.y, Is.GreaterThan(target.equipment.slots.Max(s => s.renderer.bounds.max.y)));
            var status = game.registry.statuses.Single(s => s.Id == "han-khi");
            Assert.That(target.Entity.StatusEffects.Apply(status.ToSpec(), game.World.Player.ActorId, EffectSourceSnapshot.Capture(game.World.PlayerActor)).Succeeded, Is.True);
            game.SyncViews();
            Assert.That(overhead.statusText.text, Does.Contain(status.displayName));
            Tick(status.ToSpec().Duration + .1f);
            Assert.That(overhead.statusText.text, Is.Empty);
            game.World.DealDamage(target.Entity, game.World.Player.ActorId, 100000);
            game.SyncViews();
            Assert.That(overhead.panel.activeSelf, Is.False);
        }

        [Test]
        public void SmallerRigKeepsScaleShadowAndColliderAcrossEveryOutfit()
        {
            foreach (var view in new[] { game.playerView, game.sceneActors.Single(v => v.faction == ActorFaction.HostileMonster) })
            {
                Assert.That(view.equipment.visual.localScale.y, Is.EqualTo(.625f));
                Assert.That(view.Entity.Transform.CollisionRadius, Is.EqualTo(.22f * .625f).Within(.00001f));
                Assert.That(view.GetComponent<LinhThu.Client.Actors.ActorShadow>().referenceHeight, Is.EqualTo(2.6f * .625f).Within(.00001f));
            }
            var controller = game.playerView.equipment.animator.runtimeAnimatorController;
            for (int i = 0; i < 3; i++)
            {
                Assert.That(game.CycleOutfit().Succeeded, Is.True);
                Assert.That(game.playerView.equipment.visual.localScale.y, Is.EqualTo(.625f));
                Assert.That(game.playerView.equipment.animator.runtimeAnimatorController, Is.SameAs(controller));
            }
        }

        [Test]
        public void EggAndOreBlockTheirBaseButRemainReachableFromTheFront()
        {
            foreach (var kind in new[] { WorldNodeKind.SpiritEggNode, WorldNodeKind.OreNode })
            {
                var node = game.World.Nodes.Values.Single(n => n.Spec.Kind == kind);
                var radius = game.World.PlayerActor.Transform.CollisionRadius;
                Assert.That(game.World.Geometry.CanStand(node.Position + new WorldPoint(0, .18f), radius), Is.False, kind.ToString());
                var front = node.Position + new WorldPoint(0, -.4f);
                Assert.That(game.World.Geometry.CanStand(front, radius), Is.True);
                Assert.That(game.World.Geometry.ClearLine(front, node.Position), Is.True);
                Approach(node);
                Assert.That(game.FocusedNodeId, Is.EqualTo(node.Id));
                Assert.That(game.InteractFocused().Succeeded, Is.True);
            }
        }

        [Test]
        public void FieldClearsTheRoadAndCampClearsCherryCanopies()
        {
            var field = game.sceneNodes.Single(n => n.config.kind == WorldNodeKind.SpiritFieldPlot);
            var fieldBounds = field.GetComponentInChildren<MeshRenderer>().bounds;
            var path = game.map.GetComponentInChildren<LinhThu.Client.Environment.MeadowTerrainPath>();
            for (int segment = 0; segment < path.knots.Length - 1; segment++)
                for (int step = 0; step <= 32; step++)
                {
                    float t = step / 32f;
                    Vector3 point = path.transform.TransformPoint(path.Evaluate(segment, t));
                    float roadWidth = Mathf.Lerp(path.knots[segment].halfWidth, path.knots[segment + 1].halfWidth, Mathf.SmoothStep(0, 1, t));
                    Assert.That(Vector3.Distance(point, fieldBounds.ClosestPoint(point)), Is.GreaterThan(roadWidth + .2f), "The ridged field must not overlap the earth spline.");
                }
            var camp = scene.GetRootGameObjects().Single(r => r.name == "Camp").GetComponentInChildren<MeshRenderer>().bounds;
            foreach (var tree in game.map.GetComponentsInChildren<LinhThu.Client.Environment.ModularMapProp>().Where(p => p.visual != null && p.visual.sprite.name == "cherry-tree"))
                Assert.That(camp.Intersects(tree.visual.bounds), Is.False, tree.name);
        }
    }
}
