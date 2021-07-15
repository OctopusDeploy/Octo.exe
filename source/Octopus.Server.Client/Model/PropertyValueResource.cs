using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Octopus.Client.Model
{
    /// <summary>
    /// Property-values can be sensitive or non-sensitive.
    /// </summary>
    [JsonConverter(typeof(PropertyValueJsonConverter))]
    public class PropertyValueResource
    {
        public PropertyValueResource(string value)
            :this(value, false)
        { }

        public PropertyValueResource(string value, bool isSensitive)
        {
            if (isSensitive)
            {
                IsSensitive = true;
                SensitiveValue = value;
                return;
            }

            Value = value;
            IsSensitive = false;
        }

        [JsonConstructor]
        public PropertyValueResource(SensitiveValue sensitiveValue)
        {
            IsSensitive = true;
            SensitiveValue = sensitiveValue;
        }

        public bool IsSensitive { get; }

        public string Value { get; }

        public SensitiveValue SensitiveValue { get; }

        public static implicit operator PropertyValueResource(string value)
        {
            return new PropertyValueResource(value);
        }

        public class PropertyValueJsonConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var propertyValue = (PropertyValueResource)value;

                if (propertyValue.IsSensitive == false)
                {
                    writer.WriteValue(propertyValue.Value);
                    return;
                }

                serializer.Serialize(writer, propertyValue.SensitiveValue);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Null:
                        return null;

                    // If it is an object we assume it's a SensitiveValue
                    case JsonToken.StartObject:
                        {
                            var jo = JObject.Load(reader);
                            return new PropertyValueResource(
                                new SensitiveValue
                                {
                                    HasValue = jo.GetValue("HasValue")?.ToObject<bool>() ?? false,
                                    NewValue = jo.GetValue("NewValue").ToObject<string>(),
                                    Hint = jo.GetValue("Hint")?.ToObject<string>()
                                });
                        }

                    // Otherwise treat it as a string
                    default:
                        return new PropertyValueResource(Convert.ToString(reader.Value));
                }

            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(PropertyValueResource).GetTypeInfo().IsAssignableFrom(objectType);
            }
        }
    }
}