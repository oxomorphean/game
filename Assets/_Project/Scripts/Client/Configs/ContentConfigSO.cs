using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Stats;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    public abstract class ContentConfigSO : ScriptableObject
    {
        public string definitionId;
        public string displayName;
        [TextArea]
        public string description;
        public string Id
        {
            get
            {
                if (string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(displayName))
                    throw new InvalidOperationException(name + " requires an ID and display name.");
                return definitionId;
            }
        }
    }

    [Serializable]
    public struct StatValueData
    {
        public StatId stat;
        public float value;
        public StatValueData(StatId stat, float value)
        {
            this.stat = stat;
            this.value = value;
        }
    }

    [Serializable]
    public struct StatModifierData
    {
        public StatId stat;
        public ModifierType type;
        public float value;
        public StatModifierSpec ToSpec() => new StatModifierSpec(stat, type, value);
    }

    internal static class ConfigData
    {
        internal static T Required<T>(T value, string context)
            where T : UnityEngine.Object => value != null ? value : throw new InvalidOperationException("Missing " + context);
        internal static T[] Required<T>(T[] values, string context) => values ?? throw new InvalidOperationException("Missing " + context);
        internal static Dictionary<StatId, float> Stats(StatValueData[] values) => Required(values, "base stats").ToDictionary(x => x.stat, x => x.value);
    }
}
