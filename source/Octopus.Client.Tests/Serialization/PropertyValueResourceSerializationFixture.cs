using Newtonsoft.Json;
using NUnit.Framework;
using Octopus.Client.Model;

namespace Octopus.Client.Tests.Serialization
{
    [TestFixture]
    public class PropertyValueResourceSerializationFixture
    {
        [Test]
        public void PropertyValueResourceDeserializesCorrectly()
        {
            // PropertyValueResource requires PropertyValueJsonConverter to deserialize correctly,
            // this test ensures the converter doesn't need to be specified explicitly when (de)serializing.

            var subject = new PropertyValueResource("foo");
            var serialized = JsonConvert.SerializeObject(subject);
            var deserialized = JsonConvert.DeserializeObject<PropertyValueResource>(serialized);
            Assert.AreEqual(subject.Value, deserialized.Value);
        }
    }
}
