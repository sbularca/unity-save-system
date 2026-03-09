using System;
using System.Text;
using Newtonsoft.Json;

namespace Jovian.SaveSystem {
    /// <summary>
    /// Serializes data to/from JSON using Newtonsoft.Json.
    /// </summary>
    public sealed class JsonSaveSerializer : ISaveSerializer {
        private readonly JsonSerializerSettings serializerSettings;

        public JsonSaveSerializer() {
            serializerSettings = new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
        }

        public byte[] Serialize<TData>(TData data) {
            string json = JsonConvert.SerializeObject(data, serializerSettings);
            return Encoding.UTF8.GetBytes(json);
        }

        public TData Deserialize<TData>(byte[] payload) {
            if(payload == null || payload.Length == 0) {
                throw new ArgumentException("Payload is null or empty.", nameof(payload));
            }

            string json = Encoding.UTF8.GetString(payload);
            return JsonConvert.DeserializeObject<TData>(json, serializerSettings);
        }
    }
}
