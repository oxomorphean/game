using System;
using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Client.Environment;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LinhThu.Tests.ClientEditMode
{
    public sealed class EnvironmentFeelTests
    {
        private static WorldState World()
        {
            var registry = ContentIntegrationTests.Registry;
            return WorldFactory.Create(registry.BuildContent(), new WorldGeometry(new WorldPoint(20, 20), Array.Empty<WorldObstacle>()),
                new SeededRandomSource(7), new DomainEventBus(), new WorldInitialState(registry.player.Id, registry.startingRealm.Id,
                    registry.mapDefinitionId, default, registry.inventoryCapacity, registry.CreateRules(), registry.CreateRelations(), beasts: registry.CreateBeastSeeds()));
        }

        [Test]
        public void MeadowUsesPaintedTexturesAndSharedSunlightAcrossGrassAndPath()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Content/Scenes/MainGame.unity");
            try
            {
                var roots = scene.GetRootGameObjects();
                var ground = roots.SelectMany(r => r.GetComponentsInChildren<MeadowGroundSurface>()).Single();
                var block = new MaterialPropertyBlock();
                ground.Rebuild();
                ground.GetComponent<MeshRenderer>().GetPropertyBlock(block);
                var light = block.GetVector("_DappledLight");
                Assert.That(light.x, Is.GreaterThan(0));
                var groundMaterial = ground.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(AssetDatabase.GetAssetPath(groundMaterial.GetTexture("_GrassTex")), Does.EndWith("/meadow-grass.png"));
                Assert.That(AssetDatabase.GetAssetPath(groundMaterial.GetTexture("_EarthTex")), Does.EndWith("/meadow-earth.png"));
                foreach (var path in roots.SelectMany(r => r.GetComponentsInChildren<MeadowTerrainPath>()))
                {
                    Assert.That(path.groundSurface, Is.SameAs(ground));
                    path.Rebuild();
                    path.GetComponent<MeshRenderer>().GetPropertyBlock(block);
                    Assert.That(block.GetVector("_DappledLight"), Is.EqualTo(light));
                }
                var props = roots.SelectMany(r => r.GetComponentsInChildren<ModularMapProp>())
                    .Where(p => p.visual != null && (p.visual.sprite.name == "bush" || p.visual.sprite.name == "grass-tuft")).ToArray();
                Assert.That(props.Length, Is.GreaterThan(0));
                foreach (var prop in props)
                {
                    Assert.That(prop.GetComponent<InteractiveFoliage>(), Is.Not.Null, prop.name);
                    Assert.That(prop.GetComponent<InteractiveFoliage>().visual, Is.SameAs(prop.visual.transform));
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FoliageRespondsToPlayerOrCompanionAndRecoversWithoutMovingGameplay(bool companion)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var world = World();
                var actor = world.PlayerActor;
                if (companion)
                {
                    var beast = world.Player.Beasts.All.Single();
                    Assert.That(world.Execute(new SummonCommand(world.Player.ActorId, beast.Id)).Succeeded, Is.True);
                    world.Motor.Displace(world.PlayerActor.Transform, new WorldPoint(4, 0));
                    actor = world.Actors[beast.ActorId.Value];
                }
                else world.Execute(new MoveCommand(actor.Id, new WorldPoint(1, 0)));
                world.Tick(.1f);
                Assert.That(actor.Transform.CurrentVelocity.Length, Is.GreaterThan(0));
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Content/Prefabs/Environment/grass-tuft.prefab");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                root.transform.position = new Vector3(actor.Transform.Position.X + .1f, actor.Transform.Position.Y);
                var foliage = root.GetComponent<InteractiveFoliage>();
                Assert.That(foliage, Is.Not.Null);
                foliage.windBend = 0;
                foliage.Bind(world);
                var rootPosition = root.transform.position;
                var actorPosition = actor.Transform.Position;
                foliage.Present(.1f, 0);
                float deflection = Mathf.Abs(foliage.ContactAngle);
                Assert.That(deflection, Is.GreaterThan(.1f));
                Assert.That(root.transform.position, Is.EqualTo(rootPosition));
                Assert.That(actor.Transform.Position, Is.EqualTo(actorPosition));
                if (companion)
                {
                    var beast = world.Player.Beasts.All.Single();
                    Assert.That(world.Execute(new RecallCommand(world.Player.ActorId, beast.Id)).Succeeded, Is.True);
                    Assert.That(world.Actors.ContainsKey(actor.Id), Is.False);
                    Assert.That(actor.Transform.CurrentVelocity.Length, Is.GreaterThan(0), "The retired actor still has velocity; foliage must read only the live roster.");
                }
                else actor.SetStaggered(true);
                for (int i = 0; i < 20; i++) foliage.Present(.1f, i * .1f);
                Assert.That(Mathf.Abs(foliage.ContactAngle), Is.LessThan(deflection * .01f));
                foliage.Bind(World());
                Assert.That(foliage.ContactAngle, Is.EqualTo(0), "Loading a new world must release the old roster and contact pose.");
                foliage.windBend = 2;
                foliage.Present(.1f, 2);
                var first = foliage.visual.localRotation;
                foliage.Present(.1f, 3);
                Assert.That(Quaternion.Angle(first, foliage.visual.localRotation), Is.GreaterThan(.01f));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void FootstepsEmitOnStartBrakeAndDashWithoutRepeatingPerSyncOrChangingState()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var world = World();
                var root = (GameObject)PrefabUtility.InstantiatePrefab(ContentIntegrationTests.Registry.player.prefab.gameObject, scene);
                var view = root.GetComponent<ActorView>();
                view.Bind(world.PlayerActor);
                var dust = view.footsteps;
                Assert.That(dust, Is.Not.Null);
                Assert.That(dust.frontBoot, Is.Not.Null);
                Assert.That(dust.backBoot, Is.Not.Null);
                var particles = dust.particles;
                Assert.That(particles.main.maxParticles, Is.LessThanOrEqualTo(64));
                world.Execute(new MoveCommand(world.Player.ActorId, new WorldPoint(1, 0)));
                world.Tick(.1f);
                view.Sync();
                int starting = particles.particleCount;
                Assert.That(starting, Is.GreaterThan(0));
                view.Sync(); view.Sync();
                Assert.That(particles.particleCount, Is.EqualTo(starting));
                world.Execute(new MoveCommand(world.Player.ActorId, default));
                world.Tick(.1f);
                view.Sync();
                int braking = particles.particleCount;
                Assert.That(braking, Is.GreaterThan(starting));
                world.Execute(new DashCommand(world.Player.ActorId, new WorldPoint(1, 0)));
                var position = world.PlayerActor.Transform.Position;
                view.Sync();
                Assert.That(particles.particleCount, Is.GreaterThan(braking));
                Assert.That(world.PlayerActor.Transform.Position, Is.EqualTo(position));
                particles.Simulate(1, true, false);
                Assert.That(particles.particleCount, Is.Zero);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
