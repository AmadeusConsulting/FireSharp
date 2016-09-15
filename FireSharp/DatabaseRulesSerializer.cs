using System;

using Newtonsoft.Json;

namespace FireSharp
{
    public class DatabaseRulesSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var rules = value as DatabaseRules;
            serializer.Serialize(writer, rules?.Rules);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(DatabaseRules) == objectType;
        }
    }
}