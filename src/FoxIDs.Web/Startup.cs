using FoxIDs.Web.Infrastructure.Hosting;
using FoxIDs.Web.Infrastructure.Transformers;
using FoxIDs.Web.Models.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Westwind.AspNetCore.Markdown;

namespace FoxIDs.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        private IConfiguration Configuration { get; }
        private IWebHostEnvironment CurrentEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(options => { options.DeveloperMode = CurrentEnvironment.IsDevelopment(); });

            var settings = services.BindConfig<Settings>(Configuration, nameof(Settings));

            services.AddInfrastructure(settings, CurrentEnvironment);
            services.AddLogic(settings);

            services.AddControllersWithViews()
                .AddApplicationPart(typeof(MarkdownPageProcessorMiddleware).Assembly);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler($"/{Constants.Route.DefaultSiteController}/Error");
                app.UseHsts();
            }

            app.UseSecurityHeaders(CurrentEnvironment);
            app.UseHttpsRedirection();

            app.UseMarkdown();

            app.UseStaticFilesCacheControl(CurrentEnvironment);
            app.UseProxyClientIpMiddleware();

            app.UseCookiePolicy();

            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404)
                {
                    context.Request.Path = $"/{Constants.Route.DefaultSiteController}";
                    await next();
                }
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: $"{{controller={Constants.Route.DefaultSiteController}}}/{{action={Constants.Route.DefaultSiteAction}}}/{{id?}}");

                endpoints.MapDynamicControllerRoute<GitHubFileRouteTransformer>($"docs/{{**{Constants.Route.RouteTransformerPathKey}}}");
                endpoints.MapDynamicControllerRoute<SitemapTransformer>("sitemap.xml");
                endpoints.MapDynamicControllerRoute<RobotsTransformer>("robots.txt");
                endpoints.MapDynamicControllerRoute<SiteRouteTransformer>($"{{**{Constants.Route.RouteTransformerPathKey}}}");
            });
        }
    }
}
