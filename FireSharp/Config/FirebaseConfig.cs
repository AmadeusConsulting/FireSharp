using FireSharp.Interfaces;
using System;

using FireSharp.Logging;

namespace FireSharp.Config
{
    public class FirebaseConfig : IFirebaseConfig
    {
        private string _basePath;

        private ILogManager _logManager;

        public FirebaseConfig()
        {
            Serializer = new JsonNetSerializer();
        }

        public string BasePath
        {
            get
            {
                return _basePath.EndsWith("/") ? _basePath : $"{_basePath}/";
            }
            set { _basePath = value; }
        }

        public string Host { get; set; }
        public string AuthSecret { get; set; }

        public TimeSpan? RequestTimeout { get; set; }

        public ISerializer Serializer { get; set; }

        public ILogManager LogManager
        {
            get
            {
                return _logManager ?? (_logManager = new NoOpLogManager());
            }
            set
            {
                _logManager = value;
            }
        }
    }
}
