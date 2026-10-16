using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace LinhThu.Client.Actors
{
    /// <summary>
    /// Reads the current bone pose for bounds and frozen afterimages. SpriteSkin owns rendering.
    /// Its batched buffer is updated later in LateUpdate, after our action/pose presenters.
    /// </summary>
    public sealed class SpritePoseGeometry
    {
        private sealed class MeshData
        {
            public Vector2[] vertices;
            public BoneWeight[] weights;
            public Matrix4x4[] bindPoses;
            public SpriteBone[] bones;
        }

        private static readonly Dictionary<Sprite, MeshData> meshes = new Dictionary<Sprite, MeshData>();
        private readonly SpriteRenderer renderer;
        private SpriteSkin skin;
        private Matrix4x4[] matrices = Array.Empty<Matrix4x4>();

        public SpritePoseGeometry(SpriteRenderer renderer) => this.renderer = renderer;
        public bool IsSkinned => Skin != null && Skin.enabled && Skin.rootBone != null;
        private SpriteSkin Skin => skin != null ? skin : (skin = renderer.GetComponent<SpriteSkin>());

        private static MeshData Data(Sprite sprite)
        {
            if (meshes.TryGetValue(sprite, out var data)) return data;
            var poses = sprite.GetBindPoses();
            data = new MeshData { vertices = sprite.vertices, bindPoses = poses.ToArray(), bones = sprite.GetBones() };
            data.weights = poses.Length > 0 && sprite.HasVertexAttribute(VertexAttribute.BlendWeight)
                ? sprite.GetVertexAttribute<BoneWeight>(VertexAttribute.BlendWeight).ToArray() : Array.Empty<BoneWeight>();
            meshes.Add(sprite, data);
            return data;
        }

        public void ValidateSwap(Sprite next, string slotId)
        {
            if (Skin == null || next == null) return;
            var data = Data(next);
            var transforms = Skin.boneTransforms;
            if (data.bindPoses.Length == 0 || data.bindPoses.Length != transforms.Length || data.bones.Length != transforms.Length ||
                data.weights.Length != data.vertices.Length || Skin.rootBone == null)
                throw new InvalidOperationException("Outfit lacks compatible skin data: " + slotId);
            var previous = renderer.sprite != null ? Data(renderer.sprite) : null;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == null || transforms[i].name != data.bones[i].name ||
                    (data.bones[i].parentId < 0 ? transforms[i] != Skin.rootBone :
                        data.bones[i].parentId >= transforms.Length || transforms[i].parent != transforms[data.bones[i].parentId]))
                    throw new InvalidOperationException("Outfit changes bone binding: " + slotId);
                if (previous != null)
                {
                    if (previous.bindPoses.Length != data.bindPoses.Length)
                        throw new InvalidOperationException("Outfit changes bind pose: " + slotId);
                    for (int element = 0; element < 16; element++)
                        if (Mathf.Abs(previous.bindPoses[i][element] - data.bindPoses[i][element]) > .00001f)
                            throw new InvalidOperationException("Outfit changes bind pose: " + slotId);
                }
            }
            foreach (var w in data.weights)
                if (!ValidWeight(w.weight0, w.boneIndex0, transforms.Length) || !ValidWeight(w.weight1, w.boneIndex1, transforms.Length) ||
                    !ValidWeight(w.weight2, w.boneIndex2, transforms.Length) || !ValidWeight(w.weight3, w.boneIndex3, transforms.Length) ||
                    Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1) > .0001f)
                    throw new InvalidOperationException("Outfit has invalid skin weights: " + slotId);
        }

        private static bool ValidWeight(float weight, int index, int count) =>
            !float.IsNaN(weight) && !float.IsInfinity(weight) && weight >= 0 && weight <= 1 && index >= 0 && index < count;

        private MeshData Prepare()
        {
            var data = Data(renderer.sprite);
            if (!IsSkinned) return data;
            var transforms = Skin.boneTransforms;
            if (matrices.Length != transforms.Length) matrices = new Matrix4x4[transforms.Length];
            for (int i = 0; i < matrices.Length; i++) matrices[i] = transforms[i].localToWorldMatrix * data.bindPoses[i];
            return data;
        }

        private Vector3 WorldVertex(MeshData data, int index, bool skinned)
        {
            Vector3 p = data.vertices[index];
            if (skinned)
            {
                var w = data.weights[index];
                p = matrices[w.boneIndex0].MultiplyPoint3x4(p) * w.weight0 +
                    matrices[w.boneIndex1].MultiplyPoint3x4(p) * w.weight1 +
                    matrices[w.boneIndex2].MultiplyPoint3x4(p) * w.weight2 +
                    matrices[w.boneIndex3].MultiplyPoint3x4(p) * w.weight3;
                if (!renderer.flipX && !renderer.flipY) return p;
                p = renderer.transform.InverseTransformPoint(p);
            }
            if (renderer.flipX) p.x = -p.x;
            if (renderer.flipY) p.y = -p.y;
            return renderer.transform.TransformPoint(p);
        }

        public void CopyWorldVertices(List<Vector3> destination)
        {
            destination.Clear();
            if (renderer.sprite == null) return;
            var data = Prepare(); var skinned = IsSkinned;
            for (int i = 0; i < data.vertices.Length; i++) destination.Add(WorldVertex(data, i, skinned));
        }

        public Bounds WorldBounds
        {
            get
            {
                if (renderer.sprite == null) return new Bounds(renderer.transform.position, Vector3.zero);
                var data = Prepare(); var skinned = IsSkinned;
                if (data.vertices.Length == 0) return renderer.bounds;
                var bounds = new Bounds(WorldVertex(data, 0, skinned), Vector3.zero);
                for (int i = 1; i < data.vertices.Length; i++) bounds.Encapsulate(WorldVertex(data, i, skinned));
                return bounds;
            }
        }
    }
}
