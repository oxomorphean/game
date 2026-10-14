using System;
using System.Collections.Generic;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Persistence
{
    public interface ISaveMigration
    {
        int FromVersion { get; }

        int ToVersion { get; }

        DomainResult<GameSaveDto> Migrate(GameSaveDto source);
    }

    public sealed class SaveMigration
    {
        private readonly Dictionary<int, ISaveMigration> migrations = new Dictionary<int, ISaveMigration>();
        public SaveMigration(IEnumerable<ISaveMigration> migrations = null)
        {
            foreach (var migration in migrations ?? Array.Empty<ISaveMigration>())
            {
                if (migration.ToVersion <= migration.FromVersion)
                    throw new ArgumentException("Migration must advance version.");
                this.migrations.Add(migration.FromVersion, migration);
            }
        }

        public DomainResult<GameSaveDto> ToCurrent(GameSaveDto source)
        {
            if (source == null || source.SchemaId != "linhthu.foundation" || source.SchemaVersion > GameSaveDto.CurrentVersion)
                return DomainResult<GameSaveDto>.Fail(DomainError.UnsupportedSchema);
            var current = source;
            while (current.SchemaVersion != GameSaveDto.CurrentVersion)
            {
                if (!migrations.TryGetValue(current.SchemaVersion, out var migration) || migration.ToVersion > GameSaveDto.CurrentVersion)
                    return DomainResult<GameSaveDto>.Fail(DomainError.UnsupportedSchema);
                var next = migration.Migrate(current);
                if (!next.Succeeded)
                    return next;
                if (next.Value == null || next.Value.SchemaId != source.SchemaId || next.Value.SchemaVersion != migration.ToVersion)
                    return DomainResult<GameSaveDto>.Fail(DomainError.InvalidSave, "Migration contract failed.");
                current = next.Value;
            }

            return DomainResult<GameSaveDto>.Ok(current);
        }
    }
}
