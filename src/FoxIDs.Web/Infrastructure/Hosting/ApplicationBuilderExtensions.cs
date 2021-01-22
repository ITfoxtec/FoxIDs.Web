using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;

namespace FoxIDs.Web.Infrastructure.Hosting
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseStaticFilesCacheControl(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseStaticFiles();
            }
            else
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = (context) =>
                    {
                        var headers = context.Context.Response.GetTypedHeaders();
                        headers.CacheControl = new CacheControlHeaderValue()
                        {
                            MaxAge = TimeSpan.FromDays(365),
                        };
                    }
                });
            }

            return app;
        }

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use(async (context, next) =>
            {
                var response = context.Response;

                response.SetHeader("X-Content-Type-Options", "nosniff");

                response.SetHeader("Referrer-Policy", "no-referrer");

                response.SetHeader("X-XSS-Protection", "1; mode=block");

                response.SetHeader("X-Frame-Options", "deny");

                var csp = string.Join(" ", CreateCsp(env));
                response.SetHeader("Content-Security-Policy", csp);
                response.SetHeader("X-Content-Security-Policy", csp);

                await next();
            });

            return app;
        }

        private static IEnumerable<string> CreateCsp(IWebHostEnvironment env)
        {
            yield return "block-all-mixed-content;";

            yield return "default-src 'self';";
            yield return "connect-src 'self' https://dc.services.visualstudio.com/v2/track https://www.google-analytics.com/r/collect https://www.google-analytics.com/j/collect;";
            //yield return "font-src 'self';";
            yield return "img-src 'self' data: 'unsafe-inline' https://www.google-analytics.com/r/collect https://aka.ms/deploytoazurebutton https://raw.githubusercontent.com/Azure/azure-quickstart-templates;";
            //yield return "img-src 'self' data: https://www.google-analytics.com/r/collect;";
            yield return "script-src 'self' 'unsafe-inline' https://az416426.vo.msecnd.net https://www.google-analytics.com https://ajax.googleapis.com;";
            yield return "style-src 'self' 'unsafe-inline';";

            yield return "base-uri 'self';";
            yield return "form-action 'self';";

            yield return "sandbox allow-forms allow-modals allow-popups allow-same-origin allow-scripts;";

            yield return "frame-ancestors 'none';";

            if (!env.IsDevelopment())
            {
                yield return "upgrade-insecure-requests;";
            }
        }

        public static IApplicationBuilder UseProxyClientIpMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ProxyClientIpMiddleware>();
        }
    }
}
