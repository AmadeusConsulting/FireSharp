using Newtonsoft.Json;

namespace FireSharp.Interfaces
{
    public class JsonNetSerializer : ISerializer
    {
        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public string Serialize<T>(T value, bool formatted = false)
        {
            return JsonConvert.SerializeObject(value, formatted ? Formatting.Indented : Formatting.None);
        }
    }
}