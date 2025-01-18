using System.Net;
using DnsUpdater.Models;

namespace DnsUpdater.Services.IpProviders
{
	public class DefaultIpProvider(
		ILogger<DefaultIpProvider> logger,
		KeyedServiceProvider<IIpProvider> keyedIpServiceProvider,
		KeyedServiceKeyProvider<string, IIpProvider> ipProviderKeyProvider) : IIpProvider
	{
		private int _currentIndex;

		public async Task<Result<IPAddress>> GetCurrentIpAddress(CancellationToken cancellationToken)
		{
			var keys = ipProviderKeyProvider.Keys;
			
			var attempts = keys.Count;

			var errors = new List<string>();
			
			while (attempts > 0)
			{
				var key = GetCurrentProviderKey(keys);
				
				try
				{
					var ipProvider = keyedIpServiceProvider.GetRequiredKeyedService(key);

					var currentIpAddress = await ipProvider.GetCurrentIpAddress(cancellationToken);

					if (currentIpAddress.Success)
					{
						logger.LogInformation("Current IP address {Ip} from provider {Provider}.", currentIpAddress.Data, key);
					
						return currentIpAddress;	
					}
				}
				catch (Exception ex)
				{
					errors.Add($"{key} - {ex.Message}");
					
					logger.LogError(ex, "Failed to get current ip address from {Provider} provider.", key);
				}
				
				attempts--;
			}

			errors.Add($"Cannot get current ip address in {keys.Count} attempts."); 
			
			return Result.CreateErrorResult<IPAddress>(string.Join('\n', errors));
		}

		private string GetCurrentProviderKey(IReadOnlyList<string> keys)
		{
			if (_currentIndex < keys.Count)
			{
				var key = keys[_currentIndex];

				logger.LogDebug("Using {Provider} ({ProviderNumber}/{ProviderTotal}) provider to get current IP address", key, _currentIndex + 1, keys.Count);

				_currentIndex = _currentIndex == keys.Count - 1 ? 0 : _currentIndex + 1;
				
				return key;
			}

			throw new InvalidOperationException("No IP providers found. Please check service registration.");
		}
	}
}