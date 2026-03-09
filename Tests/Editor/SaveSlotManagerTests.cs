using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Jovian.SaveSystem.Tests.Editor {
    public class SaveSlotManagerTests {
        private InMemorySaveStorage storage;
        private SaveSystemSettings settings;
        private SaveSlotManager slotManager;

        [SetUp]
        public void SetUp() {
            storage = new InMemorySaveStorage();
            settings = new SaveSystemSettings { maxAutoSavesPerSession = 3 };
            slotManager = new SaveSlotManager(storage, settings);
        }

        [Test]
        public void CreateSession_ReturnsNonEmptyId() {
            string sessionId = slotManager.CreateSession();

            Assert.IsNotNull(sessionId);
            Assert.IsNotEmpty(sessionId);
        }

        [Test]
        public void CreateSession_AppearsInAllSessions() {
            string sessionId = slotManager.CreateSession();

            IReadOnlyList<SaveSessionInfo> sessions = slotManager.GetAllSessions();

            Assert.AreEqual(1, sessions.Count);
            Assert.AreEqual(sessionId, sessions[0].sessionId);
        }

        [Test]
        public void AllocateManualSlot_IncrementsSlotNumber() {
            string sessionId = slotManager.CreateSession();

            SaveSlotInfo slot1 = slotManager.AllocateManualSlot(sessionId);
            SaveSlotInfo slot2 = slotManager.AllocateManualSlot(sessionId);

            Assert.AreEqual(1, slot1.slotNumber);
            Assert.AreEqual(2, slot2.slotNumber);
            Assert.AreEqual(SaveSlotType.Manual, slot1.slotType);
        }

        [Test]
        public void AllocateAutoSlot_RotatesWhenMaxReached() {
            string sessionId = slotManager.CreateSession();

            SaveSlotInfo slot1 = slotManager.AllocateAutoSlot(sessionId);
            SaveSlotInfo slot2 = slotManager.AllocateAutoSlot(sessionId);
            SaveSlotInfo slot3 = slotManager.AllocateAutoSlot(sessionId);

            // 4th should rotate to reuse the oldest
            SaveSlotInfo slot4 = slotManager.AllocateAutoSlot(sessionId);

            // slot4 should reuse slot1's file path (oldest by timestamp)
            Assert.AreEqual(slot1.filePath, slot4.filePath);
        }

        [Test]
        public void AllocateQuickSlot_AlwaysReturnsSameSlot() {
            string sessionId = slotManager.CreateSession();

            SaveSlotInfo slot1 = slotManager.AllocateQuickSlot(sessionId);
            SaveSlotInfo slot2 = slotManager.AllocateQuickSlot(sessionId);

            Assert.AreEqual(slot1.filePath, slot2.filePath);
            Assert.AreEqual(SaveSlotType.Quick, slot1.slotType);
        }

        [Test]
        public void GetSlots_ReturnsOnlySessionSlots() {
            string session1 = slotManager.CreateSession();
            string session2 = slotManager.CreateSession();

            slotManager.AllocateManualSlot(session1);
            slotManager.AllocateManualSlot(session1);
            slotManager.AllocateManualSlot(session2);

            IReadOnlyList<SaveSlotInfo> slots1 = slotManager.GetSlots(session1);
            IReadOnlyList<SaveSlotInfo> slots2 = slotManager.GetSlots(session2);

            Assert.AreEqual(2, slots1.Count);
            Assert.AreEqual(1, slots2.Count);
        }

        [Test]
        public void HasAnySaves_FalseWhenEmpty_TrueAfterAllocation() {
            Assert.IsFalse(slotManager.HasAnySaves());

            string sessionId = slotManager.CreateSession();
            slotManager.AllocateManualSlot(sessionId);

            Assert.IsTrue(slotManager.HasAnySaves());
        }

        [Test]
        public void DeleteSlot_RemovesFromIndex() {
            string sessionId = slotManager.CreateSession();
            SaveSlotInfo slot = slotManager.AllocateManualSlot(sessionId);

            slotManager.DeleteSlot(slot);

            Assert.AreEqual(0, slotManager.GetSlots(sessionId).Count);
        }

        [Test]
        public void DeleteSession_RemovesAllSlotsAndSession() {
            string sessionId = slotManager.CreateSession();
            slotManager.AllocateManualSlot(sessionId);
            slotManager.AllocateAutoSlot(sessionId);
            slotManager.AllocateQuickSlot(sessionId);

            slotManager.DeleteSession(sessionId);

            Assert.AreEqual(0, slotManager.GetAllSessions().Count);
            Assert.AreEqual(0, slotManager.GetSlots(sessionId).Count);
        }

        /// <summary>
        /// In-memory ISaveStorage for testing without file system.
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
