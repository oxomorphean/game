using System;
using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Client.Bootstrap;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LinhThu.Tests.ClientEditMode
{
    public sealed class ContentIntegrationTests
    {
        public static ContentRegistrySO Registry => AssetDatabase.LoadAssetAtPath<ContentRegistrySO>("Assets/_Project/Content/Configs/ContentRegistry.asset");
        [Test]
        public void AuthoredRegistryBuildsWorldWithoutMutatingConfigs()
        {
            var registry = Registry;
            var before = EditorJsonUtility.ToJson(registry.player);
            var world = WorldFactory.Create(registry.BuildContent(), new WorldGeometry(new WorldPoint(10, 6), Array.Empty<WorldObstacle>()), new SeededRandomSource(registry.randomSeed), new DomainEventBus(), new WorldInitialState(registry.player.Id, registry.startingRealm.Id, registry.mapDefinitionId, default, registry.inventoryCapacity, registry.CreateRules(), registry.CreateRelations(), beasts: registry.CreateBeastSeeds()));
            Assert.That(world.PlayerActor.Abilities.Cooldowns.Count, Is.EqualTo(3));
            Assert.That(world.Player.Beasts.All.Count, Is.EqualTo(1));
            world.PlayerActor.Stats.SetBase(StatId.Attack, 123);
            Assert.That(EditorJsonUtility.ToJson(registry.player), Is.EqualTo(before));
        }

        [Test]
        public void ActorAndNodeConfigsHaveMatchingPrefabReferences()
        {
            foreach (var actor in Registry.actors)
            {
                Assert.That(actor.prefab, Is.Not.Null, actor.Id);
                Assert.That(actor.prefab.config, Is.SameAs(actor));
            }

            foreach (var node in Registry.nodes)
            {
                Assert.That(node.prefab, Is.Not.Null, node.Id);
                Assert.That(node.prefab.config, Is.SameAs(node));
            }
        }

        [Test]
        public void PrefabsContainNoMissingScriptsOrBrokenSerializedReferences()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/_Project/Content/Prefabs"}))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, path);
                foreach (var component in prefab.GetComponentsInChildren<Component>(true).Where(c => c != null))
                {
                    var serialized = new SerializedObject(component);
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                            Assert.That(property.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)), Is.True, path + ": " + property.propertyPath);
                }
            }
        }

        [Test]
        public void SwappingAllOutfitsPreservesRunningClipPoseAndBindingPaths()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Content/Prefabs/Actors/HeroBase.prefab");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var rig = root.GetComponent<ModularEquipmentRenderer>();
                var controller = rig.animator.runtimeAnimatorController;
                rig.animator.Rebind();
                rig.animator.Play("Run", 0, .37f);
                rig.animator.Update(0);
                var state = rig.animator.GetCurrentAnimatorStateInfo(0);
                Assert.That(state.shortNameHash, Is.EqualTo(Animator.StringToHash("Run")));
                var transforms = root.GetComponentsInChildren<Transform>(true);
                var positions = transforms.Select(t => t.localPosition).ToArray();
                var rotations = transforms.Select(t => t.localRotation).ToArray();
                var scales = transforms.Select(t => t.localScale).ToArray();
                foreach (var outfit in Registry.outfits)
                {
                    rig.ApplyOutfit(outfit);
                    Assert.That(rig.animator.runtimeAnimatorController, Is.SameAs(controller));
                    Assert.That(rig.animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(state.normalizedTime));
                    CollectionAssert.AreEqual(positions, transforms.Select(t => t.localPosition));
                    CollectionAssert.AreEqual(rotations, transforms.Select(t => t.localRotation));
                    CollectionAssert.AreEqual(scales, transforms.Select(t => t.localScale));
                    foreach (var slot in rig.slots)
                        Assert.That(slot.renderer.sprite == outfit.slots.Single(s => s.slotId == slot.id).sprite, Is.True, outfit.Id + ": " + slot.id);
                }

                foreach (var clip in controller.animationClips)
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                        Assert.That(string.IsNullOrEmpty(binding.path) || rig.animator.transform.Find(binding.path) != null, Is.True, clip.name + ": " + binding.path);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void InvalidOutfitFailsBeforeChangingAnySprite()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var invalid = UnityEngine.Object.Instantiate(Registry.outfits[0]);
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(Registry.player.prefab.gameObject, scene);
                var rig = root.GetComponent<ModularEquipmentRenderer>();
                var before = rig.slots.Select(s => s.renderer.sprite).ToArray();
                invalid.slots = invalid.slots.Take(invalid.slots.Length - 1).ToArray();
                Assert.Throws<InvalidOperationException>(() => rig.ApplyOutfit(invalid));
                CollectionAssert.AreEqual(before, rig.slots.Select(s => s.renderer.sprite));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(invalid);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void ClientAssemblyHasNoDevToolsDependency()
        {
            Assert.That(typeof(ActorView).Assembly.GetReferencedAssemblies().Any(a => a.Name == "LinhThu.DevTools"), Is.False);
            Assert.That(typeof(WorldState).Assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("UnityEngine")), Is.False);
        }

        [Test]
        public void FullAppearancePaletteAndPropertyBlocksPreserveSharedMaterialsAndPose()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(Registry.player.prefab.gameObject, scene);
                var rig = root.GetComponent<ModularEquipmentRenderer>();
                var materials = rig.slots.Select(s => s.renderer.sharedMaterial).ToArray();
                Assert.That(materials.Distinct().Count(), Is.EqualTo(1));
                Assert.That(materials[0].shader.name, Is.EqualTo("LinhThu/Modular Hero Sprite"));
                rig.animator.Rebind();
                rig.animator.Play("Run", 0, .41f);
                rig.animator.Update(0);
                float time = rig.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                var transforms = root.GetComponentsInChildren<Transform>(true);
                var poses = transforms.Select(t => (t.localPosition, t.localRotation, t.localScale)).ToArray();
                foreach (var outfit in Registry.outfits)
                foreach (var hair in outfit.hairVariants)
                foreach (var eyes in outfit.eyeVariants)
                {
                    rig.ApplyAppearance(new LinhThu.Domain.Actors.ActorAppearance(outfit.appearanceId, hair.id, eyes.id));
                    foreach (var slot in rig.slots)
                    {
                        var expected = slot.id == "hair-front" ? hair.front : slot.id == "hair-back" ? hair.back
                            : slot.id == "hair-ornament" ? hair.ornament : slot.id == "eyes" ? eyes.sprite
                            : outfit.slots.Single(s => s.slotId == slot.id).sprite;
                        Assert.That(slot.renderer.sprite == expected, Is.True, slot.id);
                    }
                    CollectionAssert.AreEqual(poses, transforms.Select(t => (t.localPosition, t.localRotation, t.localScale)));
                    Assert.That(rig.animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(time));
                }
                var tint = new Color(.8f, .6f, .9f, 1);
                rig.SetTint("torso", tint);
                rig.HitFlash();
                var block = new MaterialPropertyBlock();
                foreach (var slot in rig.slots)
                {
                    slot.renderer.GetPropertyBlock(block);
                    Assert.That(block.GetFloat("_HitFlash"), Is.EqualTo(1));
                    if (slot.id == "torso") Assert.That(block.GetColor("_AppearanceTint"), Is.EqualTo(tint));
                }
                rig.AdvancePresentation(1);
                Assert.That(rig.FlashAmount, Is.Zero);
                CollectionAssert.AreEqual(materials, rig.slots.Select(s => s.renderer.sharedMaterial));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
