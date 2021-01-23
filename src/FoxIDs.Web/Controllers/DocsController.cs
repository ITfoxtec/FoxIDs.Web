using Azure;
using FoxIDs.Web;
using FoxIDs.Web.Logic;
using FoxIDs.Web.Models;
using FoxIDs.Web.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ITfoxtecWebCore.Controllers
{
    public class DocsController : Controller
    {
        private readonly ILogger<DocsController> logger;
        private readonly Settings settings;
        private readonly GitHubFileLogic gitHubFileLogic;

        public DocsController(ILogger<DocsController> logger, Settings settings, IFoxIDsGitHubFileLogic foxIDsGitHubFileLogic)
        {
            this.logger = logger;
            this.settings = settings;
            gitHubFileLogic = foxIDsGitHubFileLogic as GitHubFileLogic;
        }

        public async Task<ActionResult> Index()
        {
            if (HttpContext.Items.ContainsKey(Constants.Route.GithubImage))
            {
                var pageItem = HttpContext.Items[Constants.Route.GithubImage] as string;
                var image = await gitHubFileLogic.LoadImageAsync(pageItem.ToLower());
                return File(image, GetImageMimeType(pageItem));
            }

            try
            {
                var page = "Index";
                var isDefaultPage = true;
                if (HttpContext.Items.ContainsKey(Constants.Route.GithubPage))
                {
                    var pageItem = HttpContext.Items[Constants.Route.GithubPage] as string;
                    if (settings.ReloadGitHubPassword.Equals(pageItem, StringComparison.Ordinal))
                    {
                        await gitHubFileLogic.UpdateFilesAsync();
                    }
                    else if (!"README".Equals(pageItem, StringComparison.OrdinalIgnoreCase))
                    {
                        page = pageItem;
                        isDefaultPage = false;
                    }
                }

                (var title, var pageContent) = await gitHubFileLogic.LoadFileWithTitle($"{page.ToLower()}.md");
                ViewData["Title"] = isDefaultPage || title.IsNullOrEmpty() ? "Docs" : $"Docs - {title}";
                var sidebar = await gitHubFileLogic.LoadFileAsync("_sidebar.md");
                return View("GitHubFile", new GitHubFileViewModel { PageContent = pageContent, Sidebar = sidebar });

            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, $"Unable to load FoxIDs path '{Request.Path}'.");
                return RedirectToAction();
            }        
        }

        private string GetImageMimeType(string pageItem)
        {
            var pageSplit = pageItem.Split('.');
            if(pageSplit?.Length != 2)
            {
                throw new Exception($"Invalid page image item '{pageItem}'");
            }

            var extension = pageSplit[1];
            return extension switch
            {
                "png" => "image/png",
                "jpeg" => "image/jpeg",
                "svg" => "image/svg+xml",
                _ => "image/png"
            };            
        }
    }
}
