using Hangfire;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                WorkerCount = 2
            });
            //GlobalJobFilters.Filters.Add(new LogEverythingAttribute());
            //GlobalJobFilters.Filters.Add(new DisableConcurrentExecutionAttribute(60));

        }
    }
}
