using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace FoxIDs.Web.Controllers
{
    public class RobotsController : Controller
    {
        public IActionResult Index()
        {
            var content = new StringBuilder();
            content.AppendLine("User-agent: *");
            content.AppendLine("Allow: /");

            return new ContentResult
            {
                Content = content.ToString(),
                ContentType = "text/plain"            
            };
        }
    }
}
