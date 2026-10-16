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
        public void BasicMaleBaseKeepsAllBodyPartsAndExplicitBlankAppearance()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(Registry.player.prefab.gameObject, scene);
                var rig = root.GetComponent<ModularEquipmentRenderer>();
                var body = new[] { "head", "neck", "torso", "shorts", "upper-front", "fore-front", "hand-front",
                    "upper-back", "fore-back", "hand-back", "thigh-front", "shin-front", "boot-front", "thigh-back", "shin-back", "boot-back" };
                var outfit = Registry.outfits.Single(o => o.appearanceId == "basic");
                rig.ApplyOutfit(outfit);
                CollectionAssert.AreEquivalent(body, rig.slots.Where(s => s.renderer.sprite != null).Select(s => s.id));
                Assert.That(rig.slots.Where(s => s.renderer.sprite != null).Select(s => s.renderer.sprite).Distinct().Count(), Is.EqualTo(16));
                foreach (var slot in rig.slots.Where(s => body.Contains(s.id)))
                {
                    Assert.That(slot.optional, Is.False, slot.id + " must not silently lose a body part.");
                    Assert.That(slot.renderer.sprite.rect.size, Is.EqualTo(new Vector2(1087, 1446)));
                    Assert.That(slot.renderer.sprite.pixelsPerUnit, Is.EqualTo(500));
                }
                Assert.That(outfit.hairVariants.All(h => h.hidden && h.front == null && h.back == null && h.ornament == null), Is.True);
                Assert.That(outfit.eyeVariants.All(e => e.hidden && e.sprite == null && e.closedSprite == null), Is.True);
                Assert.That(outfit.face.enabled, Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void WardrobeUsesDistinctGarmentsOnTheApprovedBodyCoordinates()
        {
            var body = Registry.outfits.Single(o => o.appearanceId == "basic");
            var classic = Registry.outfits.Single(o => o.appearanceId == "classic");
            var jade = Registry.outfits.Single(o => o.appearanceId == "jade");
            var garments = new[] { "neck", "torso", "shorts", "upper-front", "fore-front", "upper-back", "fore-back",
                "thigh-front", "shin-front", "boot-front", "thigh-back", "shin-back", "boot-back" };
            foreach (var outfit in new[] { classic, jade })
            {
                Assert.DoesNotThrow(outfit.ValidateSlots);
                Assert.That(outfit.includeInRobeCycle, Is.True);
                foreach (var slotId in garments)
                {
                    var original = body.slots.Single(s => s.slotId == slotId).sprite;
                    var garment = outfit.slots.Single(s => s.slotId == slotId).sprite;
                    Assert.That(garment, Is.Not.Null, outfit.appearanceId + ": " + slotId);
                    Assert.That(garment, Is.Not.SameAs(original));
                    Assert.That(garment.rect.size, Is.EqualTo(original.rect.size));
                    Assert.That(garment.pivot, Is.EqualTo(original.pivot));
                    Assert.That(garment.pixelsPerUnit, Is.EqualTo(original.pixelsPerUnit));
                }
                foreach (var slotId in new[] { "head", "hand-front", "hand-back" })
                    Assert.That(outfit.slots.Single(s => s.slotId == slotId).sprite,
                        Is.SameAs(body.slots.Single(s => s.slotId == slotId).sprite), slotId);
                Assert.That(outfit.eyeVariants.All(e => !e.hidden && e.sprite != null && e.closedSprite != null), Is.True);
                Assert.That(outfit.face.enabled, Is.True);
            }
            foreach (var slotId in garments)
                Assert.That(classic.slots.Single(s => s.slotId == slotId).sprite,
                    Is.Not.SameAs(jade.slots.Single(s => s.slotId == slotId).sprite), slotId);
        }

        [Test]
        public void BothHairstylesAreSharedByBothOutfitsAndMatchHeadBinding()
        {
            var classic = Registry.outfits.Single(o => o.appearanceId == "classic");
            var jade = Registry.outfits.Single(o => o.appearanceId == "jade");
            var head = classic.slots.Single(s => s.slotId == "head").sprite;
            CollectionAssert.AreEquivalent(new[] { "ink", "silver" }, classic.hairVariants.Select(h => h.id));
            CollectionAssert.AreEquivalent(new[] { "ink", "silver" }, jade.hairVariants.Select(h => h.id));
            foreach (var hair in classic.hairVariants)
            {
                var other = jade.hairVariants.Single(h => h.id == hair.id);
                Assert.That(hair.hidden || other.hidden, Is.False);
                var sprites = new[] { hair.front, hair.back, hair.ornament };
                CollectionAssert.AreEqual(sprites, new[] { other.front, other.back, other.ornament });
                foreach (var sprite in sprites)
                {
                    Assert.That(sprite, Is.Not.Null, hair.id);
                    Assert.That(sprite.rect.size, Is.EqualTo(head.rect.size));
                    Assert.That(sprite.pivot, Is.EqualTo(head.pivot));
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(head.pixelsPerUnit));
                }
            }
            Assert.That(classic.hairVariants.SelectMany(h => new[] { h.front, h.back, h.ornament }).Distinct().Count(), Is.EqualTo(6));
            var rig = Registry.player.prefab.GetComponent<ModularEquipmentRenderer>();
            foreach (var side in new[] { "front", "back" })
                Assert.That(rig.slots.Single(s => s.id == "fore-" + side).renderer.sortingOrder,
                    Is.GreaterThan(rig.slots.Single(s => s.id == "hand-" + side).renderer.sortingOrder), "Cuff must cover the hand joint cap.");
        }

        [Test]
        public void BlankAppearanceMustBeExplicitAndCannotHideBrokenSpriteReferences()
        {
            var config = UnityEngine.Object.Instantiate(Registry.outfits.Single(o => o.appearanceId == "basic"));
            try
            {
                Assert.DoesNotThrow(config.ValidateSlots);
                config.hairVariants[0].hidden = false;
                Assert.Throws<InvalidOperationException>(config.ValidateSlots);
                config.hairVariants[0].hidden = true;
                config.eyeVariants[0].hidden = false;
                Assert.Throws<InvalidOperationException>(config.ValidateSlots);
                config.eyeVariants[0].hidden = true;
                config.hairVariants[0].front = config.slots.First(s => s.sprite != null).sprite;
                Assert.Throws<InvalidOperationException>(config.ValidateSlots);
                config.hairVariants[0].front = null;
                config.eyeVariants[0].sprite = config.slots.First(s => s.sprite != null).sprite;
                Assert.Throws<InvalidOperationException>(config.ValidateSlots);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
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
