using System;

namespace Jovian.SaveSystem {
    /// <summary>
    /// Describes a save session (a new game playthrough).
    /// </summary>
    [Serializable]
    public sealed class SaveSessionInfo {
        public string sessionId;
        public long creationDateUtc;
        public long lastSaveDateUtc;

        public DateTime CreationDateTime => DateTimeOffset.FromUnixTimeMilliseconds(creationDateUtc).UtcDateTime;
        public DateTime LastSaveDateTime => DateTimeOffset.FromUnixTimeMilliseconds(lastSaveDateUtc).UtcDateTime;
    }
}
