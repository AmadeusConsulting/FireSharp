using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FireSharp.Logging;

using log4net;

using ILog = FireSharp.Logging.ILog;

namespace FireSharp.Test.Console
{
    public class Log4NetLogManager : ILogManager
    {
        public ILog GetLogger<T>()
        {
            log4net.ILog logger = LogManager.GetLogger(typeof(T));

            return new Log4NetLoggingAdapter(logger);
        }

        public ILog GetLogger<T>(T clazz) where T : class
        {
            return GetLogger<T>();
        }
    }
}
