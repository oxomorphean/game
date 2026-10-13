using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace LinhThu.Prototype
{
    public enum CartoonBodyKind { Human, Turtle }

    /// <summary>Renders a six-bone sprite; movement and gameplay remain owned by the caller.</summary>
    public sealed class CartoonCharacterRig : MonoBehaviour
    {
        public Sprite source;
        public CartoonBodyKind kind;
        public float displaySize = 1.25f;
        public SpriteRenderer contactShadow;
        public bool Moving { get; private set; }
        public float Facing { get; private set; } = 1;
        public float VisualTime { get; private set; }
        public int BoneCount => bones == null ? 0 : bones.Length;
        public SpriteSkin Skin { get; private set; }
        private Transform[] bones;
        private Vector3[] rest;
        private Sprite runtimeSprite;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer castShadow;
        private Transform visual;
        private float scale;
        private float motionBlend, stridePhase;
        private void Start()
        {
            if (source == null) throw new InvalidOperationException("High-view character needs an imported sprite.");
            bool human = kind == CartoonBodyKind.Human;
            visual = new GameObject(human ? "Cultivator SpriteSkin" : "Huyen Giap Quy SpriteSkin").transform;
            visual.SetParent(transform, false);
            spriteRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            float pivotY = human ? .14f : .19f;
            runtimeSprite = Sprite.Create(source.texture, source.rect, new Vector2(.5f, pivotY), 256, 0, SpriteMeshType.FullRect);
            runtimeSprite.name = human ? "High-view cultivator rig" : "High-view turtle rig";
            var size = runtimeSprite.bounds.size;
            scale = displaySize / (human ? size.y : size.x);
            visual.localScale = Vector3.one * scale;
            const int cols = 24, rows = 18;
            var vertices = new Vector2[(cols + 1) * (rows + 1)];
            var triangles = new ushort[cols * rows * 6];
            var weights = new NativeArray<BoneWeight>(vertices.Length, Allocator.Temp);
            var poses = new NativeArray<Matrix4x4>(6, Allocator.Temp);
            try
            {
                for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= cols; x++)
                {
                    int index = y * (cols + 1) + x;
                    float u = (float)x / cols, v = (float)y / rows;
                    vertices[index] = new Vector2(u * runtimeSprite.rect.width, v * runtimeSprite.rect.height);
                    int bone = 0; float strength = 0;
                    if (human)
                    {
                        if (v > .50f) { bone = 1; strength = Mathf.SmoothStep(0, 1, (v - .50f) / .12f); }
                        else if (v < .32f && u > .30f && u < .66f) { bone = u < .50f ? 2 : 3; strength = Mathf.Clamp01((.34f - v) / .14f); }
                        else if (u > .64f && v > .16f) { bone = 4; strength = Mathf.Clamp01((u - .62f) / .14f); }
                        else if (u < .35f && v > .22f) { bone = 5; strength = Mathf.Clamp01((.37f - u) / .14f); }
                    }
                    else
                    {
                        if (u > .70f && v > .28f) { bone = 1; strength = Mathf.SmoothStep(0, 1, (u - .70f) / .10f); }
                        else if (v < .36f && u > .48f && u < .70f) { bone = 2; strength = Mathf.Clamp01((.39f - v) / .16f); }
                        else if (v < .36f && u < .40f) { bone = 3; strength = Mathf.Clamp01((.39f - v) / .16f); }
                        else if (v < .31f && u >= .70f) { bone = 4; strength = Mathf.Clamp01((.34f - v) / .14f); }
                        else if (u < .23f && v > .30f && v < .52f) { bone = 5; strength = Mathf.Clamp01((.26f - u) / .09f); }
                    }
                    weights[index] = new BoneWeight { boneIndex0 = bone, weight0 = strength, boneIndex1 = 0, weight1 = 1 - strength };
                }
                int t = 0;
                for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                {
                    ushort a = (ushort)(y * (cols + 1) + x), b = (ushort)(a + 1), c = (ushort)(a + cols + 1), d = (ushort)(c + 1);
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
                runtimeSprite.OverrideGeometry(vertices, triangles);
                var names = human ? new[] { "Body", "Head", "Left leg", "Right leg", "Sword arm", "Left arm" }
                    : new[] { "Body", "Head", "Front near", "Rear near", "Front far", "Tail" };
                var anchors = human
                    ? new[] { new Vector2(.50f, .41f), new Vector2(.50f, .57f), new Vector2(.43f, .24f), new Vector2(.55f, .24f), new Vector2(.65f, .40f), new Vector2(.34f, .40f) }
                    : new[] { new Vector2(.50f, .50f), new Vector2(.77f, .43f), new Vector2(.60f, .29f), new Vector2(.29f, .28f), new Vector2(.74f, .26f), new Vector2(.22f, .39f) };
                bones = new Transform[6]; rest = new Vector3[6]; var definitions = new SpriteBone[6];
                for (int i = 0; i < bones.Length; i++)
                {
                    var bone = new GameObject(names[i]).transform;
                    bone.SetParent(i == 0 ? visual : bones[0], false);
                    var point = new Vector3((anchors[i].x - .5f) * size.x, (anchors[i].y - pivotY) * size.y, 0);
                    bone.localPosition = i == 0 ? point : point - bones[0].localPosition;
                    bones[i] = bone; rest[i] = bone.localPosition;
                    definitions[i] = new SpriteBone { name = names[i], position = rest[i], rotation = Quaternion.identity, length = .2f, parentId = i == 0 ? -1 : 0 };
                    poses[i] = bone.worldToLocalMatrix * visual.localToWorldMatrix;
                }
                runtimeSprite.SetBones(definitions);
                runtimeSprite.SetBindPoses(poses);
                runtimeSprite.SetVertexAttribute(VertexAttribute.BlendWeight, weights);
            }
            finally { poses.Dispose(); weights.Dispose(); }
            spriteRenderer.sprite = runtimeSprite;
            spriteRenderer.sharedMaterial = Resources.Load<Material>("Art/DaylightSprite");
            if (spriteRenderer.sharedMaterial == null) throw new InvalidOperationException("Missing daylight sprite material.");
            Skin = visual.gameObject.AddComponent<SpriteSkin>();
            Skin.autoRebind = false; Skin.SetRootBone(bones[0]); Skin.SetBoneTransforms(bones);
            castShadow = new GameObject("Bóng đổ theo mặt trời").AddComponent<SpriteRenderer>();
            castShadow.transform.SetParent(transform, false); castShadow.sprite = runtimeSprite;
            castShadow.sharedMaterial = Resources.Load<Material>("Art/DaylightShadow");
            if (castShadow.sharedMaterial == null) throw new InvalidOperationException("Missing sunlight shadow material.");
            castShadow.sortingOrder = -29;
            castShadow.localBounds = new Bounds(Vector3.zero, Vector3.one * runtimeSprite.bounds.size.magnitude * 4);
        }

        public void SetMotion(Vector2 displacement)
        {
            Moving = displacement.sqrMagnitude > .000001f;
            if (Mathf.Abs(displacement.x) > .0001f) Facing = Mathf.Sign(displacement.x);
        }

        private void LateUpdate()
        {
            if (bones == null) return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            VisualTime += dt;
            motionBlend = Mathf.MoveTowards(motionBlend, Moving ? 1 : 0, dt * 8);
            stridePhase += dt * 10 * motionBlend;
            for (int i = 0; i < bones.Length; i++) { bones[i].localPosition = rest[i]; bones[i].localRotation = Quaternion.identity; }
            bool human = kind == CartoonBodyKind.Human;
            if (human)
            {
                float unit = displaySize / scale, stride = Mathf.Sin(stridePhase), walk = motionBlend;
                float breath = Mathf.Sin(VisualTime * 2.8f) * .0018f * (1 - walk);
                float bob = (1 - Mathf.Cos(stridePhase * 2)) * .003f * walk;
                bones[0].localPosition += new Vector3(0, breath + bob, 0) * unit;
                bones[0].localRotation = Quaternion.Euler(0, 0, -1.6f * walk);
                bones[1].localRotation = Quaternion.Euler(0, 0, .5f * walk + Mathf.Sin(VisualTime * 2.8f - .35f) * .22f * (1 - walk));
                bones[2].localPosition += new Vector3(stride * .018f, Mathf.Max(0, stride) * .033f, 0) * (unit * walk);
                bones[3].localPosition += new Vector3(-stride * .018f, Mathf.Max(0, -stride) * .033f, 0) * (unit * walk);
                bones[2].localRotation = Quaternion.Euler(0, 0, stride * 9 * walk);
                bones[3].localRotation = Quaternion.Euler(0, 0, -stride * 9 * walk);
                bones[4].localRotation = Quaternion.Euler(0, 0, -stride * 5 * walk);
                bones[5].localRotation = Quaternion.Euler(0, 0, stride * 7 * walk);
            }
            else
            {
                float phase = VisualTime * (Moving ? 10 : 3), amount = Moving ? 1 : .12f;
                bones[0].localPosition += Vector3.up * Mathf.Sin(phase * 2) * .013f * amount;
                bones[0].localRotation = Quaternion.identity;
                bones[1].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phase) * 1.5f * amount);
                bones[2].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phase) * 5 * amount);
                bones[3].localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(phase) * 4.5f * amount);
                bones[4].localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(phase) * 2.2f * amount);
                bones[5].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phase) * 2 * amount);
            }
            spriteRenderer.color = Color.white;
            visual.localScale = new Vector3(scale * Facing, scale, scale);
            castShadow.transform.localScale = visual.localScale;
            castShadow.enabled = true;
            spriteRenderer.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
            if (contactShadow != null) contactShadow.sortingOrder = -27;
        }

        private void OnDestroy() { if (runtimeSprite != null) Destroy(runtimeSprite); }
    }
}
