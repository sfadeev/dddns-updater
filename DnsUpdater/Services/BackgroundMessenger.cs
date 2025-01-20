using DnsUpdater.Models;
using Microsoft.Extensions.Options;

namespace DnsUpdater.Services
{
	public class BackgroundMessenger(ILogger<BackgroundMessenger> logger, IOptions<AppOptions> appOptions,
		IHealthcheckService healthcheckService, IMessageSender messageSender) : BackgroundService
	{
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			logger.LogInformation("Service started.");

			await healthcheckService.Start(cancellationToken);
			
			var options = appOptions.Value;
			
			await messageSender.Send(Messages.ServiceStarted(options.BaseUrl), MessageType.Info, cancellationToken);
		}
		
		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			logger.LogInformation("Service stopping.");

			await messageSender.Send(Messages.ServiceStopped(), MessageType.Info, cancellationToken);

			await base.StopAsync(cancellationToken);
		}
	}
}