using System;
using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Client.Bootstrap;
using LinhThu.Client.Camera;
using LinhThu.Client.Environment;
using LinhThu.Client.World;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LinhThu.Tests.ClientEditMode
{
    public sealed class GroundingAndExpansionTests
    {
        private Scene scene;
        private GameBootstrapper game;
        [SetUp] public void Open()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Content/Scenes/MainGame.unity");
            game = All<GameBootstrapper>().Single(); game.Initialize();
        }
        [TearDown] public void Close()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            DaylightPresentation.ApplyLighting(12);
        }
        private T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>()).ToArray();
        private ModularMapProp[] Trees => All<ModularMapProp>().Where(p => p.visual != null && p.visual.sprite.name == "cherry-tree").ToArray();
        private void Place(WorldPoint point)
        {
            typeof(Transform2D).GetProperty(nameof(Transform2D.Position)).SetValue(game.World.PlayerActor.Transform, point);
            typeof(Transform2D).GetProperty(nameof(Transform2D.CurrentVelocity)).SetValue(game.World.PlayerActor.Transform, default(WorldPoint));
            game.SyncViews();
        }

        [Test]
        public void ContactShadowsStayAtVisibleFeetDespiteTransparentSpritePadding()
        {
            // These authored sprites have their pivot at the opaque foot, with transparent
            // pixels below it. Renderer.bounds.min alone therefore makes them appear to float.
            var groundedSprites = new[] { "rock", "bush", "grass-tuft", "white-flowers" };
            var props = All<ModularMapProp>().Where(p => p.visual != null && p.visual.sprite != null
                && groundedSprites.Contains(p.visual.sprite.name)).ToArray();
            Assert.That(props.Length, Is.GreaterThan(40));
            foreach (var prop in props)
            {
                var sort = prop.GetComponent<YSortRenderer>();
                var blob = prop.GetComponent<GroundContactShadow>();
                blob.Present();
                float footY = prop.visual.transform.position.y;
                Assert.That(Mathf.Abs(sort.GroundY - footY), Is.LessThan(.035f), prop.name);
                Assert.That(Mathf.Abs(blob.shadow.bounds.center.y - footY), Is.LessThan(.06f), prop.name);
                Assert.That(blob.shadow.bounds.min.y, Is.LessThan(footY), "Shadow must extend below the visible foot: " + prop.name);
            }
        }

        [Test]
        public void EveryActorAndRaisedPropHasVisibleContactShadowInItsOwnBand()
        {
            DaylightPresentation.ApplyLighting(12);
            Assert.That(Trees.Length, Is.EqualTo(3));
            Assert.That(All<MeadowGroundSurface>().Single().GetComponent<Renderer>().sortingOrder, Is.EqualTo(-10000));
            Assert.That(All<MeadowTerrainPath>().Single().GetComponent<Renderer>().sortingOrder, Is.EqualTo(-9500));
            foreach (var view in All<ActorView>())
            {
                var blob = view.GetComponent<GroundContactShadow>(); Assert.That(blob, Is.Not.Null, view.name); blob.Present();
                Assert.That(blob.opacity, Is.EqualTo(.38f));
                Assert.That(blob.shadow.GetComponentInParent<SortingGroup>(), Is.Null, "Shadow must be outside the body's sorting group.");
                Assert.That(blob.shadow.color, Is.EqualTo(new Color(7 / 255f, 18 / 255f, 14 / 255f, .38f)));
                Assert.That(Vector2.Distance(blob.shadow.transform.position, view.transform.position), Is.LessThan(.06f));
            }
            foreach (var prop in All<ModularMapProp>())
            {
                var sort = prop.GetComponent<YSortRenderer>(); Assert.That(sort, Is.Not.Null, prop.name); sort.Refresh();
                if (prop.floorLayer)
                {
                    Assert.That(sort.targetRenderer.sortingOrder, Is.InRange(-9500, -9000), prop.name);
                    continue;
                }
                Assert.That(prop.contactShadow, Is.Not.Null, prop.name);
                Assert.That(prop.contactShadow.enabled, Is.True, prop.name);
                Assert.That(prop.contactShadow.color.a, Is.InRange(.23f, .35f), prop.name);
                Assert.That(prop.contactShadow.sortingOrder, Is.LessThan(sort.targetRenderer.sortingOrder), prop.name);
                Assert.That(Mathf.Abs(prop.contactShadow.bounds.center.y - sort.GroundY), Is.LessThan(.15f), prop.name);
            }
            foreach (var shadow in All<Renderer>().Where(r => r.sharedMaterial != null && r.sharedMaterial.shader.name.Contains("Shadow")))
            {
                Assert.That(shadow.sortingOrder, Is.InRange(-8500, -8000), shadow.name);
                Assert.That(shadow.gameObject.activeInHierarchy, Is.True, shadow.name);
            }
        }

        [Test]
        public void WalkAndDashCannotCrossAnyCherryTrunkFromAnyDirection()
        {
            var world = game.World; var actor = world.PlayerActor;
            foreach (var tree in Trees)
            {
                var trunk = tree.GetComponent<ObstacleAuthoring>().Definition();
                Assert.That(trunk.Box, Is.False); Assert.That(trunk.HalfSize.X, Is.InRange(.48f, .60f));
                Assert.That(world.Geometry.CanStand(trunk.Center, actor.Transform.CollisionRadius), Is.False);
                Assert.That(world.Geometry.CanStand(trunk.Center + new WorldPoint(1, 0), actor.Transform.CollisionRadius), Is.True, "Canopy is not a collider.");
                for (int side = 0; side < 8; side++)
                    foreach (bool dash in new[] { false, true })
                    {
                        world.Execute(new MoveCommand(actor.Id, default));
                        for (int i = 0; i < 90; i++) world.Tick(1f / 60);
                        var outward = new WorldPoint(Mathf.Cos(side * Mathf.PI / 4), Mathf.Sin(side * Mathf.PI / 4));
                        var origin = trunk.Center + outward * (trunk.HalfSize.X + actor.Transform.CollisionRadius + .35f);
                        Assert.That(world.Geometry.CanStand(origin, actor.Transform.CollisionRadius), Is.True, tree.name);
                        Place(origin);
                        var result = dash ? world.Execute(new DashCommand(actor.Id, outward * -1)) : world.Execute(new MoveCommand(actor.Id, outward * -1));
                        Assert.That(result.Succeeded, Is.True);
                        var previous = origin;
                        for (int i = 0; i < 40; i++)
                        {
                            world.Tick(1f / 60);
                            Assert.That(world.Geometry.CanStand(actor.Transform.Position, actor.Transform.CollisionRadius), Is.True, tree.name);
                            Assert.That(trunk.Crosses(previous, actor.Transform.Position), Is.False, "A simulation step cannot tunnel through the trunk.");
                            previous = actor.Transform.Position;
                        }
                    }
            }
        }

        [Test]
        public void CanopiesAndTentFadeOnlyBehindAndRestoreWithoutChangingFootprints()
        {
            var fades = All<CanopyOcclusionFade>(); Assert.That(fades.Length, Is.EqualTo(4));
            var playerSort = game.playerView.GetComponent<YSortRenderer>();
            foreach (var fade in fades)
            {
                var obstacle = fade.GetComponent<ObstacleAuthoring>().Definition();
                float ground = fade.groundSort.GroundY;
                game.playerView.transform.position = new Vector3(fade.visual.bounds.center.x, ground + .65f, 0);
                fade.groundSort.Refresh(); playerSort.Refresh();
                Assert.That(fade.groundSort.targetRenderer.sortingOrder, Is.GreaterThan(playerSort.group.sortingOrder));
                fade.Present(.05f); Assert.That(fade.Alpha, Is.InRange(.43f, .99f), "Fade must be gradual.");
                fade.Present(.5f); Assert.That(fade.Alpha, Is.EqualTo(.42f).Within(.0001f), fade.name);
                if (fade.visual is SpriteRenderer sprite) Assert.That(sprite.color.a, Is.EqualTo(.42f).Within(.0001f));
                else { var block = new MaterialPropertyBlock(); fade.visual.GetPropertyBlock(block); Assert.That(block.GetColor("_Color").a, Is.EqualTo(.42f).Within(.0001f)); }
                game.playerView.transform.position = new Vector3(fade.visual.bounds.center.x, ground - .2f, 0);
                playerSort.Refresh(); fade.Present(.5f);
                Assert.That(playerSort.group.sortingOrder, Is.GreaterThan(fade.groundSort.targetRenderer.sortingOrder));
                Assert.That(fade.Alpha, Is.EqualTo(1));
                game.playerView.transform.position = new Vector3(fade.visual.bounds.max.x + 3, ground + 1, 0);
                fade.Present(.5f); Assert.That(fade.Alpha, Is.EqualTo(1), "Being north is insufficient without visual overlap.");
                Assert.That(fade.GetComponent<ObstacleAuthoring>().Definition().Center, Is.EqualTo(obstacle.Center));
            }
        }

        [Test]
        public void AllFiveCooldownsHideNamesAndKeepGoldBadgesAboveOverlay()
        {
            foreach (var slot in game.hud.skills)
            {
                slot.SetCooldown(1, 1.2f);
                Assert.That(slot.Name.gameObject.activeSelf || slot.Detail.gameObject.activeSelf, Is.False);
                Assert.That(slot.CooldownText.gameObject.activeSelf && slot.Cooldown.gameObject.activeSelf, Is.True);
                Assert.That(slot.CooldownText.text, Is.EqualTo("1.0s"));
                Assert.That((Color32)slot.Key.color, Is.EqualTo(new Color32(242, 217, 141, 255)));
                Assert.That(slot.Key.transform.parent.GetSiblingIndex(), Is.GreaterThan(slot.Cooldown.transform.GetSiblingIndex()));
                slot.SetCooldown(0, 1.2f);
                Assert.That(slot.Name.gameObject.activeSelf && slot.Detail.gameObject.activeSelf, Is.True);
                Assert.That(slot.CooldownText.gameObject.activeSelf || slot.Cooldown.gameObject.activeSelf, Is.False);
            }
            game.hud.dashButton.onClick.Invoke(); game.hud.Refresh();
            Assert.That(game.World.PlayerActor.Dash.Active, Is.True);
            Assert.That(game.hud.skills[3].Name.gameObject.activeSelf, Is.False);
            Assert.That(game.hud.skills[3].CooldownText.text, Does.EndWith("s"));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void EachCommittedSkillImmediatelyPosesTheArmWithoutMovingDomainRoot(int slot)
        {
            var feedback = game.playerView.actionFeedback; Assert.That(feedback, Is.Not.Null);
            var arm = feedback.upperArm.localRotation;
            Assert.That(game.Cast(slot, new WorldPoint(1, 0)).Succeeded, Is.True);
            // Thanh Phong may displace through its Domain effect; presentation adds no movement.
            var root = game.World.PlayerActor.Transform.Position;
            Assert.That(feedback.CastWeight, Is.GreaterThan(.99f));
            feedback.Present(0, 0);
            Assert.That(Quaternion.Angle(arm, feedback.upperArm.localRotation), Is.GreaterThan(20));
            Assert.That(game.World.PlayerActor.Transform.Position, Is.EqualTo(root));
            feedback.ResetPresentation(); Assert.That(Quaternion.Angle(arm, feedback.upperArm.localRotation), Is.LessThan(.001f));
            Assert.That(game.Cast(slot, new WorldPoint(1, 0)).Succeeded, Is.False);
            Assert.That(feedback.CastWeight, Is.Zero, "Rejected requests must not animate a successful cast.");
        }

        [Test]
        public void SwordBobsAndDashLeavesThreeFrozenFadingSilhouettes()
        {
            var feedback = game.playerView.actionFeedback;
            feedback.Present(0, 0); var sword = feedback.spiritSword.localPosition;
            feedback.Present(0, .5f); Assert.That(feedback.spiritSword.localPosition.y, Is.GreaterThan(sword.y));
            Assert.That(game.Dash(new WorldPoint(1, 0)).Succeeded, Is.True);
            feedback.Present(.02f, 0);
            var first = scene.GetRootGameObjects().Single(r => r.name == "DashAfterimage");
            var mesh = first.GetComponentsInChildren<MeshFilter>().First(m => m.sharedMesh.vertexCount > 0).sharedMesh;
            var vertices = mesh.vertices;
            for (int i = 0; i < 6; i++) { game.World.Tick(.02f); game.SyncViews(); feedback.Present(.02f, .02f * i); }
            Assert.That(feedback.ActiveAfterimages, Is.EqualTo(3));
            CollectionAssert.AreEqual(vertices, mesh.vertices, "Old silhouettes must stay at their captured world positions.");
            Assert.That(first.transform.parent, Is.Null);
            feedback.Present(.4f, 1); Assert.That(feedback.ActiveAfterimages, Is.Zero);
        }

        [Test]
        public void DamageShakesOnlyDummyVisualAndReturnsExactlyToRest()
        {
            var dummy = game.sceneActors.Single(v => v.trainingDummy != null);
            var root = dummy.transform.position; var rest = dummy.trainingDummy.visual.transform.localPosition;
            game.World.DealDamage(dummy.Entity, game.World.Player.ActorId, 1); game.World.Events.Flush();
            Assert.That(dummy.trainingDummy.FlashAmount, Is.EqualTo(1));
            dummy.trainingDummy.AdvancePresentation(.02f);
            Assert.That(dummy.trainingDummy.visual.transform.localPosition, Is.Not.EqualTo(rest));
            Assert.That(dummy.transform.position, Is.EqualTo(root));
            dummy.trainingDummy.AdvancePresentation(.3f);
            Assert.That(dummy.trainingDummy.FlashAmount, Is.Zero);
            Assert.That(dummy.trainingDummy.visual.transform.localPosition, Is.EqualTo(rest));
            for (int i = 0; i < 60; i++) game.World.Tick(1f / 60);
            Assert.That(dummy.Entity.Transform.Position, Is.EqualTo(new WorldPoint(root.x, root.y)), "Own footprint must not depenetrate the stationary dummy.");
        }

        [Test]
        public void WiderWorldPreservesGrassScaleAndCameraSmoothlyClampsAtBothEdges()
        {
            var ground = game.map.GetComponentInChildren<MeadowGroundSurface>();
            var material = ground.GetComponent<Renderer>().sharedMaterial;
            float scale = material.GetFloat("_GrassScale"); var texture = material.GetTexture("_GrassTex");
            game.map.size = new Vector2(60, game.map.size.y); game.map.SynchronizeGroundBounds();
            Assert.That(ground.GetComponent<MeshFilter>().sharedMesh.bounds.size.x, Is.EqualTo(60));
            Assert.That(material.GetFloat("_GrassScale"), Is.EqualTo(scale)); Assert.That(material.GetTexture("_GrassTex"), Is.SameAs(texture));
            var geometry = new WorldGeometry(game.map.WorldBounds, All<ObstacleAuthoring>().Select(o => o.Definition()));
            Assert.That(geometry.CanStand(new WorldPoint(25, 0), .2f), Is.True);
            var follow = All<TopDownCameraController>().Single();
            foreach (float aspect in new[] { 16f / 9, 21f / 9 })
            {
                follow.viewCamera.aspect = aspect;
                foreach (float side in new[] { -1f, 1f })
                {
                    follow.transform.position = new Vector3(0, 0, -10);
                    game.playerView.transform.position = new Vector3(side * 29, 0, 0);
                    follow.Follow(.01f);
                    Assert.That(Mathf.Abs(follow.transform.position.x), Is.InRange(.01f, 5), "Follow must interpolate, not snap.");
                    for (int i = 0; i < 120; i++) follow.Follow(1f / 60);
                    float halfView = follow.viewCamera.orthographicSize * aspect;
                    Assert.That(Mathf.Abs(follow.transform.position.x), Is.EqualTo(30 - halfView).Within(.01f));
                    Assert.That(Mathf.Abs(follow.transform.position.x) + halfView, Is.LessThanOrEqualTo(30.0001f));
                }
            }
        }

        [Test]
        public void RelocatedLargeStonesClearTheRoadAndPondFitsInitialView()
        {
            var path = game.map.GetComponentInChildren<MeadowTerrainPath>();
            foreach (string name in new[] { "Đá lát 07", "Đá lát 10" })
            {
                var rock = All<ModularMapProp>().Single(p => p.name == name);
                var footprint = rock.GetComponent<ObstacleAuthoring>().Definition();
                for (int segment = 0; segment < path.knots.Length - 1; segment++)
                    for (int sample = 0; sample <= 32; sample++)
                    {
                        float t = sample / 32f;
                        var point = path.transform.TransformPoint(path.Evaluate(segment, t));
                        float width = Mathf.Lerp(path.knots[segment].halfWidth, path.knots[segment + 1].halfWidth, Mathf.SmoothStep(0, 1, t));
                        Assert.That(footprint.Overlaps(new WorldPoint(point.x, point.y), width + .1f), Is.False, name);
                    }
            }
            var pond = All<ModularMapProp>().Single(p => p.visual != null && p.visual.sprite.name == "pond-cluster");
            Assert.That(pond.transform.position.x, Is.EqualTo(5.9f).Within(.001f));
            game.gameCamera.aspect = 16f / 9;
            All<TopDownCameraController>().Single().Follow(1);
            Assert.That(game.gameCamera.WorldToViewportPoint(pond.visual.bounds.max).x, Is.LessThan(1));
            Assert.That(game.gameCamera.WorldToViewportPoint(pond.visual.bounds.min).y, Is.GreaterThan(0));
        }
    }
}
