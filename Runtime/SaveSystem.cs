using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Jovian.SaveSystem {
    /// <summary>
    /// Facade that orchestrates serialization, storage, and slot management.
    /// Thread-safe: uses SemaphoreSlim for async and lock for sync operations.
    /// </summary>
    public sealed class SaveSystem : ISaveSystem {
        private readonly ISaveSerializer serializer;
        private readonly ISaveStorage storage;
        private readonly ISaveSlotManager slotManager;
        private readonly int saveVersion;

        private readonly object syncLock = new object();
        private readonly SemaphoreSlim asyncLock = new SemaphoreSlim(1, 1);

        public SaveSystem(
            ISaveSerializer serializer,
            ISaveStorage storage,
            ISaveSlotManager slotManager,
            SaveSystemSettings settings) {
            this.serializer = serializer;
            this.storage = storage;
            this.slotManager = slotManager;
            saveVersion = settings.currentSaveVersion;
        }

        public string CreateSession() {
            return slotManager.CreateSession();
        }

        public bool HasAnySaves() {
            return slotManager.HasAnySaves();
        }

        public void Save<TData>(string sessionId, TData data, SaveSlotType slotType) {
            lock(syncLock) {
                SaveSlotInfo slot = AllocateSlot(sessionId, slotType);
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                SaveEnvelope envelope = new SaveEnvelope {
                    version = saveVersion,
                    timestampUtc = timestamp,
                    slotType = slotType,
                    payload = JToken.FromObject(data)
                };

                byte[] envelopeBytes = serializer.Serialize(envelope);
                storage.Write(slot.filePath, envelopeBytes);

                slotManager.UpdateSlotMetadata(slot, timestamp, saveVersion);
                slotManager.PersistIndex();

                Debug.Log($"[SaveSystem] Saved {slotType} to {slot.filePath}");
            }
        }

        public TData Load<TData>(SaveSlotInfo slot) {
            lock(syncLock) {
                return LoadInternal<TData>(slot);
            }
        }

        public async Task SaveAsync<TData>(string sessionId, TData data, SaveSlotType slotType) {
            await asyncLock.WaitAsync().ConfigureAwait(false);
            try {
                SaveSlotInfo slot = AllocateSlot(sessionId, slotType);
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                SaveEnvelope envelope = new SaveEnvelope {
                    version = saveVersion,
                    timestampUtc = timestamp,
                    slotType = slotType,
                    payload = JToken.FromObject(data)
                };

                byte[] envelopeBytes = serializer.Serialize(envelope);
                await storage.WriteAsync(slot.filePath, envelopeBytes).ConfigureAwait(false);

                slotManager.UpdateSlotMetadata(slot, timestamp, saveVersion);
                slotManager.PersistIndex();

                Debug.Log($"[SaveSystem] Saved {slotType} to {slot.filePath}");
            } finally {
                asyncLock.Release();
            }
        }

        public async Task<TData> LoadAsync<TData>(SaveSlotInfo slot) {
            await asyncLock.WaitAsync().ConfigureAwait(false);
            try {
                return LoadInternal<TData>(slot);
            } finally {
                asyncLock.Release();
            }
        }

        public IReadOnlyList<SaveSlotInfo> GetSlots(string sessionId) {
            return slotManager.GetSlots(sessionId);
        }

        public IReadOnlyList<SaveSessionInfo> GetAllSessions() {
            return slotManager.GetAllSessions();
        }

        public void DeleteSlot(SaveSlotInfo slot) {
            lock(syncLock) {
                slotManager.DeleteSlot(slot);
            }
        }

        public void DeleteSession(string sessionId) {
            lock(syncLock) {
                slotManager.DeleteSession(sessionId);
            }
        }

        private SaveSlotInfo AllocateSlot(string sessionId, SaveSlotType slotType) {
            return slotType switch {
                SaveSlotType.Manual => slotManager.AllocateManualSlot(sessionId),
                SaveSlotType.Auto => slotManager.AllocateAutoSlot(sessionId),
                SaveSlotType.Quick => slotManager.AllocateQuickSlot(sessionId),
                _ => throw new ArgumentOutOfRangeException(nameof(slotType), slotType, "Unknown slot type.")
            };
        }

        private TData LoadInternal<TData>(SaveSlotInfo slot) {
            byte[] envelopeBytes = storage.Read(slot.filePath);
            SaveEnvelope envelope = serializer.Deserialize<SaveEnvelope>(envelopeBytes);
            return envelope.payload.ToObject<TData>();
        }
    }
}
