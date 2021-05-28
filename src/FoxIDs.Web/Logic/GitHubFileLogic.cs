using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FoxIDs.Web.Models;
using FoxIDs.Web.Models.Config;
using ITfoxtec.Identity;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Web.Logic
{
    // Markdown to HTML component.
    // https://github.com/RickStrahl/Westwind.AspNetCore.Markdown

    public class GitHubFileLogic 
    {
        private readonly ILogger<GitHubFileLogic> logger;
        private readonly Settings settings;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly GitHubSettings gitHubSettings;
        private readonly BlobContainerClient containerClient;

        public GitHubFileLogic(ILogger<GitHubFileLogic> logger, Settings settings, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.settings = settings;
            gitHubSettings = settings.FoxIDsGitHub;
            this.httpClientFactory = httpClientFactory;

            var blobServiceClient = new BlobServiceClient(settings.BlobConnectionString);
            var containerName = $"{MaxLengthSubString(settings.BaseSitePath.TrimEnd('/').Substring(8).Replace(".azurewebsites.net", string.Empty).Replace('.', '-').Replace(':', '-').Replace("--", "-"), 30)}-github-{settings.FoxIDsGitHub.Branch}-{settings.GitHubSiteFolder.Trim('/')}".ToLower();
            try
            {
                containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                if (!containerClient.Exists())
                {
                    containerClient = blobServiceClient.CreateBlobContainer(containerName);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failing blob container name '{containerName}'.", ex);
            }
        }

        private string MaxLengthSubString(string value, int maxLength)
        {
            if (value.Length > maxLength)
            {
                return value.Substring(0, maxLength);
            }
            else
            {
                return value;
            }
        }

        private string PagesApiInfoEndpoint => $"https://api.github.com/repos/{gitHubSettings.Repository}/{gitHubSettings.Project}/contents/{gitHubSettings.Folder}?ref={gitHubSettings.Branch}";
        private string ImagesApiInfoEndpoint => $"https://api.github.com/repos/{gitHubSettings.Repository}/{gitHubSettings.Project}/contents/{gitHubSettings.Folder}/{Constants.GithubImageFolder}?ref={gitHubSettings.Branch}";

        private string PagesApiFileEndpoint => $"https://api.github.com/repos/{gitHubSettings.Repository}/{gitHubSettings.Project}/contents/{gitHubSettings.Folder}/[file]?ref={gitHubSettings.Branch}";
        private string ImagesApiFileEndpoint => $"https://api.github.com/repos/{gitHubSettings.Repository}/{gitHubSettings.Project}/contents/{gitHubSettings.Folder}/{Constants.GithubImageFolder}/[file]?ref={gitHubSettings.Branch}";

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await HandleUploadFilesAsync(stoppingToken);
        }

        private async Task HandleUploadFilesAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                try
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await UpdateFilesAsync();
                    stoppingToken.ThrowIfCancellationRequested();
                    await Task.Delay(new TimeSpan(24, 0, 0), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ObjectDisposedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError($"Upload GitHub files failed, repositoryUri '{PagesApiFileEndpoint}'. {ex}");
                }
            }
        }

        public async Task UpdateFilesAsync()
        {
#if DEBUG
            if (settings.FoxIDsGitHub.LoadFiles)
            {
                return;
            }
#endif

            var blobNames = new List<string>();
            blobNames.AddRange(await UploadFilesAsync(PagesApiInfoEndpoint, PagesApiFileEndpoint));
            blobNames.AddRange(await UploadFilesAsync(ImagesApiInfoEndpoint, ImagesApiFileEndpoint, isImages: true));
            await DeleteOldBlobsAsync(blobNames);
        }

        private async Task<List<string>> UploadFilesAsync(string apiInfoEndpoint, string fileApiEndpoint, bool isImages = false)
        {
            var httpClient = httpClientFactory.CreateClient(Constants.GitHubApiHttpClientNameKey);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.GitHubApiToken);
            var infoResponse = await httpClient.GetAsync(apiInfoEndpoint);

            var blobNames = new List<string>();
            var pages = new List<string>();
            var fileInfoList = await infoResponse.ToObjectAsync<List<GitHubFileInfo>>();
            foreach (var fileInfo in fileInfoList)
            {
                if (isImages || (fileInfo.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && !fileInfo.Name.EndsWith(".vsdx", StringComparison.OrdinalIgnoreCase) && !fileInfo.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase)))
                {
                    var itemResponse = await httpClient.GetAsync(new Uri(fileApiEndpoint.Replace("[file]", fileInfo.Name)));
                    var itemFile = await itemResponse.ToObjectAsync<GitHubFile>();
                    byte[] itemBytes;
                    if(isImages)
                    {
                        itemBytes = Convert.FromBase64String(itemFile.Content);
                    }
                    else
                    {
                        if (!itemFile.Name.StartsWith("_", StringComparison.OrdinalIgnoreCase))
                        {
                            pages.Add($"{itemFile.Name.Remove(itemFile.Name.Length - 3)}:{GetLastModified(itemResponse)}");
                        }

                        var responseText = Encoding.UTF8.GetString(Convert.FromBase64String(itemFile.Content));
                        var pageContent = FixupMarkdown(responseText);
                        itemBytes = Encoding.UTF8.GetBytes(pageContent);
                    }

                    var blobName = isImages ? $"{Constants.GithubImageFolder}/{fileInfo.Name}" : fileInfo.Name;
                    blobNames.Add(blobName);
                    var blobClient = containerClient.GetBlobClient(blobName);
                    using (var pageContentStream = new MemoryStream(itemBytes))
                    {
                        await blobClient.UploadAsync(pageContentStream, overwrite: true);
                    }
                }
            }

            if(!isImages)
            {
                var sitemapBlobClient = containerClient.GetBlobClient(Constants.GithubSitePages);
                var pagesBytes = Encoding.UTF8.GetBytes(string.Join(',', pages));
                using (var pageContentStream = new MemoryStream(pagesBytes))
                {
                    await sitemapBlobClient.UploadAsync(pageContentStream, overwrite: true);
                }
            }

            return blobNames;
        }

        private string GetLastModified(HttpResponseMessage itemResponse)
        {
            var lastModifiedValue = itemResponse.Content.Headers.GetValues("last-modified").FirstOrDefault();
            if (lastModifiedValue.IsNullOrEmpty())
            {
                return DateTime.UtcNow.ToString("yyyy-MM-dd");
            }
            else
            {
                return DateTime.Parse(lastModifiedValue).ToString("yyyy-MM-dd");
            }
        }

        private async Task DeleteOldBlobsAsync(List<string> blobNames)
        {
            var currentBlobs = containerClient.GetBlobsAsync();
            await foreach(var cb in currentBlobs)
            {
                if (!Constants.GithubSitePages.Equals(cb.Name, StringComparison.OrdinalIgnoreCase) && !blobNames.Where(bn => bn.Equals(cb.Name, StringComparison.OrdinalIgnoreCase)).Any())
                {
                    await containerClient.DeleteBlobIfExistsAsync(cb.Name);
                }
            }
        }

        public async Task<List<string>> LoadSitePagesAsync()
        {
            var blobClient = containerClient.GetBlobClient(Constants.GithubSitePages);
            BlobDownloadInfo download = await blobClient.DownloadAsync();
            using var reader = new StreamReader(download.Content);
            return reader.ReadToEnd().Split(',').ToList();
        }

        public async Task<Stream> LoadImageAsync(string image)
        {
#if DEBUG
            if (settings.FoxIDsGitHub.LoadFiles)
            {
                return new FileStream(Path.Combine(settings.FoxIDsGitHub.DocsFileDirectory, image), FileMode.Open);
            }
#endif
            var blobClient = containerClient.GetBlobClient(image);
            BlobDownloadInfo download = await blobClient.DownloadAsync();
            return download.Content;
        }

        public async Task<string> LoadFileAsync(string page)
        {
#if DEBUG
            if (settings.FoxIDsGitHub.LoadFiles)
            {
                var fileContent = await File.ReadAllTextAsync(Path.Combine(settings.FoxIDsGitHub.DocsFileDirectory, page));
                var pageContent = FixupMarkdown(fileContent);
                return pageContent;
            }
#endif
            var blobClient = containerClient.GetBlobClient(page);
            BlobDownloadInfo download = await blobClient.DownloadAsync();
            using var reader = new StreamReader(download.Content, Encoding.UTF8);
            //return FixupMarkdown(reader.ReadToEnd());
            return reader.ReadToEnd();
        }

        public async Task<(string title, string markdown)> LoadFileWithTitle(string page)
        {
            var markdown = await LoadFileAsync(page);
            var title = GetTitle(markdown);
            return (title, markdown);
        }

        private string GetTitle(string markdown)
        {
            var doc = Markdig.Markdown.Parse(markdown);
            foreach (var item in doc)
            {
                if (item is HeadingBlock headingBlock)
                {
                    foreach (var inline in headingBlock.Inline)
                    {
                        return inline.ToString();
                    }
                }
            }
            return null;
        }

        private string FixupMarkdown(string markdown)
        {
            var doc = Markdig.Markdown.Parse(markdown);
            var baseUri = new Uri(new Uri(settings.BaseSitePath), settings.GitHubSiteFolder);
            foreach (var item in doc)
            {
                markdown = FixupHeading(markdown, baseUri, item);
                markdown = FixupQuoteBlock(markdown, baseUri, item);
                markdown = FixupParagraph(markdown, baseUri, item);
                markdown = FixupList(markdown, baseUri, item);
            }
            return markdown;
        }

        private static string FixupHeading(string markdown, Uri baseUri, Block item)
        {
            if (item is HeadingBlock heading)
            {
            }

            return markdown;
        }

        private static string FixupQuoteBlock(string markdown, Uri baseUri, Block item)
        {
            if (item is QuoteBlock quoteBlock)
            {
                foreach (var block in quoteBlock)
                {     
                    markdown = FixupParagraph(markdown, baseUri, block);
                }
            }

            return markdown;
        }

        private static string FixupList(string markdown, Uri baseUri, Block item)
        {
            if (item is ListBlock listBlock)
            {
                foreach (var block in listBlock)
                {
                    if (!(block is ListItemBlock itemBlock))
                        continue;

                    foreach (var subItem in itemBlock)
                    {
                        if(subItem is ListBlock)
                        {
                            markdown = FixupList(markdown, baseUri, subItem);
                        }
                        else
                        {
                            markdown = FixupParagraph(markdown, baseUri, subItem);
                        }
                    }
                }
            }

            return markdown;
        }

        private static string FixupParagraph(string markdown, Uri baseUri, Block item)
        {
            if (item is ParagraphBlock paragraph)
            {
                foreach (var inline in paragraph.Inline)
                {
                    markdown = FixupInline(markdown, baseUri, inline);
                }
            }

            return markdown;
        }

        private static string FixupInline(string markdown, Uri baseUri, Inline inline)
        {
            if (inline is LinkInline link)
            {
                if (!link.Url.Contains("://") && !link.Url.StartsWith("#"))
                {
                    var url = link.Url;
                    url = url.Replace("index.md", "").Replace(".md", "");

                    // Fix up the relative URL into an absolute URL
                    var newUrl = new Uri(baseUri, url).ToString();

                    markdown = markdown.Replace("](" + link.Url + ")", "](" + newUrl + ")");
                }
            }
            else if(inline is EmphasisInline emphasisInline)
            {
                foreach(var emphasisItem in emphasisInline)
                {
                    markdown = FixupInline(markdown, baseUri, emphasisItem);
                }
            }

            return markdown;
        }
    }
}
