using System;
using System.Collections.Generic;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Cultivation;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Core
{
    public interface IDomainContent
    {
        bool TryGetWildBeast(string id, out WildBeastSpec spec);
        bool TryGetActor(string id, out ActorSpec spec);
        bool TryGetRealm(string id, out RealmSpec spec);
        bool TryGetTechnique(string id, out TechniqueSpec spec);
        bool TryGetStatus(string id, out StatusEffectSpec spec);
        bool TryGetBeast(string id, out BeastSpeciesSpec spec);
        bool TryGetItem(string id, out ItemSpec spec);
        bool TryGetNode(string id, out WorldNodeSpec spec);
    }

    /// <summary>Composition-time catalogue. Definitions are immutable and IDs cannot be replaced.</summary>
    public sealed class DomainContent : IDomainContent
    {
        private readonly Dictionary<string, WildBeastSpec> wildBeasts = new Dictionary<string, WildBeastSpec>();
        public void Add(WildBeastSpec x) => wildBeasts.Add(x.DefinitionId, x);
        public bool TryGetWildBeast(string id, out WildBeastSpec spec) => Get(wildBeasts, id, out spec);
        private readonly Dictionary<string, ActorSpec> actors = new Dictionary<string, ActorSpec>();
        private readonly Dictionary<string, RealmSpec> realms = new Dictionary<string, RealmSpec>();
        private readonly Dictionary<string, TechniqueSpec> techniques = new Dictionary<string, TechniqueSpec>();
        private readonly Dictionary<string, StatusEffectSpec> statuses = new Dictionary<string, StatusEffectSpec>();
        private readonly Dictionary<string, BeastSpeciesSpec> beasts = new Dictionary<string, BeastSpeciesSpec>();
        private readonly Dictionary<string, ItemSpec> items = new Dictionary<string, ItemSpec>();
        private readonly Dictionary<string, WorldNodeSpec> nodes = new Dictionary<string, WorldNodeSpec>();
        public void Add(ActorSpec x) => actors.Add(x.DefinitionId, x);
        public void Add(RealmSpec x) => realms.Add(x.DefinitionId, x);
        public void Add(TechniqueSpec x) => techniques.Add(x.DefinitionId, x);
        public void Add(StatusEffectSpec x) => statuses.Add(x.DefinitionId, x);
        public void Add(BeastSpeciesSpec x) => beasts.Add(x.DefinitionId, x);
        public void Add(ItemSpec x) => items.Add(x.DefinitionId, x);
        public void Add(WorldNodeSpec x) => nodes.Add(x.DefinitionId, x);
        private static bool Get<T>(Dictionary<string, T> values, string id, out T value)
        {
            if (id == null)
            {
                value = default;
                return false;
            }

            return values.TryGetValue(id, out value);
        }

        public bool TryGetActor(string id, out ActorSpec spec) => Get(actors, id, out spec);
        public bool TryGetRealm(string id, out RealmSpec spec) => Get(realms, id, out spec);
        public bool TryGetTechnique(string id, out TechniqueSpec spec) => Get(techniques, id, out spec);
        public bool TryGetStatus(string id, out StatusEffectSpec spec) => Get(statuses, id, out spec);
        public bool TryGetBeast(string id, out BeastSpeciesSpec spec) => Get(beasts, id, out spec);
        public bool TryGetItem(string id, out ItemSpec spec) => Get(items, id, out spec);
        public bool TryGetNode(string id, out WorldNodeSpec spec) => Get(nodes, id, out spec);
    }
}
