using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jovian.SaveSystem {
    /// <summary>
    /// Top-level facade for the save system. Orchestrates serialization,
    /// storage, and slot management. This is the main API the game interacts with.
    /// </summary>
    public interface ISaveSystem {
        string CreateSession();
        bool HasAnySaves();

        void Save<TData>(string sessionId, TData data, SaveSlotType slotType);
        TData Load<TData>(SaveSlotInfo slot);

        Task SaveAsync<TData>(string sessionId, TData data, SaveSlotType slotType);
        Task<TData> LoadAsync<TData>(SaveSlotInfo slot);

        IReadOnlyList<SaveSlotInfo> GetSlots(string sessionId);
        IReadOnlyList<SaveSessionInfo> GetAllSessions();
        void DeleteSlot(SaveSlotInfo slot);
        void DeleteSession(string sessionId);
    }
}
