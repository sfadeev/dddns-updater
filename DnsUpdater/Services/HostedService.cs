using System.Diagnostics;
using System.Net;

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
		IIpProvider ipProvider, KeyedServiceProvider<IDnsProvider> keyedDnsServiceProvider) : BackgroundService
	{
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			var pollDelay = TimeSpan.FromSeconds(30);

			logger.LogInformation("Service starting, poll interval {delay}.", pollDelay);

			var settings = ReadSettings();
		
			logger.LogInformation("Getting {count} item(s) from settings.", settings.Length);
		
			while (cancellationToken.IsCancellationRequested == false)
			{
				var sw = new Stopwatch();
				
				sw.Start();
				
				var currentIpAddress = await ipProvider.GetCurrentIpAddress(cancellationToken);

				logger.LogInformation("Current IP Address : {ip}", currentIpAddress);
				
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

								logger.LogInformation("Resolved IPs for {domain} : {ip}", domain, ips);

								if (ips.Contains(currentIpAddress))
								{
									logger.LogInformation("Resolved IPs contains current IP address, ignoring.");
								}
								else
								{
									logger.LogInformation(
										"Resolved IPs does not contains current IP address, updating DNS records.");

									var result = await provider.UpdateAsync(dnsSettings, domain, currentIpAddress, cancellationToken);
								
									logger.LogInformation("Update IP Address for {domain} to {ip} : {result}",
										domain, currentIpAddress, result);
								}
							}
							catch (Exception ex)
							{
								logger.LogError(ex, "Failed to process domain {domain}", domain);
							}
						}
					}
				}
			
				var sleepDelay = pollDelay - sw.Elapsed;

				if (sleepDelay > TimeSpan.Zero)
				{
					if (logger.IsEnabled(LogLevel.Debug))
						logger.LogDebug("Service sleeping for {delay}.", sleepDelay);
                
					await Task.Delay(sleepDelay, cancellationToken);
				}
			}
		
			logger.LogInformation("Service stopping.");
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