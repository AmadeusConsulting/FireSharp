using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireSharp.Extensions
{
    public static class DateTimeOffsetExtensions
    {
        private readonly static DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero); 

        public static long ToUnixTimestampSeconds(this DateTimeOffset dateTime)
        {
            var timeSinceEpoch = dateTime - Epoch;

            return (long)timeSinceEpoch.TotalSeconds;
        }
    }
}
