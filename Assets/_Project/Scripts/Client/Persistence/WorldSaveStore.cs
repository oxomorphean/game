using System;
using System.IO;
using System.Text;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Persistence;
using LinhThu.Domain.World;

namespace LinhThu.Client.Persistence
{
    /// <summary>Explicit foundation save only. Never discovers or migrates legacy files.</summary>
    public sealed class WorldSaveStore
    {
        public string FilePath { get; }

        private readonly SaveSerializer serializer = new SaveSerializer();
        private readonly GameSaveMapper mapper = new GameSaveMapper();
        public WorldSaveStore(string filePath) => FilePath = Path.GetFullPath(filePath);
        public void Write(WorldState world)
        {
            var bytes = new UTF8Encoding(false, true).GetBytes(serializer.Serialize(mapper.Capture(world)));
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string pending = FilePath + "." + Guid.NewGuid().ToString("N") + ".pending";
            try
            {
                using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(FilePath))
                    File.Replace(pending, FilePath, FilePath + ".bak");
                else
                    File.Move(pending, FilePath);
            }
            finally
            {
                if (File.Exists(pending))
                    File.Delete(pending);
            }
        }

        public DomainResult<WorldState> Read(IDomainContent content, WorldGeometry geometry, uint seed)
        {
            try
            {
                if (!File.Exists(FilePath))
                    return DomainResult<WorldState>.Fail(DomainError.NotFound, "Chưa có bản lưu Foundation.");
                if (new FileInfo(FilePath).Length > SaveValidation.MaxJsonCharacters * 4L)
                    return DomainResult<WorldState>.Fail(DomainError.InvalidSave, "Bản lưu vượt giới hạn dung lượng.");
                var parsed = serializer.TryDeserialize(File.ReadAllText(FilePath, new UTF8Encoding(false, true)));
                return parsed.Succeeded ? mapper.TryRestore(parsed.Value, content, geometry, new SeededRandomSource(seed), new DomainEventBus()) : DomainResult<WorldState>.Fail(parsed.ErrorCode, parsed.Message);
            }
            catch (IOException e)
            {
                return DomainResult<WorldState>.Fail(DomainError.InvalidSave, e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                return DomainResult<WorldState>.Fail(DomainError.InvalidSave, e.Message);
            }
            catch (DecoderFallbackException e)
            {
                return DomainResult<WorldState>.Fail(DomainError.InvalidSave, e.Message);
            }
        }
    }
}
