using System;
using System.Collections.Generic;
using System.Reflection;

using FireSharp.Response;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace FireSharp
{
    public class DatabaseRulesSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var rules = value as DatabaseRules;
            if (rules != null)
            {
                serializer.Serialize(writer, rules.Rules);
            }
            else
            {
                var dict = value as IDictionary<string, object>;
                serializer.Serialize(writer, value);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException($"Expected to read Dictionary Json Object for Database rules, but instead got {reader.TokenType}");
            }

            return new DatabaseRules((IDictionary<string, object>)ReadValue(reader));
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(DatabaseRules) == objectType || typeof(IDictionary<string, object>).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        /// <summary>
        /// Reads the value.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">
        /// Unexpected Token when converting IDictionary<string, object>
        /// </exception>
        /// <remarks>
        /// Adapted from http://stackoverflow.com/a/38029052
        /// </remarks>
        private object ReadValue(JsonReader reader)
        {
            while (reader.TokenType == JsonToken.Comment)
            {
                if (!reader.Read())
                {
                    throw new JsonSerializationException("Unexpected Token when converting IDictionary<string, object>");
                }
            }

            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return ReadObject(reader);
                case JsonToken.StartArray:
                    return ReadArray(reader);
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Undefined:
                case JsonToken.Null:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    return reader.Value;
                default:
                    throw new JsonSerializationException($"Unexpected token when converting IDictionary<string, object>: {reader.TokenType}");
            }
        }

        /// <summary>
        /// Reads the array.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">Unexpected end when reading IDictionary<string, object></exception>
        /// <remarks>
        /// Adapted from http://stackoverflow.com/a/38029052
        /// </remarks>
        private object ReadArray(JsonReader reader)
        {
            IList<object> list = new List<object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Comment:
                        break;
                    default:
                        var v = ReadValue(reader);

                        list.Add(v);
                        break;
                    case JsonToken.EndArray:
                        return list;
                }
            }

            throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
        }

        /// <summary>
        /// Reads the object.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">
        /// Unexpected end when reading IDictionary<string, object>
        /// or
        /// Unexpected end when reading IDictionary<string, object>
        /// </exception>
        /// <remarks>
        /// Adapted from http://stackoverflow.com/a/38029052
        /// </remarks>
        private IDictionary<string, object> ReadObject(JsonReader reader)
        {
            var obj = new Dictionary<string, object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        var propertyName = reader.Value.ToString();

                        if (!reader.Read())
                        {
                            throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
                        }

                        var v = ReadValue(reader);

                        obj[propertyName] = v;
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.EndObject:
                        return obj;
                }
            }

            throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
        }
    }
}