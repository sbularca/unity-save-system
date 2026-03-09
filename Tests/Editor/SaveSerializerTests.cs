using System;
using NUnit.Framework;

namespace Jovian.SaveSystem.Tests.Editor {
    public class SaveSerializerTests {
        [Serializable]
        private sealed class TestData {
            public string name;
            public int score;
            public float[] positions;
        }

        [Test]
        public void JsonSerializer_RoundTrip_PreservesData() {
            JsonSaveSerializer serializer = new JsonSaveSerializer();
            TestData original = new TestData {
                name = "TestPlayer",
                score = 42,
                positions = new[] { 1.5f, 2.5f, 3.5f }
            };

            byte[] bytes = serializer.Serialize(original);
            TestData deserialized = serializer.Deserialize<TestData>(bytes);

            Assert.AreEqual(original.name, deserialized.name);
            Assert.AreEqual(original.score, deserialized.score);
            Assert.AreEqual(original.positions, deserialized.positions);
        }

        [Test]
        public void JsonSerializer_ProducesNonEmptyBytes() {
            JsonSaveSerializer serializer = new JsonSaveSerializer();
            TestData data = new TestData { name = "Test", score = 1 };

            byte[] bytes = serializer.Serialize(data);

            Assert.IsNotNull(bytes);
            Assert.Greater(bytes.Length, 0);
        }

        [Test]
        public void JsonSerializer_DeserializeNull_Throws() {
            JsonSaveSerializer serializer = new JsonSaveSerializer();

            Assert.Throws<ArgumentException>(() => serializer.Deserialize<TestData>(null));
            Assert.Throws<ArgumentException>(() => serializer.Deserialize<TestData>(Array.Empty<byte>()));
        }

        [Test]
        public void BinarySerializer_RoundTrip_PreservesData() {
            BinarySaveSerializer serializer = new BinarySaveSerializer("test-key-123");
            TestData original = new TestData {
                name = "BinaryPlayer",
                score = 99,
                positions = new[] { 10f, 20f, 30f }
            };

            byte[] bytes = serializer.Serialize(original);
            TestData deserialized = serializer.Deserialize<TestData>(bytes);

            Assert.AreEqual(original.name, deserialized.name);
            Assert.AreEqual(original.score, deserialized.score);
            Assert.AreEqual(original.positions, deserialized.positions);
        }

        [Test]
        public void BinarySerializer_OutputDiffersFromJson() {
            JsonSaveSerializer jsonSerializer = new JsonSaveSerializer();
            BinarySaveSerializer binarySerializer = new BinarySaveSerializer("test-key");
            TestData data = new TestData { name = "Test", score = 1 };

            byte[] jsonBytes = jsonSerializer.Serialize(data);
            byte[] binaryBytes = binarySerializer.Serialize(data);

            Assert.AreNotEqual(jsonBytes, binaryBytes);
        }

        [Test]
        public void BinarySerializer_DifferentKeys_ProduceDifferentOutput() {
            BinarySaveSerializer serializer1 = new BinarySaveSerializer("key-alpha");
            BinarySaveSerializer serializer2 = new BinarySaveSerializer("key-bravo");
            TestData data = new TestData { name = "Test", score = 1 };

            byte[] bytes1 = serializer1.Serialize(data);
            byte[] bytes2 = serializer2.Serialize(data);

            Assert.AreNotEqual(bytes1, bytes2);
        }

        [Test]
        public void BinarySerializer_EmptyKey_Throws() {
            Assert.Throws<ArgumentException>(() => new BinarySaveSerializer(""));
            Assert.Throws<ArgumentException>(() => new BinarySaveSerializer(null));
        }

        [Test]
        public void BinarySerializer_DeserializeNull_Throws() {
            BinarySaveSerializer serializer = new BinarySaveSerializer("key");

            Assert.Throws<ArgumentException>(() => serializer.Deserialize<TestData>(null));
            Assert.Throws<ArgumentException>(() => serializer.Deserialize<TestData>(Array.Empty<byte>()));
        }
    }
}
