using System;
using System.IO;
using LinhThu.Domain;
using UnityEngine;

namespace LinhThu.Prototype
{
    /// <summary>Local-only prototype storage. Online builds must replace this with server persistence.</summary>
    public sealed class LocalSaveStore
    {
        private const string Key = "linhthu.logic.v1";
        public string Location => Application.platform == RuntimePlatform.WebGLPlayer ? "Bộ nhớ trình duyệt này" : Path.Combine(Application.persistentDataPath, "thandinh-v1.json");
        public bool Exists => Application.platform == RuntimePlatform.WebGLPlayer ? PlayerPrefs.HasKey(Key) : File.Exists(Location);
        public static double Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public GameSession Load()
        {
            string json = Application.platform == RuntimePlatform.WebGLPlayer ? PlayerPrefs.GetString(Key) : File.ReadAllText(Location);
            return Decode(json, (int)DateTime.UtcNow.Ticks);
        }
        public static GameSession Decode(string json, int seed)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 2_000_000) throw new InvalidDataException("Bản lưu trống hoặc quá lớn.");
            var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
            if (envelope == null || envelope.schema != 1 || envelope.state == null || envelope.rules == null || envelope.content == null) throw new InvalidDataException("Bản lưu thiếu dữ liệu hoặc sai phiên bản.");
            // JsonUtility writes null inline classes as their zero-valued fields.
            // Only that exact representation means no active meal; malformed meals remain invalid.
            if (envelope.state.beasts != null)
            foreach (var beast in envelope.state.beasts)
            {
                var meal = beast?.digestion;
                if (meal != null && string.IsNullOrEmpty(meal.itemId) && meal.remaining == 0 && meal.duration == 0 &&
                    meal.qi == 0 && meal.attack == 0 && meal.defense == 0 && meal.root == 0) beast.digestion = null;
            }
            return new GameSession(envelope.state, envelope.rules, envelope.content, new RandomChance(seed));
        }
        public static string Encode(GameSession session) => JsonUtility.ToJson(new SaveEnvelope { rules = session.Rules, content = session.Content, state = session.State }, true);
        public void Save(GameSession session)
        {
            session.State.savedUtc = Math.Max(session.State.savedUtc, Now);
            string json = Encode(session);
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                PlayerPrefs.SetString(Key, json); PlayerPrefs.Save();
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Location));
                string temp = Location + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(Location)) File.Replace(temp, Location, Location + ".previous");
                else File.Move(temp, Location);
            }
        }
        public void Backup()
        {
            if (!Exists) return;
            if (Application.platform == RuntimePlatform.WebGLPlayer) { PlayerPrefs.SetString(Key + ".before-reset", PlayerPrefs.GetString(Key)); PlayerPrefs.Save(); }
            else File.Copy(Location, Location + ".before-reset", true);
        }
        public bool CanRestore => Application.platform == RuntimePlatform.WebGLPlayer ? PlayerPrefs.HasKey(Key + ".before-reset") : File.Exists(Location + ".before-reset");
        public GameSession RestoreBeforeReset()
        {
            if (!CanRestore) throw new InvalidOperationException("Chưa có bản trước khi tạo mới.");
            string json = Application.platform == RuntimePlatform.WebGLPlayer ? PlayerPrefs.GetString(Key + ".before-reset") : File.ReadAllText(Location + ".before-reset");
            return Decode(json, (int)DateTime.UtcNow.Ticks);
        }
    }
}
