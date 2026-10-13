using System;
using System.Collections.Generic;
using System.IO;
using LinhThu.Domain;
using UnityEngine;

namespace LinhThu.Prototype
{
    /// <summary>Local prototype saves. Valid v1 data upgrades in memory; Load retains the original v1 save once before any v2 write.</summary>
    public sealed class LocalSaveStore
    {
        private const string Key = "linhthu.logic.v1";
        private const string UpgradeBackup = ".before-logic-v2";
        private const int SchemaVersion = 2;
        [Serializable] private sealed class SaveVersions
        {
            public int schema = 0;
            public GameRules rules = null;
            public GameContent content = null;
            public StateVersions state = null;
        }
        [Serializable] private sealed class StateVersions
        {
            public int version = 0, nextDropId = 0;
            public List<GroundDrop> drops = null;
        }
        public string Location => Application.platform == RuntimePlatform.WebGLPlayer ? "Bộ nhớ trình duyệt này" : Path.Combine(Application.persistentDataPath, "thandinh-v1.json");
        public bool Exists => Application.platform == RuntimePlatform.WebGLPlayer ? PlayerPrefs.HasKey(Key) : File.Exists(Location);
        public static double Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public GameSession Load()
        {
            string json = Application.platform == RuntimePlatform.WebGLPlayer ? PlayerPrefs.GetString(Key) : File.ReadAllText(Location);
            var session = Decode(json, (int)DateTime.UtcNow.Ticks, out bool upgraded);
            if (upgraded) PreserveV1(json);
            return session;
        }
        /// <summary>Decodes matching schema/state versions 1 or 2. Migration keeps savedUtc; the caller applies offline growth once.</summary>
        public static GameSession Decode(string json, int seed) => Decode(json, seed, out _);
        private static GameSession Decode(string json, int seed, out bool upgraded)
        {
            upgraded = false;
            if (string.IsNullOrWhiteSpace(json) || json.Length > 2_000_000) throw new InvalidDataException("Bản lưu trống hoặc quá lớn.");
            var versions = JsonUtility.FromJson<SaveVersions>(json);
            if (versions == null || versions.state == null || versions.rules == null || versions.content == null)
                throw new InvalidDataException("Bản lưu thiếu trạng thái, luật hoặc nội dung.");
            if ((versions.schema != 1 && versions.schema != SchemaVersion) || versions.state.version != versions.schema)
                throw new InvalidDataException("Phiên bản bản lưu không khớp: schema " + versions.schema + ", trạng thái " + versions.state.version + ".");
            if (versions.schema == SchemaVersion && (versions.state.drops == null || versions.state.nextDropId < 1))
                throw new InvalidDataException("Bản lưu logic v2 thiếu dữ liệu vật phẩm đã thả.");
            var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
            if (envelope == null || envelope.state == null || envelope.rules == null || envelope.content == null)
                throw new InvalidDataException("Bản lưu thiếu dữ liệu.");
            if (versions.schema == 1)
            {
                var defaults = new GameRules();
                envelope.rules.followerDelaySeconds = defaults.followerDelaySeconds;
                envelope.rules.followerNearRadius = defaults.followerNearRadius;
                envelope.rules.followerCatchupDistance = defaults.followerCatchupDistance;
                envelope.rules.wildRoamRadius = defaults.wildRoamRadius;
                envelope.rules.wildLeashRadius = defaults.wildLeashRadius;
                envelope.rules.dropLifetimeSeconds = defaults.dropLifetimeSeconds;
                envelope.state.drops = new List<GroundDrop>();
                envelope.state.nextDropId = 1;
                envelope.state.version = SchemaVersion;
                envelope.schema = SchemaVersion;
                upgraded = true;
            }
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
        /// <summary>Writes only schema/state v2; it neither changes timestamps nor applies offline progress.</summary>
        public static string Encode(GameSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (session.State.version != SchemaVersion) throw new InvalidDataException("Phiên hiện tại chưa dùng dữ liệu logic v2.");
            return JsonUtility.ToJson(new SaveEnvelope { schema = SchemaVersion, rules = session.Rules, content = session.Content, state = session.State }, true);
        }
        private void PreserveV1(string json)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                if (PlayerPrefs.HasKey(Key + UpgradeBackup)) return;
                PlayerPrefs.SetString(Key + UpgradeBackup, json);
                PlayerPrefs.Save();
            }
            else
            {
                string backup = Location + UpgradeBackup;
                if (File.Exists(backup)) return;
                string temp = backup + ".tmp";
                File.WriteAllText(temp, json);
                File.Move(temp, backup);
            }
        }
        public void Save(GameSession session)
        {
            string error = session.ValidateState();
            if (error != null) throw new InvalidDataException(error);
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
