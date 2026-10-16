using System;
using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LinhThu.Tests.ClientEditMode
{
    public sealed class FacialExpressionTests
    {
        private Scene scene;
        private ActorView view;
        private WorldState world;
        private ModularEquipmentRenderer Rig => view.equipment;
        private SpriteRenderer Part(string id) => Rig.slots.Single(s => s.id == id).renderer;

        [SetUp]
        public void OpenIsolatedHero()
        {
            var registry = ContentIntegrationTests.Registry;
            scene = EditorSceneManager.NewPreviewScene();
            var root = (GameObject)PrefabUtility.InstantiatePrefab(registry.player.prefab.gameObject, scene);
            view = root.GetComponent<ActorView>();
            world = WorldFactory.Create(registry.BuildContent(), new WorldGeometry(new WorldPoint(20, 20), Array.Empty<WorldObstacle>()),
                new SeededRandomSource(7), new DomainEventBus(), new WorldInitialState(registry.player.Id, registry.startingRealm.Id,
                    registry.mapDefinitionId, default, registry.inventoryCapacity, registry.CreateRules(), registry.CreateRelations()));
            view.Bind(world.PlayerActor);
            Assert.That(view.face, Is.Not.Null);
            Assert.That(view.face.view, Is.SameAs(view));
        }

        [TearDown]
        public void CloseHero() { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }

        [Test]
        public void BlinkChangesOnlyEyesAndReturnsToTheSelectedPalette()
        {
            var before = Rig.slots.ToDictionary(s => s.id, s => s.renderer.sprite);
            var appearance = world.PlayerActor.Appearance;
            var position = world.PlayerActor.Transform.Position;
            var state = world.PlayerActor.State;
            var hp = world.PlayerActor.Vitals.CurrentHp;
            var settings = Rig.CurrentOutfit.face;
            view.face.Present(settings.blinkInterval - settings.blinkDuration * .5f);
            Assert.That(Part("eyes").sprite, Is.SameAs(Rig.CurrentOutfit.eyeVariants.Single(e => e.id == appearance.EyeId).closedSprite));
            foreach (var slot in Rig.slots.Where(s => s.id != "eyes")) Assert.That(slot.renderer.sprite, Is.SameAs(before[slot.id]), slot.id);
            view.face.Present(settings.blinkDuration);
            Assert.That(Part("eyes").sprite, Is.SameAs(before["eyes"]));
            Assert.That(world.PlayerActor.Appearance, Is.EqualTo(appearance));
            Assert.That(world.PlayerActor.Transform.Position, Is.EqualTo(position));
            Assert.That(world.PlayerActor.State, Is.EqualTo(state));
            Assert.That(world.PlayerActor.Vitals.CurrentHp, Is.EqualTo(hp));
        }

        [Test]
        public void CastAndHitDriveMouthAndOppositeBrowsThenReleaseThem()
        {
            view.face.Present(0);
            var eyes = Part("eyes").sprite; var mouth = Part("mouth").sprite;
            var near = Part("eyebrow-front").transform; var far = Part("eyebrow-back").transform;
            var nearRest = near.localRotation; var farRest = far.localRotation;
            Assert.That(near.position, Is.Not.EqualTo(far.position), "Each brow needs its own pivot.");
            view.actionFeedback.OnCast(new WorldPoint(1, 0)); view.face.Present(0);
            Assert.That(Part("eyes").sprite, Is.SameAs(eyes));
            Assert.That(Part("mouth").sprite, Is.SameAs(Rig.CurrentOutfit.face.callingMouth));
            Assert.That(Mathf.DeltaAngle(nearRest.eulerAngles.z, near.localEulerAngles.z), Is.EqualTo(-12).Within(.001));
            Assert.That(Mathf.DeltaAngle(farRest.eulerAngles.z, far.localEulerAngles.z), Is.EqualTo(12).Within(.001));
            view.actionFeedback.ResetPresentation(); view.face.Present(0);
            Assert.That(Part("mouth").sprite, Is.SameAs(mouth));
            Assert.That(near.localRotation, Is.EqualTo(nearRest)); Assert.That(far.localRotation, Is.EqualTo(farRest));
            view.HitFlash(); view.face.Present(0);
            Assert.That(Part("eyes").sprite, Is.Not.SameAs(eyes));
            Assert.That(Part("mouth").sprite, Is.Not.SameAs(mouth));
            Rig.AdvancePresentation(1); view.face.Present(0);
            Assert.That(Part("eyes").sprite, Is.SameAs(eyes)); Assert.That(Part("mouth").sprite, Is.SameAs(mouth));
        }

        [Test]
        public void PaletteAndOutfitSwapDuringBlinkPreservesClipAndCastExpression()
        {
            Rig.animator.Rebind(); Rig.animator.Play("Run", 0, .37f); Rig.animator.Update(0);
            var controller = Rig.animator.runtimeAnimatorController;
            var time = Rig.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            var bones = Rig.visual.GetComponentsInChildren<Transform>().Where(t => t != Part("eyebrow-front").transform && t != Part("eyebrow-back").transform).ToArray();
            var poses = bones.Select(t => (t.localPosition, t.localRotation, t.localScale)).ToArray();
            view.actionFeedback.OnCast(new WorldPoint(1, 0));
            view.face.Present(3.15f);
            foreach (var outfit in ContentIntegrationTests.Registry.outfits.Where(o => o.face.enabled))
            foreach (var eye in outfit.eyeVariants)
            {
                Rig.ApplyAppearance(new ActorAppearance(outfit.appearanceId, "silver", eye.id)); view.face.Present(0);
                Assert.That(Part("eyes").sprite, Is.SameAs(eye.closedSprite));
                Assert.That(Part("mouth").sprite, Is.SameAs(outfit.face.callingMouth));
                CollectionAssert.AreEqual(poses, bones.Select(t => (t.localPosition, t.localRotation, t.localScale)));
                Assert.That(Rig.animator.runtimeAnimatorController, Is.SameAs(controller));
                Assert.That(Rig.animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(time));
            }
            view.face.Present(.2f);
            Assert.That(Part("eyes").sprite, Is.SameAs(Rig.CurrentOutfit.eyeVariants.Single(e => e.id == Rig.CurrentAppearance.EyeId).sprite));
        }

        [Test]
        public void RebindAndBlankBaseDoNotRetainExpressionLayers()
        {
            view.actionFeedback.OnCast(new WorldPoint(1, 0)); view.face.Present(3.15f);
            view.Unbind(); view.Bind(world.PlayerActor); view.face.Present(0);
            Assert.That(Part("eyes").sprite, Is.SameAs(Rig.CurrentOutfit.eyeVariants.Single(e => e.id == Rig.CurrentAppearance.EyeId).sprite));
            Assert.That(Part("mouth").sprite, Is.SameAs(Rig.CurrentOutfit.slots.Single(s => s.slotId == "mouth").sprite));
            Rig.ApplyOutfit(ContentIntegrationTests.Registry.outfits.Single(o => o.appearanceId == "basic"));
            view.face.Present(3.15f);
            foreach (var id in new[] { "eyes", "mouth", "eyebrow-front", "eyebrow-back", "face-mark" }) Assert.That(Part(id).sprite, Is.Null, id);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MissingOrMisalignedExpressionFailsBeforeChangingTheRig(bool missing)
        {
            var invalid = UnityEngine.Object.Instantiate(Rig.CurrentOutfit);
            var texture = new Texture2D(16, 16);
            var badSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * .5f, 100);
            try
            {
                var before = Rig.slots.Select(s => s.renderer.sprite).ToArray();
                if (missing) invalid.eyeVariants[0].closedSprite = null;
                else invalid.face.callingMouth = badSprite;
                Assert.Throws<InvalidOperationException>(() => Rig.ApplyOutfit(invalid));
                CollectionAssert.AreEqual(before, Rig.slots.Select(s => s.renderer.sprite));
            }
            finally { UnityEngine.Object.DestroyImmediate(invalid); UnityEngine.Object.DestroyImmediate(badSprite); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
