using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinhThu.Client.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace LinhThu.Tests.ClientEditMode
{
    public sealed class JointSkinningTests
    {
        private Scene scene;
        private ActorView view;
        private ModularEquipmentRenderer Rig => view.equipment;
        private ModularEquipmentRenderer.Slot Slot(string id) => Rig.slots.Single(s => s.id == id);
        private Transform Bone(string name) => Rig.visual.GetComponentsInChildren<Transform>().Single(t => t.name == name);
        private static readonly string[] Pairs = {
            "neck|torso", "torso|shorts", "torso|upper-front", "torso|upper-back",
            "upper-front|fore-front", "upper-back|fore-back", "fore-front|hand-front", "fore-back|hand-back",
            "shorts|thigh-front", "shorts|thigh-back", "thigh-front|shin-front", "thigh-back|shin-back",
            "shin-front|boot-front", "shin-back|boot-back" };

        [SetUp]
        public void OpenHero()
        {
            var registry = ContentIntegrationTests.Registry;
            scene = EditorSceneManager.NewPreviewScene();
            var root = (GameObject)PrefabUtility.InstantiatePrefab(registry.player.prefab.gameObject, scene);
            view = root.GetComponent<ActorView>();
            var world = WorldFactory.Create(registry.BuildContent(), new WorldGeometry(new WorldPoint(20, 20), Array.Empty<WorldObstacle>()),
                new SeededRandomSource(7), new DomainEventBus(), new WorldInitialState(registry.player.Id, registry.startingRealm.Id,
                    registry.mapDefinitionId, default, registry.inventoryCapacity, registry.CreateRules(), registry.CreateRelations()));
            view.Bind(world.PlayerActor);
        }

        [TearDown]
        public void CloseHero() { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }

        [Test]
        public void EveryOutfitHasNormalizedWeightsAndRetainsTheOriginalBindCoordinates()
        {
            Assert.That(Rig.GetComponentsInChildren<SpriteSkin>().Length, Is.EqualTo(15));
            var vertices = new List<Vector3>(); var unique = new HashSet<Sprite>();
            foreach (var outfit in Rig.outfits)
            {
                Rig.ApplyOutfit(outfit);
                foreach (var slot in Rig.slots.Where(s => s.Geometry.IsSkinned))
                {
                    var skin = slot.renderer.GetComponent<SpriteSkin>(); var sprite = slot.renderer.sprite;
                    unique.Add(sprite);
                    Assert.That(skin.SetBoneTransforms(skin.boneTransforms), Is.EqualTo(SpriteSkinState.Ready), slot.id);
                    Assert.That(sprite.GetBindPoses().Length, Is.EqualTo(16));
                    Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(1087, 1446)));
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(500));
                    foreach (var w in sprite.GetVertexAttribute<BoneWeight>(VertexAttribute.BlendWeight))
                    {
                        Assert.That(w.weight0 + w.weight1 + w.weight2 + w.weight3, Is.EqualTo(1).Within(.00001));
                        foreach (var index in new[] { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 }) Assert.That(index, Is.InRange(0, 15));
                    }
                    slot.Geometry.CopyWorldVertices(vertices);
                    var original = sprite.vertices;
                    for (int i = 0; i < original.Length; i++)
                        Assert.That(Vector3.Distance(vertices[i], slot.renderer.transform.TransformPoint(original[i])), Is.LessThan(.00001), slot.id);
                }
            }
            Assert.That(unique.Count, Is.EqualTo(41));
            Assert.That(Rig.visual.localScale, Is.EqualTo(Vector3.one * .625f));
            foreach (var t in Rig.GetComponentsInChildren<SpriteSkin>().First().boneTransforms) Assert.That(t.localScale, Is.EqualTo(Vector3.one), t.name);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void OverlapRemainsCoincidentAtExtremeJointsForEveryOutfitAndFacing(int facing)
        {
            Rig.SetFacing(facing);
            // Beyond the attack's 12-degree chest lean; limb bends include seated/fishing extremes.
            Bone("chest").localRotation = Quaternion.Euler(0, 0, -25);
            Bone("upper_front").localRotation = Quaternion.Euler(0, 0, -65);
            Bone("fore_front").localRotation = Quaternion.Euler(0, 0, 85);
            Bone("upper_back").localRotation = Quaternion.Euler(0, 0, 38);
            Bone("fore_back").localRotation = Quaternion.Euler(0, 0, -70);
            Bone("thigh_front").localRotation = Quaternion.Euler(0, 0, -60);
            Bone("shin_front").localRotation = Quaternion.Euler(0, 0, 90);
            Bone("thigh_back").localRotation = Quaternion.Euler(0, 0, 55);
            Bone("shin_back").localRotation = Quaternion.Euler(0, 0, -85);
            foreach (var outfit in Rig.outfits)
            {
                Rig.ApplyOutfit(outfit);
                foreach (var pair in Pairs)
                {
                    var ids = pair.Split('|'); var first = CanvasVertices(Slot(ids[0])); var second = CanvasVertices(Slot(ids[1]));
                    var shared = first.Keys.Intersect(second.Keys).ToArray();
                    Assert.That(shared.Length, Is.GreaterThan(3), outfit.appearanceId + ": missing artwork overlap " + pair);
                    foreach (var key in shared)
                        Assert.That(Vector3.Distance(first[key], second[key]), Is.LessThan(.00001), outfit.appearanceId + " " + pair + " " + key);
                }
            }
        }

        [Test]
        public void AttackBendsUpperTorsoWhileItsLowerEdgeStaysAttachedToPelvis()
        {
            var torso = Slot("torso");
            var original = CanvasVertices(torso);
            var low = original.Keys.OrderBy(p => Vector2.Distance(p, new Vector2(560, 700))).First();
            var high = original.Keys.OrderBy(p => Vector2.Distance(p, new Vector2(560, 840))).First();
            view.actionFeedback.OnCast(new WorldPoint(1, 0)); view.actionFeedback.Present(0, 0);
            var bent = CanvasVertices(torso);
            Assert.That(Mathf.DeltaAngle(0, Bone("chest").localEulerAngles.z), Is.EqualTo(-12).Within(.001));
            Assert.That(Vector3.Distance(original[low], bent[low]), Is.LessThan(.00001), "The lower waist must follow pelvis, not chest.");
            Assert.That(Vector3.Distance(original[high], bent[high]), Is.GreaterThan(.005), "Upper torso must keep the intended attack lean.");
            var bounds = torso.Geometry.WorldBounds;
            foreach (var vertex in bent.Values)
                Assert.That(vertex.x, Is.InRange(bounds.min.x - .00001f, bounds.max.x + .00001f));
            Assert.That(view.VisualBounds.max.y, Is.GreaterThan(bounds.max.y));
        }

        [Test]
        public void SwapDuringAnimationPreservesTimeBonesAndPose()
        {
            Rig.animator.Rebind(); Rig.animator.Play("Run", 0, .37f); Rig.animator.Update(0);
            view.actionFeedback.OnCast(new WorldPoint(1, 0)); view.actionFeedback.Present(0, 0);
            var time = Rig.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            var controller = Rig.animator.runtimeAnimatorController;
            var bones = Rig.GetComponentsInChildren<SpriteSkin>().First().boneTransforms;
            var poses = bones.Select(t => (t.localPosition, t.localRotation, t.localScale)).ToArray();
            foreach (var outfit in Rig.outfits)
            {
                Rig.ApplyOutfit(outfit);
                foreach (var skin in Rig.GetComponentsInChildren<SpriteSkin>())
                {
                    CollectionAssert.AreEqual(bones, skin.boneTransforms);
                    Assert.That(skin.SetBoneTransforms(skin.boneTransforms), Is.EqualTo(SpriteSkinState.Ready));
                }
                CollectionAssert.AreEqual(poses, bones.Select(t => (t.localPosition, t.localRotation, t.localScale)));
                Assert.That(Rig.animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(time));
                Assert.That(Rig.animator.runtimeAnimatorController, Is.SameAs(controller));
            }
        }

        [Test]
        public void UnweightedClothingFailsAtomicallyEvenWithMatchingCanvasAndPivot()
        {
            var invalid = UnityEngine.Object.Instantiate(Rig.CurrentOutfit);
            var original = Slot("torso").renderer.sprite;
            var sprite = Sprite.Create(original.texture, original.rect, original.pivot / original.rect.size, original.pixelsPerUnit);
            try
            {
                int torsoIndex = Array.FindIndex(invalid.slots, s => s.slotId == "torso");
                invalid.slots[torsoIndex].sprite = sprite;
                var before = Rig.slots.Select(s => s.renderer.sprite).ToArray(); var appearance = Rig.CurrentAppearance;
                Assert.Throws<InvalidOperationException>(() => Rig.ApplyOutfit(invalid));
                CollectionAssert.AreEqual(before, Rig.slots.Select(s => s.renderer.sprite));
                Assert.That(Rig.CurrentAppearance, Is.EqualTo(appearance));
            }
            finally { UnityEngine.Object.DestroyImmediate(invalid); UnityEngine.Object.DestroyImmediate(sprite); }
        }

        [Test]
        public void DashSnapshotUsesThisPoseImmediatelyAndStaysFrozenAfterBonesMove()
        {
            var torso = Slot("torso");
            var before = CanvasVertices(torso);
            var anchor = before.Keys.OrderBy(p => Vector2.Distance(p, new Vector2(560, 700))).First();
            Bone("chest").localRotation = Quaternion.Euler(0, 0, -25);
            typeof(ActorActionFeedback).GetMethod("CaptureGhost", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view.actionFeedback, null);
            var ghost = scene.GetRootGameObjects().Single(g => g.name == "DashAfterimage");
            var mesh = ghost.GetComponentsInChildren<MeshFilter>().Single(f => f.name == "torso").sharedMesh;
            var frozen = mesh.vertices;
            var sprite = torso.renderer.sprite;
            int index = Array.FindIndex(sprite.vertices, p => Vector2Int.RoundToInt(p * sprite.pixelsPerUnit + sprite.pivot) == anchor);
            Assert.That(Vector3.Distance(frozen[index], before[anchor]), Is.LessThan(.00001), "A rigid or stale snapshot would detach the waist.");
            Bone("chest").localRotation = Quaternion.Euler(0, 0, 20); view.transform.position += Vector3.right;
            CollectionAssert.AreEqual(frozen, mesh.vertices);
            Assert.That(torso.Geometry.WorldBounds.center.x, Is.GreaterThan(mesh.bounds.center.x + .5f));
        }

        private static Dictionary<Vector2Int, Vector3> CanvasVertices(ModularEquipmentRenderer.Slot slot)
        {
            var world = new List<Vector3>(); slot.Geometry.CopyWorldVertices(world);
            var sprite = slot.renderer.sprite; var local = sprite.vertices;
            return Enumerable.Range(0, local.Length).ToDictionary(i => Vector2Int.RoundToInt(local[i] * sprite.pixelsPerUnit + sprite.pivot), i => world[i]);
        }
    }
}
