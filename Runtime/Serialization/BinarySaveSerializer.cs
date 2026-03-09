using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace Jovian.SaveSystem {
    /// <summary>
    /// Serializes data to an obfuscated binary format.
    /// Pipeline: JSON string → UTF-8 bytes → XOR obfuscation → DeflateStream compression.
    /// </summary>
    public sealed class BinarySaveSerializer : ISaveSerializer {
        private readonly byte[] keyBytes;
        private readonly JsonSerializerSettings serializerSettings;

        public BinarySaveSerializer(string obfuscationKey) {
            if(string.IsNullOrEmpty(obfuscationKey)) {
                throw new ArgumentException("Obfuscation key must not be null or empty.", nameof(obfuscationKey));
            }

            keyBytes = Encoding.UTF8.GetBytes(obfuscationKey);
            serializerSettings = new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            };
        }

        public byte[] Serialize<TData>(TData data) {
            string json = JsonConvert.SerializeObject(data, serializerSettings);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] obfuscated = ApplyXor(jsonBytes);
            return Compress(obfuscated);
        }

        public TData Deserialize<TData>(byte[] payload) {
            if(payload == null || payload.Length == 0) {
                throw new ArgumentException("Payload is null or empty.", nameof(payload));
            }

            byte[] decompressed = Decompress(payload);
            byte[] deobfuscated = ApplyXor(decompressed);
            string json = Encoding.UTF8.GetString(deobfuscated);
            return JsonConvert.DeserializeObject<TData>(json, serializerSettings);
        }

        private byte[] ApplyXor(byte[] data) {
            byte[] result = new byte[data.Length];
            for(int i = 0; i < data.Length; i++) {
                result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            }
            return result;
        }

        private static byte[] Compress(byte[] data) {
            using(MemoryStream output = new MemoryStream()) {
                using(DeflateStream deflate = new DeflateStream(output, CompressionLevel.Fastest, leaveOpen: true)) {
                    deflate.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        private static byte[] Decompress(byte[] data) {
            using(MemoryStream input = new MemoryStream(data)) {
                using(DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress)) {
                    using(MemoryStream output = new MemoryStream()) {
                        deflate.CopyTo(output);
                        return output.ToArray();
                    }
                }
            }
        }
    }
}
