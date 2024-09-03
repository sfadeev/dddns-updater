using System.Net;

namespace DnsUpdater.Services.IpProviders
{
	public abstract class TextIpProvider(IHttpClientFactory httpClientFactory) : IIpProvider
	{
		public abstract Task<IPAddress> GetCurrentIpAddress(CancellationToken cancellationToken);

		protected async Task<IPAddress> GetCurrentIpAddress(string uri, CancellationToken cancellationToken)
		{
			var client = httpClientFactory.CreateClient();

			var response = await client.GetAsync(uri, cancellationToken);

			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync(cancellationToken);

			var contentTrimmed = content.Trim();
			
			return IPAddress.Parse(contentTrimmed);
		}
	}
	
	public class IfconfigIpProvider(IHttpClientFactory httpClientFactory) : TextIpProvider(httpClientFactory)
	{
		public override async Task<IPAddress> GetCurrentIpAddress(CancellationToken cancellationToken)
		{
			return await GetCurrentIpAddress("https://ifconfig.io/ip", cancellationToken);
		}
	}
	
	public class IpifiIpProvider(IHttpClientFactory httpClientFactory) : TextIpProvider(httpClientFactory)
	{
		public override async Task<IPAddress> GetCurrentIpAddress(CancellationToken cancellationToken)
		{
			return await GetCurrentIpAddress("https://api.ipify.org/", cancellationToken);
		}
	}
}