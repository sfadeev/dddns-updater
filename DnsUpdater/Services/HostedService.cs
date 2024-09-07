using System.Diagnostics;
using System.Net;
using DnsUpdater.Models;

namespace DnsUpdater.Services
{
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
		IMessageSender messageSender) : BackgroundService
	{
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			var pollDelay = TimeSpan.FromSeconds(5 * 60);

			logger.LogInformation("Service starting, poll interval {pollDelay}.", pollDelay);

			var settings = ReadSettings();
		
			logger.LogInformation("Getting {count} item(s) from settings.", settings.Length);

			await messageSender.Send(Messages.ServiceStarted(pollDelay, settings.Length), MessageType.Info, cancellationToken);
		
			while (cancellationToken.IsCancellationRequested == false)
			{
				var sw = new Stopwatch();
				
				sw.Start();
				
				var currentIpAddress = await ipProvider.GetCurrentIpAddress(cancellationToken);

				logger.LogInformation("Current IP address : {ip}", currentIpAddress);
				
				foreach (var dnsSettings in settings)
				{
					logger.LogInformation("Processing provider {provider} for domain(s) {domains}", dnsSettings.Provider, dnsSettings.Domains);
				
					if (dnsSettings.Domains != null)
					{
						var provider = keyedDnsServiceProvider.GetRequiredKeyedService(dnsSettings.Provider);
				
						foreach (var domain in dnsSettings.Domains)
						{
							try
							{
								var ips = await ResolveIpAddress(domain, cancellationToken);
								
								if (ips.Contains(currentIpAddress))
								{
									logger.LogInformation(
										"Resolved IPs {ips} for {domain} contains current IP address.", ips, domain);
								}
								else
								{
									logger.LogInformation(
										"Resolved IPs {ips} for {domain} does not contains current IP address, updating DNS records.", ips, domain);

									var result = await provider.UpdateAsync(dnsSettings, domain, currentIpAddress, cancellationToken);
									
									if (result.Success)
									{
										logger.LogInformation("Address for {domain} updated to {ip}", domain, currentIpAddress);
										
										await messageSender.Send(Messages.SuccessUpdated(dnsSettings.Provider, domain, currentIpAddress), MessageType.Success, cancellationToken);
									}
									else
									{
										logger.LogInformation("Address for {domain} not updated — {message}", domain, result.Message);

										await messageSender.Send(Messages.WarningNotUpdated(dnsSettings.Provider, domain, result.Message), MessageType.Warning, cancellationToken);
									}
								}
							}
							catch (Exception ex)
							{
								logger.LogError(ex, "Failed to process {domain}", domain);
								
								await messageSender.Send(Messages.FailedUpdate(dnsSettings.Provider, domain, ex.Message), MessageType.Failure, cancellationToken);
							}
						}
					}
				}
			
				var sleepDelay = pollDelay - sw.Elapsed;

				if (sleepDelay > TimeSpan.Zero)
				{
					if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Service sleeping for {delay}.", sleepDelay);
                
					await Task.Delay(sleepDelay, cancellationToken);
				}
			}
		
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			logger.LogInformation("Service stopping.");

			await messageSender.Send(Messages.ServiceStopped(), MessageType.Info, cancellationToken);

			await base.StopAsync(cancellationToken);
		}

		private DnsProviderSettings[] ReadSettings()
		{
			var result = new List<DnsProviderSettings>();
		
			var settings = configuration.GetSection("Settings");

			foreach (var item in settings.GetChildren())
			{
				var settingsItem = item.Get<DnsProviderSettings>()!;
			
				settingsItem.ConfigurationSection = item;
			
				result.Add(settingsItem);
			}
		
			return result.ToArray();
		}

		private async Task<IPAddress[]> ResolveIpAddress(string hostNameOrAddress, CancellationToken cancellationToken)
		{
			var hostEntry = await Dns.GetHostEntryAsync(hostNameOrAddress, cancellationToken);

			return hostEntry.AddressList;
		}
	}
}