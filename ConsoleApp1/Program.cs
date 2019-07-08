using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.States;
using Microsoft.Owin.Hosting;

namespace ConsoleApp1
{
    class Program
    {



        static void Main(string[] args)
        {

            using (WebApp.Start<Startup>("http://localhost:9000"))
            {
//                for (int i = 0; i < 10; i++)
//                {
//                    BackgroundJob.Enqueue(() => Job.DoLongTask1());
//                    BackgroundJob.Enqueue(() => Job.DoLongTask2());
//                }
                RecurringJob.AddOrUpdate(()=>Job.DoLongTask1(), "0 4 * * *");
                RecurringJob.AddOrUpdate(() => Job.DoLongTask2(), "0 4 * * *");

                


                Console.WriteLine("Hangfire on");
                Console.ReadKey();
            }


            
        }


        
    }
}
