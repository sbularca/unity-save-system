using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Jovian.SaveSystem {
    /// <summary>
    /// Manages save slot allocation, session tracking, and auto-save rotation.
    /// Maintains a persistent index file that tracks all sessions and their slots.
    /// </summary>
    public sealed class SaveSlotManager : ISaveSlotManager {
        private const string IndexFileName = "index.json";
        private const string ManualPrefix = "manual_";
        private const string AutoPrefix = "auto_";
        private const string QuickFileName = "quick.sav";
        private const string SaveExtension = ".sav";

        private readonly ISaveStorage storage;
        private readonly int maxAutoSaves;
        private SaveIndex index;

        public SaveSlotManager(ISaveStorage storage, SaveSystemSettings settings) {
            this.storage = storage;
            maxAutoSaves = settings.maxAutoSavesPerSession;
            LoadIndex();
        }

        public string CreateSession() {
            string sessionId = Guid.NewGuid().ToString("N");
            SaveSessionInfo session = new SaveSessionInfo {
                sessionId = sessionId,
                creationDateUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                lastSaveDateUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            index.sessions.Add(session);
            storage.CreateDirectory(sessionId);
            PersistIndex();
            return sessionId;
        }

        public SaveSlotInfo AllocateManualSlot(string sessionId) {
            List<SaveSlotInfo> sessionSlots = GetOrCreateSessionSlots(sessionId);
            int nextNumber = sessionSlots
                .Where(s => s.slotType == SaveSlotType.Manual)
                .Select(s => s.slotNumber)
                .DefaultIfEmpty(0)
                .Max() + 1;

            SaveSlotInfo slot = new SaveSlotInfo {
                sessionId = sessionId,
                slotType = SaveSlotType.Manual,
                slotNumber = nextNumber,
                filePath = $"{sessionId}/{ManualPrefix}{nextNumber:D3}{SaveExtension}",
                timestampUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            index.slots.Add(slot);
            return slot;
        }

        public SaveSlotInfo AllocateAutoSlot(string sessionId) {
            List<SaveSlotInfo> sessionSlots = GetOrCreateSessionSlots(sessionId);
            List<SaveSlotInfo> autoSlots = sessionSlots
                .Where(s => s.slotType == SaveSlotType.Auto)
                .OrderBy(s => s.slotNumber)
                .ToList();

            if(autoSlots.Count >= maxAutoSaves) {
                // Rotate: reuse the oldest slot
                var oldest = autoSlots.OrderBy(s => s.timestampUtc).First();
                oldest.timestampUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return oldest;
            }

            int nextNumber = autoSlots.Count + 1;
            SaveSlotInfo slot = new SaveSlotInfo {
                sessionId = sessionId,
                slotType = SaveSlotType.Auto,
                slotNumber = nextNumber,
                filePath = $"{sessionId}/{AutoPrefix}{nextNumber:D3}{SaveExtension}",
                timestampUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            index.slots.Add(slot);
            return slot;
        }

        public SaveSlotInfo AllocateQuickSlot(string sessionId) {
            List<SaveSlotInfo> sessionSlots = GetOrCreateSessionSlots(sessionId);
            SaveSlotInfo existingQuick = sessionSlots.FirstOrDefault(s => s.slotType == SaveSlotType.Quick);

            if(existingQuick != null) {
                existingQuick.timestampUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return existingQuick;
            }

            SaveSlotInfo slot = new SaveSlotInfo {
                sessionId = sessionId,
                slotType = SaveSlotType.Quick,
                slotNumber = 1,
                filePath = $"{sessionId}/{QuickFileName}",
                timestampUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            index.slots.Add(slot);
            return slot;
        }

        public IReadOnlyList<SaveSlotInfo> GetSlots(string sessionId) {
            return index.slots
                .Where(s => s.sessionId == sessionId)
                .OrderByDescending(s => s.timestampUtc)
                .ToList();
        }

        public IReadOnlyList<SaveSessionInfo> GetAllSessions() {
            return index.sessions
                .OrderByDescending(s => s.lastSaveDateUtc)
                .ToList();
        }

        public bool HasAnySaves() {
            return index.slots.Count > 0;
        }

        public void DeleteSlot(SaveSlotInfo slot) {
            storage.Delete(slot.filePath);
            index.slots.RemoveAll(s => s.filePath == slot.filePath);
            PersistIndex();
        }

        public void DeleteSession(string sessionId) {
            List<SaveSlotInfo> slotsToDelete = index.slots
                .Where(s => s.sessionId == sessionId)
                .ToList();

            foreach(SaveSlotInfo slot in slotsToDelete) {
                storage.Delete(slot.filePath);
            }

            index.slots.RemoveAll(s => s.sessionId == sessionId);
            index.sessions.RemoveAll(s => s.sessionId == sessionId);
            PersistIndex();
        }

        public void UpdateSlotMetadata(SaveSlotInfo slot, long timestampUtc, int saveVersion) {
            slot.timestampUtc = timestampUtc;
            slot.saveVersion = saveVersion;

            SaveSessionInfo session = index.sessions.FirstOrDefault(s => s.sessionId == slot.sessionId);
            if(session != null) {
                session.lastSaveDateUtc = timestampUtc;
            }
        }

        public void PersistIndex() {
            string json = JsonConvert.SerializeObject(index, Formatting.Indented);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            storage.Write(IndexFileName, bytes);
        }

        private void LoadIndex() {
            if(!storage.Exists(IndexFileName)) {
                index = new SaveIndex();
                return;
            }

            try {
                byte[] bytes = storage.Read(IndexFileName);
                string json = Encoding.UTF8.GetString(bytes);
                index = JsonConvert.DeserializeObject<SaveIndex>(json) ?? new SaveIndex();
            } catch(Exception) {
                index = new SaveIndex();
            }
        }

        private List<SaveSlotInfo> GetOrCreateSessionSlots(string sessionId) {
            if(!index.sessions.Any(s => s.sessionId == sessionId)) {
                SaveSessionInfo session = new SaveSessionInfo {
                    sessionId = sessionId,
                    creationDateUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    lastSaveDateUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                index.sessions.Add(session);
                storage.CreateDirectory(sessionId);
            }

            return index.slots.Where(s => s.sessionId == sessionId).ToList();
        }

        /// <summary>
        /// Internal index structure persisted to disk.
        /// </summary>
        [Serializable]
        private sealed class SaveIndex {
            public List<SaveSessionInfo> sessions = new List<SaveSessionInfo>();
            public List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
        }
    }
}
