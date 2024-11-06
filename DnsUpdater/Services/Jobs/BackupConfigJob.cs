using DnsUpdater.Models;
using Quartz;

namespace DnsUpdater.Services.Jobs
{
	[DisallowConcurrentExecution]
	public class BackupConfigJob(ILogger<UpdateDnsJob> logger, IMessageSender messageSender, IUpdateStorage storage) : IJob
	{
		public async Task Execute(IJobExecutionContext context)
		{
			try
			{
				var result = await storage.Backup(BackupMode.Auto, context.CancellationToken);

				if (result.Success)
				{
					await messageSender.Send(
						Messages.BackupCreated(result.Data!.Name, result.Data!.Length),
						MessageType.Info, context.CancellationToken);
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to backup configs.");
				
				var message = ExceptionUtils.BuildMessage(ex);
				
				await messageSender.Send(
					Messages.BackupFailed(message),
					MessageType.Failure, context.CancellationToken);
			}
		}
	}
}