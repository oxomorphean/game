using System;
using System.Collections.Generic;
using LinhThu.Domain.Actors;
using UnityEngine;

namespace LinhThu.Client.Actors
{
    /// <summary>Authored additive action poses, evaluated after the shared locomotion Animator.</summary>
    [DefaultExecutionOrder(-10), DisallowMultipleComponent]
    public sealed class ActionPoseController : MonoBehaviour
    {
        [Serializable]
        public sealed class BonePose
        {
            public Transform bone;
            public Vector3 localPosition;
            public float angle;
        }

        [Serializable]
        public sealed class ActionPose
        {
            public ActorActionState state;
            public BonePose[] bones = Array.Empty<BonePose>();
            [NonSerialized] public float weight;
        }

        public ActorView view;
        public ActionPose[] poses = Array.Empty<ActionPose>();
        [Min(.01f)] public float transitionSeconds = .22f;
        private readonly Dictionary<Transform, (Vector3 position, Quaternion rotation)> animated = new Dictionary<Transform, (Vector3, Quaternion)>();

        private void Update() => RestoreAnimatedPose();
        private void LateUpdate() => Present(Time.deltaTime);

        public void RestoreAnimatedPose()
        {
            foreach (var pair in animated)
                if (pair.Key != null)
                {
                    pair.Key.localPosition = pair.Value.position;
                    pair.Key.localRotation = pair.Value.rotation;
                }
            animated.Clear();
        }

        public void Present(float deltaTime)
        {
            if (view == null || view.Entity == null) return;
            foreach (var pose in poses)
            {
                pose.weight = Mathf.MoveTowards(pose.weight, view.Entity.State == pose.state ? 1 : 0, Mathf.Max(0, deltaTime) / Mathf.Max(.01f, transitionSeconds));
                if (pose.weight <= 0) continue;
                foreach (var target in pose.bones)
                {
                    if (target.bone == null) throw new InvalidOperationException("Action pose is missing a bone.");
                    if (!animated.ContainsKey(target.bone)) animated.Add(target.bone, (target.bone.localPosition, target.bone.localRotation));
                    target.bone.localPosition = Vector3.Lerp(target.bone.localPosition, target.localPosition, pose.weight);
                    target.bone.localRotation = Quaternion.Slerp(target.bone.localRotation, Quaternion.Euler(0, 0, target.angle), pose.weight);
                }
            }
        }

        public void ResetPose()
        {
            RestoreAnimatedPose();
            foreach (var pose in poses) pose.weight = 0;
        }
        private void OnDisable() => ResetPose();
    }
}
