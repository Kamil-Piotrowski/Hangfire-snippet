using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Job
    {
        [LogEverything]
        public static void Execute()
        {
            Console.WriteLine("Doing the job");
        }
        
        public static void DoLongTask()
        {
            Thread.Sleep(10000);
            //Console.WriteLine("Done the job");
        }
        //[DisableConcurrentExecution(60)]
        //[SingleJob]
        //[Queue("first")]
        [AutomaticRetry(Attempts = 1,DelaysInSeconds = new []{1,1}, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public static void DoLongTask1()
        {
            DoLongTask();
            throw new Exception();
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
    }
}
