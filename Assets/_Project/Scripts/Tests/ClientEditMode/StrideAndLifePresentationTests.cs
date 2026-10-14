using System;
using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Client.Bootstrap;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LinhThu.Tests.ClientEditMode
{
    public sealed class StrideAndLifePresentationTests
    {
        [Test]
        public void ProportionsAndStartingRobeArePreservedAcrossAllAppearancesAndClips()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var registry = ContentIntegrationTests.Registry;
                Assert.That(registry.startingOutfit.outfit.appearanceId, Is.EqualTo("classic").Or.EqualTo("jade"));
                Assert.That(registry.player.ToSpec().BaseStats[LinhThu.Domain.Stats.StatId.MoveSpeed], Is.InRange(2.8f, 3.4f));
                var root = (GameObject)PrefabUtility.InstantiatePrefab(registry.player.prefab.gameObject, scene);
                var rig = root.GetComponent<ModularEquipmentRenderer>(); var visual = rig.animator.transform;
                foreach (var outfit in registry.outfits)
                {
                    rig.ApplyOutfit(outfit);
                    foreach (var clip in rig.animator.runtimeAnimatorController.animationClips)
                    {
                        clip.SampleAnimation(visual.gameObject, clip.length * .3f);
                        var head = visual.Find("root/pelvis/chest/head");
                        Assert.That(head.localScale.x, Is.EqualTo(.78f).Within(.0001f));
                        Assert.That(head.localScale.y, Is.EqualTo(.78f).Within(.0001f));
                        foreach (string slot in new[] { "head", "eyes", "mouth", "hair-front", "hair-back", "hair-ornament" })
                            Assert.That(rig.slots.Single(s => s.id == slot).renderer.transform.IsChildOf(head), Is.True, slot);
                        Assert.That(visual.Find("root/pelvis/chest/upper_front").localPosition.x, Is.GreaterThan(-.30f));
                        Assert.That(visual.Find("root/pelvis/chest/upper_back").localPosition.x, Is.LessThan(.23f));
                        Assert.That(Mathf.Abs(rig.visual.localScale.x), Is.EqualTo(.625f));
                    }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void RunPlantedFootMatchesWorldVelocityAtNormalAndBoostedSpeeds()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(ContentIntegrationTests.Registry.player.prefab.gameObject, scene);
                var animator = root.GetComponent<ActorAnimator>();
                var visual = animator.animator.transform;
                var clip = animator.animator.runtimeAnimatorController.animationClips.Single(c => c.name == "Run");
                Assert.That(animator.baseStrideSpeed, Is.EqualTo(.9375f).Within(.0001f));
                foreach (float speed in new[] { .2f, 1.4f, 2.8f, 4.2f, 6f })
                {
                    float rate = animator.PlaybackSpeed(speed);
                    Assert.That(rate, Is.EqualTo(speed / animator.baseStrideSpeed).Within(.0001f));
                    foreach (string side in new[] { "front", "back" })
                    {
                        var foot = visual.Find("root/pelvis/thigh_" + side + "/shin_" + side + "/foot_" + side);
                        float from = side == "front" ? .06f : .46f, to = from + .24f;
                        clip.SampleAnimation(visual.gameObject, from); Vector3 p = foot.position;
                        clip.SampleAnimation(visual.gameObject, to);
                        float drift = foot.position.x - p.x + speed * (to - from) / rate;
                        Assert.That(Mathf.Abs(drift), Is.LessThan(.003f), side + " at " + speed);
                        Assert.That(Mathf.Abs(foot.position.y - p.y), Is.LessThan(.003f));
                    }
                }
                float knee = animator.highSpeedSoftCap;
                Assert.That(animator.PlaybackSpeed(knee + .001f) - animator.PlaybackSpeed(knee), Is.EqualTo(.001f / animator.baseStrideSpeed).Within(.00001f));
                Assert.That(animator.PlaybackSpeed(12), Is.GreaterThan(animator.PlaybackSpeed(8)));
                Assert.That(animator.PlaybackSpeed(12), Is.LessThan(12 / animator.baseStrideSpeed));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void RunKeepsHipsNearStandingHeightAndKneesBelowHips()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(ContentIntegrationTests.Registry.player.prefab.gameObject, scene);
                var animator = root.GetComponent<ActorAnimator>().animator;
                var visual = animator.transform;
                var clips = animator.runtimeAnimatorController.animationClips;
                clips.Single(c => c.name == "Idle").SampleAnimation(visual.gameObject, 0);
                var pelvis = visual.Find("root/pelvis");
                float standingHeight = pelvis.localPosition.y;
                var run = clips.Single(c => c.name == "Run");
                for (int i = 0; i <= 64; i++)
                {
                    run.SampleAnimation(visual.gameObject, run.length * i / 64);
                    Assert.That(pelvis.localPosition.y, Is.GreaterThanOrEqualTo(standingHeight - .08f), "Run must not become a crouch.");
                    foreach (string side in new[] { "front", "back" })
                    {
                        var thigh = pelvis.Find("thigh_" + side);
                        var shin = thigh.Find("shin_" + side);
                        Assert.That(shin.position.y, Is.LessThan(thigh.position.y - .06f), side + " knee must remain below hip.");
                        Assert.That(Mathf.Abs(Mathf.DeltaAngle(0, shin.localEulerAngles.z)), Is.LessThan(85), side + " knee folds excessively.");
                    }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void MeditationFishingAndToolSocketReadDomainAndRestoreLocomotion()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Content/Scenes/MainGame.unity");
            try
            {
                var game = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameBootstrapper>()).Single();
                game.Initialize(); var actor = game.World.PlayerActor; var view = game.playerView;
                var pose = view.actionPose; var tools = view.tools; var rig = view.equipment;
                Assert.That(tools.toolSocket.parent.name, Is.EqualTo("hand_front"));
                var pelvis = rig.animator.transform.Find("root/pelvis"); float standingY = pelvis.localPosition.y;
                Assert.That(game.ToggleMeditate().Succeeded, Is.True);
                pose.RestoreAnimatedPose(); pose.Present(.3f); tools.Present(0);
                Assert.That(pelvis.localPosition.y, Is.LessThan(standingY - .2f));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(0, rig.animator.transform.Find("root/pelvis/thigh_front").localEulerAngles.z)), Is.GreaterThan(40));
                Assert.That(tools.meditationParticles.isPlaying, Is.True);
                Assert.That(tools.fishingVisuals.activeSelf, Is.False);
                game.World.Execute(new MoveCommand(actor.Id, new WorldPoint(1, 0)));
                pose.RestoreAnimatedPose(); pose.Present(.3f); tools.Present(.3f);
                Assert.That(pelvis.localPosition.y, Is.EqualTo(standingY).Within(.001f));
                var spot = game.World.Nodes.Values.Single(n => n.Spec.Kind == WorldNodeKind.FishingSpot);
                game.World.Motor.Displace(actor.Transform, new WorldPoint(0, -4.5f) - actor.Transform.Position);
                game.World.Motor.Displace(actor.Transform, new WorldPoint(spot.Position.X, -4.5f) - actor.Transform.Position);
                game.World.Motor.Displace(actor.Transform, spot.Position - actor.Transform.Position);
                Assert.That((actor.Transform.Position - spot.Position).Length, Is.LessThan(.01f), "Approach along the open path around the ore.");
                Assert.That(game.Fish(spot.Id).Succeeded, Is.True);
                game.SyncViews(); pose.RestoreAnimatedPose(); pose.Present(.3f); tools.Present(1);
                Assert.That(tools.fishingVisuals.activeSelf, Is.True);
                Assert.That(Vector3.Distance(tools.bambooRod.GetPosition(0), tools.toolSocket.position), Is.LessThan(.0001f));
                Assert.That(Vector2.Distance(tools.fishingLine.GetPosition(2), new Vector2(actor.LifeActivity.FishingTarget.X, actor.LifeActivity.FishingTarget.Y)), Is.LessThan(.06f));
                for (int i = 0; i < 41; i++) game.World.Tick(.1f);
                Assert.That(actor.LifeActivity.FishBiting, Is.True);
                Assert.That(game.Fish().Succeeded, Is.True);
                tools.Present(5); Assert.That(tools.fishingVisuals.activeSelf, Is.False);
                Assert.That(game.World.Player.Inventory.Count("spirit-fish"), Is.EqualTo(1));
                Assert.That(rig.CurrentAppearance.OutfitId, Is.EqualTo("classic").Or.EqualTo("jade"));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void DustEmitsOncePerRunLandingAtSteadyVelocity()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var r = ContentIntegrationTests.Registry;
                var world = WorldFactory.Create(r.BuildContent(), new WorldGeometry(new WorldPoint(20, 20), Array.Empty<WorldObstacle>()),
                    new SeededRandomSource(7), new DomainEventBus(), new WorldInitialState(r.player.Id, r.startingRealm.Id, r.mapDefinitionId, default, r.inventoryCapacity, r.CreateRules(), r.CreateRelations()));
                var root = (GameObject)PrefabUtility.InstantiatePrefab(r.player.prefab.gameObject, scene);
                var view = root.GetComponent<ActorView>(); view.Bind(world.PlayerActor);
                world.Execute(new MoveCommand(world.Player.ActorId, new WorldPoint(1, 0)));
                for (int i = 0; i < 4; i++) world.Tick(.1f);
                view.Sync(); var animator = view.actorAnimator.animator; var dust = view.footsteps;
                animator.Rebind(); animator.SetBool("Running", true); animator.Play("Run", 0, .1f); animator.Update(0);
                dust.particles.Clear(); dust.PresentStride(world.PlayerActor);
                animator.Play("Run", 0, .49f); animator.Update(0); dust.PresentStride(world.PlayerActor);
                Assert.That(dust.particles.particleCount, Is.Zero);
                animator.Play("Run", 0, .51f); animator.Update(0); dust.PresentStride(world.PlayerActor);
                Assert.That(dust.particles.particleCount, Is.EqualTo(2));
                dust.PresentStride(world.PlayerActor); dust.PresentStride(world.PlayerActor);
                Assert.That(dust.particles.particleCount, Is.EqualTo(2));
                animator.Play("Run", 0, 1.01f); animator.Update(0); dust.PresentStride(world.PlayerActor);
                Assert.That(dust.particles.particleCount, Is.EqualTo(4));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
