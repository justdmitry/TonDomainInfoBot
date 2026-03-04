using RecurrentTasks;
using TonLibDotNet;

namespace TonDomainInfoBot
{
    public class ReconnectTask(ITonClient tonClient, ILogger<ReconnectTask> logger) : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(29);

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            await tonClient.InitIfNeeded(cancellationToken);
            var blocks = await tonClient.Sync();
            logger.LogInformation("Synced to block {Seqno}", blocks.Seqno);
        }
    }
}
