using Hangfire;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.States;

namespace ConsoleApp1
{
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            
            GlobalConfiguration.Configuration
                .UseColouredConsoleLogProvider()
                .UseSqlServerStorage("ConsoleApp1.Properties.Settings.Database1ConnectionString");
            app.UseHangfireDashboard();





            app.UseHangfireServer(new BackgroundJobServerOptions {
                WorkerCount = 10,
                Queues = new []{"DEFAULT", "second"}
            });
            //GlobalJobFilters.Filters.Add(new LogEverythingAttribute());
            GlobalJobFilters.Filters.Add(new ContinuationsSupportAttribute(new HashSet<string>{SucceededState.StateName, FailedState.StateName, DeletedState.StateName}));
            GlobalJobFilters.Filters.Add(new SingleInstanceFilterAttribute());

        }
    }
}
