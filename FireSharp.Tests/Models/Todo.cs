using Newtonsoft.Json;

namespace FireSharp.Tests.Models
{
    public class Todo
    {
        public string name { get; set; }
        public int priority { get; set; }

        [JsonIgnore]
        public string notSerialized { get; set; }
    }
}