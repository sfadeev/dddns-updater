using System.Net;
using DnsUpdater.Models;

namespace DnsUpdater.Services
{
	public class DnsUpdaterSettings
	{
		public TimeSpan PollInterval { get; init; } = TimeSpan.FromMinutes(5);
	}
	
	public class DnsProviderSettings
	{
		public required string Provider { get; init; }
	
		public string? Username { get; init; }
	
		public string? Password { get; init; }
	
		public string[]? Domains { get; init; }

		public IConfigurationSection? ConfigurationSection { get; internal set; }
	}

	public class HostedService(ILogger<HostedService> logger, IConfiguration configuration,
		IIpProvider ipProvider, KeyedServiceProvider<IDnsProvider> keyedDnsServiceProvider,
		IHealthcheckService healthcheckService, IMessageSender messageSender, IUpdateStorage storage) : BackgroundService
	{
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			var settings = configuration.GetSection("DnsUpdater").Get<DnsUpdaterSettings>() ?? new DnsUpdaterSettings();
			
			var dnsSettings = ReadDnsSettings();
		
			logger.LogInformation("Service started, poll interval {pollDelay}, {count} item(s) in settings.", settings.PollInterval,  dnsSettings.Length);

			await healthcheckService.Start(cancellationToken);
			
			await messageSender.Send(Messages.ServiceStarted(settings.PollInterval, dnsSettings.Length), MessageType.Info, cancellationToken);
			
			while (cancellationToken.IsCancellationRequested == false)
			{
				var currentIpAddress = await ipProvider.GetCurrentIpAddress(cancellationToken);

				logger.LogInformation("Current IP address : {ip}", currentIpAddress);

				if (currentIpAddress.IsPrivateV4())
				{
					await messageSender.Send(Messages.PrivateIpWarning(currentIpAddress), MessageType.Warning, cancellationToken);
					
					await Sleep(settings.PollInterval, cancellationToken);

					continue;
				}

				var tasks = dnsSettings
					// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
					.Where(x => x.Provider != null && x.Domains?.Length > 0)
					.Select(x => Process(currentIpAddress, x, cancellationToken));
				
				await Task.WhenAll(tasks);

				await Sleep(settings.PollInterval, cancellationToken);
			}
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
								logger.LogInformation("Address for {domain} not updated — {message}", domain, result.Message);
								
								var message = Messages.WarningNotUpdated(settings.Provider, domain, result.Message);

								await healthcheckService.Log(message, cancellationToken);

								await messageSender.Send(message, MessageType.Warning, cancellationToken);
							}
						}
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Failed to process {domain}", domain);
						
						await storage.Store(domain, currentIpAddress, settings.Provider, false, ex.Message, cancellationToken);
						
						var message = Messages.FailedUpdateDomain(settings.Provider, domain, ex.Message);

						await healthcheckService.Failure(message, cancellationToken);

						await messageSender.Send(message, MessageType.Failure, cancellationToken);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to process provider {provider} for domain(s) {domains}\"", settings.Provider, settings.Domains);
								
				await messageSender.Send(Messages.FailedProcess(settings.Provider, ex.Message), MessageType.Failure, cancellationToken);
			}
		}

		private async Task Sleep(TimeSpan delay, CancellationToken cancellationToken)
		{
			if (delay > TimeSpan.Zero)
			{
				if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Sleeping for {delay}, till {time}.", delay, DateTime.Now.Add(delay));
                
				await Task.Delay(delay, cancellationToken);
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			logger.LogInformation("Service stopping.");

			await messageSender.Send(Messages.ServiceStopped(), MessageType.Info, cancellationToken);

			await base.StopAsync(cancellationToken);
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
			var hostEntry = await Dns.GetHostEntryAsync(hostNameOrAddress, cancellationToken);

			return hostEntry.AddressList;
		}
	}
}