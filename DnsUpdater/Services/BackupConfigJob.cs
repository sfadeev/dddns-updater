using DnsUpdater.Models;
using Quartz;

namespace DnsUpdater.Services
{
	[DisallowConcurrentExecution]
	public class BackupConfigJob(ILogger<UpdateDnsJob> logger,
		IMessageSender messageSender, IUpdateStorage storage) : IJob
	{
		public async Task Execute(IJobExecutionContext context)
		{
			try
			{
				var result = await storage.Backup(BackupMode.Auto, context.CancellationToken);

				if (result.Success)
				{
					await messageSender.Send(
						Messages.CreatedBackup(result.Data!.Name, result.Data!.Length),
						MessageType.Info, context.CancellationToken);
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to backup configs.");
				
				await messageSender.Send(
					Messages.FailedBackup(ex.Message),
					MessageType.Failure, context.CancellationToken);
			}
		}
	}
}