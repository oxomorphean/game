using System;
using System.Linq;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Persistence
{
    public static class SaveValidation
    {
        public const int MaxJsonCharacters = 4 * 1024 * 1024;
        public static DomainResult Structure(GameSaveDto dto)
        {
            if (dto == null)
                return DomainResult.Fail(DomainError.InvalidSave, "Missing save.");
            if (dto.SchemaId != "linhthu.foundation" || dto.SchemaVersion != 1)
                return DomainResult.Fail(DomainError.UnsupportedSchema);
            try
            {
                Guard.Id(dto.MapDefinitionId);
                Require(dto.Player != null && dto.Random != null && dto.Rules != null, "Missing world data.");
                Require(dto.Actors != null && dto.Actors.Count > 0 && dto.Actors.Count <= 10000 && dto.Nodes != null && dto.Nodes.Count <= 10000 && dto.Projectiles != null && dto.Projectiles.Count <= 10000, "Entity count exceeds limits.");
                Require(dto.Actors.All(a => a != null && a.BaseStats != null && a.BaseStats.Count <= 22 && a.ManualModifiers != null && a.ManualModifiers.Count <= 1024 && a.Statuses != null && a.Statuses.Count <= 256 && a.Cooldowns != null && a.Cooldowns.Count <= 1024), "Invalid actor collection.");
                Require(dto.Actors.All(a => a.Statuses.All(s => s != null && ValidSource(s.Source)) && a.ManualModifiers.All(m => m != null)), "Invalid actor source.");
                Require(dto.Projectiles.All(p => p != null && ValidSource(p.Source) && p.HitTargets != null && p.HitTargets.Count <= 1000) && dto.Nodes.All(n => n != null), "Invalid world collection.");
                var p = dto.Player;
                Require(p.Inventory != null && p.Inventory.Count <= 10000 && p.Inventory.All(x => x != null) && p.Equipment != null && p.Equipment.Count <= 100 && p.Equipment.All(x => x != null) && p.Beasts != null && p.Beasts.Count <= 1000 && p.Beasts.All(x => x != null), "Invalid player collection.");
                Require(dto.Rules.Elements != null && dto.Rules.Elements.Count <= 36 && dto.Rules.Elements.All(x => x != null) && dto.Rules.Hostilities != null && dto.Rules.Hostilities.Count <= 25 && dto.Rules.Hostilities.All(x => x != null), "Invalid rules.");
                Require(dto.NextEntityId > 0 && dto.NextSourceId > 0 && dto.TickIndex >= 0 && !double.IsNaN(dto.SimulationSeconds) && !double.IsInfinity(dto.SimulationSeconds) && dto.SimulationSeconds >= 0, "Invalid clock or counters.");
                Require(dto.DesiredMovement.ToPoint().Finite && dto.DesiredMovement.ToPoint().Length <= 1.00001f, "Invalid movement intent.");
                return DomainResult.Ok();
            }
            catch (ArgumentException e)
            {
                return DomainResult.Fail(DomainError.InvalidSave, e.Message);
            }
        }

        private static bool ValidSource(SourceSaveDto source) => source != null && source.Stats != null && source.Stats.Count <= 22;
        internal static void Require(bool condition, string message)
        {
            if (!condition)
                throw new ArgumentException(message);
        }
    }
}
