using System;
using UnityEngine;

namespace LinhThu.Prototype.Characters
{
    public enum HeroMotion { Idle, Run, Attack, Sweep, Dash, Hurt, Down }

    [Serializable]
    public sealed class DirectionalHeroClip
    {
        public HeroMotion motion;
        [Min(1)] public int framesPerDirection = 1;
        [Min(1)] public float framesPerSecond = 12;
        public bool loop;
        [Range(0, 1)] public float impactPhase = .45f;
        [Tooltip("Eight rows: E, NE, N, NW, W, SW, S, SE. Each row has the same frame count.")]
        public Sprite[] frames;

        public Sprite Sample(int direction, float time, float phase = -1)
        {
            int index = phase >= 0 ? Mathf.FloorToInt(Mathf.Clamp01(phase) * (framesPerDirection - 1) + .00001f) :
                Mathf.FloorToInt(Mathf.Max(0, time) * framesPerSecond);
            index = loop && phase < 0 ? index % framesPerDirection : Mathf.Min(index, framesPerDirection - 1);
            return frames[Mathf.Clamp(direction, 0, 7) * framesPerDirection + index];
        }
    }

    /// <summary>Shared authored sprites; phase, facing and damage feedback are per actor.</summary>
    [CreateAssetMenu(menuName = "Linh Thu/Directional Hero")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom("LinhThu.Prototype.Combat")]
    public sealed class DirectionalHeroDefinition : ScriptableObject
    {
        public DirectionalHeroClip[] clips;
        public float scale = 1;

        public DirectionalHeroClip Clip(HeroMotion motion)
        {
            foreach (var clip in clips) if (clip.motion == motion) return clip;
            throw new InvalidOperationException("Missing hero animation: " + motion);
        }

        public void Validate()
        {
            foreach (HeroMotion motion in Enum.GetValues(typeof(HeroMotion)))
            {
                var clip = Clip(motion);
                if (clip.framesPerDirection < 1 || clip.framesPerSecond <= 0 || clip.frames == null ||
                    clip.frames.Length != 8 * clip.framesPerDirection || Array.Exists(clip.frames, f => f == null))
                    throw new InvalidOperationException("Incomplete eight-direction clip: " + motion);
            }
        }
    }
}
