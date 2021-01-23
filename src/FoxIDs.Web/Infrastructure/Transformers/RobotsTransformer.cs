using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Web.Infrastructure.Transformers
{
    public class RobotsTransformer : DynamicRouteValueTransformer
    {
        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            try
            {
                values = new RouteValueDictionary();
                values["controller"] = "robots";
                values["action"] = "index";           
                return new ValueTask<RouteValueDictionary>(values);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failing URL '{httpContext.Request.Scheme}://{httpContext.Request.Host.ToUriComponent()}{httpContext.Request.Path.Value}'", ex);
            }
        }
    }
}
