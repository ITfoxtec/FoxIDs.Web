using FoxIDs.Web.Logic;
using FoxIDs.Web.Models.Config;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Web.Controllers
{
    public class SitemapController : Controller
    {
        private const string defaultLastmod = "2021-01-23";
        private readonly Settings settings;
        private readonly GitHubFileLogic gitHubFileLogic;

        public SitemapController(Settings settings, IFoxIDsGitHubFileLogic foxIDsGithubFileLogic)
        {
            this.settings = settings;
            gitHubFileLogic = foxIDsGithubFileLogic as GitHubFileLogic;
        }

        public async Task<ActionResult> Index()
        {
            return new ContentResult
            {
                Content = await SitemapXmlAsync(),
                ContentType = "application/xml",
            };
        }

        private async Task<string> SitemapXmlAsync()
        {
            return
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xhtml=""http://www.w3.org/1999/xhtml"" xsi:schemaLocation=""http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd http://www.w3.org/1999/xhtml http://www.w3.org/2002/08/xhtml/xhtml1-strict.xsd"">
  <url>
    <loc>{settings.BaseSitePath}</loc>
    <lastmod>{defaultLastmod}</lastmod>
    <changefreq>daily</changefreq>
    <priority>0.9</priority>
  </url>
{await FoxIDsSitemapXmlAsync()}
  <url>
    <loc>{settings.BaseSitePath}support</loc>
    <lastmod>{defaultLastmod}</lastmod>
    <changefreq>daily</changefreq>
    <priority>0.8</priority>
  </url>
</urlset>";
        }

        private async Task<string> FoxIDsSitemapXmlAsync()
        {
            var sitemap = new List<string>();
            var sitePages = await gitHubFileLogic.LoadSitePagesAsync();
            foreach (var page in sitePages)
            {
                var pageSplit = page.Split(':');

                if (page.StartsWith("index", StringComparison.OrdinalIgnoreCase))
                {
                    sitemap.Add(
$@"  <url>
    <loc>{settings.BaseSitePath}docs</loc>
    <lastmod>{pageSplit[1]}</lastmod>
    <changefreq>daily</changefreq>
    <priority>1.0</priority>
  </url>");
                }
                else
                {
                    sitemap.Add(
$@"  <url>
    <loc>{settings.BaseSitePath}docs/{pageSplit[0]}</loc>
    <lastmod>{pageSplit[1]}</lastmod>
    <changefreq>daily</changefreq>
    <priority>0.9</priority>
  </url>");
                }
            }

            return string.Join(Environment.NewLine, sitemap);
        }
    }
}
