using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Jovian.SaveSystem.Tests.Editor {
    public class SaveSystemFacadeTests {
        [Serializable]
        private sealed class GameState {
            public string playerName;
            public int level;
            public float health;
        }

        private InMemorySaveStorage storage;
        private SaveSystemSettings settings;
        private ISaveSystem saveSystem;

        [SetUp]
        public void SetUp() {
            storage = new InMemorySaveStorage();
            settings = new SaveSystemSettings {
                maxAutoSavesPerSession = 3,
                currentSaveVersion = 1
            };
            JsonSaveSerializer serializer = new JsonSaveSerializer();
            SaveSlotManager slotManager = new SaveSlotManager(storage, settings);
            saveSystem = new SaveSystem(serializer, storage, slotManager, settings);
        }

        [Test]
        public void SaveAndLoad_ManualSlot_RoundTrips() {
            string sessionId = saveSystem.CreateSession();
            GameState original = new GameState {
                playerName = "Hero",
                level = 10,
                health = 95.5f
            };

            saveSystem.Save(sessionId, original, SaveSlotType.Manual);

            IReadOnlyList<SaveSlotInfo> slots = saveSystem.GetSlots(sessionId);
            Assert.AreEqual(1, slots.Count);

            GameState loaded = saveSystem.Load<GameState>(slots[0]);
            Assert.AreEqual(original.playerName, loaded.playerName);
            Assert.AreEqual(original.level, loaded.level);
            Assert.AreEqual(original.health, loaded.health);
        }

        [Test]
        public void SaveAndLoad_QuickSlot_OverwritesPrevious() {
            string sessionId = saveSystem.CreateSession();

            saveSystem.Save(sessionId, new GameState { playerName = "First" }, SaveSlotType.Quick);
            saveSystem.Save(sessionId, new GameState { playerName = "Second" }, SaveSlotType.Quick);

            IReadOnlyList<SaveSlotInfo> slots = saveSystem.GetSlots(sessionId);
            SaveSlotInfo quickSlot = slots.First(s => s.slotType == SaveSlotType.Quick);

            GameState loaded = saveSystem.Load<GameState>(quickSlot);
            Assert.AreEqual("Second", loaded.playerName);
        }

        [Test]
        public void HasAnySaves_ReflectsState() {
            Assert.IsFalse(saveSystem.HasAnySaves());

            string sessionId = saveSystem.CreateSession();
            saveSystem.Save(sessionId, new GameState { playerName = "Test" }, SaveSlotType.Manual);

            Assert.IsTrue(saveSystem.HasAnySaves());
        }

        [Test]
        public void DeleteSlot_RemovesSave() {
            string sessionId = saveSystem.CreateSession();
            saveSystem.Save(sessionId, new GameState { playerName = "Delete Me" }, SaveSlotType.Manual);

            IReadOnlyList<SaveSlotInfo> slots = saveSystem.GetSlots(sessionId);
            saveSystem.DeleteSlot(slots[0]);

            Assert.AreEqual(0, saveSystem.GetSlots(sessionId).Count);
        }

        [Test]
        public void DeleteSession_RemovesEverything() {
            string sessionId = saveSystem.CreateSession();
            saveSystem.Save(sessionId, new GameState { playerName = "A" }, SaveSlotType.Manual);
            saveSystem.Save(sessionId, new GameState { playerName = "B" }, SaveSlotType.Auto);
            saveSystem.Save(sessionId, new GameState { playerName = "C" }, SaveSlotType.Quick);

            saveSystem.DeleteSession(sessionId);

            Assert.AreEqual(0, saveSystem.GetAllSessions().Count);
            Assert.IsFalse(saveSystem.HasAnySaves());
        }

        [Test]
        public void MultipleSessions_AreIndependent() {
            string session1 = saveSystem.CreateSession();
            string session2 = saveSystem.CreateSession();

            saveSystem.Save(session1, new GameState { playerName = "Player1" }, SaveSlotType.Manual);
            saveSystem.Save(session2, new GameState { playerName = "Player2" }, SaveSlotType.Manual);

            Assert.AreEqual(1, saveSystem.GetSlots(session1).Count);
            Assert.AreEqual(1, saveSystem.GetSlots(session2).Count);

            GameState loaded1 = saveSystem.Load<GameState>(saveSystem.GetSlots(session1)[0]);
            GameState loaded2 = saveSystem.Load<GameState>(saveSystem.GetSlots(session2)[0]);

            Assert.AreEqual("Player1", loaded1.playerName);
            Assert.AreEqual("Player2", loaded2.playerName);
        }

        /// <summary>
        /// In-memory ISaveStorage for testing.
        /// </summary>
        private sealed class InMemorySaveStorage : ISaveStorage {
            private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            private readonly HashSet<string> directories = new HashSet<string>();

            public void Write(string path, byte[] data) { files[path] = data; }
            public byte[] Read(string path) { return files[path]; }
            public bool Exists(string path) { return files.ContainsKey(path); }
            public void Delete(string path) { files.Remove(path); }
            public string[] List(string directoryPath) {
                return files.Keys
                    .Where(k => k.StartsWith(directoryPath))
                    .ToArray();
            }
            public void CreateDirectory(string path) { directories.Add(path); }

            public Task WriteAsync(string path, byte[] data) { Write(path, data); return Task.CompletedTask; }
            public Task<byte[]> ReadAsync(string path) { return Task.FromResult(Read(path)); }
            public Task<bool> ExistsAsync(string path) { return Task.FromResult(Exists(path)); }
            public Task DeleteAsync(string path) { Delete(path); return Task.CompletedTask; }
            public Task<string[]> ListAsync(string directoryPath) { return Task.FromResult(List(directoryPath)); }
        }
    }
}
