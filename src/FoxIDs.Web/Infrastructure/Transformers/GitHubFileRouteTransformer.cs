using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Web.Infrastructure.Transformers
{
    public class GitHubFileRouteTransformer : DynamicRouteValueTransformer
    {
        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            try
            {
                var path = values[Constants.Route.RouteTransformerPathKey] is string ? values[Constants.Route.RouteTransformerPathKey] as string : string.Empty;
                var route = path.Split('/').Where(r => !r.IsNullOrWhiteSpace()).ToArray();

                values["controller"] = "docs";
                values["action"] = "index";
                if(route.Length > 0)
                {
                    if (route[0].Equals(Constants.GithubImageFolder) && route.Length > 1)
                    {
                        httpContext.Items[Constants.Route.GithubImage] = $"{route[0]}/{route[1]}";
                    }
                    else
                    {
                        httpContext.Items[Constants.Route.GithubPage] = route[0];
                    }
                }

                return new ValueTask<RouteValueDictionary>(values);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failing URL '{httpContext.Request.Scheme}://{httpContext.Request.Host.ToUriComponent()}{httpContext.Request.Path.Value}'", ex);
            }
        }
    }
}
