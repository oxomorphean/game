using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Persistence
{
    public sealed class SaveSerializer
    {
        private readonly SaveMigration migrations;
        private static JsonSerializerSettings Settings() => new JsonSerializerSettings{TypeNameHandling = TypeNameHandling.None, MetadataPropertyHandling = MetadataPropertyHandling.Ignore, MaxDepth = 64, MissingMemberHandling = MissingMemberHandling.Ignore, Converters = {new StringEnumConverter{AllowIntegerValues = false}}};
        public SaveSerializer(SaveMigration migrations = null)
        {
            this.migrations = migrations ?? new SaveMigration();
        }

        public string Serialize(GameSaveDto dto)
        {
            var valid = SaveValidation.Structure(dto);
            if (!valid.Succeeded)
                throw new ArgumentException(valid.Message);
            var json = JsonConvert.SerializeObject(dto, Formatting.None, Settings());
            if (json.Length > SaveValidation.MaxJsonCharacters)
                throw new ArgumentException("Save exceeds size limit.");
            return json;
        }

        public DomainResult<GameSaveDto> TryDeserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > SaveValidation.MaxJsonCharacters)
                return DomainResult<GameSaveDto>.Fail(DomainError.InvalidSave, "Empty or oversized save.");
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json))
                {MaxDepth = 64})
                {
                    var tree = JObject.Load(reader, new JsonLoadSettings{DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error});
                    if (reader.Read())
                        return DomainResult<GameSaveDto>.Fail(DomainError.InvalidSave, "Trailing JSON.");
                    if (tree["SchemaId"] == null || tree["SchemaVersion"] == null)
                        return DomainResult<GameSaveDto>.Fail(DomainError.UnsupportedSchema);
                    var dto = tree.ToObject<GameSaveDto>(JsonSerializer.Create(Settings()));
                    var migrated = migrations.ToCurrent(dto);
                    if (!migrated.Succeeded)
                        return migrated;
                    var valid = SaveValidation.Structure(migrated.Value);
                    return valid.Succeeded ? migrated : DomainResult<GameSaveDto>.Fail(valid.ErrorCode, valid.Message);
                }
            }
            catch (JsonException error)
            {
                return DomainResult<GameSaveDto>.Fail(DomainError.InvalidSave, error.Message);
            }
            catch (ArgumentException error)
            {
                return DomainResult<GameSaveDto>.Fail(DomainError.InvalidSave, error.Message);
            }
            catch (OverflowException error)
            {
                return DomainResult<GameSaveDto>.Fail(DomainError.InvalidSave, error.Message);
            }
        }
    }
}
