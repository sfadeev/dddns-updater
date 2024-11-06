using System.Net;
using DnsUpdater.Models;
using Quartz;

namespace DnsUpdater.Services.Jobs
{
	public class DnsProviderSettings
	{
		public required string Provider { get; init; }
	
		public string? Username { get; init; }
	
		public string? Password { get; init; }
	
		public string[]? Domains { get; init; }

		public IConfigurationSection? ConfigurationSection { get; internal set; }
	}

	[DisallowConcurrentExecution]
	public class UpdateDnsJob(ILogger<UpdateDnsJob> logger, IConfiguration configuration,
		IIpProvider ipProvider, KeyedServiceProvider<IDnsProvider> keyedDnsServiceProvider,
		IHealthcheckService healthcheckService, IMessageSender messageSender, IUpdateStorage storage) : IJob
	{
		public async Task Execute(IJobExecutionContext context)
		{
			await ExecuteAsync(context.CancellationToken);
		}

		private async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			await healthcheckService.Success(cancellationToken);

			var dnsSettings = ReadDnsSettings();
			
			var currentIpAddressResult = await ipProvider.GetCurrentIpAddress(cancellationToken);
			
			if (currentIpAddressResult.Success == false)
			{
				await messageSender.Send(Messages.CurrentIpError(currentIpAddressResult.Error), MessageType.Failure, cancellationToken);

				return;
			}

			var currentIpAddress = currentIpAddressResult.Data!;
			
			if (currentIpAddress.IsPrivateV4())
			{
				await messageSender.Send(Messages.PrivateIpWarning(currentIpAddress), MessageType.Warning, cancellationToken);
				
				return;
			}

			var tasks = dnsSettings
				// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
				.Where(x => x.Provider != null && x.Domains?.Length > 0)
				.Select(x => Process(currentIpAddress, x, cancellationToken));
				
			await Task.WhenAll(tasks);
		}

		private async Task Process(IPAddress currentIpAddress, DnsProviderSettings settings, CancellationToken cancellationToken)
		{
			logger.LogDebug("Processing provider {provider} for domain(s) {domains}", settings.Provider, settings.Domains);

			try
			{
				var provider = keyedDnsServiceProvider.GetRequiredKeyedService(settings.Provider);
				
				foreach (var domain in settings.Domains!)
				{
					try
					{
						var ips = await ResolveIpAddress(domain, cancellationToken);
							
						if (ips.Contains(currentIpAddress))
						{
							logger.LogDebug("Resolved IPs {ips} for {domain} contains current IP address.", ips, domain);
						}
						else
						{
							logger.LogInformation("Resolved IPs {ips} for {domain} does not contains current IP address, updating DNS records.", ips, domain);

							var result = await provider.UpdateAsync(settings, domain, currentIpAddress, cancellationToken);
								
							if (result.Success)
							{
								logger.LogInformation("Address for {domain} updated to {ip}", domain, currentIpAddress);

								await storage.Store(domain, currentIpAddress, settings.Provider, true, null, cancellationToken);
								
								await healthcheckService.Success(cancellationToken);
								
								await messageSender.Send(Messages.SuccessUpdated(settings.Provider, domain, currentIpAddress), MessageType.Success, cancellationToken);
							}
							else
							{
								logger.LogInformation("Address for {domain} not updated — {message}", domain, result.Error);
								
								var message = Messages.WarningNotUpdated(settings.Provider, domain, result.Error);

								await healthcheckService.Log(message, cancellationToken);

								await messageSender.Send(message, MessageType.Warning, cancellationToken);
							}
						}
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Failed to process {domain}", domain);
						
						var error = ExceptionUtils.BuildMessage(ex);
						
						await storage.Store(domain, currentIpAddress, settings.Provider, false, error, cancellationToken);
						
						var message = Messages.FailedUpdateDomain(settings.Provider, domain, error);

						await healthcheckService.Failure(message, cancellationToken);

						await messageSender.Send(message, MessageType.Failure, cancellationToken);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to process provider {provider} for domain(s) {domains}\"", settings.Provider, settings.Domains);

				var error = ExceptionUtils.BuildMessage(ex);
				
				await messageSender.Send(Messages.FailedProcess(settings.Provider, error), MessageType.Failure, cancellationToken);
			}
		}
		
		private DnsProviderSettings[] ReadDnsSettings()
		{
			var result = new List<DnsProviderSettings>();
		
			var settings = configuration.GetSection("Settings");

			foreach (var item in settings.GetChildren())
			{
				var settingsItem = item.Get<DnsProviderSettings>();

				if (settingsItem != null)
				{
					settingsItem.ConfigurationSection = item;

					result.Add(settingsItem);
				}
			}
		
			return result.ToArray();
		}

		private static async Task<IPAddress[]> ResolveIpAddress(string hostNameOrAddress, CancellationToken cancellationToken)
		{
			try
			{
				var hostEntry = await Dns.GetHostEntryAsync(hostNameOrAddress, cancellationToken);

				return hostEntry.AddressList;
			}
			catch (Exception ex)
			{
				throw new ApplicationException($"Failed to resolve IP address for {hostNameOrAddress}", ex);
			}
		}
	}
}