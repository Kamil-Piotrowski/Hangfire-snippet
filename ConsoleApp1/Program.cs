using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Owin.Hosting;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>("http://localhost:9000"))
            {
                RecurringJob.AddOrUpdate(() => Job.DoLongTask1(), Cron.Daily);
                RecurringJob.AddOrUpdate(() => Job.DoLongTask2(), Cron.Daily);
                RecurringJob.AddOrUpdate(() => Job.DoLongTask3(), Cron.Daily);
                Console.WriteLine("Hangfire on");
                Console.ReadKey();
            }
        }


        
    }
}
