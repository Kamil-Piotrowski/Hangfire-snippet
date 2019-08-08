using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Logging;

namespace ConsoleApp1
{
    class Job
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        

        [LogEverything]
        public static void Execute()
        {
            Console.WriteLine("Doing the job");
        }
        
        public static void DoLongTask()
        {
            Thread.Sleep(5000);
            //Console.WriteLine("Done the job");
        }
        //[DisableConcurrentExecution(60)]
        //[SingleJob]
        //[Queue("first")]
        [Followup("ConsoleApp1.Job", "dummy", "aaa")]
        //[AutomaticRetry(Attempts = 1,DelaysInSeconds = new []{1,1}, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public static void DoLongTask1()
        {
            
            DoLongTask();
            //throw new Exception();
        }
        //[DisableConcurrentExecution(60)]
        //[SingleJob]
        //[Queue("second")]
        public static void DoLongTask2()
        {
            DoLongTask();
        }
        //[DisableConcurrentExecution(60)]
        //[SingleJob]
        public static void DoLongTask3()
        {
            DoLongTask();
        }

        public static void dummy(string message)
        {
            Logger.InfoFormat(message);
        }

      
    }
}
