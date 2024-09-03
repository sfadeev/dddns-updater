using System.Net;

namespace DnsUpdater.Services.IpProviders
{
	public class DefaultIpProvider(
		ILogger<DefaultIpProvider> logger,
		KeyedServiceProvider<IIpProvider> keyedIpServiceProvider,
		KeyedServiceKeyProvider<string, IIpProvider> ipProviderKeyProvider) : IIpProvider
	{
		private int _currentIndex;

		public async Task<IPAddress> GetCurrentIpAddress(CancellationToken cancellationToken)
		{
			var keys = ipProviderKeyProvider.Keys;
			
			var attempts = keys.Count;

			while (attempts > 0)
			{
				var key = GetCurrentProviderKey(keys);
				
				try
				{
					var ipProvider = keyedIpServiceProvider.GetRequiredKeyedService(key);
			
					return await ipProvider.GetCurrentIpAddress(cancellationToken);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to get current ip address from {key} provider.", key);
				}
				
				attempts--;
			}
			
			throw new InvalidOperationException($"Cant not get current ip address in {keys.Count} attempts.");
		}

		private string GetCurrentProviderKey(IReadOnlyList<string> keys)
		{
			if (_currentIndex < keys.Count)
			{
				var key = keys[_currentIndex];

				logger.LogInformation("Using {key} ({number}/{total}) provider to get current ip address", key, _currentIndex + 1, keys.Count);

				_currentIndex = _currentIndex == keys.Count - 1 ? 0 : _currentIndex + 1;
				
				return key;
			}

			throw new InvalidOperationException("No IP providers found. Please check service registration.");
		}
	}
}