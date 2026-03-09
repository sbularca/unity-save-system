using System;
using Newtonsoft.Json.Linq;

namespace Jovian.SaveSystem {
    /// <summary>
    /// On-disk wrapper that pairs minimal metadata with the game data payload.
    /// Payload is stored as JToken so it remains readable JSON when using JsonSaveSerializer.
    /// </summary>
    [Serializable]
    public sealed class SaveEnvelope {
        public int version;
        public long timestampUtc;
        public SaveSlotType slotType;
        public JToken payload;
    }
}
