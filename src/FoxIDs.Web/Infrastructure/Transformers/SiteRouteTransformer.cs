using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Web.Infrastructure.Transformers
{
    public class SiteRouteTransformer : DynamicRouteValueTransformer
    {
        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            try
            {
                var path = values[Constants.Route.RouteTransformerPathKey] is string ? values[Constants.Route.RouteTransformerPathKey] as string : string.Empty;
                var route = path.Split('/').Where(r => !r.IsNullOrWhiteSpace()).ToArray();

                if(!values.ContainsKey("controller"))
                {
                    values["controller"] = Constants.Route.DefaultSiteController;
                    if (route.Length == 1)
                    {
                        values["action"] = route[0];
                    }
                    else
                    {
                        values["action"] = Constants.Route.DefaultSiteAction;
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
