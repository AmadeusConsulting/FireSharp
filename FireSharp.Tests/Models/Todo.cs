using Newtonsoft.Json;

namespace FireSharp.Tests.Models
{
    public class Todo
    {
        public string name { get; set; }
        public int priority { get; set; }

        [JsonIgnore]
        public string notSerialized { get; set; }

        public Assignee assignee { get; set; }
    }

    public class Assignee
    {
        public string firstName { get; set; }

        public string lastName { get; set; }

        public string position { get; set; }
    }
}