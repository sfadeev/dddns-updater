using DnsUpdater.Models;

namespace DnsUpdater.Services
{
	public class BackgroundMessenger(ILogger<BackgroundMessenger> logger,
		IHealthcheckService healthcheckService, IMessageSender messageSender) : BackgroundService
	{
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			logger.LogInformation("Service started.");

			await healthcheckService.Start(cancellationToken);
			
			await messageSender.Send(Messages.ServiceStarted(), MessageType.Info, cancellationToken);
		}
		
		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			logger.LogInformation("Service stopping.");

			await messageSender.Send(Messages.ServiceStopped(), MessageType.Info, cancellationToken);

			await base.StopAsync(cancellationToken);
		}
	}
}