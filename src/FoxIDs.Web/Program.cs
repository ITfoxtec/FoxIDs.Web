using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoxIDs.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel(options => options.AddServerHeader = false)
                .UseStartup<Startup>()
                .ConfigureLogging((context, logging) =>
                {
                    var instrumentationKey = context.Configuration.GetSection("ApplicationInsights:InstrumentationKey").Value;

                    if (string.IsNullOrWhiteSpace(instrumentationKey))
                    {
                        return;
                    }

                    // When not in development, remove other loggers like console, debug, event source etc. and only use ApplicationInsights logging
                    if (!context.HostingEnvironment.IsDevelopment())
                    {
                        logging.ClearProviders();
                    }

                    logging.AddApplicationInsights(instrumentationKey);
                });
    }
}
