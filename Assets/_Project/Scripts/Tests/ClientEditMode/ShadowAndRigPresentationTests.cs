using System;
using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Client.Bootstrap;
using LinhThu.Client.Environment;
using LinhThu.Domain.Core;
using LinhThu.Domain.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LinhThu.Tests.ClientEditMode
{
    public sealed class ShadowAndRigPresentationTests
    {
        [Test]
        public void SunProjectionStaysShortAndMildAndCrossesNoonContinuously()
        {
            try
            {
                for (int minute = 360; minute <= 1080; minute++)
                {
                    DaylightPresentation.ApplyLighting(minute / 60f);
                    Assert.That(DaylightPresentation.ShadowLength, Is.InRange(.35f, .85f));
                    Assert.That(DaylightPresentation.ShadowOpacity, Is.InRange(.18f, .28f));
                    Assert.That((DaylightPresentation.ShadowDirection * DaylightPresentation.ShadowLength).magnitude, Is.LessThanOrEqualTo(.64f));
                }
                DaylightPresentation.ApplyLighting(11.99f);
                var before = DaylightPresentation.ShadowDirection * DaylightPresentation.ShadowLength;
                DaylightPresentation.ApplyLighting(12.01f);
                Assert.That(Vector2.Distance(before, DaylightPresentation.ShadowDirection * DaylightPresentation.ShadowLength), Is.LessThan(.002f));
            }
            finally { DaylightPresentation.ApplyLighting(12); }
        }

        [Test]
        public void ActorsHaveOneGroundBlobAndOnlyLargePropsProjectShadows()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Content/Scenes/MainGame.unity");
            try
            {
                var game = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameBootstrapper>()).Single();
                game.Initialize();
                foreach (var view in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ActorView>()))
                {
                    var blob = view.GetComponent<GroundContactShadow>();
                    Assert.That(blob, Is.Not.Null, view.name);
                    Assert.That(blob.shadow.sharedMaterial.shader.name, Is.EqualTo("LinhThu/Ground Contact Shadow"));
                    Assert.That(blob.shadow.transform.parent, Is.EqualTo(view.transform));
                    Assert.That(view.GetComponentsInChildren<MeshRenderer>().Count(r => r.sharedMaterial.shader.name.Contains("Shadow")), Is.EqualTo(1));
                    Assert.That(view.GetComponentsInChildren<SpriteRenderer>().Any(r => r.sharedMaterial.shader.name.Contains("Shadow")), Is.False);
                }
                foreach (var prop in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ModularMapProp>()))
                    if (prop.castShadow != null)
                        Assert.That(prop.visual.sprite.name, Is.EqualTo("cherry-tree").Or.EqualTo("rock"), prop.name);
                var shadow = game.playerView.GetComponent<GroundContactShadow>();
                DaylightPresentation.ApplyLighting(17 + 50 / 60f);
                shadow.Present(); var normalSize = shadow.shadow.transform.localScale; var normalPosition = shadow.shadow.transform.position;
                game.playerView.equipment.SetFacing(-1); shadow.Present();
                Assert.That(shadow.shadow.transform.position, Is.EqualTo(normalPosition), "Mirroring bones must not mirror the light.");
                Assert.That(game.World.Execute(new DashCommand(game.World.Player.ActorId, new LinhThu.Domain.Navigation.WorldPoint(1, 0))).Succeeded, Is.True);
                shadow.Present();
                Assert.That(shadow.shadow.transform.localScale.x, Is.LessThan(normalSize.x));
                Assert.That(shadow.shadow.transform.localScale.y, Is.LessThan(normalSize.y));
                shadow.heightSource.localPosition += Vector3.up * .3f;
                var dashSize = shadow.shadow.transform.localScale;
                shadow.Present(); Assert.That(shadow.shadow.transform.localScale.x, Is.LessThan(dashSize.x));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); DaylightPresentation.ApplyLighting(12); }
        }

        [Test]
        public void RobeShortcutNeverSelectsUnderwearAndDummyTintDoesNotAffectPlayerMaterial()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Content/Scenes/MainGame.unity");
            try
            {
                var game = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameBootstrapper>()).Single(); game.Initialize();
                CollectionAssert.AreEquivalent(new[] { "classic", "jade" }, game.registry.outfits.Where(o => o.includeInRobeCycle).Select(o => o.appearanceId));
                string first = game.World.PlayerActor.Appearance.OutfitId;
                Assert.That(first, Is.EqualTo("classic").Or.EqualTo("jade"));
                for (int i = 0; i < 8; i++)
                {
                    string previous = game.World.PlayerActor.Appearance.OutfitId;
                    Assert.That(game.CycleOutfit().Succeeded, Is.True);
                    Assert.That(game.World.PlayerActor.Appearance.OutfitId, Is.EqualTo(previous == "classic" ? "jade" : "classic"));
                }
                var dummy = game.sceneActors.Single(a => a.config.Id == "training-dummy").equipment;
                Assert.That(dummy.CurrentAppearance.OutfitId, Is.EqualTo("classic"));
                var dummySlot = dummy.slots.First(s => s.renderer.sprite != null).renderer;
                var playerSlot = game.playerView.equipment.slots.First(s => s.renderer.sprite != null).renderer;
                Assert.That(dummySlot.sharedMaterial, Is.SameAs(playerSlot.sharedMaterial));
                var block = new MaterialPropertyBlock(); dummySlot.GetPropertyBlock(block);
                Assert.That(block.GetColor("_MaterialTint"), Is.EqualTo(dummy.materialTint));
                Assert.That(block.GetFloat("_MonochromeTint"), Is.EqualTo(1));
                Assert.That(dummy.materialTint.r, Is.GreaterThan(dummy.materialTint.g));
                Assert.That(dummy.materialTint.g, Is.GreaterThan(dummy.materialTint.b));
                dummy.HitFlash(); dummy.AdvancePresentation(1); dummySlot.GetPropertyBlock(block);
                Assert.That(block.GetColor("_MaterialTint"), Is.EqualTo(dummy.materialTint));
                playerSlot.GetPropertyBlock(block); Assert.That(block.GetColor("_MaterialTint"), Is.EqualTo(Color.white));
                var bar = game.hud.message.transform.parent.GetComponent<UnityEngine.UI.Image>();
                Assert.That(bar, Is.Not.Null); Assert.That(bar.color.a, Is.InRange(.5f, .85f));
                Assert.That(bar.raycastTarget, Is.False);
                var field = game.sceneNodes.Single(n => n.config.kind == WorldNodeKind.SpiritFieldPlot);
                Assert.That(field.transform.eulerAngles.z, Is.EqualTo(0));
                Assert.That(field.GetComponentsInChildren<Renderer>().Any(r => r.sharedMaterial.shader.name.Contains("Shadow")), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void ProjectedShadowCarriesLocalBorderUvsForAtlasSprites()
        {
            var root = new GameObject("Shadow atlas fixture");
            var texture = new Texture2D(32, 32);
            var sprite = Sprite.Create(texture, new Rect(8, 4, 16, 16), new Vector2(.5f, .1f), 16);
            SunShadowMesh shadow = null;
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/DaylightShadow.mat");
                shadow = new SunShadowMesh(renderer); shadow.Refresh(SunShadowSettings.Default, Vector3.zero, 1);
                var mesh = shadow.Renderer.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.uv2.Length, Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.uv2.Min(uv => uv.x), Is.EqualTo(0)); Assert.That(mesh.uv2.Max(uv => uv.x), Is.EqualTo(1));
                Assert.That(mesh.uv2.Min(uv => uv.y), Is.EqualTo(0)); Assert.That(mesh.uv2.Max(uv => uv.y), Is.EqualTo(1));
                Assert.That(mesh.uv.Min(uv => uv.x), Is.EqualTo(.25f)); Assert.That(mesh.uv.Max(uv => uv.x), Is.EqualTo(.75f));
            }
            finally { shadow?.Dispose(); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
