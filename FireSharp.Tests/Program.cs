using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using NUnit.ConsoleRunner;

namespace FireSharp.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Runner.Main(new[] { Assembly.GetExecutingAssembly().Location, "/xml:nunit-results.xml" }.Concat(args).ToArray());
        }
    }
}
