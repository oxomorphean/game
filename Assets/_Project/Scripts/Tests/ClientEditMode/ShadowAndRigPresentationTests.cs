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
        [TestCase(7 + 5 / 60f)]
        [TestCase(11.5f)]
        [TestCase(17 + 50 / 60f)]
        public void AuthoredTreeRendersTransientShadowInEditModeAndCleansItUp(float hour)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var previous = RenderTexture.active;
            var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(128, 128, TextureFormat.RGBA32, false, true);
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Content/Prefabs/Environment/cherry-tree.prefab");
                var tree = UnityEngine.Object.Instantiate(prefab);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tree, scene);
                var prop = tree.GetComponent<ModularMapProp>();
                typeof(ModularMapProp).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(prop, null);
                var mesh = tree.GetComponentsInChildren<MeshRenderer>().SingleOrDefault(r => r.name == "Bóng mặt trời · silhouette");
                Assert.That(mesh, Is.Not.Null, "Prefab Mode must use the same UV/trunk mesh path as Play.");
                Assert.That((mesh.gameObject.hideFlags & HideFlags.DontSave) != 0, Is.True);
                Assert.That(prop.castShadow.enabled, Is.True, "Editor preview must not persist a disabled source renderer into the prefab.");
                foreach (var renderer in tree.GetComponentsInChildren<Renderer>()) if (renderer != mesh && renderer != prop.castShadow) renderer.enabled = false;
                DaylightPresentation.ApplyLighting(hour);
                var camera = PixelCamera(scene, target, 5);
                camera.Render(); ReadPixels(target, pixels);
                Assert.That(pixels.GetPixels().Count(p => p.a > .01f), Is.GreaterThan(15), "The prefab must actually render a silhouette shadow.");
                prop.enabled = false;
                Assert.That(tree.GetComponentsInChildren<MeshRenderer>().Any(r => r.name == "Bóng mặt trời · silhouette"), Is.False);
                Assert.That(prop.castShadow.enabled, Is.True);
                var sourceBlock = new MaterialPropertyBlock(); prop.castShadow.GetPropertyBlock(sourceBlock);
                Assert.That(sourceBlock.GetFloat("_ShadowSourceHidden"), Is.Zero);
                prop.enabled = true;
                typeof(ModularMapProp).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(prop, null);
                Assert.That(tree.GetComponentsInChildren<MeshRenderer>().Count(r => r.name == "Bóng mặt trời · silhouette"), Is.EqualTo(1));
            }
            finally
            {
                RenderTexture.active = previous; EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(pixels); target.Release(); UnityEngine.Object.DestroyImmediate(target);
                DaylightPresentation.ApplyLighting(12);
            }
        }

        [Test]
        public void AuthoredContactShadowRendersAnEllipseWithTransparentCorners()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var previous = RenderTexture.active;
            var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(128, 128, TextureFormat.RGBA32, false, true);
            try
            {
                var root = new GameObject("Contact pixels");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Environment/Meadow/Shapes/SoftContactFootprint.asset").OfType<Sprite>().Single();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/GroundContactShadow.mat");
                renderer.color = new Color(7 / 255f, 18 / 255f, 14 / 255f, .38f);
                root.transform.localScale = new Vector3(1, .4f, 1);
                var camera = PixelCamera(scene, target, .6f);
                camera.Render(); ReadPixels(target, pixels);
                Assert.That(pixels.GetPixel(64, 64).a, Is.InRange(.35f, .4f));
                Assert.That(pixels.GetPixel(64, 76).a, Is.InRange(.05f, .3f), "The ellipse must feather rather than end at a rectangular edge.");
                foreach (int x in new[] { 16, 112 })
                    foreach (int y in new[] { 48, 80 })
                        Assert.That(pixels.GetPixel(x, y).a, Is.LessThan(.004f), "Sprite quad corners must not render.");
            }
            finally
            {
                RenderTexture.active = previous;
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(pixels); target.Release(); UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [TestCase(7 + 5 / 60f)]
        [TestCase(11.5f)]
        [TestCase(17 + 50 / 60f)]
        public void FadingCanopyShadowPreservesItsSilhouetteAndFadesItsTrunk(float hour)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var previous = RenderTexture.active;
            var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(128, 128, TextureFormat.RGBA32, false, true);
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Sprite sprite = null; SunShadowMesh shadow = null;
            try
            {
                var colors = new Color[32 * 32];
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                        colors[y * 32 + x] = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 21)) < 8
                            ? new Color(.7f, .4f, 1, 1) : Color.clear;
                texture.SetPixels(colors); texture.Apply();
                sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(.5f, 0), 32, 0, SpriteMeshType.FullRect);
                var root = new GameObject("Canopy pixels");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                var renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/DaylightShadow.mat");
                shadow = new SunShadowMesh(renderer);
                var settings = SunShadowSettings.Default; settings.canopyBlueRatio = .65f;
                var camera = PixelCamera(scene, target, 1);
                DaylightPresentation.ApplyLighting(hour);
                renderer.color = Color.white; shadow.Refresh(settings, Vector3.zero, 1);
                camera.Render(); ReadPixels(target, pixels); var opaque = pixels.GetPixels();
                float originalAlpha = opaque.Sum(c => c.a);
                Assert.That(originalAlpha, Is.GreaterThan(2), "Test must actually render a shadow.");
                foreach (float opacity in new[] { .32f, .12f })
                {
                    renderer.color = new Color(1, 1, 1, opacity); shadow.Refresh(settings, Vector3.zero, 1);
                    camera.Render(); ReadPixels(target, pixels); var faint = pixels.GetPixels();
                    for (int i = 0; i < opaque.Length; i++)
                        if (opaque[i].a < .001f)
                            Assert.That(faint[i].a, Is.LessThan(.004f), "Tint opacity must not fill transparent canopy pixels into a quad.");
                    Assert.That(faint.Sum(c => c.a) / originalAlpha, Is.EqualTo(opacity).Within(.035f), "Trunk and canopy must both respect opacity.");
                }
            }
            finally
            {
                RenderTexture.active = previous; shadow?.Dispose();
                EditorSceneManager.ClosePreviewScene(scene); DaylightPresentation.ApplyLighting(12);
                if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(pixels);
                target.Release(); UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Camera PixelCamera(UnityEngine.SceneManagement.Scene scene, RenderTexture target, float size)
        {
            var root = new GameObject("Shadow pixel camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var camera = root.AddComponent<Camera>(); camera.enabled = false; camera.scene = scene;
            camera.transform.position = new Vector3(0, 0, -2); camera.orthographic = true; camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear; camera.targetTexture = target;
            return camera;
        }
        private static void ReadPixels(RenderTexture target, Texture2D pixels)
        {
            RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
        }

        [TestCase(1, 1, 0)]
        [TestCase(-1, 1, 0)]
        [TestCase(.62f, .62f, 0)]
        [TestCase(-.62f, 1.25f, 35)]
        public void ContactShadowKeepsWorldSunDirectionWhenOwnerIsMirroredScaledOrRotated(float scaleX, float scaleY, float angle)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Contact shadow fixture");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.position = new Vector3(3, -2, .5f);
                root.transform.rotation = Quaternion.Euler(0, 0, angle);
                root.transform.localScale = new Vector3(scaleX, scaleY, 1);
                var blob = root.AddComponent<GroundContactShadow>();
                blob.footOffset = new Vector2(.15f, .08f);
                var shadow = new GameObject("Shadow");
                shadow.transform.SetParent(root.transform, false);
                blob.shadow = shadow.AddComponent<SpriteRenderer>();
                foreach (float hour in new[] { 7 + 5 / 60f, 11.5f, 17 + 50 / 60f })
                {
                    DaylightPresentation.ApplyLighting(hour);
                    blob.Present();
                    var foot = root.transform.TransformPoint(blob.footOffset);
                    var offset = blob.shadow.transform.position - foot;
                    float expectedX = DaylightPresentation.ShadowDirection.x * DaylightPresentation.ShadowLength * .06f;
                    Assert.That(offset.x, Is.EqualTo(expectedX).Within(.00001f));
                    Assert.That(offset.y, Is.EqualTo(0).Within(.00001f), "Sunlight must not gain a vertical offset from owner rotation.");
                    Assert.That(offset.z, Is.EqualTo(0).Within(.00001f));
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); DaylightPresentation.ApplyLighting(12); }
        }

        [TestCase("rock")]
        [TestCase("cherry-tree")]
        public void SunShadowAnchorAndHeightFollowPropEditsAfterInitialization(string prefabName)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Content/Prefabs/Environment/{prefabName}.prefab");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var prop = root.GetComponent<ModularMapProp>();
                var lateUpdate = typeof(ModularMapProp).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                // Complete OnValidate's initial refresh before simulating subsequent transform edits.
                lateUpdate.Invoke(prop, null);
                var block = new MaterialPropertyBlock();
                for (int step = 1; step <= 3; step++)
                {
                    root.transform.position = new Vector3(step * 3, step * -2, 0);
                    root.transform.localScale = new Vector3(step % 2 == 0 ? -1 : 1, .5f + step * .25f, 1);
                    lateUpdate.Invoke(prop, null);
                    prop.castShadow.GetPropertyBlock(block);
                    Assert.That(block.GetVector("_GroundOrigin"), Is.EqualTo(new Vector4(step * 3, step * -2, 0, 1)));
                    float expectedHeight = prop.visual.sprite.rect.height / prop.visual.sprite.pixelsPerUnit * Mathf.Abs(prop.visual.transform.lossyScale.y);
                    Assert.That(block.GetVector("_ShadowVolume").x, Is.EqualTo(expectedHeight).Within(.00001f));
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

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
                    Assert.That(view.GetComponentsInChildren<MeshRenderer>().Count(r => r.sharedMaterial.shader.name.Contains("Shadow")), Is.Zero);
                    Assert.That(view.GetComponentsInChildren<SpriteRenderer>().Count(r => r.sharedMaterial.shader.name.Contains("Shadow")), Is.EqualTo(1));
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
        public void RobeShortcutNeverSelectsUnderwearAndTrainingPillarHasNoHumanRig()
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
                var dummy = game.sceneActors.Single(a => a.config.Id == "training-dummy");
                Assert.That(dummy.equipment == null && dummy.actorAnimator == null, Is.True);
                Assert.That(dummy.GetComponentsInChildren<SpriteRenderer>().All(r => r == dummy.GetComponent<GroundContactShadow>().shadow), Is.True, "Pillar must not retain a face, hair or body sprites.");
                Assert.That(dummy.singleSpriteVisual.GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo("TrainingPillar"));
                Assert.That(dummy.singleSpriteVisual.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(500));
                var playerSlot = game.playerView.equipment.slots.First(s => s.renderer.sprite != null).renderer;
                var block = new MaterialPropertyBlock(); playerSlot.GetPropertyBlock(block);
                Assert.That(block.GetColor("_MaterialTint"), Is.EqualTo(Color.white));
                var bar = game.hud.message.transform.parent.GetComponent<UnityEngine.UI.Image>();
                Assert.That(bar, Is.Not.Null); Assert.That(bar.type, Is.EqualTo(UnityEngine.UI.Image.Type.Sliced));
                Assert.That(bar.sprite.border.x, Is.GreaterThan(0)); Assert.That(bar.raycastTarget, Is.False);
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
