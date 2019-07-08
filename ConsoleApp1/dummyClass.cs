using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Logging;

namespace ConsoleApp1
{
    class dummyClass
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        public void logSomething(string text)
        {
            Logger.InfoFormat(text);
        }
    }
}
