using System.Collections.Generic;

namespace Jovian.SaveSystem {
    /// <summary>
    /// Manages save slot allocation, session tracking, and auto-save rotation.
    /// </summary>
    public interface ISaveSlotManager {
        SaveSlotInfo AllocateManualSlot(string sessionId);
        SaveSlotInfo AllocateAutoSlot(string sessionId);
        SaveSlotInfo AllocateQuickSlot(string sessionId);

        IReadOnlyList<SaveSlotInfo> GetSlots(string sessionId);
        IReadOnlyList<SaveSessionInfo> GetAllSessions();

        string CreateSession();
        void DeleteSlot(SaveSlotInfo slot);
        void DeleteSession(string sessionId);
        bool HasAnySaves();

        void UpdateSlotMetadata(SaveSlotInfo slot, long timestampUtc, int saveVersion);
        void PersistIndex();
    }
}
