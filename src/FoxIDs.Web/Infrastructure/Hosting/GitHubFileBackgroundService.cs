using FoxIDs.Web.Logic;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Web.Infrastructure.Hosting
{
    public class GitHubFileBackgroundService : BackgroundService
    {
        private readonly GitHubFileLogic gitHubFileLogic;

        public GitHubFileBackgroundService(GitHubFileLogic gitHubFileLogic)
        {
            this.gitHubFileLogic = gitHubFileLogic;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return gitHubFileLogic.ExecuteAsync(stoppingToken);
        }
    }
}
