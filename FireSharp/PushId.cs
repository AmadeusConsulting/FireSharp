using System;
using System.Collections.Generic;
using System.Linq;

namespace FireSharp
{
    /// <summary>
    ///  ID Generator that creates 20-character string identifiers that are:
    ///  * based on timestamp
    ///  * based on random data to prevent collisions
    ///  * sortable in creation order
    ///  * monotonically increasing
    /// </summary>
    /// <remarks>
    /// Gist located at https://gist.github.com/mikelehen/3596a30bd69384624c11 used as a reference implementation
    /// </remarks>
    public static class PushId
    {
        private static string PushCharacters = "-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";

        private static long LastPushTimeStamp = long.MinValue;

        private static readonly List<char> LastRandCharacters = new List<char>();

        private static readonly object IdSync = new Object();

        public static string NewId()
        {
            lock (IdSync)
            {
                var prng = new Random();
                var now = DateTimeOffset.Now;
                var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
                var timeStamp = (long)(now - epoch).TotalMilliseconds;
                var duplicateTime = timeStamp == LastPushTimeStamp;

                var timeStampChars = new char[8];
                for (int i = 7; i >= 0; --i)
                {
                    timeStampChars[i] = PushCharacters[(int)(timeStamp % 64L)];
                    timeStamp = timeStamp >> 6;
                }

                if (timeStamp != 0)
                {
                    throw new Exception("Entire timestamp should have been converted");
                }

                var id = timeStampChars.Aggregate(string.Empty, (str, c) => $"{str}{c}");

                if (!duplicateTime)
                {
                    for (int i = 0; i < 12; ++i)
                    {
                        var randChar = (char)((int)Math.Floor(prng.NextDouble() * 64));
                        if (LastRandCharacters.Count > i)
                        {
                            LastRandCharacters[i] = randChar;
                        }
                        else
                        {
                            LastRandCharacters.Add(randChar);
                        }
                    }
                }
                else
                {
                    int idx = 11;
                    for (; idx >= 0 && LastRandCharacters[idx] == 63; idx--)
                    {
                        LastRandCharacters[idx] = (char)0;
                    }
                    LastRandCharacters[idx]++;
                }
                for (int i = 0; i < 12; i++)
                {
                    id += PushCharacters[LastRandCharacters[i]];
                }

                if (id.Length != 20)
                {
                    throw new Exception("Id Length should be 20.");
                }

                LastPushTimeStamp = timeStamp;

                return id;
            }
        }
    }
}
