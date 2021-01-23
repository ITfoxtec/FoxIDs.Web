using FoxIDs.Web.Infrastructure.GitHubApi;
using FoxIDs.Web.Infrastructure.Transformers;
using FoxIDs.Web.Logic;
using FoxIDs.Web.Models.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using Westwind.AspNetCore.Markdown;

namespace FoxIDs.Web.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogic(this IServiceCollection services, Settings settings)
        {
            services.AddSingleton<IFoxIDsGitHubFileLogic>((serviceProvider) =>
            {
                return new GitHubFileLogic(serviceProvider.GetService<ILogger<GitHubFileLogic>>(), settings, serviceProvider.GetService<IHttpClientFactory>());
            });

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, Settings settings, IWebHostEnvironment env)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddMarkdown();

            services.AddScoped<GitHubFileRouteTransformer>();
            services.AddScoped<SitemapTransformer>();
            services.AddScoped<RobotsTransformer>();
            services.AddScoped<SiteRouteTransformer>();

            services.AddHttpContextAccessor();
            services.AddHttpClient(Constants.GitHubApiHttpClientNameKey)
                .AddHttpMessageHandler<GitHubApiMessageHandler>();
            services.AddTransient<GitHubApiMessageHandler>();

            return services;
        }

    }
}
