using System;

namespace Jovian.SaveSystem {
    /// <summary>
    /// Describes a single save slot within a session.
    /// </summary>
    [Serializable]
    public sealed class SaveSlotInfo {
        public string sessionId;
        public SaveSlotType slotType;
        public int slotNumber;
        public string filePath;
        public long timestampUtc;
        public int saveVersion;

        public string DisplayLabel =>
            slotType switch {
                SaveSlotType.Manual => $"Manual Save {slotNumber}",
                SaveSlotType.Auto => $"Auto Save {slotNumber}",
                SaveSlotType.Quick => "Quick Save",
                _ => "Unknown"
            };

        public DateTime TimestampDateTime => DateTimeOffset.FromUnixTimeMilliseconds(timestampUtc).UtcDateTime;
    }
}
